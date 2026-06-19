import 'package:flutter/cupertino.dart';
import 'package:mobile_app/l10n/app_localizations.dart';

import '../models/post.dart';
import '../ui/app_theme.dart';
import '../ui/app_widgets.dart';
import 'post_tag_pill.dart';

Future<PostTagDto?> showPostTagPicker(
  BuildContext context, {
  required List<PostTagDto> tags,
  required PostTagDto? selectedTag,
}) async {
  if (tags.isEmpty) return selectedTag;

  return showCupertinoModalPopup<PostTagDto?>(
    context: context,
    builder: (ctx) => CupertinoActionSheet(
      title: const Text('选择标签'),
      message: const Text('标签由管理员预设，可不选择。'),
      actions: [
        CupertinoActionSheetAction(
          onPressed: () => Navigator.of(ctx).pop(null),
          child: const Text('无标签'),
        ),
        for (final tag in tags)
          CupertinoActionSheetAction(
            onPressed: () => Navigator.of(ctx).pop(tag),
            child: Text(tag.name ?? '未命名标签'),
          ),
      ],
      cancelButton: CupertinoActionSheetAction(
        onPressed: () => Navigator.of(ctx).pop(selectedTag),
        child: Text(AppLocalizations.of(context).cancel),
      ),
    ),
  );
}

class PostTagSelectorCard extends StatelessWidget {
  const PostTagSelectorCard({
    super.key,
    required this.tag,
    required this.onPressed,
    this.emptyText = '',
    this.enabled = true,
    this.label = 'Tag',
  });

  final PostTagDto? tag;
  final VoidCallback? onPressed;
  final String emptyText;
  final bool enabled;
  final String label;

  @override
  Widget build(BuildContext context) {
    return AppTappableCard(
      padding: const EdgeInsets.all(20),
      radius: AppRadii.lg,
      onPressed: enabled ? onPressed : null,
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 6),
                PostTagPill(tag: tag, emptyText: emptyText, clickable: false),
              ],
            ),
          ),
          Icon(
            CupertinoIcons.chevron_down,
            size: 18,
            color: enabled
                ? AppColors.mutedForeground
                : AppColors.mutedForeground.withValues(alpha: 0.5),
          ),
        ],
      ),
    );
  }
}

class PostTagInlineSelector extends StatelessWidget {
  const PostTagInlineSelector({
    super.key,
    required this.tag,
    required this.onPressed,
    this.emptyText = '',
    this.enabled = true,
    this.label = 'Tag',
    this.actionLabel = '更改',
  });

  final PostTagDto? tag;
  final VoidCallback? onPressed;
  final String emptyText;
  final bool enabled;
  final String label;
  final String actionLabel;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Text(label, style: AppTextStyles.label(context)),
        const SizedBox(width: 10),
        GestureDetector(
          behavior: HitTestBehavior.opaque,
          onTap: enabled ? onPressed : null,
          child: PostTagPill(tag: tag, emptyText: emptyText, clickable: false),
        ),
        const Spacer(),
        if (enabled)
          CupertinoButton(
            padding: EdgeInsets.zero,
            onPressed: onPressed,
            child: Text(actionLabel),
          ),
      ],
    );
  }
}
