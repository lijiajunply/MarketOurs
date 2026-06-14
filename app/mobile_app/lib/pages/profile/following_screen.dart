import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../services/follow_service.dart';
import '../../services/error_messages.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class FollowingScreen extends ConsumerStatefulWidget {
  const FollowingScreen({super.key, this.initialTab = 'following'});

  final String initialTab;

  @override
  ConsumerState<FollowingScreen> createState() => _FollowingScreenState();
}

class _FollowingScreenState extends ConsumerState<FollowingScreen> {
  final _followService = FollowService();
  late String _activeTab;
  List<UserSimpleDto> _followingList = [];
  List<UserSimpleDto> _blockedList = [];
  bool _isLoading = true;
  String? _actionLoadingId;

  @override
  void initState() {
    super.initState();
    _activeTab = widget.initialTab;
    _loadData();
  }

  Future<void> _loadData() async {
    final user = ref.read(authControllerProvider).asData?.value.user;
    if (user == null) {
      if (mounted) setState(() => _isLoading = false);
      return;
    }

    setState(() => _isLoading = true);
    try {
      final results = await Future.wait([
        _followService
            .getFollowing(user.id, pageIndex: 1, pageSize: 50)
            .then((res) => _parseUsers(res.data)),
        _followService
            .getBlocked(pageIndex: 1, pageSize: 50)
            .then((res) => _parseUsers(res.data)),
      ]);
      if (mounted) {
        setState(() {
          _followingList = results[0];
          _blockedList = results[1];
        });
      }
    } catch (error) {
      if (mounted) {
        await AppFeedback.showError(
          context,
          message: extractErrorFromException(error),
        );
      }
    }
    if (mounted) setState(() => _isLoading = false);
  }

  List<UserSimpleDto> _parseUsers(dynamic data) {
    if (data is! Map<String, dynamic>) {
      return const [];
    }

    return (data['items'] as List?)
            ?.map((e) => UserSimpleDto.fromJson(e as Map<String, dynamic>))
            .toList() ??
        const [];
  }

  Future<void> _handleUnfollow(String userId) async {
    setState(() => _actionLoadingId = userId);
    try {
      await _followService.toggleFollow(userId);
      if (mounted) {
        setState(() {
          _followingList.removeWhere((u) => u.id == userId);
        });
      }
    } catch (error) {
      if (mounted) {
        await AppFeedback.showError(
          context,
          message: extractErrorFromException(error),
        );
      }
    }
    if (mounted) setState(() => _actionLoadingId = null);
  }

  Future<void> _handleUnblock(String userId) async {
    setState(() => _actionLoadingId = userId);
    try {
      await _followService.unblockUser(userId);
      if (mounted) {
        setState(() {
          _blockedList.removeWhere((u) => u.id == userId);
        });
      }
    } catch (error) {
      if (mounted) {
        await AppFeedback.showError(
          context,
          message: extractErrorFromException(error),
        );
      }
    }
    if (mounted) setState(() => _actionLoadingId = null);
  }

  @override
  Widget build(BuildContext context) {
    final list = _activeTab == 'following' ? _followingList : _blockedList;

    return AppPageScaffold(
      title: '社交管理',
      navigationBarStyle: AppNavigationBarStyle.compact,
      slivers: [
        CupertinoSliverRefreshControl(onRefresh: _loadData),
        SliverToBoxAdapter(
          child: AppResponsiveCenter(
            padding: AppResponsive.sliverPagePadding(context),
            child: Column(
              children: [
                CupertinoSlidingSegmentedControl<String>(
                  groupValue: _activeTab,
                  backgroundColor: CupertinoDynamicColor.resolve(
                    AppColors.secondary,
                    context,
                  ),
                  children: const {
                    'following': Padding(
                      padding: EdgeInsets.symmetric(horizontal: 16),
                      child: Text('我的关注', style: TextStyle(fontSize: 14)),
                    ),
                    'blocked': Padding(
                      padding: EdgeInsets.symmetric(horizontal: 16),
                      child: Text('屏蔽列表', style: TextStyle(fontSize: 14)),
                    ),
                  },
                  onValueChanged: (v) {
                    if (v != null && v != _activeTab) {
                      setState(() => _activeTab = v);
                    }
                  },
                ),
                const SizedBox(height: 20),
                if (_isLoading)
                  const Padding(
                    padding: EdgeInsets.only(top: 40),
                    child: CupertinoActivityIndicator(),
                  )
                else if (list.isEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 40),
                    child: AppEmptyState(
                      icon: _activeTab == 'following'
                          ? CupertinoIcons.person_2
                          : CupertinoIcons.hand_raised,
                      title: _activeTab == 'following' ? '还没有关注任何人' : '没有屏蔽任何人',
                      description: _activeTab == 'following'
                          ? '去发现感兴趣的用户并关注他们吧'
                          : '你的屏蔽列表是空的',
                    ),
                  )
                else
                  ...list.map(
                    (user) => _UserTile(
                      user: user,
                      isBlocked: _activeTab == 'blocked',
                      isLoading: _actionLoadingId == user.id,
                      onAction: () => _activeTab == 'following'
                          ? _handleUnfollow(user.id!)
                          : _handleUnblock(user.id!),
                      onTap: () =>
                          context.push(buildPublicProfileLocation(user.id!)),
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

class _UserTile extends StatelessWidget {
  const _UserTile({
    required this.user,
    required this.isBlocked,
    required this.isLoading,
    required this.onAction,
    required this.onTap,
  });

  final UserSimpleDto user;
  final bool isBlocked;
  final bool isLoading;
  final VoidCallback onAction;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: AppTappableCard(
        onPressed: onTap,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            AppAvatar(url: user.avatar, name: user.name, size: 44),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                user.name?.isNotEmpty == true ? user.name! : '未设置昵称',
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: AppColors.foreground,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
            CupertinoButton(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
              minimumSize: Size.zero,
              color: isBlocked
                  ? CupertinoColors.systemRed.withValues(alpha: 0.1)
                  : AppColors.secondary,
              borderRadius: BorderRadius.circular(10),
              onPressed: isLoading ? null : onAction,
              child: Text(
                isBlocked ? '取消屏蔽' : '取消关注',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: isBlocked
                      ? CupertinoColors.systemRed
                      : AppColors.mutedForeground,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
