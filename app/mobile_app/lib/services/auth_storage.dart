import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../models/auth.dart';
import '../models/auth_session.dart';
import '../models/user.dart';

abstract class AuthStorage {
  Future<AuthSession?> readSession();

  Future<String?> readAccessToken();

  Future<void> saveTokens(TokenDto token);

  Future<void> saveUser(UserDto user);

  Future<void> clear();
}

class SecureAuthStorage implements AuthStorage {
  SecureAuthStorage({FlutterSecureStorage? secureStorage})
    : _secureStorage = secureStorage ?? const FlutterSecureStorage(),
      _secureStorageEnabled = !_shouldBypassSecureStorageForCurrentPlatform();

  static const _accessTokenKey = 'auth.access_token';
  static const _refreshTokenKey = 'auth.refresh_token';
  static const _userKey = 'auth.user';

  final FlutterSecureStorage _secureStorage;
  bool _secureStorageEnabled;
  bool _loggedSecureStorageDisabled = false;

  @override
  Future<AuthSession?> readSession() async {
    final prefs = await SharedPreferences.getInstance();
    final accessToken = await _readValue(
      secureKey: _accessTokenKey,
      prefs: prefs,
    );
    final refreshToken = await _readValue(
      secureKey: _refreshTokenKey,
      prefs: prefs,
    );
    final userJson = prefs.getString(_userKey);

    if ((accessToken?.isEmpty ?? true) &&
        (refreshToken?.isEmpty ?? true) &&
        (userJson == null || userJson.isEmpty)) {
      return null;
    }

    UserDto? user;
    if (userJson != null && userJson.isNotEmpty) {
      try {
        user = UserDto.fromJson(jsonDecode(userJson) as Map<String, dynamic>);
      } catch (_) {
        final mutablePrefs = await SharedPreferences.getInstance();
        await mutablePrefs.remove(_userKey);
      }
    }

    return AuthSession(
      accessToken: accessToken,
      refreshToken: refreshToken,
      user: user,
    );
  }

  @override
  Future<String?> readAccessToken() async {
    final prefs = await SharedPreferences.getInstance();
    return _readValue(secureKey: _accessTokenKey, prefs: prefs);
  }

  @override
  Future<void> saveTokens(TokenDto token) async {
    final prefs = await SharedPreferences.getInstance();
    final writes = <Future<void>>[];

    if (token.accessToken != null) {
      writes.add(
        _writeValue(
          secureKey: _accessTokenKey,
          value: token.accessToken,
          prefs: prefs,
        ),
      );
    }

    if (token.refreshToken != null) {
      writes.add(
        _writeValue(
          secureKey: _refreshTokenKey,
          value: token.refreshToken,
          prefs: prefs,
        ),
      );
    }

    await Future.wait(writes);
  }

  @override
  Future<void> saveUser(UserDto user) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_userKey, jsonEncode(user.toJson()));
  }

  @override
  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await Future.wait([
      _deleteValue(secureKey: _accessTokenKey, prefs: prefs),
      _deleteValue(secureKey: _refreshTokenKey, prefs: prefs),
      prefs.remove(_userKey),
    ]);
  }

  Future<String?> _readValue({
    required String secureKey,
    required SharedPreferences prefs,
  }) async {
    if (!_secureStorageEnabled) {
      return prefs.getString(secureKey);
    }

    try {
      return await _secureStorage.read(key: secureKey);
    } on PlatformException catch (error) {
      _handleSecureStorageError('read', secureKey, error);
      return prefs.getString(secureKey);
    }
  }

  Future<void> _writeValue({
    required String secureKey,
    required String? value,
    required SharedPreferences prefs,
  }) async {
    if (value == null) {
      return;
    }

    if (!_secureStorageEnabled) {
      await prefs.setString(secureKey, value);
      return;
    }

    try {
      await _secureStorage.write(key: secureKey, value: value);
      await prefs.remove(secureKey);
    } on PlatformException catch (error) {
      _handleSecureStorageError('write', secureKey, error);
      await prefs.setString(secureKey, value);
    }
  }

  Future<void> _deleteValue({
    required String secureKey,
    required SharedPreferences prefs,
  }) async {
    if (!_secureStorageEnabled) {
      await prefs.remove(secureKey);
      return;
    }

    try {
      await _secureStorage.delete(key: secureKey);
    } on PlatformException catch (error) {
      _handleSecureStorageError('delete', secureKey, error);
    } finally {
      await prefs.remove(secureKey);
    }
  }

  void _handleSecureStorageError(
    String action,
    String key,
    PlatformException error,
  ) {
    if (_shouldDisableSecureStorage(error)) {
      _secureStorageEnabled = false;
      if (_loggedSecureStorageDisabled) {
        return;
      }

      _loggedSecureStorageDisabled = true;
      debugPrint(
        'Secure storage is unavailable for this app configuration; '
        'using SharedPreferences for auth tokens for the rest of this session. '
        'Original failure during $action for "$key": ${error.code} ${error.message}',
      );
      return;
    }

    _logFallback(action, key, error);
  }

  bool _shouldDisableSecureStorage(PlatformException error) {
    final code = error.code.toLowerCase();
    final message = (error.message ?? '').toLowerCase();
    return code.contains('-34018') ||
        message.contains('required entitlement') ||
        message.contains("isn't present");
  }

  static bool _shouldBypassSecureStorageForCurrentPlatform() {
    return !kReleaseMode && defaultTargetPlatform == TargetPlatform.macOS;
  }

  void _logFallback(String action, String key, PlatformException error) {
    debugPrint(
      'Secure storage $action failed for "$key", falling back to SharedPreferences: '
      '${error.code} ${error.message}',
    );
  }
}
