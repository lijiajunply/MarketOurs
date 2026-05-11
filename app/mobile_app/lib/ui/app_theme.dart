import 'package:flutter/cupertino.dart';

abstract final class AppColors {
  static const Color primary = Color(0xFF0071E3);
  static const Color primaryForeground = Color(0xFFFFFFFF);
  static const Color background = Color(0xFFFFFFFF);
  static const Color foreground = Color(0xFF1D1D1F);
  static const Color card = Color(0xFFFFFFFF);
  static const Color cardForeground = Color(0xFF1D1D1F);
  static const Color secondary = Color(0xFFF5F5F7);
  static const Color secondaryForeground = Color(0xFF1D1D1F);
  static const Color muted = Color(0xFFF5F5F7);
  static const Color mutedForeground = Color(0xFF86868B);
  static const Color border = Color(0xFFD2D2D7);
  static const Color input = Color(0xFFD2D2D7);
  static const Color destructive = Color(0xFFFF3B30);
  static const Color hot = Color(0xFFFF7A00);
  static const Color hotSoft = Color(0xFFFFF1E6);
  static const Color hotBorder = Color(0xFFFFD8BF);
}

abstract final class AppRadii {
  static const double sm = 12;
  static const double md = 16;
  static const double lg = 20;
  static const double xl = 28;
  static const double pill = 999;
}

abstract final class AppShadows {
  static const List<BoxShadow> card = [
    BoxShadow(color: Color(0x0F1D1D1F), blurRadius: 24, offset: Offset(0, 10)),
  ];

  static const List<BoxShadow> primary = [
    BoxShadow(color: Color(0x330071E3), blurRadius: 24, offset: Offset(0, 10)),
  ];
}

abstract final class AppTextStyles {
  static const TextStyle hero = TextStyle(
    fontSize: 32,
    height: 1.1,
    fontWeight: FontWeight.w800,
    color: AppColors.foreground,
  );

  static const TextStyle title = TextStyle(
    fontSize: 24,
    height: 1.2,
    fontWeight: FontWeight.w800,
    color: AppColors.foreground,
  );

  static const TextStyle sectionTitle = TextStyle(
    fontSize: 20,
    height: 1.25,
    fontWeight: FontWeight.w800,
    color: AppColors.foreground,
  );

  static const TextStyle body = TextStyle(
    fontSize: 16,
    height: 1.55,
    color: AppColors.foreground,
  );

  static const TextStyle muted = TextStyle(
    fontSize: 14,
    height: 1.5,
    color: AppColors.mutedForeground,
  );

  static const TextStyle label = TextStyle(
    fontSize: 12,
    fontWeight: FontWeight.w700,
    letterSpacing: 0.3,
    color: AppColors.mutedForeground,
  );
}

abstract final class AppDecorations {
  static BoxDecoration card({double radius = AppRadii.xl}) {
    return BoxDecoration(
      color: AppColors.card.withValues(alpha: 0.92),
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(color: AppColors.border.withValues(alpha: 0.45)),
      boxShadow: AppShadows.card,
    );
  }

  static BoxDecoration mutedCard({double radius = AppRadii.lg}) {
    return BoxDecoration(
      color: AppColors.secondary.withValues(alpha: 0.9),
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(color: AppColors.border.withValues(alpha: 0.35)),
    );
  }

  static BoxDecoration pill({
    Color background = AppColors.secondary,
    Color border = AppColors.border,
  }) {
    return BoxDecoration(
      color: background,
      borderRadius: BorderRadius.circular(AppRadii.pill),
      border: Border.all(color: border.withValues(alpha: 0.45)),
    );
  }

  static const LinearGradient hotGradient = LinearGradient(
    colors: [AppColors.hotSoft, AppColors.background],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  static const LinearGradient profileGradient = LinearGradient(
    colors: [Color(0x330071E3), Color(0x110071E3), Color(0x00FFFFFF)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );
}
