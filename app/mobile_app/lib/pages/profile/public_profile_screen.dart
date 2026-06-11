import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/ui/app_theme.dart';

import '../../models/post.dart';
import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/follow_service.dart';
import '../../services/user_service.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_widgets.dart';

class PublicProfileScreen extends ConsumerStatefulWidget {
  const PublicProfileScreen({super.key, required this.userId});

  final String userId;

  @override
  ConsumerState<PublicProfileScreen> createState() =>
      _PublicProfileScreenState();
}

class _PublicProfileScreenState extends ConsumerState<PublicProfileScreen> {
  final _userService = UserService();
  final _followService = FollowService();
  PublicUserProfileDto? _profile;
  List<PostDto> _recentPosts = const [];
  bool _isLoading = true;
  String? _errorMessage;

  bool _isFollowing = false;
  bool _isBlocked = false;
  int _followerCount = 0;
  int _followingCount = 0;
  bool _followLoading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final profileResponse = await _userService.getPublicProfile(
        widget.userId,
      );
      final postsResponse = await ref
          .read(postServiceProvider)
          .getUserPosts(widget.userId, pageIndex: 1, pageSize: 6);
      final profile = profileResponse.data;
      final posts = postsResponse.data?.items;

      if (profile == null) {
        throw Exception('用户不存在');
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _profile = profile;
        _recentPosts = posts ?? const [];
        _followerCount = profile.followerCount;
        _followingCount = profile.followingCount;
        _isFollowing = profile.relationshipStatus?.isFollowing ?? false;
        _isBlocked = profile.relationshipStatus?.isBlocked ?? false;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _errorMessage = error.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _handleToggleFollow() async {
    if (_followLoading) return;
    setState(() => _followLoading = true);
    try {
      final result = await _followService.toggleFollow(widget.userId);
      if (mounted && result.data != null) {
        setState(() {
          _isFollowing = result.data!.isFollowing;
          _followerCount = result.data!.followerCount;
        });
      }
    } catch (_) {}
    if (mounted) setState(() => _followLoading = false);
  }

  Future<void> _handleToggleBlock() async {
    if (_followLoading) return;
    setState(() => _followLoading = true);
    try {
      if (_isBlocked) {
        await _followService.unblockUser(widget.userId);
        if (mounted) setState(() => _isBlocked = false);
      } else {
        await _followService.blockUser(widget.userId);
        if (mounted) {
          setState(() {
            _isBlocked = true;
            _isFollowing = false;
          });
        }
      }
    } catch (_) {}
    if (mounted) setState(() => _followLoading = false);
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isMe = authState?.user?.id == widget.userId;

    return AppPageScaffold(
      title: '用户主页',
      child: _isLoading
          ? const Center(child: CupertinoActivityIndicator())
          : _errorMessage != null || _profile == null
          ? _ErrorState(message: _errorMessage ?? '用户不存在', onRetry: _load)
          : CustomScrollView(
              physics: const BouncingScrollPhysics(
                parent: AlwaysScrollableScrollPhysics(),
              ),
              slivers: [
                CupertinoSliverRefreshControl(onRefresh: _load),
                SliverToBoxAdapter(
                  child: AppResponsiveCenter(
                    padding: AppResponsive.sliverPagePadding(context),
                    child: AppTwoPane(
                      key: const ValueKey('public-profile-responsive-two-pane'),
                      secondaryFirstOnWide: true,
                      secondary: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          _ProfileHero(profile: _profile!, isMe: isMe),
                          const SizedBox(height: 12),
                          _FollowStats(
                            followerCount: _followerCount,
                            followingCount: _followingCount,
                          ),
                          if (!isMe && ref.watch(authControllerProvider).asData?.value.user != null) ...[
                            const SizedBox(height: 12),
                            _FollowBlockButtons(
                              isFollowing: _isFollowing,
                              isBlocked: _isBlocked,
                              isLoading: _followLoading,
                              onToggleFollow: _handleToggleFollow,
                              onToggleBlock: _handleToggleBlock,
                            ),
                          ],
                          if (isMe) ...[
                            const SizedBox(height: 12),
                            AppSecondaryButton(
                              onPressed: () =>
                                  context.push(AppRoutePaths.profile),
                              child: const Text('管理我的资料'),
                            ),
                          ],
                        ],
                      ),
                      primary: _RecentPostsSection(posts: _recentPosts),
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}

class _RecentPostsSection extends StatelessWidget {
  const _RecentPostsSection({required this.posts});

  final List<PostDto> posts;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          '最近发布',
          style: TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.w800,
            color: AppColors.foreground,
          ),
        ),
        const SizedBox(height: 8),
        const Text(
          '看看这位同学最近在 光汇 分享了什么。',
          style: TextStyle(color: CupertinoColors.systemGrey),
        ),
        const SizedBox(height: 16),
        if (posts.isEmpty)
          const AppSectionCard(child: Text('还没有公开帖子'))
        else
          ...posts.map(
            (post) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: _PostPreview(post: post),
            ),
          ),
      ],
    );
  }
}

