import 'dart:async';

import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../services/error_messages.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class BindingScreen extends ConsumerWidget {
  const BindingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final user = authState?.user;

    return AppPageScaffold(
      title: '第三方绑定',
      navigationBarStyle: AppNavigationBarStyle.compact,
      slivers: [
        CupertinoSliverRefreshControl(
          onRefresh: () =>
              ref.read(authControllerProvider.notifier).refreshProfile(),
        ),
        SliverToBoxAdapter(
          child: AppResponsiveCenter(
            padding: AppResponsive.sliverPagePadding(context, bottom: 32),
            child: _BindingSection(user: user),
          ),
        ),
      ],
    );
  }
}

class _BindingSection extends StatelessWidget {
  const _BindingSection({required this.user});

  final UserDto? user;

  @override
  Widget build(BuildContext context) {
    final providers = [
      _ProviderInfo(
        name: 'Ours',
        icon: CupertinoIcons.person_circle,
        isBound: user?.oursId != null,
        user: user,
      ),
      _ProviderInfo(
        name: 'Github',
        icon: CupertinoIcons.cloud,
        isBound: user?.githubId != null,
        user: user,
      ),
      _ProviderInfo(
        name: 'Google',
        icon: CupertinoIcons.globe,
        isBound: user?.googleId != null,
        user: user,
      ),
      _ProviderInfo(
        name: 'Weixin',
        icon: CupertinoIcons.chat_bubble_text,
        isBound: user?.weixinId != null,
        user: user,
      ),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 8, bottom: 12),
          child: Text(
            '将您的账号与第三方平台关联，方便快速登录',
            style: AppTextStyles.muted(context),
          ),
        ),
        AppTappableCard(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          radius: AppRadii.lg,
          child: Column(
            children: [
              for (int i = 0; i < providers.length; i++) ...[
                if (i > 0)
                  Container(
                    height: 1,
                    margin: const EdgeInsets.only(left: 44),
                    color: CupertinoDynamicColor.resolve(
                      AppColors.border,
                      context,
                    ).withValues(alpha: 0.3),
                  ),
                _BindingRow(
                  provider: providers[i],
                  onBind: () => _handleBind(context, providers[i].name),
                  onUnbind: () => _handleUnbind(context, providers[i]),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }

  static void _handleBind(BuildContext context, String provider) {
    final location = Uri(
      path: AppRoutePaths.oauthWebView,
      queryParameters: {
        'provider': provider,
        'purpose': 'bind',
        '_ts': DateTime.now().microsecondsSinceEpoch.toString(),
      },
    ).toString();
    context.push(location);
  }

  static Future<void> _handleUnbind(
    BuildContext context,
    _ProviderInfo provider,
  ) async {
    final currentUser = provider.user;
    if (currentUser == null) {
      await AppFeedback.showError(context, message: '请先登录');
      return;
    }

    if (!_hasText(currentUser.email) && !_hasText(currentUser.phone)) {
      await AppFeedback.showError(context, message: '请先绑定邮箱或手机号后再解绑');
      return;
    }

    final confirmed = await AppFeedback.confirm(
      context,
      title: '解绑 ${provider.name}',
      message: '解绑前需要完成本次邮箱或手机验证码校验。',
      confirmText: '继续',
      destructive: true,
    );
    if (confirmed != true || !context.mounted) return;

    final success = await showCupertinoDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (_) =>
          _UnbindThirdPartyDialog(provider: provider.name, user: currentUser),
    );

    if (success == true && context.mounted) {
      await AppFeedback.showSuccess(context, message: '${provider.name} 已解绑');
    }
  }

  static bool _hasText(String? value) =>
      value != null && value.trim().isNotEmpty;
}

class _ProviderInfo {
  final String name;
  final IconData icon;
  final bool isBound;
  final UserDto? user;

  const _ProviderInfo({
    required this.name,
    required this.icon,
    required this.isBound,
    required this.user,
  });
}

class _BindingRow extends StatelessWidget {
  const _BindingRow({
    required this.provider,
    required this.onBind,
    required this.onUnbind,
  });

  final _ProviderInfo provider;
  final VoidCallback onBind;
  final VoidCallback onUnbind;

  @override
  Widget build(BuildContext context) {
    return AppListTile(
      padding: const EdgeInsets.symmetric(vertical: 12),
      leading: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: provider.isBound
              ? const Color(0xFF34C759).withValues(alpha: 0.12)
              : AppColors.primary.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Icon(
          provider.icon,
          size: 20,
          color: provider.isBound ? const Color(0xFF34C759) : AppColors.primary,
        ),
      ),
      title: Text(
        provider.name,
        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
      ),
      subtitle: provider.isBound ? const Text('已绑定') : null,
      trailing: provider.isBound
          ? CupertinoButton(
              padding: EdgeInsets.zero,
              minimumSize: Size.zero,
              onPressed: onUnbind,
              child: const Text(
                '解绑',
                style: TextStyle(
                  color: AppColors.destructive,
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                ),
              ),
            )
          : CupertinoButton(
              padding: EdgeInsets.zero,
              minimumSize: Size.zero,
              onPressed: onBind,
              child: const Text(
                '去绑定',
                style: TextStyle(
                  color: AppColors.primary,
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
    );
  }
}

class _VerificationChannel {
  const _VerificationChannel({
    required this.id,
    required this.label,
    required this.destination,
    required this.icon,
  });

  final String id;
  final String label;
  final String destination;
  final IconData icon;
}

class _UnbindThirdPartyDialog extends ConsumerStatefulWidget {
  const _UnbindThirdPartyDialog({required this.provider, required this.user});

  final String provider;
  final UserDto user;

  @override
  ConsumerState<_UnbindThirdPartyDialog> createState() =>
      _UnbindThirdPartyDialogState();
}

class _UnbindThirdPartyDialogState
    extends ConsumerState<_UnbindThirdPartyDialog> {
  final _codeController = TextEditingController();
  Timer? _timer;
  int _countdown = 0;
  bool _isSending = false;
  bool _isSubmitting = false;
  String? _errorMessage;
  late final List<_VerificationChannel> _channels;
  _VerificationChannel? _selectedChannel;

  @override
  void initState() {
    super.initState();
    _channels = [
      if (_hasText(widget.user.email))
        _VerificationChannel(
          id: 'email',
          label: '邮箱',
          destination: widget.user.email!,
          icon: CupertinoIcons.mail,
        ),
      if (_hasText(widget.user.phone))
        _VerificationChannel(
          id: 'phone',
          label: '手机',
          destination: widget.user.phone!,
          icon: CupertinoIcons.phone,
        ),
    ];
    _selectedChannel = _channels.isEmpty ? null : _channels.first;
  }

  @override
  void dispose() {
    _timer?.cancel();
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _sendCode() async {
    final channel = _selectedChannel;
    if (channel == null || _isSending || _countdown > 0) return;

    setState(() {
      _isSending = true;
      _errorMessage = null;
    });

    try {
      final notifier = ref.read(authControllerProvider.notifier);
      if (channel.id == 'email') {
        await notifier.sendEmailCode(purpose: 'unbind-third-party');
      } else {
        await notifier.sendPhoneCode();
      }
      _startCountdown();
      if (mounted) {
        await AppFeedback.showSuccess(context, message: '验证码已发送');
      }
    } catch (error) {
      if (mounted) {
        setState(() => _errorMessage = extractErrorFromException(error));
      }
    } finally {
      if (mounted) {
        setState(() => _isSending = false);
      }
    }
  }

  Future<void> _submit() async {
    final channel = _selectedChannel;
    final code = _codeController.text.trim();
    if (channel == null || code.isEmpty || _isSubmitting) {
      setState(() => _errorMessage = '请输入验证码');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(authControllerProvider.notifier)
          .unbindThirdParty(
            provider: widget.provider,
            channel: channel.id,
            code: code,
          );
      if (mounted) {
        Navigator.of(context).pop(true);
      }
    } catch (error) {
      if (mounted) {
        setState(() => _errorMessage = extractErrorFromException(error));
      }
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _startCountdown() {
    _timer?.cancel();
    setState(() => _countdown = 60);
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_countdown <= 1) {
        timer.cancel();
        if (mounted) setState(() => _countdown = 0);
        return;
      }
      if (mounted) setState(() => _countdown -= 1);
    });
  }

  @override
  Widget build(BuildContext context) {
    final channel = _selectedChannel;

    return CupertinoAlertDialog(
      title: Text('解绑 ${widget.provider}'),
      content: Padding(
        padding: const EdgeInsets.only(top: 12),
        child: Column(
          children: [
            Text(
              '选择邮箱或手机接收验证码，验证通过后立即解绑。',
              style: AppTextStyles.muted(context),
            ),
            const SizedBox(height: 14),
            if (_channels.length > 1)
              CupertinoSlidingSegmentedControl<String>(
                groupValue: channel?.id,
                children: {
                  for (final item in _channels)
                    item.id: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 6),
                      child: Text(item.label),
                    ),
                },
                onValueChanged: (value) {
                  if (_isSubmitting || _isSending || value == null) return;
                  final next = _channels.firstWhere(
                    (item) => item.id == value,
                    orElse: () => _channels.first,
                  );
                  setState(() {
                    _selectedChannel = next;
                    _errorMessage = null;
                  });
                },
              ),
            if (channel != null) ...[
              const SizedBox(height: 12),
              Row(
                children: [
                  Icon(channel.icon, size: 16, color: AppColors.primary),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      channel.destination,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.muted(context),
                    ),
                  ),
                ],
              ),
            ],
            const SizedBox(height: 12),
            AppTextField(
              controller: _codeController,
              placeholder: '6 位验证码',
              keyboardType: TextInputType.number,
              textInputAction: TextInputAction.done,
              onFieldSubmitted: (_) => _submit(),
              suffix: CupertinoButton(
                padding: EdgeInsets.zero,
                minimumSize: Size.zero,
                onPressed: _isSending || _countdown > 0 || channel == null
                    ? null
                    : _sendCode,
                child: Text(
                  _countdown > 0
                      ? '${_countdown}s'
                      : _isSending
                      ? '发送中'
                      : '发送',
                  style: const TextStyle(
                    color: AppColors.primary,
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ),
            if (_errorMessage != null) ...[
              const SizedBox(height: 10),
              Text(
                _errorMessage!,
                style: const TextStyle(
                  color: AppColors.destructive,
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ],
        ),
      ),
      actions: [
        CupertinoDialogAction(
          onPressed: _isSubmitting
              ? null
              : () => Navigator.of(context).pop(false),
          child: const Text('取消'),
        ),
        CupertinoDialogAction(
          isDestructiveAction: true,
          onPressed: _isSubmitting ? null : _submit,
          child: Text(_isSubmitting ? '解绑中...' : '确认解绑'),
        ),
      ],
    );
  }

  static bool _hasText(String? value) =>
      value != null && value.trim().isNotEmpty;
}
