import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/user_card.dart';

import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
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
              backgroundColor: AppColors.background.withValues(alpha: 0.94),
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
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
                child: CupertinoSearchTextField(
                  controller: _searchController,
                  placeholder: '搜索帖子、话题或用户',
                  borderRadius: BorderRadius.circular(AppRadii.md),
                  backgroundColor: AppColors.secondary,
                ),
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
              sliver: _PostListSection(
                posts: state.posts,
                isLoadingMore: state.isLoadingMore,
              ),
            ),
          ],
        ),
        loading: () => const _FeedLoadingView(),
        error: (error, _) => _FeedErrorView(
          message: '$error',
          onRetry: () => ref.read(homeFeedProvider.notifier).refresh(),
        ),
      ),
    );
  }
}

class _PostListSection extends StatelessWidget {
  const _PostListSection({required this.posts, required this.isLoadingMore});

  final List<PostDto> posts;
  final bool isLoadingMore;

  @override
  Widget build(BuildContext context) {
    if (posts.isEmpty) {
      return const SliverToBoxAdapter(
        child: AppEmptyState(
          icon: CupertinoIcons.news,
          title: '还没有帖子',
          description: '等第一位同学来发布内容，或者稍后再刷新看看。',
        ),
      );
    }

    return SliverList.builder(
      itemCount: posts.length + (isLoadingMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index == posts.length) {
          return const Padding(
            padding: EdgeInsets.symmetric(vertical: 24),
            child: Center(child: CupertinoActivityIndicator()),
          );
        }
        return Padding(
          padding: const EdgeInsets.only(bottom: 16),
          child: _PostCard(post: posts[index]),
        );
      },
    );
  }
}

class _PostCard extends StatelessWidget {
  const _PostCard({required this.post});

  final PostDto post;

  @override
  Widget build(BuildContext context) {
    final title = post.title?.trim().isNotEmpty == true
        ? post.title!.trim()
        : '未命名帖子';
    final content = post.content?.trim().isNotEmpty == true
        ? post.content!.trim()
        : '这个帖子还没有填写内容。';
    final excerpt = content.length > 120
        ? '${content.substring(0, 120)}...'
        : content;

    return AppTappableCard(
      padding: EdgeInsets.zero,
      radius: AppRadii.lg,
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (post.images?.isNotEmpty == true)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(AppRadii.lg),
              ),
              child: AspectRatio(
                aspectRatio: 1.8,
                child: Image.network(
                  post.images!.first,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Container(
                    color: AppColors.muted,
                    alignment: Alignment.center,
                    child: const Icon(
                      CupertinoIcons.photo,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                ),
              ),
            ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (post.author != null)
                  UserCard(
                    user: post.author!,
                    meta: _formatCreatedAt(post.createdAt),
                    showMeta: true,
                    onTap: post.author?.id == null
                        ? null
                        : () => context.push(
                            buildPublicProfileLocation(post.author!.id!),
                          ),
                  ),
                if (post.author != null) const SizedBox(height: 12),
                Text(
                  title,
                  style: AppTextStyles.sectionTitle,
                ),
                const SizedBox(height: 8),
                Text(excerpt, style: AppTextStyles.muted),
                const SizedBox(height: 16),
                Row(
                  children: [
                    _StatItem(
                      icon: CupertinoIcons.heart,
                      label: '${post.likes ?? 0}',
                      active: false,
                    ),
                    const SizedBox(width: 16),
                    _StatItem(
                      icon: CupertinoIcons.bubble_left,
                      label: '${post.commentsCount ?? 0}',
                    ),
                    const Spacer(),
                    const Icon(
                      CupertinoIcons.chevron_right,
                      color: AppColors.mutedForeground,
                      size: 14,
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _formatCreatedAt(DateTime? dateTime) {
    if (dateTime == null) {
      return '刚刚发布';
    }

    final now = DateTime.now();
    final difference = now.difference(dateTime);

    if (difference.inMinutes < 1) {
      return '刚刚发布';
    }
    if (difference.inHours < 1) {
      return '${difference.inMinutes} 分钟前';
    }
    if (difference.inDays < 1) {
      return '${difference.inHours} 小时前';
    }
    if (difference.inDays < 7) {
      return '${difference.inDays} 天前';
    }
    return '${dateTime.year}-${dateTime.month.toString().padLeft(2, '0')}-${dateTime.day.toString().padLeft(2, '0')}';
  }
}

class _StatItem extends StatelessWidget {
  const _StatItem({
    required this.icon,
    required this.label,
    this.active = false,
  });

  final IconData icon;
  final String label;
  final bool active;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          icon,
          size: 16,
          color: active ? AppColors.destructive : AppColors.mutedForeground,
        ),
        const SizedBox(width: 4),
        Text(
          label,
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w500,
            color: AppColors.mutedForeground,
          ),
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
