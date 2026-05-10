import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_widgets.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final user = authState?.user;
    final isSubmitting = authState?.isSubmitting ?? false;

    if (user == null) {
      return AppPageScaffold(
        title: '我的',
        child: Center(
          child: AppPrimaryButton(
            onPressed: () => context.go(AppRoutePaths.login),
            child: const Text('去登录'),
          ),
        ),
      );
    }

    return AppPageScaffold(
      title: '我的',
      child: RefreshIndicator(
        onRefresh: () async {
          await ref.read(authControllerProvider.notifier).refreshProfile();
        },
        child: ListView(
          physics: const BouncingScrollPhysics(
            parent: AlwaysScrollableScrollPhysics(),
          ),
          padding: const EdgeInsets.only(bottom: 24),
          children: [
            _ProfileHero(
              user: user,
              onEdit: () => _openEditSheet(context, ref, user),
              onViewPublicProfile: () =>
                  context.push(buildPublicProfileLocation(user.id)),
            ),
            const SizedBox(height: 16),
            _ProfileCard(
              title: '资料信息',
              children: [
                _InfoRow(label: '昵称', value: _fallback(user.name, '未设置')),
                _InfoRow(label: '简介', value: _fallback(user.info, '还没有写简介')),
                _InfoRow(label: '邮箱', value: _fallback(user.email, '未绑定')),
                _VerificationRow(
                  label: '邮箱验证',
                  isVerified: user.isEmailVerified ?? false,
                  actionLabel: '发送邮箱验证码',
                  isBusy: isSubmitting,
                  onVerify: () => _startVerification(
                    context: context,
                    ref: ref,
                    sendCode: () => ref
                        .read(authControllerProvider.notifier)
                        .sendEmailCode(),
                    verifyCode: (code) => ref
                        .read(authControllerProvider.notifier)
                        .verifyEmailCode(code: code),
                    successMessage: '邮箱验证成功',
                  ),
                ),
                _InfoRow(label: '手机号', value: _fallback(user.phone, '未绑定')),
                _VerificationRow(
                  label: '手机号验证',
                  isVerified: user.isPhoneVerified ?? false,
                  actionLabel: '发送手机验证码',
                  isBusy: isSubmitting,
                  onVerify: () => _startVerification(
                    context: context,
                    ref: ref,
                    sendCode: () => ref
                        .read(authControllerProvider.notifier)
                        .sendPhoneCode(),
                    verifyCode: (code) => ref
                        .read(authControllerProvider.notifier)
                        .verifyPhone(code: code),
                    successMessage: '手机号验证成功',
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            _ProfileCard(
              title: '账号与安全',
              children: [
                _NavRow(
                  icon: CupertinoIcons.lock_rotation,
                  title: '修改密码',
                  subtitle: '更新当前账号密码',
                  onTap: () => context.push(AppRoutePaths.changePassword),
                ),
                _InfoRow(label: '角色', value: _fallback(user.role, 'User')),
              ],
            ),
            const SizedBox(height: 16),
            _ProfileCard(
              title: '登录相关',
              children: [
                _NavRow(
                  icon: CupertinoIcons.square_arrow_right,
                  title: '退出登录',
                  subtitle: '清除当前会话',
                  destructive: true,
                  onTap: isSubmitting
                      ? null
                      : () => ref.read(authControllerProvider.notifier).logout(),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openEditSheet(
    BuildContext context,
    WidgetRef ref,
    UserDto user,
  ) async {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (context) => _ProfileEditSheet(initialUser: user),
    );
  }

  Future<void> _startVerification({
    required BuildContext context,
    required WidgetRef ref,
    required Future<void> Function() sendCode,
    required Future<void> Function(String code) verifyCode,
    required String successMessage,
  }) async {
    final codeController = TextEditingController();

    try {
      await sendCode();
      if (!context.mounted) {
        return;
      }
      await AppFeedback.showMessage(context, message: '验证码已发送');
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && context.mounted) {
        await AppFeedback.showMessage(context, message: errorMessage);
      }
      return;
    }

    if (!context.mounted) {
      return;
    }

    final shouldVerify = await showCupertinoDialog<bool>(
      context: context,
      builder: (context) {
        return CupertinoAlertDialog(
          title: const Text('输入验证码'),
          content: Padding(
            padding: const EdgeInsets.only(top: 8),
            child: TextField(
              controller: codeController,
              decoration: const InputDecoration(
                hintText: '请输入收到的验证码',
              ),
            ),
          ),
          actions: [
            CupertinoDialogAction(
              onPressed: () => Navigator.of(context).pop(false),
              child: const Text('取消'),
            ),
            CupertinoDialogAction(
              onPressed: () => Navigator.of(context).pop(true),
              child: const Text('确认验证'),
            ),
          ],
        );
      },
    );

    if (shouldVerify != true) {
      return;
    }

    try {
      await verifyCode(codeController.text.trim());
      if (!context.mounted) {
        return;
      }
      await AppFeedback.showMessage(context, message: successMessage);
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && context.mounted) {
        await AppFeedback.showMessage(context, message: errorMessage);
      }
    } finally {
      codeController.dispose();
    }
  }

  String _fallback(String? value, String fallback) {
    final trimmed = value?.trim();
    return trimmed == null || trimmed.isEmpty ? fallback : trimmed;
  }
}

class _ProfileHero extends StatelessWidget {
  const _ProfileHero({
    required this.user,
    required this.onEdit,
    required this.onViewPublicProfile,
  });

  final UserDto user;
  final VoidCallback onEdit;
  final VoidCallback onViewPublicProfile;

  @override
  Widget build(BuildContext context) {
    return AppSectionCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          CircleAvatar(
            radius: 34,
            backgroundColor: const Color(0xFFF2F2F7),
            backgroundImage: user.avatar?.trim().isNotEmpty == true
                ? NetworkImage(user.avatar!.trim())
                : null,
            child: user.avatar?.trim().isNotEmpty == true
              ? null
                : Text(
                    _buildInitial(user.name),
                    style: const TextStyle(
                      color: Color(0xFF007AFF),
                      fontWeight: FontWeight.w800,
                    ),
                  ),
          ),
          const SizedBox(height: 16),
          Text(
            user.name?.trim().isNotEmpty == true ? user.name!.trim() : '未设置昵称',
            style: Theme.of(
              context,
            ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          Text(
            user.info?.trim().isNotEmpty == true
                ? user.info!.trim()
                : '这个人很低调，还没有写简介。',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: Colors.grey.shade600,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            children: [
              AppPrimaryButton(
                onPressed: onEdit,
                child: const Text('编辑资料'),
              ),
              AppSecondaryButton(
                onPressed: onViewPublicProfile,
                child: const Text('查看公开主页'),
              ),
            ],
          ),
        ],
      ),
    );
  }

  String _buildInitial(String? name) {
    final trimmed = name?.trim();
    if (trimmed == null || trimmed.isEmpty) {
      return '我';
    }
    return trimmed.substring(0, 1);
  }
}

class _ProfileCard extends StatelessWidget {
  const _ProfileCard({required this.title, required this.children});

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
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
          Text(
            title,
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
              fontWeight: FontWeight.w700,
              color: Colors.grey.shade500,
              letterSpacing: 0.5,
            ),
          ),
          const SizedBox(height: 4),
          ...children,
        ],
      ),
    );
  }
}

class _NavRow extends StatelessWidget {
  const _NavRow({
    required this.icon,
    required this.title,
    required this.subtitle,
    this.destructive = false,
    this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final bool destructive;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final color = destructive
        ? CupertinoColors.systemRed
        : CupertinoColors.activeBlue;
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: Icon(icon, color: color),
      title: Text(
        title,
        style: TextStyle(fontWeight: FontWeight.w600, color: color),
      ),
      subtitle: Text(subtitle),
      trailing: const Icon(
        CupertinoIcons.chevron_right,
        size: 16,
        color: CupertinoColors.systemGrey2,
      ),
      onTap: onTap,
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text(value),
    );
  }
}

class _VerificationRow extends StatelessWidget {
  const _VerificationRow({
    required this.label,
    required this.isVerified,
    required this.actionLabel,
    required this.isBusy,
    required this.onVerify,
  });

  final String label;
  final bool isVerified;
  final String actionLabel;
  final bool isBusy;
  final VoidCallback onVerify;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text(isVerified ? '已验证' : '未验证'),
      trailing: isVerified
          ? const Icon(Icons.verified_rounded, color: Color(0xFF16A34A))
          : TextButton(
              onPressed: isBusy ? null : onVerify,
              child: Text(actionLabel),
            ),
    );
  }
}

