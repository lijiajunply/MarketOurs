import 'dart:ui';

import 'package:flutter/cupertino.dart';

import 'app_responsive.dart';
import 'app_theme.dart';

class AppPageScaffold extends StatelessWidget {
  const AppPageScaffold({
    super.key,
    this.title,
    this.leading,
    this.trailing,
    this.bottomBar,
    this.maxContentWidth,
    this.padding,
    required this.child,
  });

  final String? title;
  final Widget? leading;
  final Widget? trailing;
  final Widget? bottomBar;
  final double? maxContentWidth;
  final EdgeInsets? padding;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final contentPadding =
        padding ?? AppResponsive.pagePadding(context, narrow: 16, wide: 24);
    final contentMaxWidth =
        maxContentWidth ??
        AppResponsive.contentMaxWidth(context, fallback: 920);

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      navigationBar: title == null
          ? null
          : CupertinoNavigationBar(
              middle: Text(
                title!,
                style: const TextStyle(
                  color: AppColors.foreground,
                  fontWeight: FontWeight.w700,
                ),
              ),
              leading: leading,
              trailing: trailing,
              backgroundColor: AppColors.background.withValues(alpha: 0.82),
              border: Border(
                bottom: BorderSide(
                  color: AppColors.border.withValues(alpha: 0.35),
                ),
              ),
              automaticallyImplyLeading: leading == null,
              padding: const EdgeInsetsDirectional.only(start: 8, end: 8),
            ),
      child: SafeArea(
        top: title == null,
        bottom: bottomBar == null,
        child: Stack(
          children: [
            Positioned.fill(
              child: Align(
                alignment: Alignment.topCenter,
                child: ConstrainedBox(
                  constraints: BoxConstraints(maxWidth: contentMaxWidth),
                  child: Padding(
                    padding: EdgeInsets.only(
                      left: contentPadding.left,
                      right: contentPadding.right,
                      top: contentPadding.top,
                      bottom: bottomBar == null ? contentPadding.bottom : 120,
                    ),
                    child: child,
                  ),
                ),
              ),
            ),
            if (bottomBar != null)
              Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                child: Align(
                  alignment: Alignment.bottomCenter,
                  child: ConstrainedBox(
                    constraints: BoxConstraints(maxWidth: contentMaxWidth),
                    child: Padding(
                      padding: EdgeInsets.fromLTRB(
                        contentPadding.left,
                        0,
                        contentPadding.right,
                        12,
                      ),
                      child: bottomBar!,
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class AppGlassCard extends StatelessWidget {
  const AppGlassCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(18),
    this.radius = AppRadii.xl,
  });

  final Widget child;
  final EdgeInsets padding;
  final double radius;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(radius),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 18, sigmaY: 18),
        child: Container(
          padding: padding,
          decoration: AppDecorations.card(radius: radius),
          child: child,
        ),
      ),
    );
  }
}

class AppSectionCard extends StatelessWidget {
  const AppSectionCard({super.key, required this.child, this.padding});

  final Widget child;
  final EdgeInsets? padding;

  @override
  Widget build(BuildContext context) {
    return AppGlassCard(
      padding: padding ?? const EdgeInsets.all(20),
      child: child,
    );
  }
}

class AppPrimaryButton extends StatelessWidget {
  const AppPrimaryButton({
    super.key,
    required this.onPressed,
    required this.child,
    this.padding = const EdgeInsets.symmetric(vertical: 16, horizontal: 20),
  });

  final VoidCallback? onPressed;
  final Widget child;
  final EdgeInsets padding;

  @override
  Widget build(BuildContext context) {
    final disabled = onPressed == null;
    return CupertinoButton(
      padding: padding,
      borderRadius: BorderRadius.circular(AppRadii.lg),
      color: disabled
          ? AppColors.primary.withValues(alpha: 0.45)
          : AppColors.primary,
      onPressed: onPressed,
      child: DefaultTextStyle(
        style: const TextStyle(
          color: AppColors.primaryForeground,
          fontSize: 16,
          fontWeight: FontWeight.w700,
        ),
        child: child,
      ),
    );
  }
}

class AppSecondaryButton extends StatelessWidget {
  const AppSecondaryButton({
    super.key,
    required this.onPressed,
    required this.child,
    this.padding = const EdgeInsets.symmetric(vertical: 16, horizontal: 18),
  });

  final VoidCallback? onPressed;
  final Widget child;
  final EdgeInsets padding;

  @override
  Widget build(BuildContext context) {
    return CupertinoButton(
      onPressed: onPressed,
      padding: padding,
      borderRadius: BorderRadius.circular(AppRadii.lg),
      color: AppColors.secondary,
      child: DefaultTextStyle(
        style: TextStyle(
          color: onPressed == null
              ? AppColors.mutedForeground
              : AppColors.secondaryForeground,
          fontSize: 15,
          fontWeight: FontWeight.w700,
        ),
        child: child,
      ),
    );
  }
}

class AppTappableCard extends StatelessWidget {
  const AppTappableCard({
    super.key,
    required this.child,
    this.onPressed,
    this.padding,
    this.radius = AppRadii.xl,
    this.color,
    this.border,
  });

