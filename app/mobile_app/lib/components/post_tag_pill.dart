import 'package:flutter/cupertino.dart';
import 'package:go_router/go_router.dart';

import '../models/post.dart';
import '../router/app_router.dart';
import '../ui/app_theme.dart';

class PostTagPill extends StatelessWidget {
  const PostTagPill({
    super.key,
    required this.tag,
    this.emptyText = '标签',
    this.clickable = true,
  });

  final PostTagDto? tag;
  final String emptyText;
  final bool clickable;

  @override
  Widget build(BuildContext context) {
    final hasTag = tag != null;
    final label = tag?.name?.trim().isNotEmpty == true ? tag!.name!.trim() : emptyText;
    final child = Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: hasTag
            ? AppColors.primary.withValues(alpha: 0.12)
            : CupertinoDynamicColor.resolve(AppColors.secondary, context),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(
          color: hasTag
              ? AppColors.primary.withValues(alpha: 0.28)
              : CupertinoDynamicColor.resolve(AppColors.border, context),
        ),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w700,
          color: hasTag
              ? AppColors.primary
              : CupertinoDynamicColor.resolve(AppColors.mutedForeground, context),
        ),
      ),
    );

    if (!clickable || tag == null) {
      return child;
    }

    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: () => context.push(buildTagLocation(tag!.id)),
      child: child,
    );
  }
}
