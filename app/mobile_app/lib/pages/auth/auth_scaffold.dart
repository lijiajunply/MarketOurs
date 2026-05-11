import 'package:flutter/cupertino.dart';

import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class AuthScaffold extends StatelessWidget {
  const AuthScaffold({
    super.key,
    required this.title,
    required this.subtitle,
    required this.child,
    this.footer,
    this.badge,
  });

  final String title;
  final String subtitle;
  final Widget child;
  final Widget? footer;
  final String? badge;

  @override
  Widget build(BuildContext context) {
    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: AppResponsive.pagePadding(context, narrow: 20, wide: 28),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 460),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (badge != null) ...[
                    Align(
                      alignment: Alignment.centerLeft,
                      child: AppBadge(
                        backgroundColor: AppColors.primary.withValues(
                          alpha: 0.1,
                        ),
                        foregroundColor: AppColors.primary,
                        child: Text(badge!),
                      ),
                    ),
                    const SizedBox(height: 18),
                  ],
                  Container(
                    padding: const EdgeInsets.all(28),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 48,
                          height: 48,
                          decoration: const BoxDecoration(
                            color: AppColors.primary,
                            borderRadius: BorderRadius.all(
                              Radius.circular(AppRadii.md),
                            ),
                          ),
                          alignment: Alignment.center,
                          child: const Text(
                            'G',
                            style: TextStyle(
                              color: AppColors.primaryForeground,
                              fontWeight: FontWeight.w800,
                              fontSize: 22,
                            ),
                          ),
                        ),
                        const SizedBox(height: 24),
                        Text(title, style: AppTextStyles.hero(context)),
                        const SizedBox(height: 12),
                        Text(subtitle, style: AppTextStyles.muted(context)),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),
                  AppGlassCard(
                    padding: const EdgeInsets.all(24),
                    radius: AppRadii.lg,
                    child: child,
                  ),
                  if (footer != null) ...[const SizedBox(height: 14), footer!],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
