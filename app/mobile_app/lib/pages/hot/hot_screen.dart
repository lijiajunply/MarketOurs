import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/user_card.dart';

import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
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
              largeTitle: const Text('热榜'),
              backgroundColor: AppColors.background.withValues(alpha: 0.94),
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
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
              sliver: state.posts.isEmpty
                  ? const SliverToBoxAdapter(
                      child: AppEmptyState(
                        icon: CupertinoIcons.flame,
                        title: '热榜暂时为空',
                        description: '等大家再热闹一点，热门帖子就会出现在这里。',
                      ),
                    )
                  : SliverList.builder(
                      itemCount: state.posts.length,
                      itemBuilder: (context, index) => Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: _HotPostCard(
                          post: state.posts[index],
                          rank: index + 1,
                        ),
                      ),
                    ),
            ),
          ],
        ),
        loading: () => const Center(child: CupertinoActivityIndicator(radius: 14)),
        error: (error, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: AppEmptyState(
              icon: CupertinoIcons.exclamationmark_triangle,
              title: '加载失败',
              description: '$error',
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

class _HotPostCard extends StatelessWidget {
  const _HotPostCard({required this.post, required this.rank});

  final PostDto post;
  final int rank;

  @override
  Widget build(BuildContext context) {
    final title = post.title?.trim().isNotEmpty == true
        ? post.title!.trim()
        : '未命名帖子';
    final content = post.content?.trim().isNotEmpty == true
        ? post.content!.trim()
        : '这个帖子还没有填写内容。';
    final excerpt = content.length > 120
        ? '${content.substring(0, 120)}...'
        : content;

    final isTop3 = rank <= 3;
    final rankColor = isTop3 ? AppColors.hot : AppColors.mutedForeground;

    return AppTappableCard(
      padding: EdgeInsets.zero,
      radius: AppRadii.lg,
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (post.images?.isNotEmpty == true)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(AppRadii.lg),
              ),
              child: AspectRatio(
                aspectRatio: 1.8,
                child: Image.network(
                  post.images!.first,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Container(
                    color: AppColors.muted,
                    alignment: Alignment.center,
                    child: const Icon(
                      CupertinoIcons.photo,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                ),
              ),
            ),
          Padding(
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
                        style: AppTextStyles.sectionTitle(context).copyWith(fontSize: 20),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                if (post.author != null)
                  UserCard(
                    user: post.author!,
                    onTap: post.author?.id == null
                        ? null
                        : () => context.push(
                            buildPublicProfileLocation(post.author!.id!),
                          ),
                  ),
                if (post.author != null) const SizedBox(height: 12),
                Text(excerpt, style: AppTextStyles.muted(context)),
                const SizedBox(height: 16),
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
                    const SizedBox(width: 16),
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(
                          CupertinoIcons.bubble_left,
                          size: 14,
                          color: AppColors.mutedForeground,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${post.commentsCount ?? 0} 讨论',
                          style: const TextStyle(
                            fontSize: 13,
                            color: AppColors.mutedForeground,
                            fontWeight: FontWeight.w500,
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
        ],
      ),
    );
  }
}
