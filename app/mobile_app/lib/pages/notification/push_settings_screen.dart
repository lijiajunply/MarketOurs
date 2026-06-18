import 'package:flutter/cupertino.dart';

import 'package:mobile_app/l10n/app_localizations.dart';
import '../../models/notification.dart';
import '../../services/notification_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
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
      if (success) {
        await AppFeedback.showSuccess(
          context,
          message: AppLocalizations.of(context)!.notificationSaved,
        );
      } else {
        await AppFeedback.showError(
          context,
          message: AppLocalizations.of(context)!.notificationSaveFailed,
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: AppLocalizations.of(context)!.notificationPushSettings,
      navigationBarStyle: AppNavigationBarStyle.compact,
      maxContentWidth: AppResponsive.readableMaxWidth(context, fallback: 720),
      child: _isLoading
          ? const Center(child: CupertinoActivityIndicator())
          : Column(
              children: [
                _buildSwitchTile(
                  title: AppLocalizations.of(context)!.notificationEmail,
                  subtitle: AppLocalizations.of(context)!.notificationEmailDesc,
                  value: _emailEnabled,
                  onChanged: (val) => setState(() => _emailEnabled = val),
                  icon: CupertinoIcons.mail,
                ),
                const SizedBox(height: 12),
                _buildSwitchTile(
                  title: AppLocalizations.of(context)!.notificationCommentPush,
                  subtitle: AppLocalizations.of(context)!.notificationCommentPushDesc,
                  value: _commentReplyEnabled,
                  onChanged: (val) =>
                      setState(() => _commentReplyEnabled = val),
                  icon: CupertinoIcons.chat_bubble_text,
                ),
                const SizedBox(height: 12),
                _buildSwitchTile(
                  title: AppLocalizations.of(context)!.notificationHotListPush,
                  subtitle: AppLocalizations.of(context)!.notificationHotListPushDesc,
                  value: _hotListEnabled,
                  onChanged: (val) => setState(() => _hotListEnabled = val),
                  icon: CupertinoIcons.flame,
                ),
                const SizedBox(height: 28),
                AppPrimaryButton(
                  onPressed: _isSaving ? null : _saveSettings,
                  child: Text(
                    _isSaving
                        ? AppLocalizations.of(context)!.profileSaving
                        : AppLocalizations.of(context)!.notificationSaveSettings,
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
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: CupertinoDynamicColor.resolve(AppColors.secondary, context),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Row(
        children: [
          Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              color: CupertinoDynamicColor.resolve(
                AppColors.secondary,
                context,
              ).withValues(alpha: 0.5),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(
              icon,
              size: 20,
              color: CupertinoDynamicColor.resolve(AppColors.primary, context),
            ),
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
                  style: TextStyle(
                    color: CupertinoDynamicColor.resolve(
                      AppColors.mutedForeground,
                      context,
                    ),
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
