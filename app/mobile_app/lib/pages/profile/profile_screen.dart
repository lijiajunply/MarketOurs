import 'dart:math';

import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../providers/theme_provider.dart';
import '../../router/app_router.dart';
import '../../services/file_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_responsive.dart';
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
            const CupertinoSliverNavigationBar(
              largeTitle: Text('我的'),
              border: null,
            ),
            SliverFillRemaining(
              child: AppResponsiveCenter(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      AppEmptyState(
                        icon: CupertinoIcons.person,
                        title: '还没有登录',
                        description: '登录后可以查看个人资料、管理安全设置。',
                        action: AppPrimaryButton(
                          onPressed: () => context.go(AppRoutePaths.login),
                          child: const Text('去登录'),
                        ),
                      ),
                      const SizedBox(height: 24),
                      const _ThemeModeSection(),
                    ],
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
            backgroundColor: CupertinoDynamicColor.resolve(
              AppColors.background,
              context,
            ).withValues(alpha: 0.94),
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
            child: AppResponsiveCenter(
              padding: AppResponsive.sliverPagePadding(context, bottom: 32),
              child: AppTwoPane(
                key: const ValueKey('profile-responsive-two-pane'),
                secondaryFirstOnWide: true,
                primary: Column(
                  children: [
                    _ProfileSection(
                      title: '资料信息',
                      children: [
                        _InfoRow(label: '昵称', value: _fallback(user.name, '未设置')),
                        _InfoRow(
                          label: '简介',
                          value: _fallback(user.info, '还没有写简介'),
                        ),
                        _InfoRow(
                          label: '邮箱',
                          value: _fallback(user.email, '未绑定'),
                        ),
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
                      title: '社交管理',
                      children: [
                        _NavRow(
                          icon: CupertinoIcons.person_2,
                          title: '关注与屏蔽',
                          subtitle: '管理关注的用户和屏蔽列表',
                          onTap: () => context.push(AppRoutePaths.following),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    const _ThemeModeSection(),
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
                  ],
                ),
                secondary: _ProfileHero(
                  user: user,
                  onViewPublicProfile: () =>
                      context.push(buildPublicProfileLocation(user.id)),
                ),
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
      await AppFeedback.showSuccess(context, message: '验证码已发送');
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && context.mounted) {
        await AppFeedback.showError(context, message: errorMessage);
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
              Text('输入验证码', style: AppTextStyles.sectionTitle(context)),
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
      await AppFeedback.showSuccess(context, message: successMessage);
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && context.mounted) {
        await AppFeedback.showError(context, message: errorMessage);
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
                  style: AppTextStyles.sectionTitle(context),
                ),
                const SizedBox(height: 4),
                Text(
                  user.info?.trim().isNotEmpty == true
                      ? user.info!.trim()
                      : '这个人很低调，还没有写简介。',
                  style: AppTextStyles.muted(context),
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
          child: Text(title, style: AppTextStyles.label(context)),
        ),
        AppTappableCard(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          radius: AppRadii.lg,
          child: Column(children: children),
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
        style: TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.w600,
          color: color,
        ),
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

class _ThemeModeSection extends ConsumerWidget {
  const _ThemeModeSection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final themeMode = ref.watch(themeModeNotifierProvider);

    return _ProfileSection(
      title: '显示设置',
      children: [
        AppListTile(
          onTap: () => _showThemeModeSheet(context, ref, themeMode),
          padding: const EdgeInsets.symmetric(vertical: 10),
          leading: Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: Icon(themeMode.icon, color: AppColors.primary, size: 18),
          ),
          title: const Text(
            '主题模式',
            style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
          ),
          subtitle: Text(themeMode.label, style: const TextStyle(fontSize: 13)),
          trailing: const Icon(
            CupertinoIcons.chevron_right,
            size: 14,
            color: AppColors.mutedForeground,
          ),
        ),
      ],
    );
  }

  void _showThemeModeSheet(
    BuildContext context,
    WidgetRef ref,
    AppThemeMode currentMode,
  ) {
    showCupertinoModalPopup<void>(
      context: context,
      builder: (sheetContext) => CupertinoActionSheet(
        title: const Text('主题模式'),
        actions: [
          for (final mode in AppThemeMode.values)
            CupertinoActionSheetAction(
              isDefaultAction: mode == currentMode,
              onPressed: () {
                Navigator.of(sheetContext).pop();
                ref.read(themeModeNotifierProvider.notifier).setMode(mode);
              },
              child: _ThemeModeActionLabel(
                mode: mode,
                isSelected: mode == currentMode,
              ),
            ),
        ],
        cancelButton: CupertinoActionSheetAction(
          isDefaultAction: true,
          onPressed: () => Navigator.of(sheetContext).pop(),
          child: const Text('取消'),
        ),
      ),
    );
  }
}

class _ThemeModeActionLabel extends StatelessWidget {
  const _ThemeModeActionLabel({
    required this.mode,
    required this.isSelected,
  });

  final AppThemeMode mode;
  final bool isSelected;

  @override
  Widget build(BuildContext context) {
    final color = isSelected
        ? AppColors.primary
        : CupertinoDynamicColor.resolve(AppColors.foreground, context);

    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Icon(mode.icon, size: 18, color: color),
        const SizedBox(width: 8),
        Text(mode.label, style: TextStyle(color: color)),
        const SizedBox(width: 8),
        Opacity(
          opacity: isSelected ? 1 : 0,
          child: const Icon(
            CupertinoIcons.check_mark,
            size: 18,
            color: AppColors.primary,
          ),
        ),
      ],
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

  String _avatarUrl = '';
  bool _isUploadingAvatar = false;
  final _imagePicker = ImagePicker();
  final _fileService = FileService();

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(
      text: widget.initialUser.name ?? '',
    );
    _infoController = TextEditingController(
      text: widget.initialUser.info ?? '',
    );
    _avatarUrl = widget.initialUser.avatar ?? '';
  }

  @override
  void dispose() {
    _nameController.dispose();
    _infoController.dispose();
    super.dispose();
  }

  void _generateRandomAvatar() {
    final random = Random();
    final seed = random.nextInt(0xFFFFFF).toRadixString(36).padLeft(5, '0');
    setState(
      () =>
          _avatarUrl = 'https://api.dicebear.com/9.x/avataaars/svg?seed=$seed',
    );
  }

  Future<void> _pickFromGallery() async {
    final picked = await _imagePicker.pickImage(
      source: ImageSource.gallery,
      imageQuality: 90,
    );
    if (picked != null) await _uploadAvatar(picked);
  }

  Future<void> _takePhoto() async {
    final picked = await _imagePicker.pickImage(
      source: ImageSource.camera,
      imageQuality: 90,
    );
    if (picked != null) await _uploadAvatar(picked);
  }

  Future<void> _uploadAvatar(XFile file) async {
    setState(() => _isUploadingAvatar = true);
    try {
      final response = await _fileService.uploadImage(file);
      final url = response.data;
      if (url != null && url.isNotEmpty && mounted) {
        setState(() => _avatarUrl = url);
      }
    } catch (_) {
      if (mounted) {
        await AppFeedback.showError(context, message: '头像上传失败');
      }
    } finally {
      if (mounted) setState(() => _isUploadingAvatar = false);
    }
  }

  void _showAvatarOptions() {
    showCupertinoModalPopup<void>(
      context: context,
      builder: (ctx) => CupertinoActionSheet(
        title: const Text('选择头像'),
        actions: [
          CupertinoActionSheetAction(
            onPressed: () {
              Navigator.pop(ctx);
              _generateRandomAvatar();
            },
            child: const Text('随机生成'),
          ),
          CupertinoActionSheetAction(
            onPressed: () {
              Navigator.pop(ctx);
              _pickFromGallery();
            },
            child: const Text('从相册选择'),
          ),
          CupertinoActionSheetAction(
            onPressed: () {
              Navigator.pop(ctx);
              _takePhoto();
            },
            child: const Text('拍照'),
          ),
        ],
        cancelButton: CupertinoActionSheetAction(
          isDefaultAction: true,
          onPressed: () => Navigator.pop(ctx),
          child: const Text('取消'),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    try {
      await ref
          .read(authControllerProvider.notifier)
          .updateProfile(
            UserUpdateDto(
              name: _nameController.text.trim(),
              info: _infoController.text.trim(),
              avatar: _avatarUrl,
            ),
          );
      if (!mounted) return;
      await AppFeedback.showSuccess(context, message: '个人资料已更新');
      if (!mounted) return;
      Navigator.of(context).pop();
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && mounted) {
        await AppFeedback.showError(context, message: errorMessage);
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
            Text('编辑资料', style: AppTextStyles.sectionTitle(context)),
            const SizedBox(height: 20),

            // Avatar picker
            Center(
              child: GestureDetector(
                onTap: _isUploadingAvatar ? null : _showAvatarOptions,
                child: Stack(
                  clipBehavior: Clip.none,
                  children: [
                    AppAvatar(
                      url: _avatarUrl,
                      name: _nameController.text,
                      size: 88,
                      radius: 44,
                    ),
                    if (_isUploadingAvatar)
                      const Positioned.fill(
                        child: Center(child: CupertinoActivityIndicator()),
                      ),
                    Positioned(
                      right: -4,
                      bottom: -4,
                      child: Container(
                        width: 32,
                        height: 32,
                        decoration: BoxDecoration(
                          color: AppColors.primary,
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          CupertinoIcons.camera,
                          size: 16,
                          color: CupertinoColors.white,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 8),
            Center(
              child: Text(
                '点击更换头像',
                style: AppTextStyles.label(context).copyWith(fontSize: 11),
              ),
            ),
            const SizedBox(height: 16),

            AppTextField(controller: _nameController, placeholder: '昵称'),
            const SizedBox(height: 12),
            AppTextField(
              controller: _infoController,
              placeholder: '个人简介',
              maxLines: 3,
            ),
            const SizedBox(height: 24),
            AppPrimaryButton(
              onPressed: isSubmitting || _isUploadingAvatar ? null : _submit,
              child: Text(isSubmitting ? '保存中...' : '保存修改'),
            ),
          ],
        ),
      ),
    );
  }
}