  final Widget child;
  final VoidCallback? onPressed;
  final EdgeInsets? padding;
  final double radius;
  final Color? color;
  final BoxBorder? border;

  @override
  Widget build(BuildContext context) {
    final card = Container(
      width: double.infinity,
      padding: padding ?? const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: color ?? AppColors.card,
        borderRadius: BorderRadius.circular(radius),
        border:
            border ??
            Border.all(color: AppColors.border.withValues(alpha: 0.45)),
        boxShadow: AppShadows.card,
      ),
      child: child,
    );

    if (onPressed == null) {
      return card;
    }

    return CupertinoButton(
      padding: EdgeInsets.zero,
      minimumSize: Size.zero,
      pressedOpacity: 0.94,
      onPressed: onPressed,
      child: card,
    );
  }
}

class AppListTile extends StatelessWidget {
  const AppListTile({
    super.key,
    required this.title,
    this.leading,
    this.subtitle,
    this.trailing,
    this.onTap,
    this.padding = const EdgeInsets.symmetric(vertical: 14),
    this.titleStyle,
  });

  final Widget title;
  final Widget? leading;
  final Widget? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;
  final EdgeInsets padding;
  final TextStyle? titleStyle;

  @override
  Widget build(BuildContext context) {
    final content = Padding(
      padding: padding,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          if (leading != null) ...[leading!, const SizedBox(width: 12)],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                DefaultTextStyle(
                  style:
                      titleStyle ??
                      const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                        color: AppColors.foreground,
                      ),
                  child: title,
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: 4),
                  DefaultTextStyle(
                    style: AppTextStyles.muted,
                    child: subtitle!,
                  ),
                ],
              ],
            ),
          ),
          if (trailing != null) ...[const SizedBox(width: 12), trailing!],
        ],
      ),
    );

    if (onTap == null) {
      return content;
    }

    return CupertinoButton(
      padding: EdgeInsets.zero,
      minimumSize: Size.zero,
      pressedOpacity: 0.94,
      onPressed: onTap,
      child: content,
    );
  }
}

class AppBadge extends StatelessWidget {
  const AppBadge({
    super.key,
    required this.child,
    this.backgroundColor = AppColors.secondary,
    this.foregroundColor = AppColors.foreground,
  });

  final Widget child;
  final Color backgroundColor;
  final Color foregroundColor;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: AppDecorations.pill(
        background: backgroundColor,
        border: backgroundColor,
      ),
      child: DefaultTextStyle(
        style: TextStyle(
          color: foregroundColor,
          fontSize: 13,
          fontWeight: FontWeight.w700,
        ),
        child: child,
      ),
    );
  }
}

class AppStatChip extends StatelessWidget {
  const AppStatChip({
    super.key,
    required this.icon,
    required this.label,
    this.iconColor,
    this.backgroundColor = AppColors.secondary,
  });

  final IconData icon;
  final String label;
  final Color? iconColor;
  final Color backgroundColor;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: AppDecorations.pill(
        background: backgroundColor,
        border: AppColors.border,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: iconColor ?? AppColors.mutedForeground),
          const SizedBox(width: 6),
          Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w700,
              color: AppColors.foreground,
            ),
          ),
        ],
      ),
    );
  }
}

class AppEmptyState extends StatelessWidget {
  const AppEmptyState({
    super.key,
    required this.icon,
    required this.title,
    required this.description,
    this.action,
  });

  final IconData icon;
  final String title;
  final String description;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return AppSectionCard(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 64,
              height: 64,
              decoration: const BoxDecoration(
                color: AppColors.secondary,
                shape: BoxShape.circle,
              ),
              child: Icon(icon, color: AppColors.mutedForeground, size: 28),
            ),
            const SizedBox(height: 16),
            Text(
              title,
              style: AppTextStyles.sectionTitle,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              description,
              style: AppTextStyles.muted,
              textAlign: TextAlign.center,
            ),
            if (action != null) ...[const SizedBox(height: 18), action!],
          ],
        ),
      ),
    );
  }
}

Future<T?> showAppBottomSheet<T>({
  required BuildContext context,
  required WidgetBuilder builder,
}) {
  return showCupertinoModalPopup<T>(
    context: context,
    builder: (sheetContext) {
      return Align(
        alignment: Alignment.bottomCenter,
        child: SafeArea(
          top: false,
          child: ClipRRect(
            borderRadius: const BorderRadius.vertical(
              top: Radius.circular(AppRadii.xl),
            ),
            child: BackdropFilter(
              filter: ImageFilter.blur(sigmaX: 22, sigmaY: 22),
              child: Container(
                margin: const EdgeInsets.only(top: 40),
                decoration: BoxDecoration(
                  color: AppColors.background.withValues(alpha: 0.94),
                  borderRadius: const BorderRadius.vertical(
                    top: Radius.circular(AppRadii.xl),
                  ),
                  border: Border.all(
                    color: AppColors.border.withValues(alpha: 0.35),
                  ),
                ),
                child: builder(sheetContext),
              ),
            ),
          ),
        ),
      );
    },
  );
}
