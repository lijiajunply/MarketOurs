import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../models/post.dart';
import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/user_service.dart';
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
  PublicUserProfileDto? _profile;
  List<PostDto> _recentPosts = const [];
  bool _isLoading = true;
  String? _errorMessage;

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
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                physics: const BouncingScrollPhysics(
                  parent: AlwaysScrollableScrollPhysics(),
                ),
                padding: const EdgeInsets.only(bottom: 24),
                children: [
                  _ProfileHero(profile: _profile!, isMe: isMe),
                  if (isMe) ...[
                    const SizedBox(height: 12),
                    AppSecondaryButton(
                      onPressed: () => context.push(AppRoutePaths.profile),
                      child: const Text('管理我的资料'),
                    ),
                  ],
                  const SizedBox(height: 24),
                  const Text(
                    '最近发布',
                    style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    '看看这位同学最近在 光汇 分享了什么。',
                    style: TextStyle(color: CupertinoColors.systemGrey),
                  ),
                  const SizedBox(height: 16),
                  if (_recentPosts.isEmpty)
                    const AppSectionCard(child: Text('还没有公开帖子'))
                  else
                    ..._recentPosts.map(
                      (post) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: _PostPreview(post: post),
                      ),
                    ),
                ],
              ),
            ),
    );
  }
}

class _ProfileHero extends StatelessWidget {
  const _ProfileHero({required this.profile, required this.isMe});

  final PublicUserProfileDto profile;
  final bool isMe;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          CircleAvatar(
            radius: 36,
            backgroundColor: const Color(0xFFF2F2F7),
            backgroundImage: profile.avatar?.trim().isNotEmpty == true
                ? NetworkImage(profile.avatar!.trim())
                : null,
            child: profile.avatar?.trim().isNotEmpty == true
                ? null
                : Text(
                    _initial(profile.name),
                    style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      color: const Color(0xFF007AFF),
                      fontWeight: FontWeight.w700,
                    ),
                  ),
          ),
          const SizedBox(height: 16),
          Text(
            profile.name?.trim().isNotEmpty == true
                ? profile.name!.trim()
                : '未设置昵称',
            style: Theme.of(
              context,
            ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              if (profile.role?.trim().isNotEmpty == true)
                _MetaChip(label: profile.role!.trim()),
              if (isMe) const _MetaChip(label: '这是你'),
            ],
          ),
          const SizedBox(height: 14),
          Text(
            profile.info?.trim().isNotEmpty == true
                ? profile.info!.trim()
                : '这个人很低调，还没有写简介。',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: Colors.grey.shade700,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            '加入时间 ${_formatDate(profile.createdAt)}',
            style: Theme.of(
              context,
            ).textTheme.labelMedium?.copyWith(color: Colors.grey.shade500),
          ),
        ],
      ),
    );
  }

  String _initial(String? name) {
    final trimmed = name?.trim();
    if (trimmed == null || trimmed.isEmpty) {
      return '我';
    }
    return trimmed.substring(0, 1);
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
        color: const Color(0xFFF2F2F7),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(label),
    );
  }
}

class _PostPreview extends StatelessWidget {
  const _PostPreview({required this.post});

  final PostDto post;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: () => context.push(buildPostDetailLocation(post.id)),
        child: Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: const Color(0xFFE8E8ED)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                post.title?.trim().isNotEmpty == true
                    ? post.title!.trim()
                    : '未命名帖子',
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 8),
              Text(
                post.content?.trim().isNotEmpty == true
                    ? post.content!.trim()
                    : '这个帖子还没有内容描述。',
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: Colors.grey.shade700, height: 1.5),
              ),
            ],
          ),
        ),
      ),
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
            const Icon(Icons.person_search_outlined, size: 48),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            FilledButton(onPressed: onRetry, child: const Text('重新加载')),
          ],
        ),
      ),
    );
  }
}
