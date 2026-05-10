import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'router/app_router.dart';

void main() {
  runApp(const ProviderScope(child: MarketOursApp()));
}

class MarketOursApp extends ConsumerWidget {
  const MarketOursApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(appRouterProvider);

    return CupertinoApp.router(
      title: '光汇',
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      theme: const CupertinoThemeData(
        primaryColor: Color(0xFF007AFF),
        scaffoldBackgroundColor: Color(0xFFF2F2F7),
        barBackgroundColor: Color(0xFFF2F2F7),
        textTheme: CupertinoTextThemeData(
          textStyle: TextStyle(
            color: Color(0xFF111827),
            fontSize: 16,
            height: 1.4,
          ),
        ),
      ),
    );
  }
}
