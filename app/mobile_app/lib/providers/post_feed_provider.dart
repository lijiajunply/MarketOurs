import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/post.dart';
import '../services/post_service.dart';

final postServiceProvider = Provider<PostService>((ref) => PostService());

final homeFeedProvider = AsyncNotifierProvider<HomeFeedNotifier, HomeFeedState>(
  HomeFeedNotifier.new,
);

class HomeFeedState {
  const HomeFeedState({
    required this.posts,
    required this.pageIndex,
    required this.hasNextPage,
    required this.isLoadingMore,
  });

  final List<PostDto> posts;
  final int pageIndex;
  final bool hasNextPage;
  final bool isLoadingMore;

  HomeFeedState copyWith({
    List<PostDto>? posts,
    int? pageIndex,
    bool? hasNextPage,
    bool? isLoadingMore,
  }) {
    return HomeFeedState(
      posts: posts ?? this.posts,
      pageIndex: pageIndex ?? this.pageIndex,
      hasNextPage: hasNextPage ?? this.hasNextPage,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    );
  }
}

class HomeFeedNotifier extends AsyncNotifier<HomeFeedState> {
  static const int _pageSize = 20;

  @override
  Future<HomeFeedState> build() => _fetchPage(pageIndex: 1);

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _fetchPage(pageIndex: 1));
  }

  Future<void> loadMore() async {
    final currentState = state.asData?.value;
    if (currentState == null ||
        currentState.isLoadingMore ||
        !currentState.hasNextPage) {
      return;
    }

    state = AsyncData(currentState.copyWith(isLoadingMore: true));

    try {
      final nextState = await _fetchPage(
        pageIndex: currentState.pageIndex + 1,
        previousPosts: currentState.posts,
      );
      state = AsyncData(nextState);
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
    }
  }

  Future<HomeFeedState> _fetchPage({
    required int pageIndex,
    List<PostDto> previousPosts = const [],
  }) async {
    final service = ref.read(postServiceProvider);
    final response = await service.getPosts(
      pageIndex: pageIndex,
      pageSize: _pageSize,
    );
    final page = response.data;

    if (page == null) {
      throw Exception(response.message ?? '帖子数据为空');
    }

    return HomeFeedState(
      posts: [...previousPosts, ...page.items],
      pageIndex: page.pageIndex,
      hasNextPage: page.hasNextPage,
      isLoadingMore: false,
    );
  }
}
