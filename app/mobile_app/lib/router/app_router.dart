import 'package:flutter/cupertino.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../pages/auth/auth_loading_screen.dart';
import '../pages/auth/forgot_password_screen.dart';
import '../pages/auth/login_screen.dart';
import '../pages/auth/oauth_webview_screen.dart';
import '../pages/auth/register_screen.dart';
import '../pages/auth/register_verify_screen.dart';
import '../pages/auth/reset_password_screen.dart';
import '../pages/home/home_screen.dart';
import '../pages/hot/hot_screen.dart';
import '../pages/post/create_post_screen.dart';
import '../pages/post/post_detail_screen.dart';
import '../pages/tag/tag_screen.dart';
import '../pages/profile/change_password_screen.dart';
import '../pages/profile/binding_screen.dart';
import '../pages/profile/following_screen.dart';
import '../pages/profile/profile_screen.dart';
import '../pages/profile/public_profile_screen.dart';
import '../pages/notification/notification_screen.dart';
import '../pages/legal/terms_screen.dart';
import '../pages/legal/privacy_screen.dart';
import '../pages/main_shell.dart';
import '../providers/auth_provider.dart';
import '../providers/notification_provider.dart';

abstract final class AppRoutePaths {
  static const splash = '/splash';
  static const login = '/login';
  static const register = '/register';
  static const registerVerify = '/register/verify';
  static const forgotPassword = '/forgot-password';
  static const resetPassword = '/reset-password';
  static const oauthWebView = '/oauth-webview';
  static const home = '/';
  static const notifications = '/notifications';
  static const hot = '/hot';
  static const profile = '/profile';
  static const changePassword = '/profile/reset-password';
  static const bindings = '/profile/bindings';
  static const following = '/following';
  static const publicProfile = '/user/:userId';
  static const createPost = '/post/create';
  static const postDetail = '/post/:postId';
  static const tag = '/tag/:tagId';
  static const terms = '/terms';
  static const privacy = '/privacy';
}

abstract final class AppRouteNames {
  static const splash = 'splash';
  static const login = 'login';
  static const register = 'register';
  static const registerVerify = 'register-verify';
  static const forgotPassword = 'forgot-password';
  static const resetPassword = 'reset-password';
  static const oauthWebView = 'oauth-webview';
  static const home = 'home';
  static const notifications = 'notifications';
  static const hot = 'hot';
  static const profile = 'profile';
  static const changePassword = 'change-password';
  static const bindings = 'bindings';
  static const following = 'following';
  static const publicProfile = 'public-profile';
  static const createPost = 'create-post';
  static const postDetail = 'post-detail';
  static const tag = 'tag';
  static const terms = 'terms';
  static const privacy = 'privacy';
}

abstract final class AppRouteParams {
  static const postId = 'postId';
  static const userId = 'userId';
  static const tagId = 'tagId';
}

String buildPostDetailLocation(String postId) {
  return '/post/$postId';
}

String buildPublicProfileLocation(String userId) {
  return '/user/$userId';
}

String buildTagLocation(String tagId) {
  return '/tag/$tagId';
}

