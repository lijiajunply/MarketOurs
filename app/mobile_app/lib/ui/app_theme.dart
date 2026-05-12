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
    color: Color(0xFFFFFFFF),
    darkColor: Color(0xFF1C1C1E),
  );

  static const Color cardForeground = CupertinoDynamicColor.withBrightness(
    color: Color(0xFF1D1D1F),
    darkColor: Color(0xFFF5F5F7),
  );

  static const Color secondary = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFF5F5F7),
    darkColor: Color(0xFF2C2C2E),
  );

  static const Color secondaryForeground = CupertinoDynamicColor.withBrightness(
    color: Color(0xFF1D1D1F),
    darkColor: Color(0xFFF5F5F7),
  );

  static const Color muted = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFF5F5F7),
    darkColor: Color(0xFF2C2C2E),
  );

  static const Color mutedForeground = CupertinoDynamicColor.withBrightness(
    color: Color(0xFF86868B),
    darkColor: Color(0xFF86868B),
  );

  static const Color border = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFE5E5E7),
    darkColor: Color(0xFF2C2C2E),
  );

  static const Color input = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFF5F5F7),
    darkColor: Color(0xFF1C1C1E),
  );

  static const Color destructive = CupertinoDynamicColor.withBrightness(
    color: Color(0xFFFF3B30),
    darkColor: Color(0xFFFF453A),
  );

  static const Color hot = Color(0xFFFF7A00);
}

abstract final class AppRadii {
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 24;
  static const double xxl = 32;
  static const double pill = 999;
}

abstract final class AppShadows {
  static const List<BoxShadow> none = [];

  static const List<BoxShadow> card = [
    BoxShadow(
      color: Color(0x0A000000),
      blurRadius: 20,
      offset: Offset(0, 4),
    ),
  ];
}

abstract final class AppTextStyles {
  static TextStyle hero(BuildContext context) => TextStyle(
    fontSize: 32,
    height: 1.2,
    fontWeight: FontWeight.w800,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
    letterSpacing: -0.5,
  );

  static TextStyle title(BuildContext context) => TextStyle(
    fontSize: 24,
    height: 1.25,
    fontWeight: FontWeight.w700,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
    letterSpacing: -0.4,
  );

  static TextStyle sectionTitle(BuildContext context) => TextStyle(
    fontSize: 18,
    height: 1.3,
    fontWeight: FontWeight.w700,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
    letterSpacing: -0.2,
  );

  static TextStyle body(BuildContext context) => TextStyle(
    fontSize: 16,
    height: 1.5,
    color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
  );

  static TextStyle muted(BuildContext context) => TextStyle(
    fontSize: 14,
    height: 1.4,
    color: CupertinoDynamicColor.resolve(AppColors.mutedForeground, context),
  );

  static TextStyle label(BuildContext context) => TextStyle(
    fontSize: 12,
    fontWeight: FontWeight.w600,
    color: CupertinoDynamicColor.resolve(AppColors.mutedForeground, context),
  );
}

abstract final class AppDecorations {
  static BoxDecoration card(BuildContext context, {double radius = AppRadii.xl}) {
    return BoxDecoration(
      color: CupertinoDynamicColor.resolve(AppColors.card, context),
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(
        color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.5),
      ),
    );
  }

  static BoxDecoration mutedCard(BuildContext context, {double radius = AppRadii.xl}) {
    return BoxDecoration(
      color: CupertinoDynamicColor.resolve(AppColors.secondary, context),
      borderRadius: BorderRadius.circular(radius),
    );
  }

  static BoxDecoration glass(BuildContext context, {double radius = AppRadii.xl}) {
    return BoxDecoration(
      color: CupertinoDynamicColor.resolve(AppColors.card, context).withValues(alpha: 0.7),
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(
        color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3),
      ),
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
              color: CupertinoDynamicColor.resolve(border, context).withValues(alpha: 0.45),
            )
          : null,
    );
  }
}
