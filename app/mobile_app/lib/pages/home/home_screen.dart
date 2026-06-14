import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/post_card.dart';

import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/error_messages.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  late final ScrollController _scrollController;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController()..addListener(_handleScroll);
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController
      ..removeListener(_handleScroll)
      ..dispose();
    super.dispose();
  }

  void _handleScroll() {
    if (!_scrollController.hasClients) {
      return;
    }

    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - 480) {
      ref.read(homeFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final feedAsync = ref.watch(homeFeedProvider);
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isAuthenticated = authState?.status == AuthStatus.authenticated;

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: feedAsync.when(
        data: (state) => CustomScrollView(
          controller: _scrollController,
          physics: const BouncingScrollPhysics(
            parent: AlwaysScrollableScrollPhysics(),
          ),
          slivers: [
            CupertinoSliverNavigationBar(
              largeTitle: const Text('首页'),
              backgroundColor: CupertinoDynamicColor.resolve(
                AppColors.background,
                context,
              ).withValues(alpha: 0.94),
              border: null,
              trailing: CupertinoButton(
                padding: EdgeInsets.zero,
                onPressed: () {
                  if (isAuthenticated) {
                    context.push(AppRoutePaths.createPost);
                  } else {
                    context.go(AppRoutePaths.login);
                  }
                },
                child: const Icon(
                  CupertinoIcons.plus_circle_fill,
                  size: 28,
                  color: AppColors.primary,
                ),
              ),
            ),
            CupertinoSliverRefreshControl(
              onRefresh: ref.read(homeFeedProvider.notifier).refresh,
            ),
            SliverToBoxAdapter(
              child: AppResponsiveCenter(
                padding: AppResponsive.sliverPagePadding(
                  context,
                  top: 12,
                  bottom: 8,
                ),
                child: CupertinoSearchTextField(
                  key: const ValueKey('home-responsive-search-field'),
                  controller: _searchController,
                  placeholder: '搜索帖子、话题或用户',
                  borderRadius: BorderRadius.circular(AppRadii.md),
                  backgroundColor: AppColors.secondary,
                  onSubmitted: (value) =>
                      ref.read(homeFeedProvider.notifier).search(value),
                  onChanged: (value) {
                    if (value.trim().isEmpty && state.keyword.isNotEmpty) {
                      ref.read(homeFeedProvider.notifier).clearSearch();
                    }
                  },
                ),
              ),
            ),
            SliverToBoxAdapter(
              child: AppResponsiveCenter(
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
                child: _SearchLoadingIndicator(
                  isVisible: state.isRefreshing,
                  keyword: state.keyword,
                ),
              ),
            ),
            AppResponsiveSliverPadding(
              child: _PostListSection(
                posts: state.posts,
                isLoadingMore: state.isLoadingMore,
                isRefreshing: state.isRefreshing,
                keyword: state.keyword,
              ),
            ),
          ],
        ),
        loading: () => const _FeedLoadingView(),
        error: (error, _) => _FeedErrorView(
          message: extractErrorFromException(error),
          onRetry: () => ref.read(homeFeedProvider.notifier).refresh(),
        ),
      ),
    );
  }
}

class _PostListSection extends StatelessWidget {
  const _PostListSection({
    required this.posts,
    required this.isLoadingMore,
    required this.isRefreshing,
    required this.keyword,
  });

  final List<PostDto> posts;
  final bool isLoadingMore;
  final bool isRefreshing;
  final String keyword;

  @override
  Widget build(BuildContext context) {
    final columnCount = AppResponsive.listColumnCount(context);

    if (posts.isEmpty && isRefreshing) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 40),
        child: Center(child: CupertinoActivityIndicator(radius: 14)),
      );
    }

    if (posts.isEmpty) {
      return AppEmptyState(
        icon: keyword.isEmpty ? CupertinoIcons.news : CupertinoIcons.search,
        title: keyword.isEmpty ? '还没有帖子' : '没有找到相关帖子',
        description: keyword.isEmpty
            ? '等第一位同学来发布内容，或者稍后再刷新看看。'
            : '换个关键词试试，或者清空搜索回到首页。',
      );
    }

    if (columnCount == 1) {
      return Column(
        key: const ValueKey('home-feed-columns-1'),
        children: [
          for (final post in posts)
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: PostCard(post: post),
            ),
          if (isLoadingMore)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: Center(child: CupertinoActivityIndicator()),
            ),
        ],
      );
    }

    return Column(
      key: const ValueKey('home-feed-columns-2'),
      children: [
        LayoutBuilder(
          builder: (context, constraints) {
            const spacing = 16.0;
            final itemWidth = (constraints.maxWidth - spacing) / 2;
            return Wrap(
              spacing: spacing,
              runSpacing: spacing,
              children: [
                for (final post in posts)
                  SizedBox(
                    width: itemWidth,
                    child: PostCard(post: post),
                  ),
              ],
            );
          },
        ),
        if (isLoadingMore)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 24),
            child: Center(child: CupertinoActivityIndicator()),
          ),
      ],
    );
  }
}

class _SearchLoadingIndicator extends StatelessWidget {
  const _SearchLoadingIndicator({
    required this.isVisible,
    required this.keyword,
  });

  final bool isVisible;
  final String keyword;

  @override
  Widget build(BuildContext context) {
    if (!isVisible) {
      return const SizedBox.shrink();
    }

    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        const CupertinoActivityIndicator(radius: 8),
        const SizedBox(width: 8),
        Text(
          keyword.isEmpty ? '正在刷新帖子...' : '正在搜索...',
          style: AppTextStyles.label(context),
        ),
      ],
    );
  }
}

class _FeedLoadingView extends StatelessWidget {
  const _FeedLoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(child: CupertinoActivityIndicator(radius: 14));
  }
}

class _FeedErrorView extends StatelessWidget {
  const _FeedErrorView({required this.message, required this.onRetry});

  final String message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: AppEmptyState(
          icon: CupertinoIcons.exclamationmark_triangle,
          title: '加载失败',
          description: message,
          action: AppPrimaryButton(
            onPressed: onRetry,
            child: const Text('重新加载'),
          ),
        ),
      ),
    );
  }
}
