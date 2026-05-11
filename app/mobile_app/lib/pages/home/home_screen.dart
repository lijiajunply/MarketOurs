import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/user_card.dart';

import '../../models/post.dart';
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

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: SafeArea(
        child: feedAsync.when(
          data: (state) => CustomScrollView(
            controller: _scrollController,
            physics: const BouncingScrollPhysics(
              parent: AlwaysScrollableScrollPhysics(),
            ),
            slivers: [
              CupertinoSliverRefreshControl(
                onRefresh: ref.read(homeFeedProvider.notifier).refresh,
              ),
              const SliverToBoxAdapter(child: _HomeHeader()),
              const SliverToBoxAdapter(child: SizedBox(height: 18)),
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
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
      ),
    );
  }
}

class _HomeHeader extends StatelessWidget {
  const _HomeHeader();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AppGlassCard(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: const [
                AppBadge(
                  backgroundColor: Color(0x190071E3),
                  foregroundColor: AppColors.primary,
                  child: Text('Campus Feed'),
                ),
                SizedBox(height: 18),
                Text('首页', style: AppTextStyles.hero),
                SizedBox(height: 10),
                Text('看看校园里刚刚发生了什么，快速浏览最新帖子与热门讨论。', style: AppTextStyles.muted),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
            decoration: AppDecorations.card(radius: AppRadii.lg),
            child: const Row(
              children: [
                Icon(
                  CupertinoIcons.search,
                  color: AppColors.mutedForeground,
                  size: 18,
                ),
                SizedBox(width: 10),
                Expanded(child: Text('搜索帖子标题或内容', style: AppTextStyles.muted)),
              ],
            ),
          ),
        ],
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
            padding: EdgeInsets.symmetric(vertical: 16),
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
      radius: AppRadii.xl,
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (post.images?.isNotEmpty == true)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(AppRadii.xl),
              ),
              child: AspectRatio(
                aspectRatio: 1.8,
                child: Image.network(
                  post.images!.first,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Container(
                    color: AppColors.secondary,
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
            padding: const EdgeInsets.all(18),
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
                if (post.author != null) const SizedBox(height: 14),
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 24,
                    height: 1.15,
                    fontWeight: FontWeight.w800,
                    color: AppColors.foreground,
                  ),
                ),
                const SizedBox(height: 10),
                Text(excerpt, style: AppTextStyles.muted),
                const SizedBox(height: 18),
                Row(
                  children: [
                    AppStatChip(
                      icon: CupertinoIcons.heart,
                      label: '${post.likes ?? 0}',
                      iconColor: const Color(0xFFFF5A5F),
                    ),
                    const SizedBox(width: 10),
                    AppStatChip(
                      icon: CupertinoIcons.eye,
                      label: '${post.watch ?? 0}',
                      iconColor: const Color(0xFF5AC8FA),
                    ),
                    const Spacer(),
                    const Icon(
                      CupertinoIcons.chevron_right,
                      color: AppColors.mutedForeground,
                      size: 18,
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
