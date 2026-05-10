import 'package:flutter/cupertino.dart';

import '../../ui/app_responsive.dart';
import '../../ui/app_widgets.dart';

class AuthScaffold extends StatelessWidget {
  const AuthScaffold({
    super.key,
    required this.title,
    required this.subtitle,
    required this.child,
    this.footer,
  });

  final String title;
  final String subtitle;
  final Widget child;
  final Widget? footer;

  @override
  Widget build(BuildContext context) {
    return CupertinoPageScaffold(
      backgroundColor: CupertinoColors.systemGroupedBackground,
      child: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: AppResponsive.pagePadding(context, narrow: 20, wide: 28),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontSize: 32,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF111827),
                    ),
                  ),
                  const SizedBox(height: 10),
                  Text(
                    subtitle,
                    style: const TextStyle(
                      color: Color(0xFF6B7280),
                      height: 1.5,
                      fontSize: 15,
                    ),
                  ),
                  const SizedBox(height: 28),
                  AppSectionCard(
                    padding: const EdgeInsets.all(20),
                    child: child,
                  ),
                  if (footer != null) ...[const SizedBox(height: 16), footer!],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
