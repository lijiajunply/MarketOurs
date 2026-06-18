import 'package:flutter/cupertino.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'l10n/app_localizations.dart';
import 'providers/locale_provider.dart';
import 'providers/theme_provider.dart';
import 'router/app_router.dart';
import 'ui/app_theme.dart';

void main() {
  runApp(const ProviderScope(child: MarketOursApp()));
}

class MarketOursApp extends ConsumerWidget {
  const MarketOursApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(appRouterProvider);
    final themeMode = ref.watch(themeModeNotifierProvider);
    final appLocale = ref.watch(localeNotifierProvider);

    return CupertinoApp.router(
      title: 'LightHub',
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      locale: appLocale,
      supportedLocales: supportedLocales,
      localizationsDelegates: [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      builder: (context, child) {
        final forcedBrightness = themeMode.forcedBrightness;
        if (forcedBrightness != null) {
          child = MediaQuery(
            data: MediaQuery.of(context).copyWith(
              platformBrightness: forcedBrightness,
            ),
            child: child!,
          );
        }
        return child!;
      },
      theme: CupertinoThemeData(
        brightness: themeMode.forcedBrightness ??
            MediaQuery.platformBrightnessOf(context),
        primaryColor: AppColors.primary,
        scaffoldBackgroundColor: AppColors.background,
        barBackgroundColor: AppColors.background,
        textTheme: CupertinoTextThemeData(
          textStyle: TextStyle(
            color: AppColors.foreground,
            fontSize: 16,
            height: 1.5,
          ),
        ),
      ),
    );
  }
}
