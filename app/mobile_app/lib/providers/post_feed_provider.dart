import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/post.dart';
import '../services/post_service.dart';

final postServiceProvider = Provider<PostService>((ref) => PostService());

final homeFeedProvider = AsyncNotifierProvider<HomeFeedNotifier, HomeFeedState>(
  HomeFeedNotifier.new,
);
final hotFeedProvider = AsyncNotifierProvider<HotFeedNotifier, HotFeedState>(
  HotFeedNotifier.new,
);

class HomeFeedState {
  const HomeFeedState({
    required this.posts,
    required this.pageIndex,
    required this.hasNextPage,
    required this.isLoadingMore,
    required this.isRefreshing,
    required this.keyword,
  });

  final List<PostDto> posts;
  final int pageIndex;
  final bool hasNextPage;
  final bool isLoadingMore;
  final bool isRefreshing;
  final String keyword;

  HomeFeedState copyWith({
    List<PostDto>? posts,
    int? pageIndex,
    bool? hasNextPage,
    bool? isLoadingMore,
    bool? isRefreshing,
    String? keyword,
  }) {
    return HomeFeedState(
      posts: posts ?? this.posts,
      pageIndex: pageIndex ?? this.pageIndex,
      hasNextPage: hasNextPage ?? this.hasNextPage,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      isRefreshing: isRefreshing ?? this.isRefreshing,
      keyword: keyword ?? this.keyword,
    );
  }
}

class HotFeedState {
  const HotFeedState({required this.posts, required this.isRefreshing});

  final List<PostDto> posts;
  final bool isRefreshing;

  HotFeedState copyWith({List<PostDto>? posts, bool? isRefreshing}) {
    return HotFeedState(
      posts: posts ?? this.posts,
      isRefreshing: isRefreshing ?? this.isRefreshing,
    );
  }
}

class HomeFeedNotifier extends AsyncNotifier<HomeFeedState> {
  static const int _pageSize = 20;

  @override
  Future<HomeFeedState> build() => _fetchPage(pageIndex: 1);

  Future<void> refresh() async {
    final currentState = state.asData?.value;
    if (currentState == null) {
      state = const AsyncLoading();
    } else {
      state = AsyncData(currentState.copyWith(isRefreshing: true));
    }

    state = await AsyncValue.guard(
      () => _fetchPage(pageIndex: 1, keyword: currentState?.keyword ?? ''),
    );
  }

  Future<void> search(String keyword) async {
    final trimmedKeyword = keyword.trim();
    final currentState = state.asData?.value;
    if (currentState == null) {
      state = const AsyncLoading();
    } else {
      state = AsyncData(
        currentState.copyWith(isRefreshing: true, keyword: trimmedKeyword),
      );
    }
    state = await AsyncValue.guard(
      () => _fetchPage(pageIndex: 1, keyword: trimmedKeyword),
    );
  }

  Future<void> clearSearch() => search('');
  Future<void> setSearchKeyword(String keyword) => search(keyword);

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
        keyword: currentState.keyword,
      );
      state = AsyncData(nextState);
    } catch (_) {
      // Keep existing data on error so the user can retry loading more.
      state = AsyncData(currentState.copyWith(isLoadingMore: false));
    }
  }

  Future<HomeFeedState> _fetchPage({
    required int pageIndex,
    List<PostDto> previousPosts = const [],
    String keyword = '',
  }) async {
    final service = ref.read(postServiceProvider);
    final response = keyword.isEmpty
        ? await service.getPosts(pageIndex: pageIndex, pageSize: _pageSize)
        : await service.searchPosts(
            pageIndex: pageIndex,
            pageSize: _pageSize,
            keyword: keyword,
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
      isRefreshing: false,
      keyword: keyword,
    );
  }
}

class HotFeedNotifier extends AsyncNotifier<HotFeedState> {
  static const int _count = 10;

  @override
  Future<HotFeedState> build() => _fetch();

  Future<void> refresh() async {
    final currentState = state.asData?.value;
    if (currentState != null) {
      state = AsyncData(currentState.copyWith(isRefreshing: true));
    } else {
      state = const AsyncLoading();
    }

    state = await AsyncValue.guard(_fetch);
  }

  Future<HotFeedState> _fetch() async {
    final service = ref.read(postServiceProvider);
    final response = await service.getHotPosts(count: _count);
    return HotFeedState(posts: response.data ?? const [], isRefreshing: false);
  }
}
