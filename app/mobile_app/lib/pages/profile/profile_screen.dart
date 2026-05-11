import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final user = authState?.user;
    final isSubmitting = authState?.isSubmitting ?? false;

    if (user == null) {
      return CupertinoPageScaffold(
        backgroundColor: AppColors.background,
        child: CustomScrollView(
          slivers: [
            const CupertinoSliverNavigationBar(largeTitle: Text('我的'), border: null),
            SliverFillRemaining(
              child: Center(
                child: AppEmptyState(
                  icon: CupertinoIcons.person,
                  title: '还没有登录',
                  description: '登录后可以查看个人资料、管理安全设置。',
                  action: AppPrimaryButton(
                    onPressed: () => context.go(AppRoutePaths.login),
                    child: const Text('去登录'),
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    }

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: CustomScrollView(
        physics: const BouncingScrollPhysics(
          parent: AlwaysScrollableScrollPhysics(),
        ),
        slivers: [
          CupertinoSliverNavigationBar(
            largeTitle: const Text('我的'),
            backgroundColor: AppColors.background.withValues(alpha: 0.94),
            border: null,
            trailing: CupertinoButton(
              padding: EdgeInsets.zero,
              onPressed: () => _openEditSheet(context, ref, user),
              child: const Icon(CupertinoIcons.pencil_circle, size: 24),
            ),
          ),
          CupertinoSliverRefreshControl(
            onRefresh: () =>
                ref.read(authControllerProvider.notifier).refreshProfile(),
          ),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Column(
                children: [
                  const SizedBox(height: 12),
                  _ProfileHero(
                    user: user,
                    onViewPublicProfile: () =>
                        context.push(buildPublicProfileLocation(user.id)),
                  ),
                  const SizedBox(height: 20),
                  _ProfileSection(
                    title: '资料信息',
                    children: [
                      _InfoRow(label: '昵称', value: _fallback(user.name, '未设置')),
                      _InfoRow(
                        label: '简介',
                        value: _fallback(user.info, '还没有写简介'),
                      ),
                      _InfoRow(label: '邮箱', value: _fallback(user.email, '未绑定')),
                      _VerificationRow(
                        label: '邮箱验证',
                        isVerified: user.isEmailVerified ?? false,
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
                    ],
                  ),
                  const SizedBox(height: 16),
                  _ProfileSection(
                    title: '账户安全',
                    children: [
                      _NavRow(
                        icon: CupertinoIcons.lock_rotation,
                        title: '修改密码',
                        subtitle: '更新当前账号密码',
                        onTap: () => context.push(AppRoutePaths.changePassword),
                      ),
                      _NavRow(
                        icon: CupertinoIcons.square_arrow_right,
                        title: '退出登录',
                        subtitle: '清除当前会话',
                        destructive: true,
                        onTap: isSubmitting
                            ? null
                            : () => ref
                                  .read(authControllerProvider.notifier)
                                  .logout(),
                      ),
                    ],
                  ),
                  const SizedBox(height: 32),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _openEditSheet(
    BuildContext context,
    WidgetRef ref,
    UserDto user,
  ) async {
    await showAppBottomSheet<void>(
      context: context,
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
      if (!context.mounted) return;
      await AppFeedback.showMessage(context, message: '验证码已发送');
    } catch (_) {
      final errorMessage = ref.read(authControllerProvider).asData?.value.errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && context.mounted) {
        await AppFeedback.showMessage(context, message: errorMessage);
      }
      codeController.dispose();
      return;
    }

    if (!context.mounted) {
      codeController.dispose();
      return;
    }

    final shouldVerify = await showAppBottomSheet<bool>(
      context: context,
      builder: (dialogContext) {
        return Padding(
          padding: EdgeInsets.fromLTRB(
            20,
            20,
            20,
            MediaQuery.of(dialogContext).viewInsets.bottom + 20,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text('输入验证码', style: AppTextStyles.sectionTitle),
              const SizedBox(height: 16),
              AppTextField(
                controller: codeController,
                placeholder: '请输入收到的验证码',
              ),
              const SizedBox(height: 20),
              Row(
                children: [
                  Expanded(
                    child: AppSecondaryButton(
                      onPressed: () => Navigator.of(dialogContext).pop(false),
                      child: const Text('取消'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: AppPrimaryButton(
                      onPressed: () => Navigator.of(dialogContext).pop(true),
                      child: const Text('确认验证'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );

    if (shouldVerify != true) {
      codeController.dispose();
      return;
    }

    try {
      await verifyCode(codeController.text.trim());
      if (!context.mounted) return;
      await AppFeedback.showMessage(context, message: successMessage);
    } catch (_) {
      final errorMessage = ref.read(authControllerProvider).asData?.value.errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && context.mounted) {
        await AppFeedback.showMessage(context, message: errorMessage);
      }
    } finally {
      codeController.dispose();
    }
  }

  static String _fallback(String? value, String fallback) {
    final trimmed = value?.trim();
    return trimmed == null || trimmed.isEmpty ? fallback : trimmed;
  }
}

class _ProfileHero extends StatelessWidget {
  const _ProfileHero({required this.user, required this.onViewPublicProfile});

  final UserDto user;
  final VoidCallback onViewPublicProfile;

  @override
  Widget build(BuildContext context) {
    return AppTappableCard(
      padding: const EdgeInsets.all(20),
      radius: AppRadii.lg,
      onPressed: onViewPublicProfile,
      child: Row(
        children: [
          AppAvatar(
            url: user.avatar,
            name: user.name,
            size: 72,
            radius: 20,
            isCircle: false,
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  user.name?.trim().isNotEmpty == true
                      ? user.name!.trim()
                      : '未设置昵称',
                  style: AppTextStyles.sectionTitle,
                ),
                const SizedBox(height: 4),
                Text(
                  user.info?.trim().isNotEmpty == true
                      ? user.info!.trim()
                      : '这个人很低调，还没有写简介。',
                  style: AppTextStyles.muted,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          const Icon(
            CupertinoIcons.chevron_right,
            size: 16,
            color: AppColors.mutedForeground,
          ),
        ],
      ),
    );
  }
}

class _ProfileSection extends StatelessWidget {
  const _ProfileSection({required this.title, required this.children});

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 8, bottom: 8),
          child: Text(title, style: AppTextStyles.label),
        ),
        AppTappableCard(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          radius: AppRadii.lg,
          child: Column(
            children: children,
          ),
        ),
      ],
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
    final color = destructive ? AppColors.destructive : AppColors.primary;
    return AppListTile(
      onTap: onTap,
      leading: Container(
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.1),
          shape: BoxShape.circle,
        ),
        child: Icon(icon, color: color, size: 18),
      ),
      title: Text(
        title,
        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: color),
      ),
      subtitle: Text(subtitle, style: const TextStyle(fontSize: 13)),
      trailing: const Icon(
        CupertinoIcons.chevron_right,
        size: 14,
        color: AppColors.mutedForeground,
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return AppListTile(
      padding: const EdgeInsets.symmetric(vertical: 10),
      title: Text(
        label,
        style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
      ),
      subtitle: Text(value, style: const TextStyle(fontSize: 14)),
    );
  }
}

class _VerificationRow extends StatelessWidget {
  const _VerificationRow({
    required this.label,
    required this.isVerified,
    required this.isBusy,
    required this.onVerify,
  });

  final String label;
  final bool isVerified;
  final bool isBusy;
  final VoidCallback onVerify;

  @override
  Widget build(BuildContext context) {
    return AppListTile(
      padding: const EdgeInsets.symmetric(vertical: 10),
      title: Text(
        label,
        style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
      ),
      subtitle: Text(isVerified ? '已通过安全验证' : '尚未进行安全验证'),
      trailing: isVerified
          ? const Icon(
              CupertinoIcons.check_mark_circled_solid,
              color: Color(0xFF34C759),
              size: 20,
            )
          : CupertinoButton(
              padding: EdgeInsets.zero,
              minimumSize: Size.zero,
              onPressed: isBusy ? null : onVerify,
              child: const Text(
                '去验证',
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

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.initialUser.name ?? '');
    _infoController = TextEditingController(text: widget.initialUser.info ?? '');
    _avatarController = TextEditingController(text: widget.initialUser.avatar ?? '');
  }

  @override
  void dispose() {
    _nameController.dispose();
    _infoController.dispose();
    _avatarController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    try {
      await ref.read(authControllerProvider.notifier).updateProfile(
            UserUpdateDto(
              name: _nameController.text.trim(),
              info: _infoController.text.trim(),
              avatar: _avatarController.text.trim(),
            ),
          );
      if (!mounted) return;
      await AppFeedback.showMessage(context, message: '个人资料已更新');
      Navigator.of(context).pop();
    } catch (_) {
      final errorMessage = ref.read(authControllerProvider).asData?.value.errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && mounted) {
        await AppFeedback.showMessage(context, message: errorMessage);
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
            const Text('编辑资料', style: AppTextStyles.sectionTitle),
            const SizedBox(height: 20),
            AppTextField(controller: _nameController, placeholder: '昵称'),
            const SizedBox(height: 12),
            AppTextField(
              controller: _infoController,
              placeholder: '个人简介',
              maxLines: 3,
            ),
            const SizedBox(height: 12),
            AppTextField(controller: _avatarController, placeholder: '头像链接 (URL)'),
            const SizedBox(height: 24),
            AppPrimaryButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '保存中...' : '保存修改'),
            ),
          ],
        ),
      ),
    );
  }
}
