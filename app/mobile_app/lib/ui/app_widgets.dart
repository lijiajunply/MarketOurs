import 'package:flutter/cupertino.dart';

import 'app_responsive.dart';

class AppPageScaffold extends StatelessWidget {
  const AppPageScaffold({
    super.key,
    this.title,
    this.leading,
    this.trailing,
    this.bottomBar,
    this.maxContentWidth,
    this.padding,
    this.centerTitle = false,
    required this.child,
  });

  final String? title;
  final Widget? leading;
  final Widget? trailing;
  final Widget? bottomBar;
  final double? maxContentWidth;
  final EdgeInsets? padding;
  final bool centerTitle;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final contentPadding =
        padding ?? AppResponsive.pagePadding(context, narrow: 16, wide: 24);
    final contentMaxWidth =
        maxContentWidth ?? AppResponsive.contentMaxWidth(context);

    return CupertinoPageScaffold(
      backgroundColor: CupertinoColors.systemGroupedBackground,
      navigationBar: title == null
          ? null
          : CupertinoNavigationBar(
              middle: Text(title!),
              leading: leading,
              trailing: trailing,
              border: const Border(
                bottom: BorderSide(
                  color: CupertinoColors.separator,
                  width: 0.0,
                ),
              ),
              automaticallyImplyLeading: leading == null,
              padding: const EdgeInsetsDirectional.only(start: 8, end: 8),
            ),
      child: SafeArea(
        top: title == null,
        bottom: bottomBar == null,
        child: Column(
          children: [
            Expanded(
              child: LayoutBuilder(
                builder: (context, constraints) {
                  return Align(
                    alignment: Alignment.topCenter,
                    child: ConstrainedBox(
                      constraints: BoxConstraints(maxWidth: contentMaxWidth),
                      child: Padding(padding: contentPadding, child: child),
                    ),
                  );
                },
              ),
            ),
            if (bottomBar != null)
              Align(
                alignment: Alignment.bottomCenter,
                child: ConstrainedBox(
                  constraints: BoxConstraints(maxWidth: contentMaxWidth),
                  child: Padding(
                    padding: EdgeInsets.only(
                      left: contentPadding.left,
                      right: contentPadding.right,
                      bottom: contentPadding.bottom,
                    ),
                    child: bottomBar!,
                  ),
                ),
              ),
          ],
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
    return Container(
      padding: padding ?? const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: CupertinoColors.secondarySystemGroupedBackground,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(
          color: CupertinoColors.separator.withValues(alpha: 0.22),
        ),
      ),
      child: child,
    );
  }
}

class AppPrimaryButton extends StatelessWidget {
  const AppPrimaryButton({
    super.key,
    required this.onPressed,
    required this.child,
    this.padding = const EdgeInsets.symmetric(vertical: 14),
  });

  final VoidCallback? onPressed;
  final Widget child;
  final EdgeInsets padding;

  @override
  Widget build(BuildContext context) {
    return CupertinoButton.filled(
      onPressed: onPressed,
      padding: padding,
      borderRadius: BorderRadius.circular(14),
      child: child,
    );
  }
}

class AppSecondaryButton extends StatelessWidget {
  const AppSecondaryButton({
    super.key,
    required this.onPressed,
    required this.child,
  });

  final VoidCallback? onPressed;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return CupertinoButton(
      onPressed: onPressed,
      padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 18),
      color: CupertinoColors.secondarySystemFill,
      borderRadius: BorderRadius.circular(14),
      child: child,
    );
  }
}