class _ProfileHero extends StatelessWidget {
  const _ProfileHero({required this.profile, required this.isMe});

  final PublicUserProfileDto profile;
  final bool isMe;

  @override
  Widget build(BuildContext context) {
    return AppSectionCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AppAvatar(
            url: profile.avatar,
            name: profile.name,
            size: 72,
          ),
          const SizedBox(height: 16),
          Text(
            profile.name?.trim().isNotEmpty == true
                ? profile.name!.trim()
                : '未设置昵称',
            style: const TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.w800,
              color: AppColors.foreground,
            ),
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              if (isMe) const _MetaChip(label: '这是你'),
            ],
          ),
          const SizedBox(height: 14),
          Text(
            profile.info?.trim().isNotEmpty == true
                ? profile.info!.trim()
                : '这个人很低调，还没有写简介。',
            style: const TextStyle(
              fontSize: 15,
              height: 1.5,
              color: AppColors.mutedForeground,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            '加入时间 ${_formatDate(profile.createdAt)}',
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.mutedForeground,
            ),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime? value) {
    if (value == null) {
      return '未知';
    }
    return '${value.year}-${value.month.toString().padLeft(2, '0')}-${value.day.toString().padLeft(2, '0')}';
  }
}

class _MetaChip extends StatelessWidget {
  const _MetaChip({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.secondary,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(label, style: const TextStyle(color: AppColors.foreground)),
    );
  }
}

class _PostPreview extends StatelessWidget {
  const _PostPreview({required this.post});

  final PostDto post;

  @override
  Widget build(BuildContext context) {
    return AppTappableCard(
      onPressed: () => context.push(buildPostDetailLocation(post.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            post.title?.trim().isNotEmpty == true
                ? post.title!.trim()
                : '未命名帖子',
            style: const TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w700,
              color: AppColors.foreground,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            post.content?.trim().isNotEmpty == true
                ? post.content!.trim()
                : '这个帖子还没有内容描述。',
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              height: 1.5,
              color: AppColors.mutedForeground,
            ),
          ),
        ],
      ),
    );
  }
}

class _FollowStats extends StatelessWidget {
  const _FollowStats({required this.followerCount, required this.followingCount});

  final int followerCount;
  final int followingCount;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: AppSectionCard(
            padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 12),
            child: Column(
              children: [
                Text(
                  '$followerCount',
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: AppColors.foreground,
                  ),
                ),
                const SizedBox(height: 4),
                const Text(
                  '粉丝',
                  style: TextStyle(
                    fontSize: 13,
                    color: AppColors.mutedForeground,
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: AppSectionCard(
            padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 12),
            child: Column(
              children: [
                Text(
                  '$followingCount',
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: AppColors.foreground,
                  ),
                ),
                const SizedBox(height: 4),
                const Text(
                  '关注',
                  style: TextStyle(
                    fontSize: 13,
                    color: AppColors.mutedForeground,
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _FollowBlockButtons extends StatelessWidget {
  const _FollowBlockButtons({
    required this.isFollowing,
    required this.isBlocked,
    required this.isLoading,
    required this.onToggleFollow,
    required this.onToggleBlock,
  });

  final bool isFollowing;
  final bool isBlocked;
  final bool isLoading;
  final VoidCallback onToggleFollow;
  final VoidCallback onToggleBlock;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: CupertinoButton(
            padding: const EdgeInsets.symmetric(vertical: 12),
            color: isFollowing
                ? AppColors.secondary
                : CupertinoColors.activeBlue,
            borderRadius: BorderRadius.circular(12),
            onPressed: isLoading || isBlocked ? null : onToggleFollow,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(
                  isFollowing
                      ? CupertinoIcons.person_badge_minus
                      : CupertinoIcons.person_badge_plus,
                  size: 18,
                  color: isFollowing
                      ? AppColors.foreground
                      : CupertinoColors.white,
                ),
                const SizedBox(width: 6),
                Text(
                  isFollowing ? '已关注' : '关注',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    color: isFollowing
                        ? AppColors.foreground
                        : CupertinoColors.white,
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(width: 12),
        CupertinoButton(
          padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
          color: isBlocked
              ? CupertinoColors.systemRed.withValues(alpha: 0.15)
              : AppColors.secondary,
          borderRadius: BorderRadius.circular(12),
          onPressed: isLoading ? null : onToggleBlock,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                isBlocked
                    ? CupertinoIcons.hand_raised_slash
                    : CupertinoIcons.hand_raised,
                size: 18,
                color: isBlocked
                    ? CupertinoColors.systemRed
                    : AppColors.mutedForeground,
              ),
              const SizedBox(width: 6),
              Text(
                isBlocked ? '已屏蔽' : '屏蔽',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                  color: isBlocked
                      ? CupertinoColors.systemRed
                      : AppColors.mutedForeground,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              CupertinoIcons.person_crop_circle_badge_exclam,
              size: 48,
              color: CupertinoColors.systemGrey2,
            ),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            AppPrimaryButton(onPressed: onRetry, child: const Text('重新加载')),
          ],
        ),
      ),
    );
  }
}
