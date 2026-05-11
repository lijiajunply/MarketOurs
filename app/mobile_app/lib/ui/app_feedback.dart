import 'dart:ui';

import 'package:flutter/cupertino.dart';

import 'app_theme.dart';

abstract final class AppFeedback {
  static Future<void> showMessage(
    BuildContext context, {
    required String message,
    String title = '提示',
  }) {
    return showCupertinoDialog<void>(
      context: context,
      builder: (context) => _FeedbackDialog(
        title: title,
        message: message,
        actions: [
          _FeedbackAction(
            label: '确定',
            onPressed: () => Navigator.of(context).pop(),
            isPrimary: true,
          ),
        ],
      ),
    );
  }

  static Future<bool?> confirm(
    BuildContext context, {
    required String message,
    String title = '确认',
    String cancelText = '取消',
    String confirmText = '确定',
    bool destructive = false,
  }) {
    return showCupertinoDialog<bool>(
      context: context,
      builder: (context) => _FeedbackDialog(
        title: title,
        message: message,
        actions: [
          _FeedbackAction(
            label: cancelText,
            onPressed: () => Navigator.of(context).pop(false),
          ),
          _FeedbackAction(
            label: confirmText,
            onPressed: () => Navigator.of(context).pop(true),
            isPrimary: !destructive,
            isDestructive: destructive,
          ),
        ],
      ),
    );
  }
}

class _FeedbackDialog extends StatelessWidget {
  const _FeedbackDialog({
    required this.title,
    required this.message,
    required this.actions,
  });

  final String title;
  final String message;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    return CupertinoPopupSurface(
      isSurfacePainted: false,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(AppRadii.xl),
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: 18, sigmaY: 18),
          child: Container(
            width: 320,
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.background.withValues(alpha: 0.98),
              borderRadius: BorderRadius.circular(AppRadii.xl),
              boxShadow: AppShadows.none,
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  title,
                  style: AppTextStyles.sectionTitle,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 10),
                Text(
                  message,
                  style: AppTextStyles.muted,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 18),
                Row(
                  children: [
                    for (final (index, action) in actions.indexed) ...[
                      if (index > 0) const SizedBox(width: 10),
                      Expanded(child: action),
                    ],
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _FeedbackAction extends StatelessWidget {
  const _FeedbackAction({
    required this.label,
    required this.onPressed,
    this.isPrimary = false,
    this.isDestructive = false,
  });

  final String label;
  final VoidCallback onPressed;
  final bool isPrimary;
  final bool isDestructive;

  @override
  Widget build(BuildContext context) {
    final backgroundColor = isDestructive
        ? AppColors.destructive.withValues(alpha: 0.12)
        : isPrimary
        ? AppColors.primary
        : AppColors.secondary;
    final foregroundColor = isDestructive
        ? AppColors.destructive
        : isPrimary
        ? AppColors.primaryForeground
        : AppColors.foreground;

    return CupertinoButton(
      padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 16),
      borderRadius: BorderRadius.circular(AppRadii.lg),
      color: backgroundColor,
      onPressed: onPressed,
      child: Text(
        label,
        style: TextStyle(color: foregroundColor, fontWeight: FontWeight.w700),
      ),
    );
  }
}
