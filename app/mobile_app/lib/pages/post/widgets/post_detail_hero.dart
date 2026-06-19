import 'package:carousel_slider/carousel_slider.dart';
import 'package:flutter/cupertino.dart';
import 'package:go_router/go_router.dart';

import '../../../../l10n/app_localizations.dart';
import '../../../components/post_tag_pill.dart';
import '../../../models/post.dart';
import '../../../router/app_router.dart';
import '../../../ui/app_theme.dart';
import '../../../ui/app_widgets.dart';
import '../../../utils/date_formatters.dart';
import '../image_viewer_screen.dart';

class PostDetailHero extends StatelessWidget {
  const PostDetailHero({
    super.key,
    required this.post,
    this.onAuthorTap,
    this.isFollowingAuthor = false,
    this.isMe = false,
    this.onFollowToggle,
  });

  final PostDto post;
  final VoidCallback? onAuthorTap;
  final bool isFollowingAuthor;
  final bool isMe;
  final VoidCallback? onFollowToggle;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (post.author != null)
          Padding(
            padding: const EdgeInsets.only(bottom: 16),
            child: Row(
              children: [
                Expanded(
                  child: GestureDetector(
                    onTap: () {
                      if (post.userId != null && post.userId!.isNotEmpty) {
                        context.push(buildPublicProfileLocation(post.userId!));
                      }
                    },
                    child: Row(
                      children: [
                        AppAvatar(
                          url: post.author?.avatar,
                          name: post.author?.name,
                          size: 40,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                post.author?.name ?? '匿名用户',
                                style: const TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              Text(
                                formatYmdDate(post.updatedAt ?? post.createdAt),
                                style: TextStyle(
                                  fontSize: 12,
                                  color: CupertinoDynamicColor.resolve(
                                    AppColors.mutedForeground,
                                    context,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                AppSecondaryButton(
                  onPressed: isMe ? onAuthorTap : onFollowToggle,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 6,
                  ),
                  child: Text(
                    isMe
                        ? AppLocalizations.of(context).myProfile
                        : isFollowingAuthor
                        ? AppLocalizations.of(context).profileUnfollow
                        : AppLocalizations.of(context).profileFollow,
                    style: TextStyle(
                      fontSize: 13,
                      color: isFollowingAuthor && !isMe
                          ? AppColors.mutedForeground
                          : null,
                    ),
                  ),
                ),
              ],
            ),
          ),
        Text(
          post.title?.trim().isNotEmpty == true ? post.title!.trim() : '未命名帖子',
          style: TextStyle(
            fontSize: 22,
            height: 1.3,
            fontWeight: FontWeight.w700,
            color: CupertinoDynamicColor.resolve(AppColors.foreground, context),
          ),
        ),
        if (post.tag != null) ...[
          const SizedBox(height: 12),
          PostTagPill(tag: post.tag, emptyText: AppLocalizations.of(context).postCreateNoTag),
        ],
        const SizedBox(height: 16),
        Text(
          post.content?.trim().isNotEmpty == true
              ? post.content!.trim()
              : '这个帖子还没有填写描述。',
          style: TextStyle(
            fontSize: 17,
            height: 1.6,
            color: CupertinoDynamicColor.resolve(
              AppColors.foreground,
              context,
            ).withValues(alpha: 0.9),
          ),
        ),
        if (post.images?.isNotEmpty == true) ...[
          const SizedBox(height: 20),
          CarouselSlider.builder(
            itemCount: post.images?.length,
            itemBuilder: (context, itemIndex, pageViewIndex) {
              return Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: GestureDetector(
                  onTap: () {
                    Navigator.of(context).push(
                      CupertinoPageRoute<void>(
                        fullscreenDialog: true,
                        builder: (_) => ImageViewerScreen(
                          images: post.images!,
                          initialIndex: itemIndex,
                        ),
                      ),
                    );
                  },
                  child: Hero(
                    tag: 'image_${post.images?[itemIndex]}',
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(AppRadii.lg),
                      child: ColoredBox(
                        color: CupertinoDynamicColor.resolve(
                          AppColors.secondary,
                          context,
                        ),
                        child: Image.network(
                          post.images?[itemIndex] ?? '',
                          width: double.infinity,
                          fit: BoxFit.cover,
                          gaplessPlayback: true,
                          errorBuilder:
                              (context, error, stackTrace) => Container(
                                height: 200,
                                width: double.infinity,
                                color: AppColors.muted,
                                child: const Icon(
                                  CupertinoIcons.photo,
                                  color: AppColors.mutedForeground,
                                ),
                              ),
                        ),
                      ),
                    ),
                  ),
                ),
              );
            },
            options: CarouselOptions(
              aspectRatio: 16 / 9,
              viewportFraction: 0.8,
              initialPage: 0,
              enableInfiniteScroll: false,
              enlargeCenterPage: true,
              enlargeFactor: 0.3,
              scrollDirection: Axis.horizontal,
            ),
          ),
        ],
      ],
    );
  }
}
