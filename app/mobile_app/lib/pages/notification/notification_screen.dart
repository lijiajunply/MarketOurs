import 'package:flutter/cupertino.dart';
import 'package:go_router/go_router.dart';

import '../../models/notification.dart';
import '../../router/app_router.dart';
import '../../services/notification_service.dart';
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
    final result = await widget.service.getNotifications();
    if (result != null) {
      setState(() {
        _notifications = result.items;
      });
    }
    setState(() => _isLoading = false);
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
        return const Color(0xFF0A84FF);
      case NotificationType.postReply:
        return const Color(0xFF34C759);
      case NotificationType.hotList:
        return const Color(0xFFFF9500);
      case NotificationType.system:
        return CupertinoColors.systemGrey;
      case NotificationType.review:
        return const Color(0xFFAF52DE);
      case NotificationType.unknown:
        return CupertinoColors.systemGrey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: '通知',
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
                    await widget.service.markAllAsRead();
                    _loadNotifications();
                  },
                  child: const Text('全部标为已读'),
                ),
                CupertinoActionSheetAction(
                  onPressed: () {
                    Navigator.of(context).pop();
                    Navigator.push(
                      context,
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
        child: const Icon(CupertinoIcons.ellipsis_circle),
      ),
      child: _isLoading && _notifications.isEmpty
          ? const Center(child: CupertinoActivityIndicator())
          : _notifications.isEmpty
          ? _buildEmptyState()
          : CustomScrollView(
              physics: const BouncingScrollPhysics(
                parent: AlwaysScrollableScrollPhysics(),
              ),
              slivers: [
                CupertinoSliverRefreshControl(onRefresh: _loadNotifications),
                SliverPadding(
                  padding: const EdgeInsets.only(bottom: 24),
                  sliver: SliverList.builder(
                    itemCount: _notifications.length,
                    itemBuilder: (context, index) {
                      final n = _notifications[index];
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: AppTappableCard(
                          onPressed: () async {
                            if (!n.isRead) {
                              await widget.service.markAsRead(n.id);
                              setState(() {
                                _notifications[index] = NotificationDto(
                                  id: n.id,
                                  userId: n.userId,
                                  title: n.title,
                                  content: n.content,
                                  type: n.type,
                                  targetId: n.targetId,
                                  isRead: true,
                                  createdAt: n.createdAt,
                                );
                              });
                            }

                            if (!context.mounted) {
                              return;
                            }

                            if (n.targetId?.isNotEmpty == true) {
                              context.push(buildPostDetailLocation(n.targetId!));
                            }
                          },
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Container(
                                width: 44,
                                height: 44,
                                decoration: BoxDecoration(
                                  color: _getIconColor(
                                    n.type,
                                  ).withValues(alpha: 0.12),
                                  borderRadius: BorderRadius.circular(14),
                                ),
                                child: Icon(
                                  _getIcon(n.type),
                                  color: _getIconColor(n.type),
                                  size: 20,
                                ),
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
                                            n.title,
                                            style: TextStyle(
                                              fontSize: 15,
                                              fontWeight: n.isRead
                                                  ? FontWeight.w500
                                                  : FontWeight.w700,
                                            ),
                                          ),
                                        ),
                                        if (!n.isRead)
                                          Container(
                                            width: 8,
                                            height: 8,
                                            decoration: const BoxDecoration(
                                              color: Color(0xFF0A84FF),
                                              shape: BoxShape.circle,
                                            ),
                                          ),
                                      ],
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      n.content,
                                      style: const TextStyle(
                                        fontSize: 14,
                                        color: Color(0xFF6B7280),
                                        height: 1.4,
                                      ),
                                      maxLines: 2,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                    const SizedBox(height: 6),
                                    Text(
                                      _formatDate(n.createdAt),
                                      style: const TextStyle(
                                        fontSize: 12,
                                        color: CupertinoColors.systemGrey,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(
            CupertinoIcons.bell_slash,
            size: 64,
            color: CupertinoColors.systemGrey4,
          ),
          const SizedBox(height: 16),
          Text(
            '暂无通知',
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: CupertinoColors.systemGrey,
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
