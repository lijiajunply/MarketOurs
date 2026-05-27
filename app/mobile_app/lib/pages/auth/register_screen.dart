import 'dart:math';

import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';
import 'auth_scaffold.dart';
import 'password_form_field.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _accountController = TextEditingController();
  final _passwordController = TextEditingController();

  String _avatarSeed = '';
  bool _isAccountDirty = false;
  bool _isPasswordDirty = false;
  bool _isAccountValid = false;
  bool _isPasswordValid = false;

  static final _emailRegex = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');
  static final _phoneRegex = RegExp(r'^1[3-9]\d{9}$');
  static final _passwordRegex = RegExp(r'^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$');

  @override
  void initState() {
    super.initState();
    _generateRandomAvatar();
  }

  @override
  void dispose() {
    _nameController.dispose();
    _accountController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  void _generateRandomAvatar() {
    final random = Random();
    final seed = random.nextInt(0xFFFFFF).toRadixString(36).padLeft(5, '0');
    setState(() => _avatarSeed = seed);
  }

  String get avatarUrl =>
      'https://api.dicebear.com/9.x/avataaars/svg?seed=$_avatarSeed';

  bool _validateAccount(String value) {
    final valid = _emailRegex.hasValue(value) || _phoneRegex.hasValue(value);
    setState(() => _isAccountValid = valid);
    return valid;
  }

  bool _validatePassword(String value) {
    final valid = _passwordRegex.hasValue(value);
    setState(() => _isPasswordValid = valid);
    return valid;
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    try {
      final registrationToken = await ref
          .read(authControllerProvider.notifier)
          .register(
            account: _accountController.text.trim(),
            password: _passwordController.text,
            name: _nameController.text.trim(),
            avatar: avatarUrl,
          );

      if (!mounted) return;

      // Auto-send verification code
      await ref
          .read(authControllerProvider.notifier)
          .sendRegistrationCode(registrationToken);

      if (!mounted) return;

      context.goNamed(
        AppRouteNames.registerVerify,
        queryParameters: {
          'registrationToken': registrationToken,
          'account': _accountController.text.trim(),
        },
      );
    } catch (_) {
      if (!mounted) return;
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty) {
        await AppFeedback.showMessage(context, message: errorMessage);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = authState?.isSubmitting ?? false;
    final submitDisabled =
        isSubmitting ||
        (_isAccountDirty && !_isAccountValid) ||
        (_isPasswordDirty && !_isPasswordValid);

    return AuthScaffold(
      title: '注册账号',
      footer: Center(
        child: CupertinoButton(
          onPressed:
              isSubmitting
                  ? null
                  : () => context.go(AppRoutePaths.login),
          child: const Text(
            '已有账号？返回登录',
            style: TextStyle(fontWeight: FontWeight.w700),
          ),
        ),
      ),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Avatar
            Center(
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  Container(
                    width: 88,
                    height: 88,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(
                        color: AppColors.primary.withValues(alpha: 0.3),
                        width: 3,
                      ),
                    ),
                    child: ClipOval(
                      child: Image.network(
                        avatarUrl,
                        fit: BoxFit.cover,
                        errorBuilder: (context, error, stackTrace) => Icon(
                          CupertinoIcons.person_circle,
                          size: 64,
                          color: AppColors.mutedForeground,
                        ),
                      ),
                    ),
                  ),
                  Positioned(
                    right: -4,
                    bottom: -4,
                    child: CupertinoButton(
                      padding: EdgeInsets.zero,
                      minimumSize: Size.zero,
                      onPressed: _generateRandomAvatar,
                      child: Container(
                        width: 32,
                        height: 32,
                        decoration: BoxDecoration(
                          color: AppColors.primary,
                          shape: BoxShape.circle,
                          boxShadow: [
                            BoxShadow(
                              color: AppColors.primary.withValues(alpha: 0.4),
                              blurRadius: 8,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: const Icon(
                          CupertinoIcons.refresh,
                          size: 16,
                          color: CupertinoColors.white,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 4),
            Center(
              child: Text(
                '点击刷新随机头像',
                style: AppTextStyles.label(context).copyWith(fontSize: 11),
              ),
            ),
            const SizedBox(height: 20),

            // Name
            Text('显示名称', style: AppTextStyles.label(context)),
            const SizedBox(height: 8),
            AppTextField(
              controller: _nameController,
              placeholder: '给自己起个名字',
              prefix: Icon(
                CupertinoIcons.person,
                color: AppColors.mutedForeground,
                size: 18,
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入显示名称';
                }
                return null;
              },
            ),
            const SizedBox(height: 20),

            // Account
            Text('账号', style: AppTextStyles.label(context)),
            const SizedBox(height: 8),
            AppTextField(
              controller: _accountController,
              placeholder: '邮箱或手机号',
              keyboardType: TextInputType.emailAddress,
              prefix: Icon(
                CupertinoIcons.mail,
                color: AppColors.mutedForeground,
                size: 18,
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入账号';
                }
                if (_isAccountDirty && !_isAccountValid) {
                  return '请输入有效的邮箱或手机号';
                }
                return null;
              },
              onChanged: (value) {
                setState(() => _isAccountDirty = true);
                _validateAccount(value);
              },
            ),
            if (_isAccountDirty && !_isAccountValid)
              Padding(
                padding: const EdgeInsets.only(left: 4, top: 6),
                child: Text(
                  '请输入有效的邮箱或手机号',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: AppColors.destructive,
                  ),
                ),
              ),
            const SizedBox(height: 20),

            // Password
            Text('密码', style: AppTextStyles.label(context)),
            const SizedBox(height: 8),
            PasswordFormField(
              controller: _passwordController,
              placeholder: '密码，至少6位，含大小写字母和数字',
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return '请输入密码';
                }
                if (_isPasswordDirty && !_isPasswordValid) {
                  return '密码需至少6位，包含大写字母、小写字母和数字';
                }
                return null;
              },
              onChanged: (value) {
                setState(() => _isPasswordDirty = true);
                _validatePassword(value);
              },
            ),
            if (_isPasswordDirty && !_isPasswordValid)
              Padding(
                padding: const EdgeInsets.only(left: 4, top: 6),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '密码需至少6位，包含大写字母、小写字母和数字',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: AppColors.destructive,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '至少6位，包含大小写字母和数字',
                      style: TextStyle(
                        fontSize: 10,
                        color: AppColors.mutedForeground,
                      ),
                    ),
                  ],
                ),
              ),
            const SizedBox(height: 28),

            AppPrimaryButton(
              onPressed: submitDisabled ? null : _submit,
              child: Text(isSubmitting ? '提交中...' : '注册'),
            ),
          ],
        ),
      ),
    );
  }
}

extension _RegExpExt on RegExp {
  bool hasValue(String value) => hasMatch(value);
}
