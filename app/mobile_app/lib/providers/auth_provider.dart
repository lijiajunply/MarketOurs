import 'dart:io';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/foundation.dart';

import '../models/auth.dart';
import '../models/auth_session.dart';
import '../models/user.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/auth_storage.dart';
import '../services/error_messages.dart';
import '../services/push_notification_service.dart';
import '../services/user_service.dart';
import 'post_feed_provider.dart';

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

    // 乐观认证:本地已缓存用户信息时立即放行进入首页,真正的令牌校验放到
    // 后台静默执行,避免启动时阻塞在加载页等待一次网络往返(原先的瓶颈)。
    if (session.user != null) {
      ref.read(homeFeedProvider);
      _validateSessionInBackground(session);
      return AuthState.authenticated(session);
    }

    // 无本地用户缓存(极少见):仍需等待网络获取用户信息后再放行。
    try {
      final user = await _restoreUserFromSession(session);
      final latestSession = await _storage.readSession();
      final restoredSession = (latestSession ?? session).copyWith(user: user);
      await _storage.saveUser(user);

      ref.read(homeFeedProvider);

      return AuthState.authenticated(restoredSession);
    } catch (_) {
      await _clearStoredSessionSafely();
      return AuthState.unauthenticated();
    }
  }

  /// 后台静默校验会话有效性:成功则用最新用户信息刷新状态;若令牌彻底失效则登出。
  /// 整个过程不阻塞首屏渲染。
  Future<void> _validateSessionInBackground(AuthSession session) async {
    try {
      final user = await _restoreUserFromSession(session);
      await _storage.saveUser(user);
      final latestSession = await _storage.readSession();
      final updatedSession = (latestSession ?? session).copyWith(user: user);

      // 仅在仍处于已认证状态时更新,避免覆盖期间用户的登出等操作。
      final current = state.asData?.value;
      if (current != null && current.status == AuthStatus.authenticated) {
        state = AsyncData(AuthState.authenticated(updatedSession));
      }
    } catch (_) {
      // 令牌彻底失效(getInfo 与 refresh 均失败)→ 清理并登出。
      await _clearStoredSessionSafely();
      final current = state.asData?.value;
      if (current != null && current.status == AuthStatus.authenticated) {
        state = AsyncData(AuthState.unauthenticated());
      }
    }
  }

  Future<bool> login({
    required String account,
    required String password,
  }) async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));
    var savedTokens = false;

    final deviceType =
        (!kIsWeb) &&
            (Platform.isLinux || Platform.isMacOS || Platform.isWindows)
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
      savedTokens = true;
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
      await PushNotificationService.instance.syncCurrentToken();
      return true;
    } catch (error) {
      if (savedTokens) {
        await _clearStoredSessionSafely();
        state = AsyncData(
          AuthState.unauthenticated(errorMessage: _normalizeError(error)),
        );
        return false;
      }
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      return false;
    }
  }

  Future<void> sendLoginCode({required String account, String? captchaToken}) async {
    try {
      await _authService.sendLoginCode(SendCodeRequest(account: account, captchaToken: captchaToken));
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
    var savedTokens = false;

    final deviceType =
        (!kIsWeb) &&
            (Platform.isLinux || Platform.isMacOS || Platform.isWindows)
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
      savedTokens = true;
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
      await PushNotificationService.instance.syncCurrentToken();
      return true;
    } catch (error) {
      if (savedTokens) {
        await _clearStoredSessionSafely();
        state = AsyncData(
          AuthState.unauthenticated(errorMessage: _normalizeError(error)),
        );
        return false;
      }
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      return false;
    }
  }

  Future<String> register({
    required String account,
    required String password,
    required String name,
    String? avatar,
  }) async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));

    try {
      final response = await _authService.register(
        UserCreateDto(
          account: account,
          password: password,
          name: name,
          avatar: avatar,
          role: 'User',
        ),
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

  Future<void> sendRegistrationCode(String registrationToken, {String? captchaToken}) async {
    await _runGuestAction(
      () => _authService.sendRegistrationCode(registrationToken, captchaToken: captchaToken),
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

  Future<void> forgotPassword({required String account, String? captchaToken}) async {
    await _runGuestAction(
      () =>
          _authService.forgotPassword(ForgotPasswordRequest(account: account, captchaToken: captchaToken)),
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
      await PushNotificationService.instance.clearRegisteredToken();
      final deviceType =
          (!kIsWeb) &&
              (Platform.isLinux || Platform.isMacOS || Platform.isWindows)
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

  Future<void> sendEmailCode({String purpose = 'verification'}) async {
    await _runAuthenticatedAction(
      () => _authService.sendEmailCode(purpose: purpose),
    );
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

  Future<void> unbindThirdParty({
    required String provider,
    required String channel,
    required String code,
  }) async {
    await _runAuthenticatedAction(
      () => _authService.unbindThirdParty(
        UnbindThirdPartyRequest(
          provider: provider,
          channel: channel,
          code: code,
        ),
      ),
    );
    await refreshProfile();
  }

  Future<bool> handleOAuthTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    final current = state.asData?.value ?? AuthState.unauthenticated();
    state = AsyncData(current.copyWith(isSubmitting: true, clearError: true));
    var savedTokens = false;

    try {
      final token = TokenDto(
        accessToken: accessToken,
        refreshToken: refreshToken,
      );
      await _storage.saveTokens(token);
      savedTokens = true;
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
      await PushNotificationService.instance.syncCurrentToken();
      return true;
    } catch (error) {
      if (savedTokens) {
        await _clearStoredSessionSafely();
        state = AsyncData(
          AuthState.unauthenticated(errorMessage: _normalizeError(error)),
        );
        return false;
      }
      state = AsyncData(
        current.copyWith(
          isSubmitting: false,
          errorMessage: _normalizeError(error),
        ),
      );
      return false;
    }
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

  Future<UserDto> _restoreUserFromSession(AuthSession session) async {
    try {
      return await _fetchCurrentUser();
    } catch (error) {
      if ((session.refreshToken?.isNotEmpty ?? false) == false) {
        rethrow;
      }

      final refreshed = await _authService.refresh(
        RefreshRequest(
          refreshToken: session.refreshToken!,
          deviceType: _resolveDeviceType(),
        ),
      );
      final token = refreshed.data;
      if (token?.accessToken == null || token?.refreshToken == null) {
        throw Exception('令牌刷新失败：服务端返回了不完整的令牌数据');
      }

      await _storage.saveTokens(token!);
      return _fetchCurrentUser();
    }
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
    return extractErrorFromException(error);
  }

  String _resolveDeviceType() {
    if (kIsWeb) {
      return 'Web';
    }

    return Platform.isLinux || Platform.isMacOS || Platform.isWindows
        ? 'Desktop'
        : 'Mobile';
  }
}
