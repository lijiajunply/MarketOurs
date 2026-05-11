import 'dart:ui';

import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../providers/auth_provider.dart';
import '../router/app_router.dart';
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
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isAuthenticated = authState?.status == AuthStatus.authenticated;
    final bottomInset = MediaQuery.viewPaddingOf(context).bottom;

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: Stack(
        children: [
          Positioned.fill(
            child: Padding(
              padding: EdgeInsets.only(bottom: 98 + bottomInset),
              child: navigationShell,
            ),
          ),
          Positioned(
            left: 16,
            right: 16,
            bottom: 16,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(AppRadii.xl),
              child: BackdropFilter(
                filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
                child: Container(
                  padding: EdgeInsets.fromLTRB(8, 10, 8, 10 + bottomInset),
                  decoration: BoxDecoration(
                    color: AppColors.background.withValues(alpha: 0.86),
                    borderRadius: BorderRadius.circular(AppRadii.xl),
                    border: Border.all(
                      color: AppColors.border.withValues(alpha: 0.35),
                    ),
                    boxShadow: AppShadows.card,
                  ),
                  child: Row(
                    children: [
                      Expanded(
                        child: _NavItem(
                          label: '首页',
                          icon: CupertinoIcons.house,
                          activeIcon: CupertinoIcons.house_fill,
                          active: navigationShell.currentIndex == 0,
                          onTap: () => _onTap(context, 0),
                        ),
                      ),
                      Expanded(
                        child: _NavItem(
                          label: '热榜',
                          icon: CupertinoIcons.flame,
                          activeIcon: CupertinoIcons.flame_fill,
                          active: navigationShell.currentIndex == 1,
                          onTap: () => _onTap(context, 1),
                        ),
                      ),
                      const SizedBox(width: 74),
                      Expanded(
                        child: _NavItem(
                          label: '通知',
                          icon: CupertinoIcons.bell,
                          activeIcon: CupertinoIcons.bell_fill,
                          active: navigationShell.currentIndex == 2,
                          onTap: () => _onTap(context, 2),
                        ),
                      ),
                      Expanded(
                        child: _NavItem(
                          label: '我的',
                          icon: CupertinoIcons.person,
                          activeIcon: CupertinoIcons.person_fill,
                          active: navigationShell.currentIndex == 3,
                          onTap: () => _onTap(context, 3),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            left: 0,
            right: 0,
            bottom: 58 + bottomInset,
            child: Center(
              child: CupertinoButton(
                color: AppColors.primary,
                padding: const EdgeInsets.symmetric(
                  horizontal: 22,
                  vertical: 16,
                ),
                borderRadius: BorderRadius.circular(AppRadii.pill),
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
                      color: AppColors.primaryForeground,
                      size: 18,
                    ),
                    SizedBox(width: 8),
                    Text(
                      '发布',
                      style: TextStyle(
                        color: AppColors.primaryForeground,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.label,
    required this.icon,
    required this.activeIcon,
    required this.active,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final IconData activeIcon;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = active ? AppColors.primary : AppColors.mutedForeground;
    return CupertinoButton(
      padding: const EdgeInsets.symmetric(vertical: 2),
      minimumSize: Size.zero,
      onPressed: onTap,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(active ? activeIcon : icon, color: color, size: 20),
          const SizedBox(height: 6),
          Text(
            label,
            style: TextStyle(
              color: color,
              fontSize: 11,
              fontWeight: active ? FontWeight.w700 : FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