final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: AppRoutePaths.splash,
    refreshListenable: _RouterRefreshNotifier(ref),
    redirect: (context, state) {
      final authAsync = ref.read(authControllerProvider);
      final path = state.uri.path;
      final isOAuthRoute = path == AppRoutePaths.oauthWebView;
      final isOAuthBindRoute =
          isOAuthRoute && state.uri.queryParameters['purpose'] == 'bind';
      final isAuthRoute = _authRoutes.contains(path);
      final isSplashRoute = path == AppRoutePaths.splash;
      final isPublicRoute = _isPublicRoute(path);

      // 如果还在初始化加载中，保持在启动页
      if (authAsync.isLoading) {
        return isSplashRoute ? null : AppRoutePaths.splash;
      }

      // 获取当前认证状态，如果没有值（比如出错），默认为未认证
      final authState = authAsync.asData?.value ?? AuthState.unauthenticated();
      final isAuthenticated = authState.status == AuthStatus.authenticated;

      if (isAuthenticated) {
        // 已登录：如果当前在启动页或认证页（登录/注册等），跳到主页。
        // 第三方绑定也使用 OAuth WebView，但它是已登录后的受保护流程。
        if (isSplashRoute || (isAuthRoute && !isOAuthBindRoute)) {
          return AppRoutePaths.home;
        }
        // 其他情况保持当前路径
        return null;
      }

      // 未登录：
      // 绑定流程必须依赖当前账号，未登录时不能直接进入绑定 WebView。
      if (isOAuthBindRoute) {
        return AppRoutePaths.login;
      }

      // 如果当前就在认证页或公开内容页，不需要跳转
      if (isAuthRoute || isPublicRoute) {
        return null;
      }

      // 如果当前在启动页或其他受保护页面，强制跳到登录页
      return AppRoutePaths.login;
    },
    routes: [
      GoRoute(
        path: AppRoutePaths.splash,
        name: AppRouteNames.splash,
        builder: (context, state) => const AuthLoadingScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.login,
        name: AppRouteNames.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.register,
        name: AppRouteNames.register,
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.registerVerify,
        name: AppRouteNames.registerVerify,
        builder: (context, state) {
          final registrationToken =
              state.uri.queryParameters['registrationToken'];
          final account = state.uri.queryParameters['account'];
          if (registrationToken == null || registrationToken.isEmpty) {
            return const LoginScreen();
          }
          return RegisterVerifyScreen(
            registrationToken: registrationToken,
            account: account,
          );
        },
      ),
      GoRoute(
        path: AppRoutePaths.forgotPassword,
        name: AppRouteNames.forgotPassword,
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.resetPassword,
        name: AppRouteNames.resetPassword,
        builder: (context, state) => ResetPasswordScreen(
          initialToken: state.uri.queryParameters['token'],
          account: state.uri.queryParameters['account'],
        ),
      ),
      GoRoute(
        path: AppRoutePaths.oauthWebView,
        name: AppRouteNames.oauthWebView,
        pageBuilder: (context, state) {
          final provider = state.uri.queryParameters['provider'] ?? 'Github';
          final purpose = state.uri.queryParameters['purpose'] ?? 'login';
          return CupertinoPage<void>(
            key: ValueKey('oauth-webview:${state.uri}'),
            child: OAuthWebViewScreen(provider: provider, purpose: purpose),
          );
        },
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            MainShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutePaths.home,
                name: AppRouteNames.home,
                builder: (context, state) => const HomeScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutePaths.hot,
                name: AppRouteNames.hot,
                builder: (context, state) => const HotScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutePaths.notifications,
                name: AppRouteNames.notifications,
                builder: (context, state) => NotificationScreen(
                  service: ref.watch(notificationServiceProvider),
                ),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutePaths.profile,
                name: AppRouteNames.profile,
                builder: (context, state) => const ProfileScreen(),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: AppRoutePaths.bindings,
        name: AppRouteNames.bindings,
        builder: (context, state) => const BindingScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.changePassword,
        name: AppRouteNames.changePassword,
        builder: (context, state) => const ChangePasswordScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.following,
        name: AppRouteNames.following,
        builder: (context, state) {
          final tab = state.uri.queryParameters['tab'] ?? 'following';
          return FollowingScreen(initialTab: tab);
        },
      ),
      GoRoute(
        path: AppRoutePaths.publicProfile,
        name: AppRouteNames.publicProfile,
        builder: (context, state) {
          final userId = state.pathParameters[AppRouteParams.userId];
          if (userId == null || userId.isEmpty) {
            throw StateError('公开主页路由缺少 userId');
          }
          return PublicProfileScreen(userId: userId);
        },
      ),
      GoRoute(
        path: AppRoutePaths.createPost,
        name: AppRouteNames.createPost,
        builder: (context, state) => const CreatePostScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.postDetail,
        name: AppRouteNames.postDetail,
        builder: (context, state) {
          final postId = state.pathParameters[AppRouteParams.postId];
          if (postId == null || postId.isEmpty) {
            throw StateError('帖子详情路由缺少 postId');
          }
          return PostDetailScreen(postId: postId);
        },
      ),
      GoRoute(
        path: AppRoutePaths.tag,
        name: AppRouteNames.tag,
        builder: (context, state) {
          final tagId = state.pathParameters[AppRouteParams.tagId];
          if (tagId == null || tagId.isEmpty) {
            throw StateError('标签页路由缺少 tagId');
          }
          return TagScreen(tagId: tagId);
        },
      ),
      GoRoute(
        path: AppRoutePaths.terms,
        name: AppRouteNames.terms,
        builder: (context, state) => const TermsScreen(),
      ),
      GoRoute(
        path: AppRoutePaths.privacy,
        name: AppRouteNames.privacy,
        builder: (context, state) => const PrivacyScreen(),
      ),
    ],
  );
});

const _authRoutes = {
  AppRoutePaths.login,
  AppRoutePaths.register,
  AppRoutePaths.registerVerify,
  AppRoutePaths.forgotPassword,
  AppRoutePaths.resetPassword,
  AppRoutePaths.oauthWebView,
};

bool _isPublicRoute(String path) {
  // Legal pages are publicly accessible.
  if (path == AppRoutePaths.terms || path == AppRoutePaths.privacy) return true;

  // Post detail pages (e.g. /post/abc123) are publicly viewable, but NOT /post/create.
  if (path == AppRoutePaths.createPost) return false;
  final postDetailPattern = RegExp(r'^/post/[^/]+$');
  if (postDetailPattern.hasMatch(path)) return true;

  // Public user profiles (e.g. /user/abc123) are publicly viewable.
  final publicProfilePattern = RegExp(r'^/user/[^/]+$');
  if (publicProfilePattern.hasMatch(path)) return true;

  final tagPattern = RegExp(r'^/tag/[^/]+$');
  if (tagPattern.hasMatch(path)) return true;

  return false;
}

class _RouterRefreshNotifier extends ChangeNotifier {
  _RouterRefreshNotifier(Ref ref) {
    _subscription = ref.listen(
      authControllerProvider,
      (previous, next) => notifyListeners(),
    );
  }

  late final ProviderSubscription _subscription;

  @override
  void dispose() {
    _subscription.close();
    super.dispose();
  }
}
