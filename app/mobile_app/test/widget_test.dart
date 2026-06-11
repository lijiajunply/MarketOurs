import 'package:flutter/cupertino.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:mobile_app/main.dart';
import 'package:mobile_app/models/api_response.dart';
import 'package:mobile_app/models/auth.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/post.dart';
import 'package:mobile_app/models/user.dart';
import 'package:mobile_app/providers/auth_provider.dart';
import 'package:mobile_app/providers/post_feed_provider.dart';
import 'package:mobile_app/services/auth_service.dart';
import 'package:mobile_app/services/auth_storage.dart';
import 'package:mobile_app/ui/app_responsive.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('shows login when no local token exists', (tester) async {
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

    expect(find.text('登录'), findsWidgets);
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

    expect(find.text('首页'), findsWidgets);
    expect(find.text('热榜'), findsWidgets);
    expect(find.byIcon(CupertinoIcons.plus_circle_fill), findsOneWidget);
  });

  testWidgets('uses bottom navigation on phone width', (tester) async {
    tester.view
      ..physicalSize = const Size(390, 844)
      ..devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

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

    expect(find.byType(CupertinoTabBar), findsOneWidget);
    expect(
      find.byKey(const ValueKey('main-shell-tablet-side-navigation')),
      findsNothing,
    );
  });

  testWidgets('uses side navigation on tablet width and switches tabs', (
    tester,
  ) async {
    tester.view
      ..physicalSize = const Size(1024, 768)
      ..devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

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

    final sideNavigation = find.byKey(
      const ValueKey('main-shell-tablet-side-navigation'),
    );

    expect(sideNavigation, findsOneWidget);
    expect(find.byType(CupertinoTabBar), findsNothing);

    await tester.tap(
      find.descendant(of: sideNavigation, matching: find.text('热榜')),
    );
    await tester.pumpAndSettle();

    expect(find.text('热榜暂时为空'), findsOneWidget);
  });

  testWidgets('uses two column home feed on desktop width', (tester) async {
    tester.view
      ..physicalSize = const Size(1366, 900)
      ..devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

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
          homeFeedProvider.overrideWith(
            () => _FakeHomeFeedNotifier(posts: _demoPosts),
          ),
          hotFeedProvider.overrideWith(() => _FakeHotFeedNotifier()),
        ],
        child: const MarketOursApp(),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.byKey(const ValueKey('home-feed-columns-2')), findsOneWidget);
    expect(find.text('桌面响应式帖子 A'), findsOneWidget);
    expect(find.text('桌面响应式帖子 B'), findsOneWidget);
  });

  testWidgets('responsive helpers classify desktop layout', (tester) async {
    tester.view
      ..physicalSize = const Size(1366, 900)
      ..devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    late bool isDesktop;
    late bool isWideTwoPane;
    late int columns;

    await tester.pumpWidget(
      CupertinoApp(
        home: Builder(
          builder: (context) {
            isDesktop = AppResponsive.isDesktop(context);
            isWideTwoPane = AppResponsive.isWideTwoPane(context);
            columns = AppResponsive.listColumnCount(context);
            return const SizedBox.shrink();
          },
        ),
      ),
    );

    expect(isDesktop, isTrue);
    expect(isWideTwoPane, isTrue);
    expect(columns, 2);
  });

  testWidgets('restores session by refreshing expired access token', (
    tester,
  ) async {
    final storage = _TestAuthStorage(
      session: AuthSession(accessToken: 'expired', refreshToken: 'refresh'),
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authStorageProvider.overrideWithValue(storage),
          authServiceProvider.overrideWithValue(
            _FakeAuthService(
              user: _demoUser,
              getInfoError: Exception('token expired'),
              refreshTokenResult: TokenDto(
                accessToken: 'refreshed-access',
                refreshToken: 'refreshed-refresh',
              ),
            ),
          ),
          homeFeedProvider.overrideWith(() => _FakeHomeFeedNotifier()),
          hotFeedProvider.overrideWith(() => _FakeHotFeedNotifier()),
        ],
        child: const MarketOursApp(),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('首页'), findsWidgets);
    expect(storage.session?.accessToken, 'refreshed-access');
    expect(storage.session?.refreshToken, 'refreshed-refresh');
    expect(storage.session?.user?.id, _demoUser.id);
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

    expect(find.text('登录'), findsWidgets);
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
  _FakeAuthService({
    this.user,
    this.loginToken,
    this.getInfoError,
    this.refreshTokenResult,
  });

  final UserDto? user;
  final TokenDto? loginToken;
  final Object? getInfoError;
  final TokenDto? refreshTokenResult;
  int _getInfoAttempts = 0;

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
    _getInfoAttempts += 1;
    if (getInfoError != null &&
        (refreshTokenResult == null || _getInfoAttempts == 1)) {
      throw getInfoError!;
    }
    return ApiResponse<UserDto>(message: 'ok', data: user ?? _demoUser);
  }

  @override
  Future<ApiResponse<TokenDto>> refresh(RefreshRequest request) async {
    return ApiResponse<TokenDto>(
      message: 'ok',
      data:
          refreshTokenResult ??
          TokenDto(
            accessToken: 'refreshed-access',
            refreshToken: 'refreshed-refresh',
          ),
    );
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
  _FakeHomeFeedNotifier({this.posts = const []});

  final List<PostDto> posts;

  @override
  Future<HomeFeedState> build() async {
    return HomeFeedState(
      posts: posts,
      pageIndex: 1,
      hasNextPage: false,
      isLoadingMore: false,
    );
  }
}

class _FakeHotFeedNotifier extends HotFeedNotifier {
  @override
  Future<HotFeedState> build() async {
    return const HotFeedState(posts: [], isRefreshing: false);
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
  Future<String?> readRefreshToken() async {
    return _session?.refreshToken;
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

final _demoPosts = [
  PostDto(
    id: 'post-a',
    title: '桌面响应式帖子 A',
    content: '用于验证桌面两列布局。',
    userId: _demoUser.id,
    author: UserSimpleDto(id: _demoUser.id, name: _demoUser.name),
    likes: 1,
    dislikes: 0,
    watch: 12,
    commentsCount: 2,
  ),
  PostDto(
    id: 'post-b',
    title: '桌面响应式帖子 B',
    content: '用于验证第二张卡片。',
    userId: _demoUser.id,
    author: UserSimpleDto(id: _demoUser.id, name: _demoUser.name),
    likes: 3,
    dislikes: 0,
    watch: 24,
    commentsCount: 4,
  ),
];
