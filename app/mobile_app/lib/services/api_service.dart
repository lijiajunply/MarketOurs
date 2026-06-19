import 'dart:convert';
import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import 'app_logger.dart';
import 'auth_storage.dart';
import '../models/auth.dart';

class ApiService {
  late final Dio dio;
  static final ApiService _instance = ApiService._internal();
  static const String _apiBaseUrlOverride =
      'http://localhost:5053';
  static const Duration _defaultTimeout = Duration(seconds: 15);
  static const Duration _uploadTimeout = Duration(minutes: 2);

  static String get baseUrl => _resolveBaseUrl();
  static const String _skipAuthExtraKey = 'skipAuth';
  static const String _skipUnauthorizedHandlerExtraKey =
      'skipUnauthorizedHandler';
  static const String _requestLogIdExtraKey = 'requestLogId';
  static const String _requestStartedAtExtraKey = 'requestStartedAt';
  AuthStorage? _authStorage;
  Future<void> Function()? _onUnauthorized;
  bool _isHandlingUnauthorized = false;
  Future<String?>? _refreshingAccessToken;

  factory ApiService() => _instance;

  ApiService._internal() {
    dio = Dio(
      BaseOptions(
        baseUrl: _resolveBaseUrl(),
        connectTimeout: _defaultTimeout,
        receiveTimeout: _defaultTimeout,
      ),
    );

    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final requestId = _buildRequestId();
          options.extra[_requestLogIdExtraKey] = requestId;
          options.extra[_requestStartedAtExtraKey] = DateTime.now();

          final shouldSkipAuth = options.extra[_skipAuthExtraKey] == true;
          final token = shouldSkipAuth
              ? null
              : await _authStorage?.readAccessToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }

