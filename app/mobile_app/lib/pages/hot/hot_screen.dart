import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/user_card.dart';

import '../../models/post.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';

class HotScreen extends ConsumerWidget {
  const HotScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final hotFeedAsync = ref.watch(hotFeedProvider);

    return Scaffold(
      body: SafeArea(
        child: hotFeedAsync.when(
          data: (state) => RefreshIndicator(
            onRefresh: ref.read(hotFeedProvider.notifier).refresh,
            child: CustomScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                const SliverToBoxAdapter(child: _HotHeader()),
                if (state.posts.isEmpty)
                  const SliverPadding(
                    padding: EdgeInsets.fromLTRB(16, 0, 16, 24),
                    sliver: SliverToBoxAdapter(child: _HotEmptyView()),
                  )
                else
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                    sliver: SliverList.builder(
                      itemCount: state.posts.length,
                      itemBuilder: (context, index) => Padding(
                        padding: EdgeInsets.only(
                          bottom: index == state.posts.length - 1 ? 0 : 16,
                        ),
                        child: _HotPostCard(
                          post: state.posts[index],
                          rank: index + 1,
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          loading: () => const _HotLoadingView(),
          error: (error, _) => _HotErrorView(
            message: '$error',
            onRetry: () => ref.read(hotFeedProvider.notifier).refresh(),
          ),
        ),
      ),
    );
  }
}

class _HotHeader extends StatelessWidget {
  const _HotHeader();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 16, 16, 20),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFFFFF1E6), Color(0xFFFFFFFF)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(28),
        border: Border.all(color: const Color(0xFFFFD8BF)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(999),
            ),
            child: const Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  Icons.local_fire_department_rounded,
                  size: 18,
                  color: Color(0xFFFF7A00),
                ),
                SizedBox(width: 8),
                Text(
                  '校园热榜',
                  style: TextStyle(
                    color: Color(0xFFFF7A00),
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 18),
          Text(
            '大家都在围观什么',
            style: theme.textTheme.headlineMedium?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            '按实时热度整理出的热门帖子榜单，快速看看最近最受关注的话题。',
            style: theme.textTheme.bodyMedium?.copyWith(
              color: Colors.grey.shade700,
              height: 1.5,
            ),
          ),
        ],
      ),
    );
  }
}

class _HotPostCard extends StatelessWidget {
  const _HotPostCard({required this.post, required this.rank});

  final PostDto post;
  final int rank;

