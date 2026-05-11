import 'package:flutter/cupertino.dart';

abstract final class AppColors {
  static const Color primary = Color(0xFF0071E3);
  static const Color primaryForeground = Color(0xFFFFFFFF);

  static const Color background = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFFFFFFF),
    darkColor: Color(0xFF000000),
  );

  static const Color foreground = CupertinoDynamicColor.withBrightness(
    color: Color(0xFF1D1D1F),
    darkColor: Color(0xFFF5F5F7),
  );

  static const Color card = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFF5F5F7),
    darkColor: Color(0xFF1C1C1E),
  );

  static const Color cardForeground = CupertinoDynamicColor.withBrightness(
    color: Color(0xFF1D1D1F),
    darkColor: Color(0xFFF5F5F7),
  );

  static const Color secondary = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFF5F5F7),
    darkColor: Color(0xFF1C1C1E),
  );

  static const Color secondaryForeground = CupertinoDynamicColor.withBrightness(
    color: Color(0xFF1D1D1F),
    darkColor: Color(0xFFF5F5F7),
  );

  static const Color muted = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFF5F5F7),
    darkColor: Color(0xFF1C1C1E),
  );

  static const Color mutedForeground = CupertinoDynamicColor.withBrightness(
    color: Color(0xFF86868B),
    darkColor: Color(0xFF8E8E93),
  );

  static const Color border = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFD2D2D7),
    darkColor: Color(0xFF38383A),
  );

  static const Color input = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFD2D2D7),
    darkColor: Color(0xFF38383A),
  );

  static const Color destructive = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFFF3B30),
    darkColor: Color(0xFFFF453A),
  );

  static const Color hot = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFFF7A00),
    darkColor: Color(0xFFFF9F0A),
  );

  static const Color hotSoft = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFFFF1E6),
    darkColor: Color(0xFF2C1A0A),
  );

  static const Color hotBorder = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFFFD8BF),
    darkColor: Color(0xFF5C3600),
  );
}

abstract final class AppRadii {
  static const double sm = 12;
  static const double md = 16;
  static const double lg = 20;
  static const double xl = 24;
  static const double pill = 999;
}

abstract final class AppShadows {
  static const List<BoxShadow> none = [];

  static const List<BoxShadow> primary = [
    BoxShadow(color: Color(0x330071E3), blurRadius: 24, offset: Offset(0, 10)),
  ];
}

abstract final class AppTextStyles {
  static TextStyle hero(BuildContext context) => TextStyle(
    fontSize: 34,
    height: 1.1,
    fontWeight: FontWeight.w800,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
    letterSpacing: -0.5,
  );

  static TextStyle title(BuildContext context) => TextStyle(
    fontSize: 28,
    height: 1.2,
    fontWeight: FontWeight.w800,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
    letterSpacing: -0.4,
  );

  static TextStyle sectionTitle(BuildContext context) => TextStyle(
    fontSize: 22,
    height: 1.25,
    fontWeight: FontWeight.w700,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
    letterSpacing: -0.2,
  );

  static TextStyle body(BuildContext context) => TextStyle(
    fontSize: 17,
    height: 1.45,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
  );

  static TextStyle muted(BuildContext context) => TextStyle(
    fontSize: 15,
    height: 1.4,
    color: CupertinoDynamicColor.resolve(AppColors.mutedForeground, context),
  );

  static TextStyle label(BuildContext context) => TextStyle(
    fontSize: 13,
    fontWeight: FontWeight.w600,
    letterSpacing: 0.1,
    color: CupertinoDynamicColor.resolve(AppColors.mutedForeground, context),
  );
}

abstract final class AppDecorations {
  static BoxDecoration card(BuildContext context, {double radius = AppRadii.lg}) {
    return BoxDecoration(
      color: CupertinoDynamicColor.resolve(AppColors.card, context),
      borderRadius: BorderRadius.circular(radius),
    );
  }

  static BoxDecoration mutedCard(BuildContext context, {double radius = AppRadii.lg}) {
    return BoxDecoration(
      color: CupertinoDynamicColor.resolve(AppColors.secondary, context),
      borderRadius: BorderRadius.circular(radius),
    );
  }

  static BoxDecoration pill(
    BuildContext context, {
    Color background = AppColors.secondary,
    Color border = AppColors.border,
    bool showBorder = false,
  }) {
    return BoxDecoration(
      color: CupertinoDynamicColor.resolve(background, context),
      borderRadius: BorderRadius.circular(AppRadii.pill),
      border: showBorder
          ? Border.all(
              color: CupertinoDynamicColor.resolve(border, context)
                  .withValues(alpha: 0.45),
            )
          : null,
    );
  }

  static LinearGradient hotGradient(BuildContext context) => LinearGradient(
    colors: [
      CupertinoDynamicColor.resolve(AppColors.hotSoft, context),
      CupertinoDynamicColor.resolve(AppColors.background, context),
    ],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  static LinearGradient profileGradient(BuildContext context) => LinearGradient(
    colors: [
      CupertinoDynamicColor.resolve(AppColors.primary, context).withValues(alpha: 0.14),
      CupertinoDynamicColor.resolve(AppColors.primary, context).withValues(alpha: 0.03),
      CupertinoDynamicColor.resolve(AppColors.background, context).withValues(alpha: 0.0),
    ],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );
}
