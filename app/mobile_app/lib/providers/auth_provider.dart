import 'dart:io';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dio/dio.dart';

import '../models/auth.dart';
import '../models/auth_session.dart';
import '../models/user.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/auth_storage.dart';
import '../services/user_service.dart';

enum AuthStatus { unknown, unauthenticated, authenticated }

class AuthState {
  const AuthState({
    required this.status,
    this.session,
    this.isSubmitting = false,
    this.errorMessage,
  });

  final AuthStatus status;
  final AuthSession? session;
  final bool isSubmitting;
  final String? errorMessage;

  UserDto? get user => session?.user;

  AuthState copyWith({
    AuthStatus? status,
    AuthSession? session,
    bool? isSubmitting,
    String? errorMessage,
    bool clearError = false,
  }) {
    return AuthState(
      status: status ?? this.status,
      session: session ?? this.session,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
    );
  }

  factory AuthState.unauthenticated({String? errorMessage}) {
    return AuthState(
      status: AuthStatus.unauthenticated,
      errorMessage: errorMessage,
    );
  }

  factory AuthState.authenticated(AuthSession session) {
    return AuthState(status: AuthStatus.authenticated, session: session);
  }
}

final authStorageProvider = Provider<AuthStorage>((ref) => SecureAuthStorage());

final authServiceProvider = Provider<AuthService>((ref) => AuthService());
final userServiceProvider = Provider<UserService>((ref) => UserService());

final authControllerProvider = AsyncNotifierProvider<AuthController, AuthState>(
  AuthController.new,
);

class AuthController extends AsyncNotifier<AuthState> {
  AuthStorage get _storage => ref.read(authStorageProvider);

  AuthService get _authService => ref.read(authServiceProvider);

  UserService get _userService => ref.read(userServiceProvider);

  @override
  Future<AuthState> build() async {
    ApiService().configureAuth(
      storage: _storage,
      onUnauthorized: _handleUnauthorized,
    );

    final session = await _storage.readSession();
    if (session == null || !session.hasToken) {
      if (session != null) {
        await _clearStoredSessionSafely();
      }
      return AuthState.unauthenticated();
    }

    try {
      final user = await _fetchCurrentUser();
      final restoredSession = session.copyWith(user: user);
      await _storage.saveUser(user);
      return AuthState.authenticated(restoredSession);
    } catch (_) {
      await _clearStoredSessionSafely();
      return AuthState.unauthenticated();
    }
  }

