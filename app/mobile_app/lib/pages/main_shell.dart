import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

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
    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: Column(
        children: [
          Expanded(child: navigationShell),
          CupertinoTabBar(
            backgroundColor: CupertinoDynamicColor.resolve(
              AppColors.background,
              context,
            ).withValues(alpha: 0.8),
            activeColor: AppColors.primary,
            inactiveColor: AppColors.mutedForeground.withValues(alpha: 0.8),
            border: Border(
              top: BorderSide(
                color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3),
                width: 0.5,
              ),
            ),
            currentIndex: navigationShell.currentIndex,
            onTap: (index) => _onTap(context, index),
            items: const [
              BottomNavigationBarItem(
                icon: Icon(CupertinoIcons.house),
                activeIcon: Icon(CupertinoIcons.house_fill),
                label: '首页',
              ),
              BottomNavigationBarItem(
                icon: Icon(CupertinoIcons.flame),
                activeIcon: Icon(CupertinoIcons.flame_fill),
                label: '热榜',
              ),
              BottomNavigationBarItem(
                icon: Icon(CupertinoIcons.bell),
                activeIcon: Icon(CupertinoIcons.bell_fill),
                label: '通知',
              ),
              BottomNavigationBarItem(
                icon: Icon(CupertinoIcons.person),
                activeIcon: Icon(CupertinoIcons.person_fill),
                label: '我的',
              ),
            ],
          ),
        ],
      ),
    );
  }
}
