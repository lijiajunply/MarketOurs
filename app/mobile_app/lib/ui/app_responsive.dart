import 'package:flutter/cupertino.dart';

abstract final class AppBreakpoints {
  static const tablet = 768.0;
  static const desktop = 1100.0;
}

abstract final class AppResponsive {
  static bool isTablet(BuildContext context) {
    return MediaQuery.sizeOf(context).width >= AppBreakpoints.tablet;
  }

  static bool isDesktop(BuildContext context) {
    return MediaQuery.sizeOf(context).width >= AppBreakpoints.desktop;
  }

  static double contentMaxWidth(BuildContext context, {double? fallback}) {
    if (isDesktop(context)) {
      return fallback ?? 920;
    }
    if (isTablet(context)) {
      return fallback ?? 720;
    }
    return fallback ?? double.infinity;
  }

  static EdgeInsets pagePadding(
    BuildContext context, {
    double narrow = 16,
    double wide = 24,
  }) {
    return EdgeInsets.symmetric(
      horizontal: isTablet(context) ? wide : narrow,
      vertical: isTablet(context) ? wide : narrow,
    );
  }
}
