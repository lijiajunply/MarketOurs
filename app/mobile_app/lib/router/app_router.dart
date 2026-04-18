import 'package:go_router/go_router.dart';

import '../pages/home/home_screen.dart';
import '../pages/post/post_detail_screen.dart';

abstract final class AppRoutePaths {
  static const home = '/';
  static const postDetail = '/posts/:postId';
}

abstract final class AppRouteNames {
  static const home = 'home';
  static const postDetail = 'post-detail';
}

abstract final class AppRouteParams {
  static const postId = 'postId';
}

String buildPostDetailLocation(String postId) {
  return '/posts/$postId';
}

GoRouter buildAppRouter() {
  return GoRouter(
    initialLocation: AppRoutePaths.home,
    routes: [
      GoRoute(
        path: AppRoutePaths.home,
        name: AppRouteNames.home,
        builder: (context, state) => const HomeScreen(),
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
}
