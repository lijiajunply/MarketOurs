import 'package:flutter/cupertino.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/post_tag_pill.dart';
import 'package:mobile_app/models/post.dart';
import 'package:mobile_app/router/app_router.dart';
import 'package:mobile_app/services/share_service.dart';
import 'package:mobile_app/ui/app_feedback.dart';
import 'package:mobile_app/ui/app_theme.dart';
import 'package:mobile_app/ui/app_widgets.dart';

class PostCard extends StatelessWidget {
  const PostCard({super.key, required this.post});

  final PostDto post;
  static const _shareService = ShareService();

  Future<void> _handleShare(BuildContext context) async {
    try {
      await _shareService.sharePost(post);
    } catch (_) {
      if (context.mounted) {
        await AppFeedback.showError(context, message: '分享失败，请稍后重试');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final title = post.title?.trim().isNotEmpty == true
        ? post.title!.trim()
        : '未命名帖子';
    final content = post.content?.trim().isNotEmpty == true
        ? post.content!.trim()
        : '这个帖子还没有填写内容。';
    final excerpt = content.length > 100
        ? '${content.substring(0, 100)}...'
        : content;

    return AppTappableCard(
      padding: EdgeInsets.zero,
      radius: AppRadii.xl,
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 12),
            child: Row(
              children: [
                AppAvatar(
                  url: post.author?.avatar,
                  name: post.author?.name,
                  size: 32,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        post.author?.name ?? '匿名用户',
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      Text(
                        _formatCreatedAt(post.createdAt),
                        style: AppTextStyles.label(context),
                      ),
                    ],
                  ),
                ),
                const Icon(
                  CupertinoIcons.ellipsis,
                  size: 18,
                  color: AppColors.mutedForeground,
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              title,
              style: AppTextStyles.sectionTitle(
                context,
              ).copyWith(fontSize: 18, height: 1.3),
            ),
          ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Text(
                    excerpt,
                    style: AppTextStyles.body(context).copyWith(
                      fontSize: 15,
                      color: CupertinoDynamicColor.resolve(
                        AppColors.foreground,
                        context,
                      ).withValues(alpha: 0.8),
                    ),
                    maxLines: 3,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                if (post.images?.isNotEmpty == true) ...[
                  const SizedBox(width: 12),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(AppRadii.md),
                    child: SizedBox(
                      width: 88,
                      height: 88,
                      child: Image.network(
                        post.images!.first,
                        fit: BoxFit.cover,
                        gaplessPlayback: true,
                        errorBuilder: (context, error, stackTrace) => Container(
                          color: AppColors.muted,
                          child: const Icon(
                            CupertinoIcons.photo,
                            color: AppColors.mutedForeground,
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              border: Border(
                top: BorderSide(
                  color: CupertinoDynamicColor.resolve(
                    AppColors.border,
                    context,
                  ).withValues(alpha: 0.3),
                ),
              ),
            ),
            child: Row(
              children: [
                _StatItem(
                  icon: CupertinoIcons.heart,
                  label: '${post.likes ?? 0}',
                  active: false,
                ),
                const SizedBox(width: 24),
                _StatItem(
                  icon: CupertinoIcons.eye,
                  label: '${post.watch ?? 0}',
                ),
                const Spacer(),
                CupertinoButton(
                  padding: EdgeInsets.zero,
                  minimumSize: Size.zero,
                  onPressed: () => _handleShare(context),
                  child: const Icon(
                    CupertinoIcons.share,
                    size: 18,
                    color: AppColors.mutedForeground,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _formatCreatedAt(DateTime? dateTime) {
    if (dateTime == null) return '刚刚';
    final now = DateTime.now();
    final diff = now.difference(dateTime);
    if (diff.inMinutes < 1) return '刚刚';
    if (diff.inHours < 1) return '${diff.inMinutes}分钟前';
    if (diff.inDays < 1) return '${diff.inHours}小时前';
    if (diff.inDays < 7) return '${diff.inDays}天前';
    return '${dateTime.year}-${dateTime.month}-${dateTime.day}';
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
    final resolvedColor = CupertinoDynamicColor.resolve(
      active ? AppColors.destructive : AppColors.mutedForeground,
      context,
    );

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 16, color: resolvedColor),
        const SizedBox(width: 4),
        Text(
          label,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w500,
            color: resolvedColor,
          ),
        ),
      ],
    );
  }
}

class SimplePostCard extends StatelessWidget {
  const SimplePostCard({super.key, required this.post});

  final PostDto post;
  static const _shareService = ShareService();

  @override
  Widget build(BuildContext context) {
    return AppTappableCard(
      padding: EdgeInsets.zero,
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (post.tag != null) ...[
                  PostTagPill(tag: post.tag),
                  const SizedBox(height: 8),
                ],
                Text(
                  post.title?.trim().isNotEmpty == true
                      ? post.title!.trim()
                      : '未命名帖子',
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppColors.foreground,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  post.content?.trim().isNotEmpty == true
                      ? post.content!.trim()
                      : '这个帖子还没有内容描述。',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    height: 1.5,
                    color: AppColors.mutedForeground,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              border: Border(
                top: BorderSide(
                  color: CupertinoDynamicColor.resolve(
                    AppColors.border,
                    context,
                  ).withValues(alpha: 0.3),
                ),
              ),
            ),
            child: Row(
              children: [
                _StatItem(
                  icon: CupertinoIcons.heart,
                  label: '${post.likes ?? 0}',
                  active: false,
                ),
                const SizedBox(width: 24),
                _StatItem(
                  icon: CupertinoIcons.eye,
                  label: '${post.watch ?? 0}',
                ),
                const Spacer(),
                CupertinoButton(
                  padding: EdgeInsets.zero,
                  minimumSize: Size.zero,
                  onPressed: () => _handleShare(context),
                  child: const Icon(
                    CupertinoIcons.share,
                    size: 18,
                    color: AppColors.mutedForeground,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _handleShare(BuildContext context) async {
    try {
      await _shareService.sharePost(post);
    } catch (_) {
      if (context.mounted) {
        await AppFeedback.showError(context, message: '分享失败，请稍后重试');
      }
    }
  }
}
