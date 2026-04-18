import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/post.dart';
import '../../providers/post_detail_provider.dart';

class PostDetailScreen extends ConsumerWidget {
  const PostDetailScreen({super.key, required this.postId});

  final String postId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final postAsync = ref.watch(postDetailProvider(postId));

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
          child: postAsync.when(
            data: (post) => _PostDetailView(post: post),
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => _PostDetailErrorView(
              message: '$error',
              onRetry: () => ref.invalidate(postDetailProvider(postId)),
            ),
          ),
        ),
      ),
    );
  }
}

class _PostDetailView extends StatelessWidget {
  const _PostDetailView({required this.post});

  final PostDto post;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return CustomScrollView(
      slivers: [
        SliverAppBar(
          pinned: true,
          backgroundColor: Colors.transparent,
          foregroundColor: const Color(0xFF2B2118),
          title: Text(
            post.title?.trim().isNotEmpty == true ? post.title!.trim() : '帖子详情',
          ),
        ),
        SliverPadding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
          sliver: SliverList.list(
            children: [
              if (post.images?.isNotEmpty ?? false) ...[
                ClipRRect(
                  borderRadius: BorderRadius.circular(28),
                  child: AspectRatio(
                    aspectRatio: 1.08,
                    child: Image.network(
                      post.images!.first,
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) => Container(
                        color: const Color(0xFFF3E4D8),
                        alignment: Alignment.center,
                        child: const Icon(
                          Icons.image_not_supported_outlined,
                          size: 36,
                          color: Color(0xFF7B6859),
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 20),
              ],
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.92),
                  borderRadius: BorderRadius.circular(28),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      post.title?.trim().isNotEmpty == true
                          ? post.title!.trim()
                          : '未命名帖子',
                      style: theme.textTheme.headlineSmall?.copyWith(
                        fontWeight: FontWeight.w800,
                        color: const Color(0xFF2B2118),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Wrap(
                      spacing: 10,
                      runSpacing: 10,
                      children: [
                        _DetailChip(
                          icon: Icons.person_outline_rounded,
                          label: post.author?.name ?? '匿名用户',
                        ),
                        _DetailChip(
                          icon: Icons.favorite_border_rounded,
                          label: '${post.likes ?? 0} 点赞',
                        ),
                        _DetailChip(
                          icon: Icons.thumb_down_alt_outlined,
                          label: '${post.dislikes ?? 0} 点踩',
                        ),
                        _DetailChip(
                          icon: Icons.remove_red_eye_outlined,
                          label: '${post.watch ?? 0} 浏览',
                        ),
                      ],
                    ),
                    const SizedBox(height: 18),
                    Text(
                      _formatCreatedAt(post.createdAt),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: const Color(0xFF9A8778),
                      ),
                    ),
                    const SizedBox(height: 20),
                    Text(
                      post.content?.trim().isNotEmpty == true
                          ? post.content!.trim()
                          : '这个帖子还没有填写描述。',
                      style: theme.textTheme.bodyLarge?.copyWith(
                        height: 1.7,
                        color: const Color(0xFF443329),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  String _formatCreatedAt(DateTime? dateTime) {
    if (dateTime == null) {
      return '刚刚发布';
    }

    return '${dateTime.year}-${dateTime.month.toString().padLeft(2, '0')}-${dateTime.day.toString().padLeft(2, '0')} ${dateTime.hour.toString().padLeft(2, '0')}:${dateTime.minute.toString().padLeft(2, '0')}';
  }
}

class _DetailChip extends StatelessWidget {
  const _DetailChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
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

class _PostDetailErrorView extends StatelessWidget {
  const _PostDetailErrorView({required this.message, required this.onRetry});

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
              Icons.article_outlined,
              size: 42,
              color: Color(0xFF7B6859),
            ),
            const SizedBox(height: 12),
            Text(
              '详情加载失败',
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
