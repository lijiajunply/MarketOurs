import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../pages/auth/auth_loading_screen.dart';
import '../pages/auth/forgot_password_screen.dart';
import '../pages/auth/login_screen.dart';
import '../pages/auth/register_screen.dart';
import '../pages/auth/register_verify_screen.dart';
import '../pages/auth/reset_password_screen.dart';
import '../pages/home/home_screen.dart';
import '../pages/hot/hot_screen.dart';
import '../pages/post/create_post_screen.dart';
import '../pages/post/post_detail_screen.dart';
import '../pages/profile/change_password_screen.dart';
import '../pages/profile/profile_screen.dart';
import '../pages/profile/public_profile_screen.dart';
import '../pages/notification/notification_screen.dart';
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
  static const home = '/';
  static const notifications = '/notifications';
  static const hot = '/hot';
  static const profile = '/profile';
  static const changePassword = '/profile/reset-password';
  static const publicProfile = '/user/:userId';
  static const createPost = '/post/create';
  static const postDetail = '/post/:postId';
}

abstract final class AppRouteNames {
  static const splash = 'splash';
  static const login = 'login';
  static const register = 'register';
  static const registerVerify = 'register-verify';
  static const forgotPassword = 'forgot-password';
  static const resetPassword = 'reset-password';
  static const home = 'home';
  static const notifications = 'notifications';
  static const hot = 'hot';
  static const profile = 'profile';
  static const changePassword = 'change-password';
  static const publicProfile = 'public-profile';
  static const createPost = 'create-post';
  static const postDetail = 'post-detail';
}

abstract final class AppRouteParams {
  static const postId = 'postId';
  static const userId = 'userId';
}

String buildPostDetailLocation(String postId) {
  return '/post/$postId';
}

String buildPublicProfileLocation(String userId) {
  return '/user/$userId';
}

final appRouterProvider = Provider<GoRouter>((ref) {
  final authAsync = ref.watch(authControllerProvider);

  return GoRouter(
    initialLocation: AppRoutePaths.splash,
    redirect: (context, state) {
      final path = state.uri.path;
      final isAuthRoute = _authRoutes.contains(path);
      final isSplashRoute = path == AppRoutePaths.splash;
      final requiresAuth = _requiresAuth(path);

      if (authAsync.isLoading) {
        return isSplashRoute ? null : AppRoutePaths.splash;
      }

      final authState = authAsync.asData?.value ?? AuthState.unauthenticated();
      final isAuthenticated = authState.status == AuthStatus.authenticated;

      if (isAuthenticated) {
        if (isSplashRoute || isAuthRoute) {
          return AppRoutePaths.home;
        }
        return null;
      }

      if (isSplashRoute) {
        return AppRoutePaths.home;
      }

      if (isAuthRoute || !requiresAuth) {
        return null;
      }

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
        path: AppRoutePaths.changePassword,
        name: AppRouteNames.changePassword,
        builder: (context, state) => const ChangePasswordScreen(),
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
    ],
  );
});

const _authRoutes = {
  AppRoutePaths.login,
  AppRoutePaths.register,
  AppRoutePaths.registerVerify,
  AppRoutePaths.forgotPassword,
  AppRoutePaths.resetPassword,
};

bool _requiresAuth(String path) {
  if (path == AppRoutePaths.notifications ||
      path == AppRoutePaths.profile ||
      path == AppRoutePaths.changePassword ||
      path == AppRoutePaths.createPost) {
    return true;
  }

  return false;
}
