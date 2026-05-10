import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../providers/auth_provider.dart';
import '../router/app_router.dart';

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
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isAuthenticated = authState?.status == AuthStatus.authenticated;
    final bottomInset = MediaQuery.viewPaddingOf(context).bottom;

    return CupertinoPageScaffold(
      backgroundColor: CupertinoColors.systemGroupedBackground,
      child: Stack(
        children: [
          Positioned.fill(
            child: Padding(
              padding: EdgeInsets.only(bottom: 74 + bottomInset),
              child: navigationShell,
            ),
          ),
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: Container(
              decoration: const BoxDecoration(
                border: Border(
                  top: BorderSide(color: CupertinoColors.separator, width: 0),
                ),
              ),
              child: CupertinoTabBar(
                currentIndex: navigationShell.currentIndex,
                onTap: (index) => _onTap(context, index),
                activeColor: const Color(0xFF007AFF),
                inactiveColor: CupertinoColors.systemGrey,
                height: 58 + bottomInset,
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
            ),
          ),
          Positioned(
            right: 20,
            bottom: 74 + bottomInset,
            child: CupertinoButton(
              color: const Color(0xFF007AFF),
              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
              borderRadius: BorderRadius.circular(22),
              onPressed: () {
                if (isAuthenticated) {
                  context.push(AppRoutePaths.createPost);
                  return;
                }
                context.go(AppRoutePaths.login);
              },
              child: const Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    CupertinoIcons.add,
                    color: CupertinoColors.white,
                    size: 18,
                  ),
                  SizedBox(width: 6),
                  Text(
                    '发布',
                    style: TextStyle(
                      color: CupertinoColors.white,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
