import 'package:flutter/cupertino.dart';
import '../../models/notification.dart';
import '../../services/notification_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_widgets.dart';

class PushSettingsScreen extends StatefulWidget {
  final NotificationService service;

  const PushSettingsScreen({super.key, required this.service});

  @override
  State<PushSettingsScreen> createState() => _PushSettingsScreenState();
}

class _PushSettingsScreenState extends State<PushSettingsScreen> {
  bool _isLoading = true;
  bool _isSaving = false;

  bool _emailEnabled = true;
  bool _hotListEnabled = true;
  bool _commentReplyEnabled = true;

  @override
  void initState() {
    super.initState();
    _loadSettings();
  }

  Future<void> _loadSettings() async {
    setState(() => _isLoading = true);
    final settings = await widget.service.getSettings();
    if (settings != null) {
      setState(() {
        _emailEnabled = settings.enableEmailNotifications;
        _hotListEnabled = settings.enableHotListPush;
        _commentReplyEnabled = settings.enableCommentReplyPush;
      });
    }
    setState(() => _isLoading = false);
  }

  Future<void> _saveSettings() async {
    setState(() => _isSaving = true);
    final success = await widget.service.updateSettings(
      PushSettingsDto(
        enableEmailNotifications: _emailEnabled,
        enableHotListPush: _hotListEnabled,
        enableCommentReplyPush: _commentReplyEnabled,
      ),
    );
    setState(() => _isSaving = false);

    if (mounted) {
      await AppFeedback.showMessage(
        context,
        message: success ? '保存成功' : '保存失败',
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: '推送设置',
      child: _isLoading
          ? const Center(child: CupertinoActivityIndicator())
          : Column(
              children: [
                _buildSwitchTile(
                  title: '邮件通知',
                  subtitle: '当收到新回复或系统通知时发送邮件',
                  value: _emailEnabled,
                  onChanged: (val) => setState(() => _emailEnabled = val),
                  icon: CupertinoIcons.mail,
                ),
                const SizedBox(height: 12),
                _buildSwitchTile(
                  title: '评论回复推送',
                  subtitle: '当有人回复您的贴子或评论时推送',
                  value: _commentReplyEnabled,
                  onChanged: (val) =>
                      setState(() => _commentReplyEnabled = val),
                  icon: CupertinoIcons.chat_bubble_text,
                ),
                const SizedBox(height: 12),
                _buildSwitchTile(
                  title: '每日热榜推送',
                  subtitle: '每天早晨接收校园最热贴子精选',
                  value: _hotListEnabled,
                  onChanged: (val) => setState(() => _hotListEnabled = val),
                  icon: CupertinoIcons.flame,
                ),
                const SizedBox(height: 28),
                AppPrimaryButton(
                  onPressed: _isSaving ? null : _saveSettings,
                  child: Text(_isSaving ? '保存中...' : '保存设置'),
                ),
              ],
            ),
    );
  }

  Widget _buildSwitchTile({
    required String title,
    required String subtitle,
    required bool value,
    required ValueChanged<bool> onChanged,
    required IconData icon,
  }) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: CupertinoColors.secondarySystemGroupedBackground,
        borderRadius: BorderRadius.circular(18),
      ),
      child: Row(
        children: [
          Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              color: CupertinoColors.systemGrey6,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, size: 20, color: const Color(0xFF007AFF)),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 4),
                Text(
                  subtitle,
                  style: const TextStyle(
                    color: CupertinoColors.systemGrey,
                    fontSize: 13,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
          CupertinoSwitch(value: value, onChanged: onChanged),
        ],
      ),
    );
  }
}