  @override
  Widget build(BuildContext context) {
    final imageUrl = post.images?.isNotEmpty == true
        ? post.images!.first
        : null;
    final isTopThree = rank <= 3;

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(
          color: isTopThree ? const Color(0xFFFFD8BF) : const Color(0xFFE8E8ED),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 18,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(24),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: () => context.push(buildPostDetailLocation(post.id)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (imageUrl != null)
                  AspectRatio(
                    aspectRatio: 1.8,
                    child: Image.network(
                      imageUrl,
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) => Container(
                        color: const Color(0xFFFFF1E6),
                        alignment: Alignment.center,
                        child: Icon(
                          Icons.image_not_supported_outlined,
                          color: Colors.orange.shade300,
                        ),
                      ),
                    ),
                  ),
                Padding(
                  padding: const EdgeInsets.all(18),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _RankBadge(rank: rank, highlight: isTopThree),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  '第 $rank 名',
                                  style: Theme.of(context).textTheme.labelLarge
                                      ?.copyWith(
                                        color: const Color(0xFFFF7A00),
                                        fontWeight: FontWeight.w700,
                                      ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  (post.title?.trim().isNotEmpty ?? false)
                                      ? post.title!.trim()
                                      : '未命名帖子',
                                  style: Theme.of(context).textTheme.titleLarge
                                      ?.copyWith(
                                        fontWeight: FontWeight.w800,
                                        height: 1.25,
                                      ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      if (post.author != null) ...[
                        UserCard(
                          user: post.author!,
                          onTap: post.author?.id == null
                              ? null
                              : () => context.push(
                                  buildPublicProfileLocation(post.author!.id!),
                                ),
                        ),
                        const SizedBox(height: 12),
                      ],
                      Text(
                        _excerpt(post.content),
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Colors.grey.shade700,
                          height: 1.5,
                        ),
                      ),
                      const SizedBox(height: 16),
                      Wrap(
                        spacing: 10,
                        runSpacing: 10,
                        children: [
                          _StatChip(
                            icon: Icons.local_fire_department_outlined,
                            label: '热榜',
                            backgroundColor: const Color(0xFFFFF1E6),
                            iconColor: const Color(0xFFFF7A00),
                          ),
                          _StatChip(
                            icon: Icons.favorite_border_rounded,
                            label: '${post.likes ?? 0}',
                          ),
                          _StatChip(
                            icon: Icons.remove_red_eye_outlined,
                            label: '${post.watch ?? 0}',
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      Text(
                        _formatCreatedAt(post.createdAt),
                        style: Theme.of(context).textTheme.labelMedium
                            ?.copyWith(color: Colors.grey.shade500),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  String _excerpt(String? content) {
    final text = (content ?? '').trim();
    if (text.isEmpty) {
      return '这个帖子还没有内容摘要，点进去看看完整内容。';
    }
    if (text.length <= 96) {
      return text;
    }
    return '${text.substring(0, 96)}...';
  }

  String _formatCreatedAt(DateTime? dateTime) {
    if (dateTime == null) {
      return '刚刚更新';
    }

    final now = DateTime.now();
    final difference = now.difference(dateTime);

    if (difference.inMinutes < 1) {
      return '刚刚更新';
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

class _RankBadge extends StatelessWidget {
  const _RankBadge({required this.rank, required this.highlight});

  final int rank;
  final bool highlight;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 52,
      height: 52,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: highlight ? const Color(0xFFFF7A00) : const Color(0xFFF2F2F7),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Text(
        rank.toString().padLeft(2, '0'),
        style: TextStyle(
          color: highlight ? Colors.white : Colors.black87,
          fontWeight: FontWeight.w800,
          fontSize: 16,
          letterSpacing: 1.4,
        ),
      ),
    );
  }
}

class _StatChip extends StatelessWidget {
  const _StatChip({
    required this.icon,
    required this.label,
    this.backgroundColor = const Color(0xFFF2F2F7),
    this.iconColor,
  });

  final IconData icon;
  final String label;
  final Color backgroundColor;
  final Color? iconColor;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 15, color: iconColor ?? Colors.grey.shade700),
          const SizedBox(width: 6),
          Text(
            label,
            style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: Colors.grey.shade800,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _HotLoadingView extends StatelessWidget {
  const _HotLoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(child: CircularProgressIndicator());
  }
}

class _HotErrorView extends StatelessWidget {
  const _HotErrorView({required this.message, required this.onRetry});

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
            Icon(
              Icons.local_fire_department_outlined,
              size: 44,
              color: Colors.orange.shade300,
            ),
            const SizedBox(height: 12),
            Text(
              '热榜加载失败',
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
              ).textTheme.bodyMedium?.copyWith(color: Colors.grey.shade600),
            ),
            const SizedBox(height: 16),
            FilledButton(onPressed: onRetry, child: const Text('重新加载')),
          ],
        ),
      ),
    );
  }
}

class _HotEmptyView extends StatelessWidget {
  const _HotEmptyView();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: Colors.grey.shade200),
      ),
      child: Column(
        children: [
          Icon(
            Icons.rocket_launch_outlined,
            size: 40,
            color: Colors.orange.shade200,
          ),
          const SizedBox(height: 12),
          Text(
            '热榜还在等内容升温',
            style: Theme.of(
              context,
            ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          Text(
            '等同学们发出更多帖子后，这里会出现最受关注的话题。',
            textAlign: TextAlign.center,
            style: Theme.of(
              context,
            ).textTheme.bodyMedium?.copyWith(color: Colors.grey.shade600),
          ),
        ],
      ),
    );
  }
}