          AppLogger.info(
            'HTTP',
            'Request started',
            context: {
              'requestId': requestId,
              'method': options.method,
              'url': options.uri.toString(),
              'query': options.queryParameters,
              'headers': _sanitizeHeaders(options.headers),
              'body': _stringifyPayload(options.data),
            },
          );
          return handler.next(options);
        },
        onResponse: (response, handler) {
          final requestId =
              response.requestOptions.extra[_requestLogIdExtraKey] as String?;
          AppLogger.info(
            'HTTP',
            'Request completed',
            context: {
              'requestId': requestId,
              'method': response.requestOptions.method,
              'url': response.requestOptions.uri.toString(),
              'statusCode': response.statusCode,
              'durationMs': _elapsedMs(response.requestOptions),
            },
          );
          return handler.next(response);
        },
        onError: (e, handler) async {
          final requestId =
              e.requestOptions.extra[_requestLogIdExtraKey] as String?;
          final responseData = _extractResponseData(e.response?.data);
          final logContext = {
            'requestId': requestId,
            'method': e.requestOptions.method,
            'url': e.requestOptions.uri.toString(),
            'statusCode': e.response?.statusCode,
            'durationMs': _elapsedMs(e.requestOptions),
            'query': e.requestOptions.queryParameters,
            'body': _stringifyPayload(e.requestOptions.data),
            'response': responseData,
          };

          if ((e.response?.statusCode ?? 0) >= 500) {
            AppLogger.error(
              'HTTP',
              'Request failed',
              context: logContext,
              error: e.error ?? e.message ?? e.type.name,
              stackTrace: e.stackTrace,
            );
          } else {
            AppLogger.warn(
              'HTTP',
              'Request failed',
              context: {...logContext, 'error': e.message ?? e.type.name},
            );
          }

          final shouldSkipUnauthorizedHandler =
              e.requestOptions.extra[_skipUnauthorizedHandlerExtraKey] == true;
          final shouldAttemptRefresh =
              e.response?.statusCode == 401 &&
              !shouldSkipUnauthorizedHandler &&
              e.requestOptions.path != '/Auth/refresh' &&
              e.requestOptions.extra['retriedAfterRefresh'] != true;

          if (shouldAttemptRefresh) {
            final refreshedToken = await _refreshAccessToken();
            if (refreshedToken != null) {
              final retryOptions = await _retryOptions(
                e.requestOptions,
                refreshedToken,
              );
              final retryResponse = await dio.fetch(retryOptions);
              return handler.resolve(retryResponse);
            }
          }

          if (e.response?.statusCode == 401 &&
              !shouldSkipUnauthorizedHandler &&
              !_isHandlingUnauthorized &&
              _onUnauthorized != null) {
            _isHandlingUnauthorized = true;
            try {
              await _onUnauthorized!.call();
            } finally {
              _isHandlingUnauthorized = false;
            }
          }
          return handler.next(e);
        },
      ),
    );
  }

  void configureAuth({
    required AuthStorage storage,
    Future<void> Function()? onUnauthorized,
  }) {
    _authStorage = storage;
    _onUnauthorized = onUnauthorized;
  }

  static Options anonymousOptions() {
    return Options(
      extra: {_skipAuthExtraKey: true, _skipUnauthorizedHandlerExtraKey: true},
    );
  }

  static Options uploadOptions({bool anonymous = false}) {
    return Options(
      sendTimeout: _uploadTimeout,
      receiveTimeout: _uploadTimeout,
      extra: anonymous
          ? {_skipAuthExtraKey: true, _skipUnauthorizedHandlerExtraKey: true}
          : null,
    );
  }

  Future<String?> _refreshAccessToken() {
    _refreshingAccessToken ??= () async {
      final refreshToken = await _authStorage?.readRefreshToken();
      if (refreshToken == null || refreshToken.isEmpty) {
        return null;
      }

      try {
        final response = await dio.post(
          '/Auth/refresh',
          data: RefreshRequest(
            refreshToken: refreshToken,
            deviceType: _resolveDeviceType(),
          ).toJson(),
          options: anonymousOptions(),
        );
        final data = _extractResponseData(response.data);
        if (data is! Map<String, dynamic>) {
          return null;
        }

        final payload = data['data'];
        if (payload is! Map<String, dynamic>) {
          return null;
        }

        final token = TokenDto.fromJson(payload);
        if (token.accessToken == null || token.refreshToken == null) {
          return null;
        }

        await _authStorage?.saveTokens(token);
        return token.accessToken;
      } catch (_) {
        return null;
      } finally {
        _refreshingAccessToken = null;
      }
    }();

    return _refreshingAccessToken!;
  }

  Future<RequestOptions> _retryOptions(
    RequestOptions requestOptions,
    String accessToken,
  ) async {
    final headers = Map<String, dynamic>.from(requestOptions.headers);
    headers['Authorization'] = 'Bearer $accessToken';

    final extra = Map<String, dynamic>.from(requestOptions.extra);
    extra['retriedAfterRefresh'] = true;

    return requestOptions.copyWith(headers: headers, extra: extra);
  }

  static String _resolveDeviceType() {
    if (kIsWeb) {
      return 'Web';
    }

    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
      case TargetPlatform.iOS:
        return 'Mobile';
      case TargetPlatform.macOS:
      case TargetPlatform.windows:
      case TargetPlatform.linux:
        return 'Desktop';
      case TargetPlatform.fuchsia:
        return 'Mobile';
    }
  }

  static String _resolveBaseUrl() {
    if (_apiBaseUrlOverride.isNotEmpty) {
      return _apiBaseUrlOverride;
    }

    if (kDebugMode) {
      return 'http://localhost:5053';
    }

    if (kIsWeb) {
      return 'http://localhost:5053';
    }

    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return 'http://10.0.2.2:5053';
      case TargetPlatform.iOS:
      case TargetPlatform.macOS:
      case TargetPlatform.windows:
      case TargetPlatform.linux:
        return 'http://localhost:5053';
      case TargetPlatform.fuchsia:
        return 'http://localhost:5053';
    }
  }

  static String _buildRequestId() {
    final millis = DateTime.now().millisecondsSinceEpoch;
    final micros = DateTime.now().microsecondsSinceEpoch.remainder(1000);
    return '$millis-$micros';
  }

  static int? _elapsedMs(RequestOptions options) {
    final startedAt = options.extra[_requestStartedAtExtraKey];
    if (startedAt is! DateTime) {
      return null;
    }
    return DateTime.now().difference(startedAt).inMilliseconds;
  }

  static Map<String, Object?> _sanitizeHeaders(Map<String, dynamic> headers) {
    return headers.map((key, value) {
      final isSensitive = key.toLowerCase() == 'authorization';
      return MapEntry(key, isSensitive ? '<redacted>' : value);
    });
  }

  static Object? _extractResponseData(dynamic data) {
    if (data == null) {
      return null;
    }

    if (data is String) {
      try {
        return jsonDecode(data);
      } catch (_) {
        return data;
      }
    }

    return data;
  }

  static Object? _stringifyPayload(dynamic data) {
    if (data == null) {
      return null;
    }

    if (data is FormData) {
      return {
        'fields': {for (final field in data.fields) field.key: field.value},
        'files': [
          for (final file in data.files)
            {
              'field': file.key,
              'filename': file.value.filename,
              'contentType': file.value.contentType?.toString(),
            },
        ],
      };
    }

    return _extractResponseData(data);
  }
}
