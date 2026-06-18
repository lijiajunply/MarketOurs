import 'package:flutter/cupertino.dart';

import '../../../ui/app_theme.dart';

class PostDetailActionBar extends StatelessWidget {
  const PostDetailActionBar({
    super.key,
    required this.likes,
    required this.dislikes,
    required this.watch,
    required this.isWorking,
    required this.isLiked,
    required this.isDisliked,
    required this.onShare,
    required this.onLike,
    required this.onDislike,
  });

  final int likes;
  final int dislikes;
  final int watch;
  final bool isWorking;
  final bool isLiked;
  final bool isDisliked;
  final VoidCallback onShare;
  final VoidCallback onLike;
  final VoidCallback onDislike;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16),
      decoration: BoxDecoration(
        border: Border(
          top: BorderSide(
            color: CupertinoDynamicColor.resolve(
              AppColors.border,
              context,
            ).withValues(alpha: 0.3),
          ),
          bottom: BorderSide(
            color: CupertinoDynamicColor.resolve(
              AppColors.border,
              context,
            ).withValues(alpha: 0.3),
          ),
        ),
      ),
      child: Row(
        children: [
          _ActionChip(
            icon: isLiked ? CupertinoIcons.heart_fill : CupertinoIcons.heart,
            label: '$likes',
            onTap: isWorking ? null : onLike,
            color: const Color(0xFFFF5A5F),
            active: isLiked,
          ),
          const SizedBox(width: 12),
          _ActionChip(
            icon: isDisliked
                ? CupertinoIcons.hand_thumbsdown_fill
                : CupertinoIcons.hand_thumbsdown,
            label: '$dislikes',
            onTap: isWorking ? null : onDislike,
            color: CupertinoDynamicColor.resolve(
              AppColors.mutedForeground,
              context,
            ),
            active: isDisliked,
          ),
          const Spacer(),
          Text(
            '$watch 次浏览',
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w500,
              color: CupertinoDynamicColor.resolve(
                AppColors.mutedForeground,
                context,
              ),
            ),
          ),
          const SizedBox(width: 12),
          CupertinoButton(
            padding: EdgeInsets.zero,
            minimumSize: Size.zero,
            onPressed: onShare,
            child: const Icon(
              CupertinoIcons.share,
              size: 18,
              color: AppColors.mutedForeground,
            ),
          ),
        ],
      ),
    );
  }
}

class _ActionChip extends StatelessWidget {
  const _ActionChip({
    required this.icon,
    required this.label,
    this.onTap,
    required this.color,
    this.active = false,
  });

  final IconData icon;
  final String label;
  final VoidCallback? onTap;
  final Color color;
  final bool active;

  @override
  Widget build(BuildContext context) {
    return CupertinoButton(
      padding: EdgeInsets.zero,
      onPressed: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: active
              ? color.withValues(alpha: 0.1)
              : CupertinoDynamicColor.resolve(AppColors.secondary, context),
          borderRadius: BorderRadius.circular(AppRadii.pill),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 18,
              color: active
                  ? color
                  : CupertinoDynamicColor.resolve(
                      AppColors.mutedForeground,
                      context,
                    ),
            ),
            const SizedBox(width: 8),
            Text(
              label,
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w700,
                color: active
                    ? color
                    : CupertinoDynamicColor.resolve(
                        AppColors.foreground,
                        context,
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
