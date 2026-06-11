import 'package:flutter/cupertino.dart';
import 'package:go_router/go_router.dart';

import '../../models/notification.dart';
import '../../router/app_router.dart';
import '../../services/notification_service.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';
import 'push_settings_screen.dart';

class NotificationScreen extends StatefulWidget {
  final NotificationService service;

  const NotificationScreen({super.key, required this.service});

  @override
  State<NotificationScreen> createState() => _NotificationScreenState();
}

class _NotificationScreenState extends State<NotificationScreen> {
  List<NotificationDto> _notifications = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadNotifications();
  }

  Future<void> _loadNotifications() async {
    setState(() => _isLoading = true);
    try {
      final result = await widget.service.getNotifications();
      if (!mounted) return;
      if (result != null) {
        setState(() {
          _notifications = result.items;
        });
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  IconData _getIcon(NotificationType type) {
    switch (type) {
      case NotificationType.commentReply:
        return CupertinoIcons.reply;
      case NotificationType.postReply:
        return CupertinoIcons.chat_bubble_2;
      case NotificationType.hotList:
        return CupertinoIcons.flame;
      case NotificationType.system:
        return CupertinoIcons.bell;
      case NotificationType.review:
        return CupertinoIcons.check_mark_circled;
      case NotificationType.unknown:
        return CupertinoIcons.bell;
    }
  }

  Color _getIconColor(NotificationType type) {
    switch (type) {
      case NotificationType.commentReply:
        return const Color(0xFF007AFF);
      case NotificationType.postReply:
        return const Color(0xFF34C759);
      case NotificationType.hotList:
        return const Color(0xFFFF9500);
      case NotificationType.system:
        return AppColors.mutedForeground;
      case NotificationType.review:
        return const Color(0xFFAF52DE);
      case NotificationType.unknown:
        return AppColors.mutedForeground;
    }
  }

  @override
  Widget build(BuildContext context) {
    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: CustomScrollView(
        physics: const BouncingScrollPhysics(
          parent: AlwaysScrollableScrollPhysics(),
        ),
        slivers: [
          CupertinoSliverNavigationBar(
            largeTitle: const Text('通知'),
            backgroundColor: CupertinoDynamicColor.resolve(
              AppColors.background,
              context,
            ).withValues(alpha: 0.94),
            border: null,
            trailing: CupertinoButton(
              padding: EdgeInsets.zero,
              minimumSize: Size.zero,
              onPressed: () {
                showCupertinoModalPopup<void>(
                  context: context,
                  builder: (_) => CupertinoActionSheet(
                    actions: [
                      CupertinoActionSheetAction(
                        onPressed: () async {
                          Navigator.of(context).pop();
                          await _markAllAsReadOptimistically();
                        },
                        child: const Text('全部标为已读'),
                      ),
                      CupertinoActionSheetAction(
                        onPressed: () {
                          Navigator.of(context).push(
                            CupertinoPageRoute(
                              builder: (_) =>
                                  PushSettingsScreen(service: widget.service),
                            ),
                          );
                        },
                        child: const Text('推送设置'),
                      ),
                    ],
                    cancelButton: CupertinoActionSheetAction(
                      onPressed: () => Navigator.of(context).pop(),
                      child: const Text('取消'),
                    ),
                  ),
                );
              },
              child: const Icon(CupertinoIcons.ellipsis_circle, size: 24),
            ),
          ),
          CupertinoSliverRefreshControl(onRefresh: _loadNotifications),
          if (_isLoading && _notifications.isEmpty)
            const SliverFillRemaining(
              child: Center(child: CupertinoActivityIndicator(radius: 14)),
            )
          else if (_notifications.isEmpty)
            SliverFillRemaining(child: _buildEmptyState())
          else
            AppResponsiveSliverPadding(
              child: _NotificationList(
                notifications: _notifications,
                iconForType: _getIcon,
                iconColorForType: _getIconColor,
                formatDate: _formatDate,
                onOpen: _openNotification,
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _openNotification(int index) async {
    final n = _notifications[index];
    if (!n.isRead) {
      setState(() {
        _notifications[index] = _copyNotification(n, isRead: true);
      });
      widget.service
          .markAsRead(n.id)
          .then((success) {
            if (!success && mounted) _loadNotifications();
          })
          .catchError((_) {
            if (mounted) _loadNotifications();
          });
    }

    if (!mounted) {
      return;
    }

    if (n.targetId?.isNotEmpty == true) {
      context.push(buildPostDetailLocation(n.targetId!));
    }
  }

  Future<void> _markAllAsReadOptimistically() async {
    final previous = _notifications;
    setState(() {
      _notifications = [
        for (final n in _notifications) _copyNotification(n, isRead: true),
      ];
    });

    try {
      final success = await widget.service.markAllAsRead();
      if (!success && mounted) setState(() => _notifications = previous);
    } catch (_) {
      if (mounted) setState(() => _notifications = previous);
    }
  }

  NotificationDto _copyNotification(NotificationDto n, {required bool isRead}) {
    return NotificationDto(
      id: n.id,
      userId: n.userId,
      title: n.title,
      content: n.content,
      type: n.type,
      targetId: n.targetId,
      isRead: isRead,
      createdAt: n.createdAt,
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(20),
            decoration: const BoxDecoration(
              color: AppColors.secondary,
              shape: BoxShape.circle,
            ),
            child: const Icon(
              CupertinoIcons.bell_slash,
              size: 32,
              color: AppColors.mutedForeground,
            ),
          ),
          const SizedBox(height: 16),
          const Text(
            '暂无通知',
            style: TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w600,
              color: AppColors.mutedForeground,
            ),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final diff = now.difference(date);
    if (diff.inDays == 0) {
      return '${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
    } else if (diff.inDays == 1) {
      return '昨天';
    } else if (diff.inDays < 7) {
      return '${diff.inDays}天前';
    }
    return '${date.year}/${date.month}/${date.day}';
  }
}

class _NotificationList extends StatelessWidget {
  const _NotificationList({
    required this.notifications,
    required this.iconForType,
    required this.iconColorForType,
    required this.formatDate,
    required this.onOpen,
  });

  final List<NotificationDto> notifications;
  final IconData Function(NotificationType type) iconForType;
  final Color Function(NotificationType type) iconColorForType;
  final String Function(DateTime date) formatDate;
  final Future<void> Function(int index) onOpen;

  @override
  Widget build(BuildContext context) {
    final columns = AppResponsive.listColumnCount(context);
    if (columns == 1) {
      return Column(
        key: const ValueKey('notification-list-columns-1'),
        children: [
          for (final entry in notifications.indexed)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: _NotificationCard(
                notification: entry.$2,
                icon: iconForType(entry.$2.type),
                iconColor: iconColorForType(entry.$2.type),
                formattedDate: formatDate(entry.$2.createdAt),
                onPressed: () {
                  onOpen(entry.$1);
                },
              ),
            ),
        ],
      );
    }

    return LayoutBuilder(
      key: const ValueKey('notification-list-columns-2'),
      builder: (context, constraints) {
        const spacing = 16.0;
        final itemWidth = (constraints.maxWidth - spacing) / 2;
        return Wrap(
          spacing: spacing,
          runSpacing: spacing,
          children: [
            for (final entry in notifications.indexed)
              SizedBox(
                width: itemWidth,
                child: _NotificationCard(
                  notification: entry.$2,
                  icon: iconForType(entry.$2.type),
                  iconColor: iconColorForType(entry.$2.type),
                  formattedDate: formatDate(entry.$2.createdAt),
                  onPressed: () {
                    onOpen(entry.$1);
                  },
                ),
              ),
          ],
        );
      },
    );
  }
}

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({
    required this.notification,
    required this.icon,
    required this.iconColor,
    required this.formattedDate,
    required this.onPressed,
  });

  final NotificationDto notification;
  final IconData icon;
  final Color iconColor;
  final String formattedDate;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return AppTappableCard(
      padding: const EdgeInsets.all(16),
      radius: AppRadii.lg,
      onPressed: onPressed,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: iconColor.withValues(alpha: 0.12),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: iconColor, size: 18),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        notification.title,
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: notification.isRead
                              ? FontWeight.w600
                              : FontWeight.w800,
                          color: AppColors.foreground,
                        ),
                      ),
                    ),
                    if (!notification.isRead)
                      Container(
                        width: 8,
                        height: 8,
                        decoration: const BoxDecoration(
                          color: AppColors.primary,
                          shape: BoxShape.circle,
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  notification.content,
                  style: AppTextStyles.muted(
                    context,
                  ).copyWith(fontSize: 14, height: 1.4),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 8),
                Text(
                  formattedDate,
                  style: AppTextStyles.label(
                    context,
                  ).copyWith(fontWeight: FontWeight.w500),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
