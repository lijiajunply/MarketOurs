import 'dart:ui';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../services/api_service.dart';
import '../services/app_logger.dart';

const _prefsKey = 'app.locale_index';

final localeNotifierProvider =
    NotifierProvider<LocaleNotifier, Locale?>(LocaleNotifier.new);

final supportedLocales = const [
  Locale('en'),
  Locale('zh'),
  Locale('ja'),
  Locale('ru'),
  Locale('fr'),
  Locale('de'),
  Locale('ko'),
];

class LocaleNotifier extends Notifier<Locale?> {
  @override
  Locale? build() {
    Future.microtask(() async {
      try {
        final prefs = await SharedPreferences.getInstance();
        final index = prefs.getInt(_prefsKey);
        if (index != null && index > 0 && index < supportedLocales.length + 1) {
          final locale = supportedLocales[index - 1];
          if (locale != state) {
            state = locale;
          }
        }
      } catch (e) {
        AppLogger.warn('LocaleNotifier', 'Failed to load locale', context: {'error': e.toString()});
      }
    });
    return null;
  }

  Future<void> setLocale(Locale? locale) async {
    state = locale;
    ApiService.setLocale(locale?.languageCode ?? 'zh');
    try {
      final prefs = await SharedPreferences.getInstance();
      if (locale == null) {
        await prefs.remove(_prefsKey);
      } else {
        final index = supportedLocales.indexOf(locale) + 1;
        await prefs.setInt(_prefsKey, index);
      }
    } catch (e) {
      AppLogger.warn('LocaleNotifier', 'Failed to persist locale', context: {'error': e.toString()});
    }
  }
}
