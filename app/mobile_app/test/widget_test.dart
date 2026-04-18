import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:mobile_app/main.dart';
import 'package:mobile_app/providers/post_feed_provider.dart';

void main() {
  testWidgets('renders home shell', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          homeFeedProvider.overrideWith(() => _FakeHomeFeedNotifier()),
        ],
        child: const MarketOursApp(),
      ),
    );

    await tester.pump();

    expect(find.text('首页'), findsOneWidget);
    expect(find.text('MarketOurs'), findsOneWidget);
  });
}

class _FakeHomeFeedNotifier extends HomeFeedNotifier {
  @override
  Future<HomeFeedState> build() async {
    return const HomeFeedState(
      posts: [],
      pageIndex: 1,
      hasNextPage: false,
      isLoadingMore: false,
    );
  }
}
