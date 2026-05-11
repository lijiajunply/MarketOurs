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
                    padding: const EdgeInsets.all(24),
                    decoration: BoxDecoration(
                      gradient: AppDecorations.profileGradient,
                      borderRadius: BorderRadius.circular(AppRadii.xl),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 52,
                          height: 52,
                          decoration: const BoxDecoration(
                            color: AppColors.primary,
                            borderRadius: BorderRadius.all(
                              Radius.circular(AppRadii.lg),
                            ),
                            boxShadow: AppShadows.primary,
                          ),
                          alignment: Alignment.center,
                          child: const Text(
                            'L',
                            style: TextStyle(
                              color: AppColors.primaryForeground,
                              fontWeight: FontWeight.w800,
                              fontSize: 24,
                            ),
                          ),
                        ),
                        const SizedBox(height: 20),
                        Text(title, style: AppTextStyles.hero),
                        const SizedBox(height: 10),
                        Text(subtitle, style: AppTextStyles.muted),
                      ],
                    ),
                  ),
                  const SizedBox(height: 22),
                  AppSectionCard(
                    padding: const EdgeInsets.all(22),
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
