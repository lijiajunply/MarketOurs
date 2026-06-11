import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../ui/app_responsive.dart';
import '../ui/app_theme.dart';

class MainShell extends ConsumerWidget {
  const MainShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  void _onTap(BuildContext context, int index) {
    navigationShell.goBranch(
      index,
      initialLocation: index == navigationShell.currentIndex,
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isTablet = AppResponsive.isTablet(context);

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: isTablet
          ? Row(
              children: [
                _TabletSideNavigation(
                  currentIndex: navigationShell.currentIndex,
                  onTap: (index) => _onTap(context, index),
                ),
                Expanded(child: navigationShell),
              ],
            )
          : Column(
              children: [
                Expanded(child: navigationShell),
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
                  currentIndex: navigationShell.currentIndex,
                  onTap: (index) => _onTap(context, index),
                  items: _navigationItems
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
              for (final entry in _navigationItems.indexed)
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

const _navigationItems = [
  _MainNavigationItem(
    icon: CupertinoIcons.house,
    activeIcon: CupertinoIcons.house_fill,
    label: '首页',
  ),
  _MainNavigationItem(
    icon: CupertinoIcons.flame,
    activeIcon: CupertinoIcons.flame_fill,
    label: '热榜',
  ),
  _MainNavigationItem(
    icon: CupertinoIcons.bell,
    activeIcon: CupertinoIcons.bell_fill,
    label: '通知',
  ),
  _MainNavigationItem(
    icon: CupertinoIcons.person,
    activeIcon: CupertinoIcons.person_fill,
    label: '我的',
  ),
];
