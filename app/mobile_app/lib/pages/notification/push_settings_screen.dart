import 'package:flutter/material.dart';
import '../../models/notification.dart';
import '../../services/notification_service.dart';

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
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(success ? '保存成功' : '保存失败')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('推送设置')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _buildSwitchTile(
                  title: '邮件通知',
                  subtitle: '当收到新回复或系统通知时发送邮件',
                  value: _emailEnabled,
                  onChanged: (val) => setState(() => _emailEnabled = val),
                  icon: Icons.email_outlined,
                ),
                const Divider(),
                _buildSwitchTile(
                  title: '评论回复推送',
                  subtitle: '当有人回复您的贴子或评论时推送',
                  value: _commentReplyEnabled,
                  onChanged: (val) =>
                      setState(() => _commentReplyEnabled = val),
                  icon: Icons.reply_all,
                ),
                const Divider(),
                _buildSwitchTile(
                  title: '每日热榜推送',
                  subtitle: '每天早晨接收校园最热贴子精选',
                  value: _hotListEnabled,
                  onChanged: (val) => setState(() => _hotListEnabled = val),
                  icon: Icons.whatshot_outlined,
                ),
                const SizedBox(height: 32),
                ElevatedButton.icon(
                  onPressed: _isSaving ? null : _saveSettings,
                  icon: _isSaving
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.save),
                  label: const Text('保存设置'),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
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
    return SwitchListTile(
      secondary: Icon(icon),
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
      subtitle: Text(subtitle),
      value: value,
      onChanged: onChanged,
    );
  }
}
