import 'dart:io';
import 'dart:math';

import 'package:flutter/cupertino.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../providers/locale_provider.dart';
import '../../providers/theme_provider.dart';
import '../../router/app_router.dart';
import '../../services/file_service.dart';
import '../../services/image_compression_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';
import '../../utils/dto_validation.dart';

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
            CupertinoSliverNavigationBar(
              largeTitle: Text(AppLocalizations.of(context).tabProfile),
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
                        title: AppLocalizations.of(context).profileNotLoggedIn,
                        description: AppLocalizations.of(
                          context,
                        ).profileNotLoggedInDesc,
                        action: AppPrimaryButton(
                          onPressed: () => context.go(AppRoutePaths.login),
                          child: Text(
                            AppLocalizations.of(context).profileGoLogin,
                          ),
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
            largeTitle: Text(AppLocalizations.of(context).tabProfile),
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
                      title: AppLocalizations.of(context).profileInfo,
                      children: [
                        _InfoRow(
                          label: AppLocalizations.of(context).profileNickname,
                          value: _fallback(user.name, '未设置'),
                        ),
                        _InfoRow(
                          label: AppLocalizations.of(context).profileBio,
                          value: _fallback(
                            user.info,
                            AppLocalizations.of(context).profileNoBio,
                          ),
                        ),
                        _InfoRow(
                          label: AppLocalizations.of(context).profileEmail,
                          value: _fallback(
                            user.email,
                            AppLocalizations.of(context).profileNoEmail,
                          ),
                        ),
                        _VerificationRow(
                          label: AppLocalizations.of(
                            context,
                          ).profileVerifyEmailTitle,
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
                            successMessage: AppLocalizations.of(
                              context,
                            ).profileEmailVerifySuccess,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    _ProfileSection(
                      title: AppLocalizations.of(context).profileSocial,
                      children: [
                        _NavRow(
                          icon: CupertinoIcons.person_2,
                          title: AppLocalizations.of(
                            context,
                          ).profileFollowBlock,
                          subtitle: AppLocalizations.of(
                            context,
                          ).profileFollowBlockDesc,
                          onTap: () => context.push(AppRoutePaths.following),
                        ),
                        _NavRow(
                          icon: CupertinoIcons.link,
                          title: AppLocalizations.of(context).profileBindings,
                          subtitle: AppLocalizations.of(
                            context,
                          ).profileManageSocialDesc,
                          onTap: () => context.push(AppRoutePaths.bindings),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    const _ThemeModeSection(),
                    const SizedBox(height: 16),
                    _ProfileSection(
                      title: AppLocalizations.of(context).profileAbout,
                      children: [
                        _NavRow(
                          icon: CupertinoIcons.doc_text,
                          title: AppLocalizations.of(context).profileTerms,
                          subtitle: AppLocalizations.of(
                            context,
                          ).profileTermsDesc,
                          onTap: () => context.push(AppRoutePaths.terms),
                        ),
                        _NavRow(
                          icon: CupertinoIcons.shield_lefthalf_fill,
                          title: AppLocalizations.of(context).profilePrivacy,
                          subtitle: AppLocalizations.of(
                            context,
                          ).profilePrivacyDesc,
                          onTap: () => context.push(AppRoutePaths.privacy),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    _ProfileSection(
                      title: AppLocalizations.of(context).profileSecurity,
                      children: [
                        _NavRow(
                          icon: CupertinoIcons.lock_rotation,
                          title: AppLocalizations.of(
                            context,
                          ).profileChangePasswordTitle,
                          subtitle: AppLocalizations.of(
                            context,
                          ).profileChangePasswordDesc,
                          onTap: () =>
                              context.push(AppRoutePaths.changePassword),
                        ),
                        _NavRow(
                          icon: CupertinoIcons.square_arrow_right,
                          title: AppLocalizations.of(context).authLogout,
                          subtitle: AppLocalizations.of(context).authLogoutDesc,
                          destructive: true,
                          onTap: isSubmitting
                              ? null
                              : () => _logout(context, ref),
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

  Future<void> _logout(BuildContext context, WidgetRef ref) async {
    final confirmed = await showCupertinoDialog<bool>(
      context: context,
      builder: (context) => CupertinoAlertDialog(
        title: const Text('确认退出登录？'),
        content: const Text('退出登录将清除当前会话，可能需要重新登录。'),
        actions: [
          CupertinoDialogAction(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('取消'),
          ),
          CupertinoDialogAction(
            isDestructiveAction: true,
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('退出登录'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    await ref.read(authControllerProvider.notifier).logout();
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
      await AppFeedback.showSuccess(
        context,
        message: AppLocalizations.of(context).authSendCodeSuccess,
      );
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
              Text(
                AppLocalizations.of(context).profileEnterCode,
                style: AppTextStyles.sectionTitle(context),
              ),
              const SizedBox(height: 16),
              AppTextField(
                controller: codeController,
                placeholder: AppLocalizations.of(context).profileEnterCodeHint,
              ),
              const SizedBox(height: 20),
              Row(
                children: [
                  Expanded(
                    child: AppSecondaryButton(
                      onPressed: () => Navigator.of(dialogContext).pop(false),
                      child: Text(AppLocalizations.of(context).cancel),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: AppPrimaryButton(
                      onPressed: () => Navigator.of(dialogContext).pop(true),
                      child: Text(
                        AppLocalizations.of(context).profileConfirmVerify,
                      ),
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
                      : AppLocalizations.of(context).profileNoNickname,
                  style: AppTextStyles.sectionTitle(context),
                ),
                const SizedBox(height: 4),
                Text(
                  user.info?.trim().isNotEmpty == true
                      ? user.info!.trim()
                      : AppLocalizations.of(context).profileOwnerLowkey,
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
    final currentLocale = ref.watch(localeNotifierProvider);
    final l10n = AppLocalizations.of(context);

    return _ProfileSection(
      title: l10n.profileDisplaySettings,
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
          title: Text(
            l10n.themeMode,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w600,
              color: AppColors.primary,
            ),
          ),
          subtitle: Text(
            l10n.themeSubtitle,
            style: const TextStyle(fontSize: 13),
          ),
          trailing: const Icon(
            CupertinoIcons.chevron_right,
            size: 14,
            color: AppColors.mutedForeground,
          ),
        ),
        const SizedBox(height: 4),
        _NavRow(
          icon: CupertinoIcons.globe,
          title: l10n.settingsLanguage,
          subtitle: _localeLabel(currentLocale, l10n),
          onTap: () => _showLanguageSheet(context, ref, currentLocale),
        ),
      ],
    );
  }

  String _themeModeLabel(AppThemeMode mode, AppLocalizations l10n) {
    return switch (mode) {
      AppThemeMode.system => l10n.themeSystem,
      AppThemeMode.light => l10n.themeLight,
      AppThemeMode.dark => l10n.themeDark,
    };
  }

  void _showThemeModeSheet(
    BuildContext context,
    WidgetRef ref,
    AppThemeMode currentMode,
  ) {
    final l10n = AppLocalizations.of(context);
    showCupertinoModalPopup<void>(
      context: context,
      builder: (sheetContext) => CupertinoActionSheet(
        title: Text(AppLocalizations.of(context).appearanceModeTitle),
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
                label: _themeModeLabel(mode, l10n),
                isSelected: mode == currentMode,
              ),
            ),
        ],
        cancelButton: CupertinoActionSheetAction(
          isDefaultAction: true,
          onPressed: () => Navigator.of(sheetContext).pop(),
          child: Text(AppLocalizations.of(context).cancel),
        ),
      ),
    );
  }

  String _localeLabel(Locale? locale, AppLocalizations l10n) {
    if (locale == null) return l10n.followSystem;

    return switch (locale.languageCode) {
      'zh' => l10n.language_zh,
      'en' => l10n.language_en,
      'ja' => l10n.language_ja,
      'ru' => l10n.language_ru,
      'fr' => l10n.language_fr,
      'de' => l10n.language_de,
      'ko' => l10n.language_ko,
      _ => locale.toString(),
    };
  }

  void _showLanguageSheet(
    BuildContext context,
    WidgetRef ref,
    Locale? currentLocale,
  ) {
    final l10n = AppLocalizations.of(context);
    showCupertinoModalPopup<void>(
      context: context,
      builder: (sheetContext) => CupertinoActionSheet(
        title: Text(l10n.settingsLanguageTitle),
        actions: [
          CupertinoActionSheetAction(
            isDefaultAction: currentLocale == null,
            onPressed: () {
              Navigator.of(sheetContext).pop();
              ref.read(localeNotifierProvider.notifier).setLocale(null);
            },
            child: _LanguageActionLabel(
              label: l10n.followSystem,
              isSelected: currentLocale == null,
            ),
          ),
          for (final locale in supportedLocales)
            CupertinoActionSheetAction(
              isDefaultAction: currentLocale == locale,
              onPressed: () {
                Navigator.of(sheetContext).pop();
                ref.read(localeNotifierProvider.notifier).setLocale(locale);
              },
              child: _LanguageActionLabel(
                label: _localeLabel(locale, l10n),
                isSelected: currentLocale == locale,
              ),
            ),
        ],
        cancelButton: CupertinoActionSheetAction(
          isDefaultAction: true,
          onPressed: () => Navigator.of(sheetContext).pop(),
          child: Text(l10n.cancel),
        ),
      ),
    );
  }
}

class _ThemeModeActionLabel extends StatelessWidget {
  const _ThemeModeActionLabel({
    required this.mode,
    required this.label,
    required this.isSelected,
  });

  final AppThemeMode mode;
  final String label;
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
        Text(label, style: TextStyle(color: color)),
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

class _LanguageActionLabel extends StatelessWidget {
  const _LanguageActionLabel({
    required this.label,
    required this.isSelected,
  });

  final String label;
  final bool isSelected;

  @override
  Widget build(BuildContext context) {
    final color = isSelected
        ? AppColors.primary
        : CupertinoDynamicColor.resolve(AppColors.foreground, context);

    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Text(label, style: TextStyle(color: color)),
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
      subtitle: Text(
        isVerified
            ? AppLocalizations.of(context).profileEmailVerified
            : AppLocalizations.of(context).profileEmailNotVerified,
      ),
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
              child: Text(
                AppLocalizations.of(context).profileVerifyEmail,
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
  XFile? _avatarFile;
  bool _isSaving = false;
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
    setState(() {
      _avatarFile = null;
      _avatarUrl = 'https://api.dicebear.com/9.x/avataaars/svg?seed=$seed';
    });
  }

  Future<void> _pickFromGallery() async {
    final picked = await _imagePicker.pickImage(
      source: ImageSource.gallery,
      imageQuality: 90,
    );
    if (picked != null) {
      setState(() {
        _avatarFile = picked;
        _avatarUrl = '';
      });
    }
  }

  Future<void> _takePhoto() async {
    final picked = await _imagePicker.pickImage(
      source: ImageSource.camera,
      imageQuality: 90,
    );
    if (picked != null) {
      setState(() {
        _avatarFile = picked;
        _avatarUrl = '';
      });
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
          child: Text(AppLocalizations.of(context).cancel),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSaving = true);
    CompressedImage? compressedAvatar;
    try {
      var avatar = _avatarUrl;
      if (_avatarFile != null) {
        try {
          // Compress avatar to WebP before upload
          compressedAvatar = await ImageCompressionService.compress(
            _avatarFile!,
            quality: ImageCompressionService.avatarQuality,
            maxWidth: ImageCompressionService.avatarMaxWidth,
            maxHeight: ImageCompressionService.avatarMaxHeight,
          );

          final uploadResponse = await _fileService.uploadAvatar(
            ImageCompressionService.toXFile(compressedAvatar),
          );
          final url = uploadResponse.data;
          if (url != null && url.isNotEmpty) {
            avatar = url;
          } else {
            if (!mounted) return;
            await AppFeedback.showError(
              context,
              message: AppLocalizations.of(context).errorAvatarUploadFailed,
            );
            return;
          }
        } catch (_) {
          if (!mounted) return;
          await AppFeedback.showError(
            context,
            message: AppLocalizations.of(context).errorAvatarUploadFailed,
          );
          return;
        }
      }

      if (!mounted) return;

      await ref
          .read(authControllerProvider.notifier)
          .updateProfile(
            UserUpdateDto(
              name: _nameController.text.trim(),
              info: _infoController.text.trim(),
              avatar: avatar,
            ),
          );
      if (!mounted) return;
      await AppFeedback.showSuccess(
        context,
        message: AppLocalizations.of(context).profileUpdated,
      );
      if (!mounted) return;
      Navigator.of(context).pop();
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (mounted) {
        await AppFeedback.showError(
          context,
          message: errorMessage?.isNotEmpty == true
              ? errorMessage!
              : AppLocalizations.of(context).profileUpdateFailed,
        );
      }
    } finally {
      // Clean up temp compressed avatar file
      if (compressedAvatar != null) {
        ImageCompressionService.cleanup([compressedAvatar]);
      }
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = _isSaving || (authState?.isSubmitting ?? false);

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
              AppLocalizations.of(context).profileEditProfile,
              style: AppTextStyles.sectionTitle(context),
            ),
            const SizedBox(height: 20),

            // Avatar picker
            Center(
              child: GestureDetector(
                onTap: isSubmitting ? null : _showAvatarOptions,
                child: Stack(
                  clipBehavior: Clip.none,
                  children: [
                    _AvatarPreview(
                      file: _avatarFile,
                      url: _avatarUrl,
                      name: _nameController.text,
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
                AppLocalizations.of(context).profileClickToChangeAvatar,
                style: AppTextStyles.label(context).copyWith(fontSize: 11),
              ),
            ),
            const SizedBox(height: 16),

            AppTextField(
              controller: _nameController,
              placeholder: AppLocalizations.of(context).profileNickname,
              maxLength: DtoLimits.userNameMax,
              validator: (v) => optionalMaxValidator(
                v,
                max: DtoLimits.userNameMax,
                maxMessage: '用户名长度不能超过 ${DtoLimits.userNameMax} 位',
              ),
            ),
            const SizedBox(height: 12),
            AppTextField(
              controller: _infoController,
              placeholder: '个人简介',
              maxLines: 3,
              maxLength: DtoLimits.userInfoMax,
              validator: (v) => optionalMaxValidator(
                v,
                max: DtoLimits.userInfoMax,
                maxMessage: '个人简介长度不能超过 ${DtoLimits.userInfoMax} 位',
              ),
            ),
            const SizedBox(height: 24),
            AppPrimaryButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(
                isSubmitting
                    ? AppLocalizations.of(context).profileSaving
                    : AppLocalizations.of(context).profileSaveChanges,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AvatarPreview extends StatelessWidget {
  const _AvatarPreview({required this.file, required this.url, this.name});

  final XFile? file;
  final String url;
  final String? name;

  @override
  Widget build(BuildContext context) {
    final localFile = file;
    if (localFile == null) {
      return AppAvatar(url: url, name: name, size: 88, radius: 44);
    }

    return Container(
      width: 88,
      height: 88,
      decoration: BoxDecoration(
        color: CupertinoDynamicColor.resolve(AppColors.secondary, context),
        borderRadius: BorderRadius.circular(44),
      ),
      clipBehavior: Clip.antiAlias,
      child: Image.file(File(localFile.path), fit: BoxFit.cover),
    );
  }
}
