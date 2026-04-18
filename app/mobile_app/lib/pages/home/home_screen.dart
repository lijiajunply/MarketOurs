import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../models/post.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  late final ScrollController _scrollController;

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController()..addListener(_handleScroll);
  }

  @override
  void dispose() {
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

    return Scaffold(
      body: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFFF7F2EB), Color(0xFFFDE8D9)],
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
          ),
        ),
        child: SafeArea(
          child: feedAsync.when(
            data: (state) => RefreshIndicator(
              onRefresh: ref.read(homeFeedProvider.notifier).refresh,
              child: CustomScrollView(
                controller: _scrollController,
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  const SliverToBoxAdapter(child: _HomeHeader()),
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                    sliver: SliverToBoxAdapter(
                      child: _WaterfallSection(
                        posts: state.posts,
                        isLoadingMore: state.isLoadingMore,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            loading: () => const _FeedLoadingView(),
            error: (error, _) => _FeedErrorView(
              message: '$error',
              onRetry: () => ref.read(homeFeedProvider.notifier).refresh(),
            ),
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
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '首页',
            style: theme.textTheme.headlineMedium?.copyWith(
              fontWeight: FontWeight.w800,
              color: const Color(0xFF2B2118),
            ),
          ),
          const SizedBox(height: 8),
          Text(
            '看看校园里刚刚出现了什么好物，双列瀑布流会把最新帖子都铺开给你。',
            style: theme.textTheme.bodyLarge?.copyWith(
              color: const Color(0xFF6F5B4D),
              height: 1.45,
            ),
          ),
          const SizedBox(height: 20),
          Container(
            padding: const EdgeInsets.all(18),
            decoration: BoxDecoration(
              color: const Color(0xFF2F241C),
              borderRadius: BorderRadius.circular(28),
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'MarketOurs',
                        style: theme.textTheme.titleLarge?.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        '我们的集市，不属于任何人。',
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: const Color(0xFFF0D7C4),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    color: const Color(0xFFDB6B2C),
                    borderRadius: BorderRadius.circular(18),
                  ),
                  child: const Icon(
                    Icons.storefront_rounded,
                    color: Colors.white,
                    size: 28,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _WaterfallSection extends StatelessWidget {
  const _WaterfallSection({required this.posts, required this.isLoadingMore});

  final List<PostDto> posts;
  final bool isLoadingMore;

  @override
  Widget build(BuildContext context) {
    if (posts.isEmpty) {
      return const _EmptyFeedView();
    }

    final columns = _splitPosts(posts);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            children: [
              for (final post in columns.left)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: _PostCard(post: post),
                ),
            ],
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            children: [
              for (final post in columns.right)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: _PostCard(post: post),
                ),
              if (isLoadingMore)
                const Padding(
                  padding: EdgeInsets.only(top: 8),
                  child: Center(child: CircularProgressIndicator()),
                ),
            ],
          ),
        ),
      ],
    );
  }

  _PostColumns _splitPosts(List<PostDto> posts) {
    final left = <PostDto>[];
    final right = <PostDto>[];
    var leftWeight = 0;
    var rightWeight = 0;

    for (final post in posts) {
      final weight = _estimateWeight(post);
      if (leftWeight <= rightWeight) {
        left.add(post);
        leftWeight += weight;
      } else {
        right.add(post);
        rightWeight += weight;
      }
    }

    return _PostColumns(left: left, right: right);
  }

  int _estimateWeight(PostDto post) {
    final titleWeight = ((post.title ?? '').length / 10).ceil();
    final contentWeight = ((post.content ?? '').length / 45).ceil();
    final imageWeight = (post.images?.isNotEmpty ?? false) ? 5 : 0;
    return 4 + titleWeight + contentWeight + imageWeight;
  }
}

class _PostColumns {
  const _PostColumns({required this.left, required this.right});

  final List<PostDto> left;
  final List<PostDto> right;
}

class _PostCard extends StatelessWidget {
  const _PostCard({required this.post});

  final PostDto post;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final imageUrl = post.images?.isNotEmpty == true
        ? post.images!.first
        : null;

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(buildPostDetailLocation(post.id)),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (imageUrl != null) ...[
                ClipRRect(
                  borderRadius: BorderRadius.circular(18),
                  child: AspectRatio(
                    aspectRatio: _imageAspectRatio(post.id),
                    child: Image.network(
                      imageUrl,
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) => Container(
                        color: const Color(0xFFF3E4D8),
                        alignment: Alignment.center,
                        child: const Icon(Icons.image_not_supported_outlined),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 12),
              ],
              Text(
                (post.title?.trim().isNotEmpty ?? false)
                    ? post.title!.trim()
                    : '未命名帖子',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                  color: const Color(0xFF2B2118),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                (post.content?.trim().isNotEmpty ?? false)
                    ? post.content!.trim()
                    : '这个帖子还没有填写描述。',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: const Color(0xFF6F5B4D),
                  height: 1.5,
                ),
                maxLines: imageUrl == null ? 7 : 5,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 14),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _StatChip(
                    icon: Icons.favorite_border_rounded,
                    label: '${post.likes ?? 0}',
                  ),
                  _StatChip(
                    icon: Icons.remove_red_eye_outlined,
                    label: '${post.watch ?? 0}',
                  ),
                  _StatChip(
                    icon: Icons.person_outline_rounded,
                    label: post.author?.name ?? '匿名',
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                _formatCreatedAt(post.createdAt),
                style: theme.textTheme.labelMedium?.copyWith(
                  color: const Color(0xFF9A8778),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  double _imageAspectRatio(String seed) {
    final value = seed.codeUnits.fold<int>(0, (sum, item) => sum + item);
    final variants = [1 / 1.2, 1 / 1.35, 1 / 1.55, 1 / 1.1];
    return variants[value % variants.length];
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

class _StatChip extends StatelessWidget {
  const _StatChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: const Color(0xFFF7F2EB),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: const Color(0xFF7B6859)),
          const SizedBox(width: 6),
          Text(
            label,
            style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: const Color(0xFF5B493B),
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

class _FeedLoadingView extends StatelessWidget {
  const _FeedLoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(child: CircularProgressIndicator());
  }
}

class _FeedErrorView extends StatelessWidget {
  const _FeedErrorView({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.cloud_off_rounded,
              size: 42,
              color: Color(0xFF7B6859),
            ),
            const SizedBox(height: 12),
            Text(
              '帖子加载失败',
              style: Theme.of(
                context,
              ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              message,
              textAlign: TextAlign.center,
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: const Color(0xFF6F5B4D)),
            ),
            const SizedBox(height: 16),
            FilledButton(onPressed: onRetry, child: const Text('重新加载')),
          ],
        ),
      ),
    );
  }
}

class _EmptyFeedView extends StatelessWidget {
  const _EmptyFeedView();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.8),
        borderRadius: BorderRadius.circular(28),
      ),
      child: Column(
        children: [
          const Icon(Icons.inbox_outlined, size: 40, color: Color(0xFF9A8778)),
          const SizedBox(height: 12),
          Text(
            '还没有帖子',
            style: Theme.of(
              context,
            ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          Text(
            '下拉刷新试试，或者等同学们先发出第一条内容。',
            textAlign: TextAlign.center,
            style: Theme.of(
              context,
            ).textTheme.bodyMedium?.copyWith(color: const Color(0xFF6F5B4D)),
          ),
        ],
      ),
    );
  }
}
