import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/user_card.dart';

import '../../models/post.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_widgets.dart';

class HotScreen extends ConsumerWidget {
  const HotScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final hotFeedAsync = ref.watch(hotFeedProvider);

    return CupertinoPageScaffold(
      backgroundColor: CupertinoColors.systemGroupedBackground,
      child: SafeArea(
        child: hotFeedAsync.when(
          data: (state) => CustomScrollView(
            physics: const BouncingScrollPhysics(
              parent: AlwaysScrollableScrollPhysics(),
            ),
            slivers: [
              CupertinoSliverRefreshControl(
                onRefresh: ref.read(hotFeedProvider.notifier).refresh,
              ),
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
      child: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _HotBadge(),
          SizedBox(height: 18),
          Text(
            '大家都在围观什么',
            style: TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.w800,
              color: Color(0xFF111827),
            ),
          ),
          SizedBox(height: 10),
          Text(
            '按实时热度整理出的热门帖子榜单，快速看看最近最受关注的话题。',
            style: TextStyle(
              color: Color(0xFF6B7280),
              height: 1.5,
              fontSize: 15,
            ),
          ),
        ],
      ),
    );
  }
}

class _HotBadge extends StatelessWidget {
  const _HotBadge();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: CupertinoColors.white,
        borderRadius: BorderRadius.circular(999),
      ),
      child: const Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            CupertinoIcons.flame_fill,
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
    );
  }
}

class _HotPostCard extends StatelessWidget {
  const _HotPostCard({required this.post, required this.rank});

  final PostDto post;
  final int rank;

  @override
  Widget build(BuildContext context) {
    final imageUrl = post.images?.isNotEmpty == true ? post.images!.first : null;
    final isTopThree = rank <= 3;

    return AppTappableCard(
      radius: 24,
      padding: EdgeInsets.zero,
      border: Border.all(
        color: isTopThree ? const Color(0xFFFFD8BF) : const Color(0xFFE8E8ED),
      ),
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (imageUrl != null)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
              child: AspectRatio(
                aspectRatio: 1.8,
                child: Image.network(
                  imageUrl,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Container(
                    color: const Color(0xFFFFF1E6),
                    alignment: Alignment.center,
                    child: const Icon(
                      CupertinoIcons.photo,
                      color: Color(0xFFFFB26B),
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
                            style: const TextStyle(
                              fontSize: 12,
                              color: Color(0xFFFF7A00),
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            (post.title?.trim().isNotEmpty ?? false)
                                ? post.title!.trim()
                                : '未命名帖子',
                            style: const TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w800,
                              height: 1.25,
                              color: Color(0xFF111827),
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
                  style: const TextStyle(
                    color: Color(0xFF6B7280),
                    height: 1.5,
                    fontSize: 15,
                  ),
                ),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: [
                    _StatChip(
                      icon: CupertinoIcons.flame,
                      label: '热榜',
                      backgroundColor: const Color(0xFFFFF1E6),
                      iconColor: const Color(0xFFFF7A00),
                    ),
                    _StatChip(
                      icon: CupertinoIcons.heart,
                      label: '${post.likes ?? 0}',
                    ),
                    _StatChip(
                      icon: CupertinoIcons.eye,
                      label: '${post.watch ?? 0}',
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Text(
                  _formatCreatedAt(post.createdAt),
                  style: const TextStyle(
                    fontSize: 12,
                    color: Color(0xFF8E8E93),
                  ),
                ),
              ],
            ),
          ),
        ],
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
          color: highlight ? CupertinoColors.white : const Color(0xFF111827),
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
          Icon(icon, size: 15, color: iconColor ?? CupertinoColors.systemGrey),
          const SizedBox(width: 6),
          Text(
            label,
            style: const TextStyle(
              fontSize: 12,
              color: Color(0xFF374151),
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
    return const Center(child: CupertinoActivityIndicator());
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
            const Icon(
              CupertinoIcons.flame_fill,
              size: 44,
              color: Color(0xFFFF7A00),
            ),
            const SizedBox(height: 12),
            const Text(
              '热榜加载失败',
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w700,
                color: Color(0xFF111827),
              ),
            ),
            const SizedBox(height: 8),
            Text(
              message,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Color(0xFF6B7280)),
            ),
            const SizedBox(height: 16),
            AppPrimaryButton(onPressed: onRetry, child: const Text('重新加载')),
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
    return AppSectionCard(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
      child: const Column(
        children: [
          Icon(
            CupertinoIcons.rocket_fill,
            size: 40,
            color: Color(0xFFFFB26B),
          ),
          SizedBox(height: 12),
          Text(
            '热榜还在等内容升温',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w700,
              color: Color(0xFF111827),
            ),
          ),
          SizedBox(height: 8),
          Text(
            '等同学们发出更多帖子后，这里会出现最受关注的话题。',
            textAlign: TextAlign.center,
            style: TextStyle(color: Color(0xFF6B7280)),
          ),
        ],
      ),
    );
  }
}
