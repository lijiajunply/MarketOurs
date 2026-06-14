import 'dart:ui';

import 'package:flutter/cupertino.dart';
import 'package:flutter_svg/flutter_svg.dart';

import 'app_responsive.dart';
import 'app_theme.dart';

enum AppNavigationBarStyle { large, compact }

class AppPageScaffold extends StatelessWidget {
  const AppPageScaffold({
    super.key,
    this.title,
    this.leading,
    this.trailing,
    this.previousPageTitle,
    this.navigationBarStyle = AppNavigationBarStyle.large,
    this.alwaysShowMiddle = true,
    this.transitionBetweenRoutes = true,
    this.bottomBar,
    this.maxContentWidth,
    this.padding,
    this.scrollController,
    this.physics = const BouncingScrollPhysics(
      parent: AlwaysScrollableScrollPhysics(),
    ),
    this.resizeToAvoidBottomInset = true,
    this.child,
    this.slivers,
  }) : assert(
         child != null || slivers != null,
         'AppPageScaffold requires either child or slivers.',
       ),
       assert(
         child == null || slivers == null,
         'AppPageScaffold cannot receive both child and slivers.',
       );

  final String? title;
  final Widget? leading;
  final Widget? trailing;
  final String? previousPageTitle;
  final AppNavigationBarStyle navigationBarStyle;
  final bool alwaysShowMiddle;
  final bool transitionBetweenRoutes;
  final Widget? bottomBar;
  final double? maxContentWidth;
  final EdgeInsets? padding;
  final ScrollController? scrollController;
  final ScrollPhysics? physics;
  final bool resizeToAvoidBottomInset;
  final Widget? child;
  final List<Widget>? slivers;