class _ProfileEditSheet extends ConsumerStatefulWidget {
  const _ProfileEditSheet({required this.initialUser});

  final UserDto initialUser;

  @override
  ConsumerState<_ProfileEditSheet> createState() => _ProfileEditSheetState();
}

class _ProfileEditSheetState extends ConsumerState<_ProfileEditSheet> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _nameController;
  late final TextEditingController _infoController;
  late final TextEditingController _avatarController;
  late final TextEditingController _emailController;
  late final TextEditingController _phoneController;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(
      text: widget.initialUser.name ?? '',
    );
    _infoController = TextEditingController(
      text: widget.initialUser.info ?? '',
    );
    _avatarController = TextEditingController(
      text: widget.initialUser.avatar ?? '',
    );
    _emailController = TextEditingController(
      text: widget.initialUser.email ?? '',
    );
    _phoneController = TextEditingController(
      text: widget.initialUser.phone ?? '',
    );
  }

  @override
  void dispose() {
    _nameController.dispose();
    _infoController.dispose();
    _avatarController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final messenger = ScaffoldMessenger.of(context);
    if (!_formKey.currentState!.validate()) {
      return;
    }

    try {
      await ref
          .read(authControllerProvider.notifier)
          .updateProfile(
            UserUpdateDto(
              name: _nameController.text.trim(),
              info: _infoController.text.trim(),
              avatar: _avatarController.text.trim(),
              email: _emailController.text.trim().isEmpty
                  ? null
                  : _emailController.text.trim(),
              phone: _phoneController.text.trim().isEmpty
                  ? null
                  : _phoneController.text.trim(),
            ),
          );
      if (!mounted) {
        return;
      }
      messenger.showSnackBar(const SnackBar(content: Text('个人资料已更新')));
      Navigator.of(context).pop();
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && mounted) {
        messenger.showSnackBar(SnackBar(content: Text(errorMessage)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = authState?.isSubmitting ?? false;

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.of(context).viewInsets.bottom + 20,
      ),
      child: Form(
        key: _formKey,
        child: ListView(
          shrinkWrap: true,
          children: [
            Text(
              '编辑资料',
              style: Theme.of(
                context,
              ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _nameController,
              decoration: const InputDecoration(labelText: '昵称'),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入昵称';
                }
                return null;
              },
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _infoController,
              maxLines: 3,
              decoration: const InputDecoration(
                labelText: '简介',
                hintText: '介绍一下自己',
                alignLabelWithHint: true,
              ),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _avatarController,
              decoration: const InputDecoration(
                labelText: '头像链接',
                hintText: '可选，填写图片 URL',
              ),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _emailController,
              decoration: const InputDecoration(labelText: '邮箱'),
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _phoneController,
              decoration: const InputDecoration(labelText: '手机号'),
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '保存中...' : '保存修改'),
            ),
          ],
        ),
      ),
    );
  }
}
