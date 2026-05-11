import 'package:flutter/cupertino.dart';

import 'package:mobile_app/models/user.dart';

import '../ui/app_theme.dart';
import '../ui/app_widgets.dart';

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
        AppAvatar(
          url: user.avatar,
          name: user.name,
          size: 40,
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
}
