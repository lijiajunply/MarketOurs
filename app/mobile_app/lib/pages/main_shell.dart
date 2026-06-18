import 'dart:async';

import 'package:flutter/cupertino.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../providers/post_feed_provider.dart';
import '../ui/app_feedback.dart';
import '../ui/app_responsive.dart';
import '../ui/app_theme.dart';

class MainShell extends ConsumerStatefulWidget {
  const MainShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  @override
  ConsumerState<MainShell> createState() => _MainShellState();
}

class _MainShellState extends ConsumerState<MainShell> {
  static const _exitWindow = Duration(seconds: 2);

  DateTime? _lastExitAttemptAt;

  void _onTap(BuildContext context, int index) {
    if (index != widget.navigationShell.currentIndex) {
      _lastExitAttemptAt = null;
    }

    widget.navigationShell.goBranch(
      index,
      initialLocation: index == widget.navigationShell.currentIndex,
    );
  }

  void _handleHomeExitAttempt() {
    final now = DateTime.now();
    final lastExitAttemptAt = _lastExitAttemptAt;

    if (lastExitAttemptAt != null &&
        now.difference(lastExitAttemptAt) <= _exitWindow) {
      unawaited(SystemNavigator.pop());
      return;
    }

    _lastExitAttemptAt = now;
    unawaited(ref.read(homeFeedProvider.notifier).refresh());
    unawaited(AppFeedback.showInfo(context, message: AppLocalizations.of(context)!.appTitle));
  }

  @override
  Widget build(BuildContext context) {
    final isTablet = AppResponsive.isTablet(context);
    final shouldInterceptExit =
        !kIsWeb && defaultTargetPlatform == TargetPlatform.android;

    final scaffold = CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: isTablet
          ? Row(
              children: [
                _TabletSideNavigation(
                  currentIndex: widget.navigationShell.currentIndex,
                  onTap: (index) => _onTap(context, index),
                ),
                Expanded(child: widget.navigationShell),
              ],
            )
          : Column(
              children: [
                Expanded(child: widget.navigationShell),
                CupertinoTabBar(
                  backgroundColor: CupertinoDynamicColor.resolve(
                    AppColors.background,
                    context,
                  ).withValues(alpha: 0.8),
                  activeColor: AppColors.primary,
                  inactiveColor: AppColors.mutedForeground.withValues(
                    alpha: 0.8,
                  ),
                  border: Border(
                    top: BorderSide(
                      color: CupertinoDynamicColor.resolve(
                        AppColors.border,
                        context,
                      ).withValues(alpha: 0.3),
                      width: 0.5,
                    ),
                  ),
                  currentIndex: widget.navigationShell.currentIndex,
                  onTap: (index) => _onTap(context, index),
                  items: _navigationItems(context)
                      .map(
                        (item) => BottomNavigationBarItem(
                          icon: Icon(item.icon),
                          activeIcon: Icon(item.activeIcon),
                          label: item.label,
                        ),
                      )
                      .toList(),
                ),
              ],
            ),
    );

    if (!shouldInterceptExit) {
      return scaffold;
    }

    return PopScope(
      canPop: widget.navigationShell.currentIndex != 0,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop || widget.navigationShell.currentIndex != 0) {
          return;
        }
        _handleHomeExitAttempt();
      },
      child: scaffold,
    );
  }
}

class _TabletSideNavigation extends StatelessWidget {
  const _TabletSideNavigation({
    required this.currentIndex,
    required this.onTap,
  });

  final int currentIndex;
  final ValueChanged<int> onTap;

  @override
  Widget build(BuildContext context) {
    final backgroundColor = CupertinoDynamicColor.resolve(
      AppColors.background,
      context,
    );
    final borderColor = CupertinoDynamicColor.resolve(
      AppColors.border,
      context,
    );

    return Container(
      key: const ValueKey('main-shell-tablet-side-navigation'),
      width: 104,
      decoration: BoxDecoration(
        color: backgroundColor.withValues(alpha: 0.92),
        border: Border(
          right: BorderSide(
            color: borderColor.withValues(alpha: 0.3),
            width: 0.5,
          ),
        ),
      ),
      child: SafeArea(
        right: false,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 10),
          child: Column(
            children: [
              for (final entry in _navigationItems(context).indexed)
                _TabletSideNavigationItem(
                  item: entry.$2,
                  isSelected: entry.$1 == currentIndex,
                  onPressed: () => onTap(entry.$1),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TabletSideNavigationItem extends StatelessWidget {
  const _TabletSideNavigationItem({
    required this.item,
    required this.isSelected,
    required this.onPressed,
  });

  final _MainNavigationItem item;
  final bool isSelected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final foregroundColor = isSelected
        ? AppColors.primary
        : CupertinoDynamicColor.resolve(
            AppColors.mutedForeground,
            context,
          ).withValues(alpha: 0.8);
    final Color? backgroundColor = isSelected
        ? AppColors.primary.withValues(alpha: 0.12)
        : null;

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: CupertinoButton(
        minimumSize: Size.zero,
        padding: EdgeInsets.zero,
        borderRadius: BorderRadius.circular(AppRadii.md),
        onPressed: onPressed,
        child: Container(
          height: 64,
          width: double.infinity,
          decoration: BoxDecoration(
            color: backgroundColor,
            borderRadius: BorderRadius.circular(AppRadii.md),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                isSelected ? item.activeIcon : item.icon,
                color: foregroundColor,
                size: 24,
              ),
              const SizedBox(height: 5),
              Text(
                item.label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: foregroundColor,
                  fontSize: 12,
                  height: 1.2,
                  fontWeight: isSelected ? FontWeight.w700 : FontWeight.w600,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _MainNavigationItem {
  const _MainNavigationItem({
    required this.icon,
    required this.activeIcon,
    required this.label,
  });

  final IconData icon;
  final IconData activeIcon;
  final String label;
}

List<_MainNavigationItem> _navigationItems(BuildContext context) {
  final l10n = AppLocalizations.of(context)!;
  return [
    _MainNavigationItem(
      icon: CupertinoIcons.house,
      activeIcon: CupertinoIcons.house_fill,
      label: l10n.tabHome,
    ),
    _MainNavigationItem(
      icon: CupertinoIcons.flame,
      activeIcon: CupertinoIcons.flame_fill,
      label: l10n.tabHot,
    ),
    _MainNavigationItem(
      icon: CupertinoIcons.bell,
      activeIcon: CupertinoIcons.bell_fill,
      label: l10n.tabNotifications,
    ),
    _MainNavigationItem(
      icon: CupertinoIcons.person,
      activeIcon: CupertinoIcons.person_fill,
      label: l10n.tabProfile,
    ),
  ];
}
