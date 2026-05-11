import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/components/user_card.dart';

import '../../models/post.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class HotScreen extends ConsumerWidget {
  const HotScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final hotFeedAsync = ref.watch(hotFeedProvider);

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: SafeArea(
        child: hotFeedAsync.when(
          data: (state) => CustomScrollView(
            physics: const BouncingScrollPhysics(
              parent: AlwaysScrollableScrollPhysics(),
            ),
            slivers: [
              CupertinoSliverRefreshControl(
                onRefresh: ref.read(hotFeedProvider.notifier).refresh,
              ),
              const SliverToBoxAdapter(child: _HotHeader()),
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
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
                          padding: EdgeInsets.only(
                            bottom: index == state.posts.length - 1 ? 0 : 16,
                          ),
                          child: _HotPostCard(
                            post: state.posts[index],
                            rank: index + 1,
                          ),
                        ),
                      ),
              ),
            ],
          ),
          loading: () => const Center(child: CupertinoActivityIndicator()),
          error: (error, _) => Center(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: AppEmptyState(
                icon: CupertinoIcons.exclamationmark_triangle,
                title: '热榜加载失败',
                description: '$error',
                action: AppPrimaryButton(
                  onPressed: () => ref.read(hotFeedProvider.notifier).refresh(),
                  child: const Text('重新加载'),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _HotHeader extends StatelessWidget {
  const _HotHeader();

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.fromLTRB(16, 16, 16, 20),
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        gradient: AppDecorations.hotGradient,
        borderRadius: BorderRadius.circular(AppRadii.xl),
        border: Border.all(color: AppColors.hotBorder),
        boxShadow: const [
          BoxShadow(
            color: Color(0x1AF59E0B),
            blurRadius: 28,
            offset: Offset(0, 14),
          ),
        ],
      ),
      child: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AppBadge(
            backgroundColor: AppColors.background,
            foregroundColor: AppColors.hot,
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(CupertinoIcons.flame_fill, size: 16, color: AppColors.hot),
                SizedBox(width: 8),
                Text('校园热榜'),
              ],
            ),
          ),
          SizedBox(height: 18),
          Text(
            '大家都在围观什么',
            style: TextStyle(
              fontSize: 30,
              height: 1.1,
              fontWeight: FontWeight.w800,
              color: AppColors.foreground,
            ),
          ),
          SizedBox(height: 10),
          Text('按实时热度整理出的热门帖子榜单，快速看看最近最受关注的话题。', style: AppTextStyles.muted),
        ],
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

    return AppTappableCard(
      padding: EdgeInsets.zero,
      radius: AppRadii.xl,
      color: rank <= 3
          ? AppColors.hotSoft.withValues(alpha: 0.55)
          : AppColors.card,
      border: Border.all(
        color: rank <= 3
            ? AppColors.hotBorder
            : AppColors.border.withValues(alpha: 0.5),
      ),
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (post.images?.isNotEmpty == true)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(AppRadii.xl),
              ),
              child: AspectRatio(
                aspectRatio: 1.8,
                child: Image.network(
                  post.images!.first,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Container(
                    color: AppColors.hotSoft,
                    alignment: Alignment.center,
                    child: const Icon(
                      CupertinoIcons.photo,
                      color: AppColors.hot,
                    ),
                  ),
                ),
              ),
            ),
          Padding(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 52,
                      height: 52,
                      decoration: BoxDecoration(
                        color: rank <= 3 ? AppColors.hot : AppColors.secondary,
                        borderRadius: BorderRadius.circular(AppRadii.lg),
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        rank.toString().padLeft(2, '0'),
                        style: TextStyle(
                          color: rank <= 3
                              ? AppColors.primaryForeground
                              : AppColors.foreground,
                          fontSize: 18,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            '热度排行',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: AppColors.hot,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            title,
                            style: const TextStyle(
                              fontSize: 22,
                              height: 1.2,
                              fontWeight: FontWeight.w800,
                              color: AppColors.foreground,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),
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
                Text(excerpt, style: AppTextStyles.muted),
                const SizedBox(height: 16),
                Row(
                  children: [
                    AppStatChip(
                      icon: CupertinoIcons.heart,
                      label: '${post.likes ?? 0}',
                      iconColor: const Color(0xFFFF5A5F),
                      backgroundColor: AppColors.background,
                    ),
                    const SizedBox(width: 10),
                    AppStatChip(
                      icon: CupertinoIcons.eye,
                      label: '${post.watch ?? 0}',
                      iconColor: AppColors.hot,
                      backgroundColor: AppColors.background,
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
