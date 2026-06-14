import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/post_card.dart';

import '../../models/post.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/error_messages.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class TagScreen extends ConsumerStatefulWidget {
  const TagScreen({super.key, required this.tagId});

  final String tagId;

  @override
  ConsumerState<TagScreen> createState() => _TagScreenState();
}

class _TagScreenState extends ConsumerState<TagScreen> {
  late final ScrollController _scrollController;
  final _searchController = TextEditingController();
  PostTagDto? _tag;
  bool _isLoadingTag = true;
  String? _tagError;

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController()..addListener(_handleScroll);
    _loadTag();
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
      ref.read(tagFeedProvider(widget.tagId).notifier).loadMore();
    }
  }

  Future<void> _loadTag() async {
    setState(() {
      _isLoadingTag = true;
      _tagError = null;
    });

    try {
      final response = await ref.read(postServiceProvider).getPostTag(widget.tagId);
      if (!mounted) return;
      setState(() {
        _tag = response.data;
        _isLoadingTag = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _tagError = extractErrorFromException(error);
        _isLoadingTag = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoadingTag) {
      return const CupertinoPageScaffold(
        backgroundColor: AppColors.background,
        child: Center(child: CupertinoActivityIndicator(radius: 14)),
      );
    }

    if (_tag == null) {
      return CupertinoPageScaffold(
        backgroundColor: AppColors.background,
        navigationBar: CupertinoNavigationBar(
          middle: const Text('标签'),
        ),
        child: SafeArea(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: AppEmptyState(
                icon: CupertinoIcons.tag,
                title: '标签暂不可用',
                description: _tagError ?? '没有找到这个标签。',
                action: AppPrimaryButton(
                  onPressed: () => context.go(AppRoutePaths.home),
                  child: const Text('返回首页'),
                ),
              ),
            ),
          ),
        ),
      );
    }

    final feedAsync = ref.watch(tagFeedProvider(widget.tagId));

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
              largeTitle: Text(_tag!.name?.trim().isNotEmpty == true ? _tag!.name!.trim() : '标签'),
              backgroundColor: CupertinoDynamicColor.resolve(
                AppColors.background,
                context,
              ).withValues(alpha: 0.94),
              border: null,
            ),
            CupertinoSliverRefreshControl(
              onRefresh: ref.read(tagFeedProvider(widget.tagId).notifier).refresh,
            ),
            SliverToBoxAdapter(
              child: AppResponsiveCenter(
                padding: AppResponsive.sliverPagePadding(
                  context,
                  top: 12,
                  bottom: 8,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    CupertinoSearchTextField(
                      controller: _searchController,
                      placeholder: '在该标签下搜索帖子',
                      borderRadius: BorderRadius.circular(AppRadii.md),
                      backgroundColor: AppColors.secondary,
                      onSubmitted: (value) =>
                          ref.read(tagFeedProvider(widget.tagId).notifier).search(value),
                      onChanged: (value) {
                        if (value.trim().isEmpty && state.keyword.isNotEmpty) {
                          ref.read(tagFeedProvider(widget.tagId).notifier).clearSearch();
                        }
                      },
                    ),
                  ],
                ),
              ),
            ),
            SliverToBoxAdapter(
              child: AppResponsiveCenter(
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
                child: _TagSearchLoadingIndicator(
                  isVisible: state.isRefreshing,
                  keyword: state.keyword,
                ),
              ),
            ),
            AppResponsiveSliverPadding(
              child: _TagPostListSection(
                posts: state.posts,
                isLoadingMore: state.isLoadingMore,
                isRefreshing: state.isRefreshing,
                keyword: state.keyword,
              ),
            ),
          ],
        ),
        loading: () => const Center(child: CupertinoActivityIndicator(radius: 14)),
        error: (error, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: AppEmptyState(
              icon: CupertinoIcons.exclamationmark_triangle,
              title: '加载失败',
              description: extractErrorFromException(error),
              action: AppPrimaryButton(
                onPressed: () => ref.read(tagFeedProvider(widget.tagId).notifier).refresh(),
                child: const Text('重新加载'),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _TagPostListSection extends StatelessWidget {
  const _TagPostListSection({
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
        icon: keyword.isEmpty ? CupertinoIcons.tag : CupertinoIcons.search,
        title: keyword.isEmpty ? '这个标签下还没有帖子' : '没有找到相关帖子',
        description: keyword.isEmpty ? '等同学们发布更多内容后，这里会出现帖子。' : '换个关键词试试。',
      );
    }

    if (columnCount == 1) {
      return Column(
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

class _TagSearchLoadingIndicator extends StatelessWidget {
  const _TagSearchLoadingIndicator({
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