  @override
  Widget build(BuildContext context) {
    final contentPadding =
        padding ?? AppResponsive.pagePadding(context, narrow: 16, wide: 24);
    final contentMaxWidth =
        maxContentWidth ?? AppResponsive.contentMaxWidth(context);
    final bottomInset = MediaQuery.viewInsetsOf(context).bottom;

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      resizeToAvoidBottomInset: bottomBar == null
          ? resizeToAvoidBottomInset
          : false,
      child: Stack(
        children: [
          CustomScrollView(
            controller: scrollController,
            physics: physics,
            slivers: [
              if (title != null)
                switch (navigationBarStyle) {
                  AppNavigationBarStyle.large => CupertinoSliverNavigationBar(
                    largeTitle: Text(
                      title!,
                      style: TextStyle(
                        color: CupertinoDynamicColor.resolve(
                          AppColors.foreground,
                          context,
                        ),
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    leading: leading,
                    trailing: trailing,
                    previousPageTitle: previousPageTitle,
                    alwaysShowMiddle: alwaysShowMiddle,
                    transitionBetweenRoutes: transitionBetweenRoutes,
                    backgroundColor: CupertinoDynamicColor.resolve(
                      AppColors.background,
                      context,
                    ).withValues(alpha: 0.94),
                    border: Border(
                      bottom: BorderSide(
                        color: CupertinoDynamicColor.resolve(
                          AppColors.border,
                          context,
                        ).withValues(alpha: 0.35),
                      ),
                    ),
                    automaticallyImplyLeading: leading == null,
                    padding: const EdgeInsetsDirectional.only(start: 8, end: 8),
                  ),
                  AppNavigationBarStyle.compact =>
                    _AppCompactNavigationBarSliver(
                      title: title!,
                      leading: leading,
                      trailing: trailing,
                      previousPageTitle: previousPageTitle,
                      transitionBetweenRoutes: transitionBetweenRoutes,
                    ),
                },
              if (slivers != null)
                ...slivers!
              else
                SliverFillRemaining(
                  hasScrollBody: false,
                  child: Align(
                    alignment: Alignment.topCenter,
                    child: ConstrainedBox(
                      constraints: BoxConstraints(maxWidth: contentMaxWidth),
                      child: Padding(
                        padding: EdgeInsets.only(
                          left: contentPadding.left,
                          right: contentPadding.right,
                          top: contentPadding.top,
                          bottom: bottomBar == null
                              ? contentPadding.bottom
                              : 120 + bottomInset,
                        ),
                        child: child!,
                      ),
                    ),
                  ),
                ),
              if (slivers != null && bottomBar != null)
                SliverToBoxAdapter(child: SizedBox(height: 120 + bottomInset)),
            ],
          ),
          if (bottomBar != null)
            Positioned(
              left: 0,
              right: 0,
              bottom: 0,
              child: SafeArea(
                top: false,
                child: Align(
                  alignment: Alignment.bottomCenter,
                  child: ConstrainedBox(
                    constraints: BoxConstraints(maxWidth: contentMaxWidth),
                    child: Padding(
                      padding: EdgeInsets.fromLTRB(
                        contentPadding.left,
                        0,
                        contentPadding.right,
                        12 + bottomInset,
                      ),
                      child: bottomBar!,
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _AppCompactNavigationBarSliver extends StatelessWidget {
  const _AppCompactNavigationBarSliver({
    required this.title,
    this.leading,
    this.trailing,
    this.previousPageTitle,
    required this.transitionBetweenRoutes,
  });

  final String title;
  final Widget? leading;
  final Widget? trailing;
  final String? previousPageTitle;
  final bool transitionBetweenRoutes;

  @override
  Widget build(BuildContext context) {
    final navigationBar = CupertinoNavigationBar(
      middle: Text(
        title,
        style: TextStyle(
          color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
          fontWeight: FontWeight.w700,
        ),
      ),
      leading: leading,
      trailing: trailing,
      previousPageTitle: previousPageTitle,
      automaticallyImplyLeading: leading == null,
      transitionBetweenRoutes: transitionBetweenRoutes,
      backgroundColor: CupertinoDynamicColor.resolve(
        AppColors.background,
        context,
      ).withValues(alpha: 0.94),
      border: Border(
        bottom: BorderSide(
          color: CupertinoDynamicColor.resolve(
            AppColors.border,
            context,
          ).withValues(alpha: 0.35),
        ),
      ),
      padding: const EdgeInsetsDirectional.only(start: 8, end: 8),
    );

    return SliverPersistentHeader(
      pinned: true,
      delegate: _AppCompactNavigationBarDelegate(
        navigationBar: navigationBar,
        height:
            navigationBar.preferredSize.height +
            MediaQuery.paddingOf(context).top,
      ),
    );
  }
}

class _AppCompactNavigationBarDelegate extends SliverPersistentHeaderDelegate {
  const _AppCompactNavigationBarDelegate({
    required this.navigationBar,
    required this.height,
  });

  final CupertinoNavigationBar navigationBar;
  final double height;

  @override
  double get minExtent => height;

  @override
  double get maxExtent => height;

  @override
  Widget build(
    BuildContext context,
    double shrinkOffset,
    bool overlapsContent,
  ) {
    return navigationBar;
  }

  @override
  bool shouldRebuild(_AppCompactNavigationBarDelegate oldDelegate) {
    return navigationBar != oldDelegate.navigationBar ||
        height != oldDelegate.height;
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
          decoration: BoxDecoration(
            color: CupertinoDynamicColor.resolve(AppColors.card, context),
            borderRadius: BorderRadius.circular(radius),
          ),
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
      color: CupertinoDynamicColor.resolve(
        disabled
            ? AppColors.primary.withValues(alpha: 0.45)
            : AppColors.primary,
        context,
      ),
      onPressed: onPressed,
      child: DefaultTextStyle(
        style: TextStyle(
          color: CupertinoDynamicColor.resolve(
            AppColors.primaryForeground,
            context,
          ),
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
      color: CupertinoDynamicColor.resolve(AppColors.secondary, context),
      child: DefaultTextStyle(
        style: TextStyle(
          color: CupertinoDynamicColor.resolve(
            onTap == null
                ? AppColors.mutedForeground
                : AppColors.secondaryForeground,
            context,
          ),
          fontSize: 15,
          fontWeight: FontWeight.w700,
        ),
        child: child,
      ),
    );
  }

  // Helper for color resolution since onTap is not in scope here but onPressed is
  VoidCallback? get onTap => onPressed;
}

class AppTappableCard extends StatelessWidget {
  const AppTappableCard({
    super.key,
    required this.child,
    this.onPressed,
    this.padding,
    this.radius = AppRadii.xl,
    this.color,
    this.showBorder = true,
    this.showShadow = true,
  });

  final Widget child;
  final VoidCallback? onPressed;
  final EdgeInsets? padding;
  final double radius;
  final Color? color;
  final bool showBorder;
  final bool showShadow;

  @override
  Widget build(BuildContext context) {
    final backgroundColor = CupertinoDynamicColor.resolve(
      color ?? AppColors.card,
      context,
    );

    final card = Container(
      width: double.infinity,
      padding: padding ?? const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(radius),
        border: showBorder
            ? Border.all(
                color: CupertinoDynamicColor.resolve(
                  AppColors.border,
                  context,
                ).withValues(alpha: 0.5),
              )
            : null,
        boxShadow: showShadow ? AppShadows.card : null,
      ),
      child: child,
    );

    if (onPressed == null) {
      return card;
    }

    return CupertinoButton(
      padding: EdgeInsets.zero,
      minimumSize: Size.zero,
      pressedOpacity: 0.92,
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
    final foregroundColor = CupertinoDynamicColor.resolve(
      AppColors.foreground,
      context,
    );

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
                      TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                        color: foregroundColor,
                      ),
                  child: title,
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: 4),
                  DefaultTextStyle(
                    style: AppTextStyles.muted(context),
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
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: AppDecorations.pill(context, background: backgroundColor),
      child: DefaultTextStyle(
        style: TextStyle(
          color: CupertinoDynamicColor.resolve(foregroundColor, context),
          fontSize: 12,
          fontWeight: FontWeight.w600,
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
    final resolvedFg = CupertinoDynamicColor.resolve(
      AppColors.foreground,
      context,
    );
    final resolvedIcon = CupertinoDynamicColor.resolve(
      iconColor ?? AppColors.mutedForeground,
      context,
    );

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: AppDecorations.pill(
        context,
        background: backgroundColor,
        showBorder: true,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: resolvedIcon),
          const SizedBox(width: 6),
          Text(
            label,
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w700,
              color: resolvedFg,
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
    return SizedBox(
      width: double.infinity,
      child: AppSectionCard(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 8),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 64,
                height: 64,
                decoration: BoxDecoration(
                  color: CupertinoDynamicColor.resolve(
                    AppColors.secondary,
                    context,
                  ),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  icon,
                  color: CupertinoDynamicColor.resolve(
                    AppColors.mutedForeground,
                    context,
                  ),
                  size: 28,
                ),
              ),
              const SizedBox(height: 16),
              Text(
                title,
                style: AppTextStyles.sectionTitle(context),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                description,
                style: AppTextStyles.muted(context),
                textAlign: TextAlign.center,
              ),
              if (action != null) ...[const SizedBox(height: 18), action!],
            ],
          ),
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
      final isPhone = AppResponsive.isPhone(sheetContext);
      final maxWidth = AppResponsive.sheetMaxWidth(sheetContext);
      final borderRadius = isPhone
          ? const BorderRadius.vertical(top: Radius.circular(AppRadii.xl))
          : BorderRadius.circular(AppRadii.xl);
      return Align(
        alignment: isPhone ? Alignment.bottomCenter : Alignment.center,
        child: SafeArea(
          top: false,
          child: Padding(
            padding: isPhone
                ? const EdgeInsets.only(top: 40)
                : const EdgeInsets.symmetric(horizontal: 20),
            child: ConstrainedBox(
              constraints: BoxConstraints(maxWidth: maxWidth),
              child: ClipRRect(
                borderRadius: borderRadius,
                child: BackdropFilter(
                  filter: ImageFilter.blur(sigmaX: 22, sigmaY: 22),
                  child: Container(
                    decoration: BoxDecoration(
                      color: CupertinoDynamicColor.resolve(
                        AppColors.background,
                        context,
                      ).withValues(alpha: 0.94),
                      borderRadius: borderRadius,
                      border: Border.all(
                        color: CupertinoDynamicColor.resolve(
                          AppColors.border,
                          context,
                        ).withValues(alpha: 0.35),
                      ),
                    ),
                    child: builder(sheetContext),
                  ),
                ),
              ),
            ),
          ),
        ),
      );
    },
  );
}

class AppAvatar extends StatelessWidget {
  const AppAvatar({
    super.key,
    this.url,
    this.name,
    this.size = 40,
    this.radius,
    this.isCircle = true,
  });

  final String? url;
  final String? name;
  final double size;
  final double? radius;
  final bool isCircle;

  String _buildInitial(String? name) {
    if (name == null || name.trim().isEmpty) return '?';
    final trimmed = name.trim();
    final parts = trimmed.split(RegExp(r'\s+'));
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return trimmed[0].toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final borderRadius = radius ?? (isCircle ? size / 2 : AppRadii.md);
    final avatarUrl = url?.trim();
    final hasImage = avatarUrl != null && avatarUrl.isNotEmpty;
    final resolvedBg = CupertinoDynamicColor.resolve(
      AppColors.secondary,
      context,
    );

    Widget? imageWidget;
    if (hasImage) {
      final lowerUrl = avatarUrl.toLowerCase();
      // Use SVG renderer for known SVG formats or DiceBear SVG URLs
      if (lowerUrl.contains('.svg') ||
          lowerUrl.contains('/svg') ||
          (lowerUrl.contains('dicebear.com') &&
              !lowerUrl.contains('/png') &&
              !lowerUrl.contains('/jpg'))) {
        imageWidget = SvgPicture.network(
          avatarUrl,
          width: size,
          height: size,
          fit: BoxFit.cover,
          placeholderBuilder: (context) => Container(
            color: resolvedBg,
            child: const Center(child: CupertinoActivityIndicator(radius: 8)),
          ),
        );
      } else {
        imageWidget = Image.network(
          avatarUrl,
          width: size,
          height: size,
          fit: BoxFit.cover,
          errorBuilder: (context, error, stackTrace) => _buildFallback(context),
          loadingBuilder: (context, child, loadingProgress) {
            if (loadingProgress == null) return child;
            return Container(
              color: resolvedBg,
              child: const Center(child: CupertinoActivityIndicator(radius: 8)),
            );
          },
        );
      }
    }

    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: resolvedBg,
        borderRadius: BorderRadius.circular(borderRadius),
      ),
      clipBehavior: Clip.antiAlias,
      alignment: Alignment.center,
      child: hasImage ? imageWidget : _buildFallback(context),
    );
  }

  Widget _buildFallback(BuildContext context) {
    return Text(
      _buildInitial(name),
      style: TextStyle(
        color: CupertinoDynamicColor.resolve(AppColors.primary, context),
        fontWeight: FontWeight.w800,
        fontSize: size * 0.4,
      ),
    );
  }
}
