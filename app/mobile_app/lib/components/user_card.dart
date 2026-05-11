import 'package:flutter/cupertino.dart';

import 'package:mobile_app/models/user.dart';

import '../ui/app_theme.dart';

class UserCard extends StatelessWidget {
  const UserCard({
    super.key,
    required this.user,
    this.onTap,
    this.showMeta = false,
    this.meta,
  });

  final UserSimpleDto user;
  final VoidCallback? onTap;
  final bool showMeta;
  final String? meta;

  @override
  Widget build(BuildContext context) {
    final name = user.name?.trim().isNotEmpty == true
        ? user.name!.trim()
        : 'Unknown';
    final content = Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 40,
          height: 40,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: AppColors.secondary,
            shape: BoxShape.circle,
            image: user.avatar?.trim().isNotEmpty == true
                ? DecorationImage(
                    image: NetworkImage(user.avatar!.trim()),
                    fit: BoxFit.cover,
                  )
                : null,
          ),
          child: user.avatar?.trim().isNotEmpty == true
              ? null
              : Text(
                  _buildInitial(user.name),
                  style: const TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.w800,
                    fontSize: 14,
                  ),
                ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                name,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontWeight: FontWeight.w700,
                  color: AppColors.foreground,
                  fontSize: 14,
                ),
              ),
              if (showMeta || meta != null) ...[
                const SizedBox(height: 2),
                Text(
                  meta ?? '',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: AppColors.mutedForeground,
                    fontSize: 12,
                  ),
                ),
              ],
            ],
          ),
        ),
      ],
    );

    if (onTap == null) {
      return content;
    }

    return CupertinoButton(
      onPressed: onTap,
      padding: EdgeInsets.zero,
      minimumSize: Size.zero,
      alignment: Alignment.centerLeft,
      child: content,
    );
  }

  String _buildInitial(String? name) {
    final trimmed = name?.trim();
    if (trimmed == null || trimmed.isEmpty) {
      return 'U';
    }
    return trimmed.substring(0, 1).toUpperCase();
  }
}
