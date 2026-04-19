import 'package:flutter/material.dart';
import 'package:mobile_app/models/user.dart';

class UserCard extends StatelessWidget {
  final UserSimpleDto user;

  const UserCard({super.key, required this.user});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(24),
          child: Image.network(
            user.avatar ?? '',
            fit: BoxFit.cover,
            errorBuilder: (context, error, stackTrace) => Container(
              alignment: Alignment.center,
              child: Icon(
                Icons.image_not_supported_outlined,
                color: Colors.grey.shade400,
              ),
            ),
          ),
        ),
        const SizedBox(width: 6),
        Text(
          user.name ?? 'Unknown',
          style: theme.textTheme.bodySmall?.copyWith(
            fontWeight: FontWeight.w500,
            color: Colors.grey.shade500,
          ),
        ),
      ],
    );
  }
}
