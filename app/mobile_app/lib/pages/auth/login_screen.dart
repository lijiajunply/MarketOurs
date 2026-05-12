import 'dart:async';

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

enum _LoginMode { password, otp }

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _accountController = TextEditingController();
  final _passwordController = TextEditingController();
  final _otpController = TextEditingController();
  _LoginMode _loginMode = _LoginMode.password;
  Timer? _countdownTimer;
  int _countdown = 0;
  bool _isSendingCode = false;

  @override
  void dispose() {
    _countdownTimer?.cancel();
    _accountController.dispose();
    _passwordController.dispose();
    _otpController.dispose();
    super.dispose();
  }

  Future<void> _sendCode() async {
    final account = _accountController.text.trim();
    if (account.isEmpty) {
      await AppFeedback.showMessage(context, message: '请输入账号');
      return;
    }
    
    setState(() => _isSendingCode = true);
    try {
      await ref
          .read(authControllerProvider.notifier)
          .sendLoginCode(account: account);
      if (!mounted) return;
      _startCountdown();
      await AppFeedback.showMessage(context, message: '验证码已发送');
    } catch (error) {
      if (!mounted) return;
      // Handle the error directly here to avoid state listener side effects
      String message = '发送失败，请稍后重试';
      if (error.toString().isNotEmpty) {
        message = error.toString().replaceFirst('Exception: ', '');
      }
      await AppFeedback.showMessage(context, message: message);
    } finally {
      if (mounted) {
        setState(() => _isSendingCode = false);
      }
    }
  }

  void _startCountdown() {
    _countdownTimer?.cancel();
    setState(() => _countdown = 60);
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_countdown <= 1) {
        timer.cancel();
        if (mounted) {
          setState(() => _countdown = 0);
        }
        return;
      }

      if (mounted) {
        setState(() => _countdown -= 1);
      }
    });
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final notifier = ref.read(authControllerProvider.notifier);
    final success = _loginMode == _LoginMode.password
        ? await notifier.login(
            account: _accountController.text.trim(),
            password: _passwordController.text,
          )
        : await notifier.loginByCode(
            account: _accountController.text.trim(),
            code: _otpController.text.trim(),
          );

    if (!mounted) {
      return;
    }

    final authState = ref.read(authControllerProvider).asData?.value;
    final errorMessage = authState?.errorMessage;
    if (success) {
      await AppFeedback.showMessage(context, message: '登录成功');
      if (!mounted) {
        return;
      }
      context.go(AppRoutePaths.home);
      return;
    }

    if (errorMessage != null && errorMessage.isNotEmpty) {
      await AppFeedback.showMessage(context, message: errorMessage);
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = authState?.isSubmitting ?? false;

    return AuthScaffold(
      badge: 'Welcome Back',
      title: '登录',
      subtitle: '支持账号密码登录，也支持验证码快捷登录。',
      footer: Center(
        child: CupertinoButton(
          onPressed: isSubmitting
              ? null
              : () => context.go(AppRoutePaths.register),
          child: const Text(
            '没有账号？去注册',
            style: TextStyle(fontWeight: FontWeight.w700),
          ),
        ),
      ),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(
              padding: const EdgeInsets.all(2),
              decoration: BoxDecoration(
                color: CupertinoDynamicColor.resolve(AppColors.secondary, context),
                borderRadius: BorderRadius.circular(AppRadii.md),
              ),
              child: Row(
                children: [
                  _ModeButton(
                    label: '密码登录',
                    isActive: _loginMode == _LoginMode.password,
                    onTap: () => setState(() => _loginMode = _LoginMode.password),
                  ),
                  _ModeButton(
                    label: '验证码登录',
                    isActive: _loginMode == _LoginMode.otp,
                    onTap: () => setState(() => _loginMode = _LoginMode.otp),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            Text('账号', style: AppTextStyles.label(context)),
            const SizedBox(height: 8),
            AppTextField(
              controller: _accountController,
              placeholder: '账号 / 邮箱 / 手机号',
              prefix: Icon(
                CupertinoIcons.mail,
                color: AppColors.mutedForeground,
                size: 18,
              ),
              textInputAction: _loginMode == _LoginMode.password
                  ? TextInputAction.next
                  : TextInputAction.done,
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入账号';
                }
                return null;
              },
            ),
            const SizedBox(height: 20),
            if (_loginMode == _LoginMode.password) ...[
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('密码', style: AppTextStyles.label(context)),
                  CupertinoButton(
                    padding: EdgeInsets.zero,
                    minimumSize: Size.zero,
                    onPressed: isSubmitting
                        ? null
                        : () => context.go(AppRoutePaths.forgotPassword),
                    child: Text(
                      '忘记密码？',
                      style: TextStyle(
                        color: AppColors.primary,
                        fontWeight: FontWeight.w700,
                        fontSize: 12,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              PasswordFormField(
                controller: _passwordController,
                placeholder: '请输入密码',
                onFieldSubmitted: (_) => isSubmitting ? null : _submit(),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return '请输入密码';
                  }
                  return null;
                },
              ),
            ] else ...[
              Text('验证码', style: AppTextStyles.label(context)),
              const SizedBox(height: 8),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: AppTextField(
                      controller: _otpController,
                      placeholder: '6 位验证码',
                      keyboardType: TextInputType.number,
                      prefix: Icon(
                        CupertinoIcons.shield_fill,
                        color: AppColors.mutedForeground,
                        size: 18,
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return '请输入验证码';
                        }
                        return null;
                      },
                    ),
                  ),
                  const SizedBox(width: 12),
                  SizedBox(
                    height: 48,
                    child: AppSecondaryButton(
                      onPressed:
                          isSubmitting || _isSendingCode || _countdown > 0
                          ? null
                          : _sendCode,
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: Text(
                        _isSendingCode
                            ? '发送中'
                            : _countdown > 0
                            ? '${_countdown}s'
                            : '获取验证码',
                        style: const TextStyle(fontSize: 14),
                      ),
                    ),
                  ),
                ],
              ),
            ],
            const SizedBox(height: 28),
            AppPrimaryButton(
              onPressed: isSubmitting ? null : _submit,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(isSubmitting ? '登录中...' : '登录'),
                  if (!isSubmitting) ...[
                    const SizedBox(width: 8),
                    const Icon(CupertinoIcons.arrow_right, size: 18),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 32),
            Row(
              children: [
                Expanded(child: Container(height: 1, color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3))),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Text('其他方式登录', style: AppTextStyles.label(context).copyWith(fontSize: 11, letterSpacing: 1.2)),
                ),
                Expanded(child: Container(height: 1, color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3))),
              ],
            ),
            const SizedBox(height: 24),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _SocialButton(icon: CupertinoIcons.person_circle, label: 'Ours'),
                const SizedBox(width: 24),
                _SocialButton(icon: CupertinoIcons.globe, label: 'Google'),
                const SizedBox(width: 24),
                _SocialButton(icon: CupertinoIcons.cloud, label: 'Github'),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ModeButton extends StatelessWidget {
  const _ModeButton({required this.label, required this.isActive, required this.onTap});
  final String label;
  final bool isActive;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: CupertinoButton(
        padding: EdgeInsets.zero,
        onPressed: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 10),
          decoration: BoxDecoration(
            color: isActive ? CupertinoDynamicColor.resolve(AppColors.card, context) : null,
            borderRadius: BorderRadius.circular(AppRadii.md - 2),
            boxShadow: isActive ? [
              BoxShadow(
                color: const Color(0x0A000000),
                blurRadius: 4,
                offset: const Offset(0, 2),
              )
            ] : null,
          ),
          child: Text(
            label,
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 14,
              fontWeight: isActive ? FontWeight.w700 : FontWeight.w600,
              color: CupertinoDynamicColor.resolve(
                isActive ? AppColors.primary : AppColors.mutedForeground,
                context,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _SocialButton extends StatelessWidget {
  const _SocialButton({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        CupertinoButton(
          padding: EdgeInsets.zero,
          onPressed: () {},
          child: Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              border: Border.all(color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.5)),
              borderRadius: BorderRadius.circular(AppRadii.md),
            ),
            child: Icon(icon, size: 24, color: CupertinoDynamicColor.resolve(AppColors.foreground, context)),
          ),
        ),
        const SizedBox(height: 6),
        Text(label, style: AppTextStyles.label(context).copyWith(fontSize: 10)),
      ],
    );
  }
}
