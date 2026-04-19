import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:mobile_app/main.dart';
import 'package:mobile_app/models/api_response.dart';
import 'package:mobile_app/models/auth.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/user.dart';
import 'package:mobile_app/providers/auth_provider.dart';
import 'package:mobile_app/providers/post_feed_provider.dart';
import 'package:mobile_app/services/auth_service.dart';
import 'package:mobile_app/services/auth_storage.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('shows login screen when no local token exists', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authStorageProvider.overrideWithValue(_TestAuthStorage()),
          authServiceProvider.overrideWithValue(_FakeAuthService()),
          homeFeedProvider.overrideWith(() => _FakeHomeFeedNotifier()),
          hotFeedProvider.overrideWith(() => _FakeHotFeedNotifier()),
        ],
        child: const MarketOursApp(),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.widgetWithText(FilledButton, '登录'), findsOneWidget);
    expect(find.text('没有账号？去注册'), findsOneWidget);
  });

  testWidgets('shows home when token restore succeeds', (tester) async {
    final storage = _TestAuthStorage(
      session: AuthSession(accessToken: 'access', refreshToken: 'refresh'),
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authStorageProvider.overrideWithValue(storage),
          authServiceProvider.overrideWithValue(
            _FakeAuthService(user: _demoUser),
          ),
          homeFeedProvider.overrideWith(() => _FakeHomeFeedNotifier()),
          hotFeedProvider.overrideWith(() => _FakeHotFeedNotifier()),
        ],
        child: const MarketOursApp(),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('首页'), findsOneWidget);
    expect(find.text('MarketOurs'), findsOneWidget);
    expect(find.text('热榜'), findsOneWidget);
  });

  testWidgets('clears invalid restored token and returns to login', (
    tester,
  ) async {
    final storage = _TestAuthStorage(
      session: AuthSession(accessToken: 'bad-access', refreshToken: 'refresh'),
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authStorageProvider.overrideWithValue(storage),
          authServiceProvider.overrideWithValue(
            _FakeAuthService(getInfoError: Exception('token expired')),
          ),
          homeFeedProvider.overrideWith(() => _FakeHomeFeedNotifier()),
          hotFeedProvider.overrideWith(() => _FakeHotFeedNotifier()),
        ],
        child: const MarketOursApp(),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.widgetWithText(FilledButton, '登录'), findsOneWidget);
    expect(storage.session, isNull);
  });

  test('login stores tokens and authenticates user', () async {
    final storage = _TestAuthStorage();
    final container = ProviderContainer(
      overrides: [
        authStorageProvider.overrideWithValue(storage),
        authServiceProvider.overrideWithValue(
          _FakeAuthService(
            loginToken: TokenDto(
              accessToken: 'new-access',
              refreshToken: 'new-refresh',
            ),
            user: _demoUser,
          ),
        ),
      ],
    );
    addTearDown(container.dispose);

    await container.read(authControllerProvider.future);
    final success = await container
        .read(authControllerProvider.notifier)
        .login(account: 'demo@example.com', password: 'secret123');

    final authState = container.read(authControllerProvider).asData?.value;

    expect(success, isTrue);
    expect(authState?.status, AuthStatus.authenticated);
    expect(storage.session?.accessToken, 'new-access');
    expect(storage.session?.user?.name, _demoUser.name);
  });

  test('logout clears session and resets auth state', () async {
    final storage = _TestAuthStorage(
      session: AuthSession(
        accessToken: 'existing-access',
        refreshToken: 'existing-refresh',
        user: _demoUser,
      ),
    );
    final container = ProviderContainer(
      overrides: [
        authStorageProvider.overrideWithValue(storage),
        authServiceProvider.overrideWithValue(
          _FakeAuthService(user: _demoUser),
        ),
      ],
    );
    addTearDown(container.dispose);

    await container.read(authControllerProvider.future);
    await container.read(authControllerProvider.notifier).logout();

    final authState = container.read(authControllerProvider).asData?.value;

    expect(authState?.status, AuthStatus.unauthenticated);
    expect(storage.session, isNull);
  });
}

class _FakeAuthService extends AuthService {
  _FakeAuthService({this.user, this.loginToken, this.getInfoError});

  final UserDto? user;
  final TokenDto? loginToken;
  final Object? getInfoError;

  @override
  Future<ApiResponse<TokenDto>> login(LoginRequest request) async {
    return ApiResponse<TokenDto>(
      message: 'ok',
      data:
          loginToken ??
          TokenDto(accessToken: 'access-token', refreshToken: 'refresh-token'),
    );
  }

  @override
  Future<ApiResponse<UserDto>> getInfo() async {
    if (getInfoError != null) {
      throw getInfoError!;
    }
    return ApiResponse<UserDto>(message: 'ok', data: user ?? _demoUser);
  }

  @override
  Future<ApiResponse<String>> register(UserCreateDto request) async {
    return ApiResponse<String>(message: 'ok', data: 'reg-token');
  }

  @override
  Future<ApiResponse> sendRegistrationCode(String regToken) async {
    return ApiResponse(message: 'ok');
  }

  @override
  Future<ApiResponse<UserDto>> verifyRegistration(
    VerifyRegistrationRequest request,
  ) async {
    return ApiResponse<UserDto>(message: 'ok', data: user ?? _demoUser);
  }

  @override
  Future<ApiResponse> forgotPassword(ForgotPasswordRequest request) async {
    return ApiResponse(message: 'ok');
  }

  @override
  Future<ApiResponse> resetPassword(ResetPasswordRequest request) async {
    return ApiResponse(message: 'ok');
  }

  @override
  Future<ApiResponse> logout({String deviceType = 'Web'}) async {
    return ApiResponse(message: 'ok');
  }
}

class _FakeHomeFeedNotifier extends HomeFeedNotifier {
  @override
  Future<HomeFeedState> build() async {
    return const HomeFeedState(
      posts: [],
      pageIndex: 1,
      hasNextPage: false,
      isLoadingMore: false,
    );
  }
}

class _FakeHotFeedNotifier extends HotFeedNotifier {
  @override
  Future<HotFeedState> build() async {
    return const HotFeedState(
      posts: [],
      isRefreshing: false,
    );
  }
}

class _TestAuthStorage implements AuthStorage {
  _TestAuthStorage({AuthSession? session}) : _session = session;

  AuthSession? _session;

  AuthSession? get session => _session;

  @override
  Future<void> clear() async {
    _session = null;
  }

  @override
  Future<String?> readAccessToken() async {
    return _session?.accessToken;
  }

  @override
  Future<AuthSession?> readSession() async {
    return _session;
  }

  @override
  Future<void> saveTokens(TokenDto token) async {
    _session = AuthSession(
      accessToken: token.accessToken,
      refreshToken: token.refreshToken,
      user: _session?.user,
    );
  }

  @override
  Future<void> saveUser(UserDto user) async {
    _session = (_session ?? const AuthSession()).copyWith(user: user);
  }
}

final _demoUser = UserDto(
  id: 'user-1',
  name: '测试用户',
  email: 'demo@example.com',
);