  Future<bool> login({
    required String account,
    required String password,
  }) async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));

    final deviceType =
        Platform.isLinux || Platform.isMacOS || Platform.isWindows
        ? 'Desktop'
        : 'Mobile';

    try {
      final response = await _authService.login(
        LoginRequest(
          account: account,
          password: password,
          deviceType: deviceType,
        ),
      );

      final token = response.data;
      if (token?.accessToken == null || token?.refreshToken == null) {
        throw Exception(response.message ?? '登录失败，请稍后重试');
      }

      await _storage.saveTokens(token!);
      final user = await _fetchCurrentUser();
      await _storage.saveUser(user);

      state = AsyncData(
        AuthState.authenticated(
          AuthSession(
            accessToken: token.accessToken,
            refreshToken: token.refreshToken,
            user: user,
          ),
        ),
      );
      return true;
    } catch (error) {
      await _clearStoredSessionSafely();
      state = AsyncData(
        AuthState.unauthenticated(errorMessage: _normalizeError(error)),
      );
      return false;
    }
  }

  Future<void> sendLoginCode({required String account}) async {
    // We don't want to use _runGuestAction here because it sets isSubmitting to true
    // on the main AuthState, which might trigger router listeners or UI overlays.
    // Instead, we call the service directly. The UI manages its own loading state (_isSendingCode).
    try {
      await _authService.sendLoginCode(SendCodeRequest(account: account));
    } catch (error) {
      final current = state.asData?.value ?? AuthState.unauthenticated();
      state = AsyncData(current.copyWith(errorMessage: _normalizeError(error)));
      rethrow;
    }
  }

  Future<bool> loginByCode({
    required String account,
    required String code,
  }) async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));

    final deviceType =
        Platform.isLinux || Platform.isMacOS || Platform.isWindows
        ? 'Desktop'
        : 'Mobile';

    try {
      final response = await _authService.loginByCode(
        LoginByCodeRequest(
          account: account,
          code: code,
          deviceType: deviceType,
        ),
      );

      final token = response.data;
      if (token?.accessToken == null || token?.refreshToken == null) {
        throw Exception(response.message ?? '登录失败，请稍后重试');
      }

      await _storage.saveTokens(token!);
      final user = await _fetchCurrentUser();
      await _storage.saveUser(user);

      state = AsyncData(
        AuthState.authenticated(
          AuthSession(
            accessToken: token.accessToken,
            refreshToken: token.refreshToken,
            user: user,
          ),
        ),
      );
      return true;
    } catch (error) {
      await _clearStoredSessionSafely();
      state = AsyncData(
        AuthState.unauthenticated(errorMessage: _normalizeError(error)),
      );
      return false;
    }
  }

  Future<String> register({
    required String account,
    required String password,
    required String name,
  }) async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));

    try {
      final response = await _authService.register(
        UserCreateDto(account: account, password: password, name: name),
      );

      final registrationToken = response.data;
      if (registrationToken == null || registrationToken.isEmpty) {
        throw Exception(response.message ?? '注册失败，请稍后重试');
      }

      state = AsyncData(
        current.copyWith(isSubmitting: false, clearError: true),
      );
      return registrationToken;
    } catch (error) {
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      rethrow;
    }
  }

  Future<void> sendRegistrationCode(String registrationToken) async {
    await _runGuestAction(
      () => _authService.sendRegistrationCode(registrationToken),
    );
  }

  Future<void> verifyRegistration({
    required String registrationToken,
    required String code,
  }) async {
    await _runGuestAction(
      () => _authService.verifyRegistration(
        VerifyRegistrationRequest(
          registrationToken: registrationToken,
          code: code,
        ),
      ),
    );
  }

  Future<void> forgotPassword({required String account}) async {
    await _runGuestAction(
      () =>
          _authService.forgotPassword(ForgotPasswordRequest(account: account)),
    );
  }

  Future<void> resetPassword({
    required String token,
    required String newPassword,
  }) async {
    await _runGuestAction(
      () => _authService.resetPassword(
        ResetPasswordRequest(token: token, newPassword: newPassword),
      ),
    );
  }

  Future<void> logout() async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));

    try {
      final deviceType =
          Platform.isLinux || Platform.isMacOS || Platform.isWindows
          ? 'Desktop'
          : 'Mobile';
      await _authService.logout(deviceType: deviceType);
    } catch (_) {
      // Clearing local session takes priority over logout request failures.
    }

    await _clearStoredSessionSafely();
    state = AsyncData(AuthState.unauthenticated());
  }

  Future<UserDto?> refreshProfile() async {
    final current = state.asData?.value;
    if (current == null || current.session == null) {
      return null;
    }

    try {
      final response = await _userService.getProfile();
      final user = response.data;
      if (user == null) {
        throw Exception(response.message ?? '个人资料加载失败');
      }

      final nextSession = current.session!.copyWith(user: user);
      await _storage.saveUser(user);
      state = AsyncData(current.copyWith(session: nextSession));
      return user;
    } catch (error) {
      state = AsyncData(current.copyWith(errorMessage: _normalizeError(error)));
      rethrow;
    }
  }

  Future<UserDto> updateProfile(UserUpdateDto request) async {
    final current = state.asData?.value;
    if (current == null || current.session == null) {
      throw Exception('请先登录');
    }

    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));
    try {
      final response = await _userService.updateProfile(request);
      final user = response.data;
      if (user == null) {
        throw Exception(response.message ?? '更新个人资料失败');
      }

      final nextSession = current.session!.copyWith(user: user);
      await _storage.saveUser(user);
      state = AsyncData(
        current.copyWith(
          session: nextSession,
          isSubmitting: false,
          clearError: true,
        ),
      );
      return user;
    } catch (error) {
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      rethrow;
    }
  }

  Future<void> changePassword({
    required String oldPassword,
    required String newPassword,
  }) async {
    final current = state.asData?.value;
    if (current == null || current.session == null) {
      throw Exception('请先登录');
    }

    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));
    try {
      await _userService.changePassword(
        ChangePasswordRequest(
          oldPassword: oldPassword,
          newPassword: newPassword,
        ),
      );
      state = AsyncData(
        current.copyWith(isSubmitting: false, clearError: true),
      );
    } catch (error) {
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      rethrow;
    }
  }

  Future<void> sendEmailCode() async {
    await _runAuthenticatedAction(() => _authService.sendEmailCode());
  }

  Future<void> sendPhoneCode() async {
    await _runAuthenticatedAction(() => _authService.sendPhoneCode());
  }

  Future<void> verifyEmailCode({required String code}) async {
    await _runAuthenticatedAction(
      () => _authService.verifyEmailCode(VerifyCodeRequest(code: code)),
    );
    await refreshProfile();
  }

  Future<void> verifyPhone({required String code}) async {
    await _runAuthenticatedAction(() => _authService.verifyPhone(code: code));
    await refreshProfile();
  }

  Future<void> clearError() async {
    final current = state.asData?.value;
    if (current == null) {
      return;
    }
    state = AsyncData(current.copyWith(clearError: true));
  }

  Future<void> _runGuestAction(Future<dynamic> Function() action) async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));

    try {
      await action();
      state = AsyncData(
        current.copyWith(isSubmitting: false, clearError: true),
      );
    } catch (error) {
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      rethrow;
    }
  }

  Future<void> _runAuthenticatedAction(
    Future<dynamic> Function() action,
  ) async {
    final current = state.asData?.value;
    if (current == null || current.session == null) {
      throw Exception('请先登录');
    }

    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));

    try {
      await action();
      state = AsyncData(
        current.copyWith(isSubmitting: false, clearError: true),
      );
    } catch (error) {
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      rethrow;
    }
  }

  Future<UserDto> _fetchCurrentUser() async {
    final response = await _authService.getInfo();
    final user = response.data;
    if (user == null) {
      throw Exception(response.message ?? '用户信息加载失败');
    }
    return user;
  }

  Future<void> _handleUnauthorized() async {
    await _clearStoredSessionSafely();
    state = AsyncData(AuthState.unauthenticated());
  }

  Future<void> _clearStoredSessionSafely() async {
    try {
      await _storage.clear();
    } catch (_) {
      // Local cleanup should not block auth state transitions.
    }
  }

  String _normalizeError(Object error) {
    if (error is DioException) {
      final data = error.response?.data;
      if (data is Map<String, dynamic>) {
        final detail = data['detail'] ?? data['message'];
        if (detail is String && detail.trim().isNotEmpty) {
          return detail.trim();
        }
      }

      final message = error.message?.trim();
      if (message != null && message.isNotEmpty) {
        return message;
      }
    }

    final message = error.toString().trim();
    if (message.startsWith('Exception:')) {
      return message.substring('Exception:'.length).trim();
    }
    return message.isEmpty ? '操作失败，请稍后重试' : message;
  }
}
