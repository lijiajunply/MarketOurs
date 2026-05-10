import 'package:flutter/cupertino.dart';
import 'package:mobile_app/models/user.dart';

class UserCard extends StatelessWidget {
  final UserSimpleDto user;
  final VoidCallback? onTap;

  const UserCard({super.key, required this.user, this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 24,
            height: 24,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFFF2F2F7),
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
                      color: Color(0xFF007AFF),
                      fontWeight: FontWeight.w700,
                      fontSize: 11,
                    ),
                  ),
          ),
          const SizedBox(width: 6),
          Text(
            user.name?.trim().isNotEmpty == true
                ? user.name!.trim()
                : 'Unknown',
            style: const TextStyle(
              fontWeight: FontWeight.w500,
              color: CupertinoColors.systemGrey,
              fontSize: 13,
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
