import 'package:flutter/cupertino.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/error_messages.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class HotScreen extends ConsumerWidget {
  const HotScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final hotFeedAsync = ref.watch(hotFeedProvider);
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isAuthenticated = authState?.status == AuthStatus.authenticated;

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: hotFeedAsync.when(
        data: (state) => CustomScrollView(
          physics: const BouncingScrollPhysics(
            parent: AlwaysScrollableScrollPhysics(),
          ),
          slivers: [
            CupertinoSliverNavigationBar(
              largeTitle: Text(AppLocalizations.of(context)!.tabHot),
              backgroundColor: CupertinoDynamicColor.resolve(
                AppColors.background,
                context,
              ).withValues(alpha: 0.94),
              border: null,
              trailing: CupertinoButton(
                padding: EdgeInsets.zero,
                onPressed: () {
                  if (isAuthenticated) {
                    context.push(AppRoutePaths.createPost);
                  } else {
                    context.go(AppRoutePaths.login);
                  }
                },
                child: const Icon(
                  CupertinoIcons.plus_circle_fill,
                  size: 28,
                  color: AppColors.primary,
                ),
              ),
            ),
            CupertinoSliverRefreshControl(
              onRefresh: ref.read(hotFeedProvider.notifier).refresh,
            ),
            AppResponsiveSliverPadding(child: _HotPostList(posts: state.posts)),
          ],
        ),
        loading: () =>
            const Center(child: CupertinoActivityIndicator(radius: 14)),
        error: (error, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: AppEmptyState(
              icon: CupertinoIcons.exclamationmark_triangle,
              title: '加载失败',
              description: extractErrorFromException(error),
              action: AppPrimaryButton(
                onPressed: () => ref.read(hotFeedProvider.notifier).refresh(),
                child: const Text('重新加载'),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _HotPostList extends StatelessWidget {
  const _HotPostList({required this.posts});

  final List<PostDto> posts;

  @override
  Widget build(BuildContext context) {
    if (posts.isEmpty) {
      return const AppEmptyState(
        icon: CupertinoIcons.flame,
        title: '热榜暂时为空',
        description: '等大家再热闹一点，热门帖子就会出现在这里。',
      );
    }

    final columns = AppResponsive.listColumnCount(context);
    if (columns == 1) {
      return Column(
        key: const ValueKey('hot-feed-columns-1'),
        children: [
          for (final entry in posts.indexed)
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: _HotPostCard(post: entry.$2, rank: entry.$1 + 1),
            ),
        ],
      );
    }

    return LayoutBuilder(
      key: const ValueKey('hot-feed-columns-2'),
      builder: (context, constraints) {
        const spacing = 16.0;
        final itemWidth = (constraints.maxWidth - spacing) / 2;
        return Wrap(
          spacing: spacing,
          runSpacing: spacing,
          children: [
            for (final entry in posts.indexed)
              SizedBox(
                width: itemWidth,
                child: _HotPostCard(post: entry.$2, rank: entry.$1 + 1),
              ),
          ],
        );
      },
    );
  }
}

class _HotPostCard extends StatelessWidget {
  const _HotPostCard({required this.post, required this.rank});

  final PostDto post;
  final int rank;

  @override
  Widget build(BuildContext context) {
    final title = post.title?.trim().isNotEmpty == true
        ? post.title!.trim()
        : '未命名帖子';
    final isTop3 = rank <= 3;
    final rankColor = isTop3 ? AppColors.hot : AppColors.mutedForeground;

    return AppTappableCard(
      padding: EdgeInsets.zero,
      radius: AppRadii.lg,
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Container(
                  width: 28,
                  height: 28,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: isTop3
                        ? AppColors.hot.withValues(alpha: 0.12)
                        : AppColors.muted,
                    shape: BoxShape.circle,
                  ),
                  child: Text(
                    '$rank',
                    style: TextStyle(
                      color: rankColor,
                      fontSize: 14,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    title,
                    style: AppTextStyles.sectionTitle(
                      context,
                    ).copyWith(fontSize: 20),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(CupertinoIcons.flame, color: rankColor, size: 14),
                    const SizedBox(width: 4),
                    Text(
                      '${post.watch ?? 0} 热度',
                      style: TextStyle(
                        fontSize: 13,
                        color: rankColor,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
                const Spacer(),
                const Icon(
                  CupertinoIcons.chevron_right,
                  color: AppColors.mutedForeground,
                  size: 14,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
