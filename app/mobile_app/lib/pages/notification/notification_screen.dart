import 'package:flutter/material.dart';
import '../../models/notification.dart';
import '../../services/notification_service.dart';

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
        return Icons.reply;
      case NotificationType.postReply:
        return Icons.comment;
      case NotificationType.hotList:
        return Icons.whatshot;
      case NotificationType.system:
        return Icons.notifications;
    }
  }

  Color _getIconColor(NotificationType type) {
    switch (type) {
      case NotificationType.commentReply:
        return Colors.blue;
      case NotificationType.postReply:
        return Colors.green;
      case NotificationType.hotList:
        return Colors.orange;
      case NotificationType.system:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('通知中心'),
        actions: [
          IconButton(
            icon: const Icon(Icons.done_all),
            onPressed: () async {
              await widget.service.markAllAsRead();
              _loadNotifications();
            },
          )
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadNotifications,
        child: _isLoading && _notifications.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : _notifications.isEmpty
                ? const Center(child: Text('暂无通知'))
                : ListView.builder(
                    itemCount: _notifications.length,
                    itemBuilder: (context, index) {
                      final n = _notifications[index];
                      return ListTile(
                        leading: CircleAvatar(
                          backgroundColor: _getIconColor(n.type).withOpacity(0.1),
                          child: Icon(_getIcon(n.type), color: _getIconColor(n.type)),
                        ),
                        title: Text(
                          n.title,
                          style: TextStyle(
                            fontWeight: n.isRead ? FontWeight.normal : FontWeight.bold,
                          ),
                        ),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(n.content, maxLines: 2, overflow: TextOverflow.ellipsis),
                            const SizedBox(height: 4),
                            Text(
                              n.createdAt.toString().split('.')[0],
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ],
                        ),
                        onTap: () async {
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
                          // Navigate to post detail if targetId exists
                        },
                      );
                    },
                  ),
      ),
    );
  }
}
