import 'package:flutter/material.dart';
import 'package:mobile_app/models/user.dart';

class UserCard extends StatelessWidget {
  final UserSimpleDto user;
  final VoidCallback? onTap;

  const UserCard({super.key, required this.user, this.onTap});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(999),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircleAvatar(
            radius: 12,
            backgroundColor: const Color(0xFFF2F2F7),
            backgroundImage: user.avatar?.trim().isNotEmpty == true
                ? NetworkImage(user.avatar!.trim())
                : null,
            child: user.avatar?.trim().isNotEmpty == true
                ? null
                : Text(
                    _buildInitial(user.name),
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: const Color(0xFF007AFF),
                      fontWeight: FontWeight.w700,
                    ),
                  ),
          ),
          const SizedBox(width: 6),
          Text(
            user.name?.trim().isNotEmpty == true
                ? user.name!.trim()
                : 'Unknown',
            style: theme.textTheme.bodySmall?.copyWith(
              fontWeight: FontWeight.w500,
              color: Colors.grey.shade500,
            ),
          ),
        ],
      ),
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
