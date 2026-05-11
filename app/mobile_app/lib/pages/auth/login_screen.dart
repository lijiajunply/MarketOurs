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
    if (!_formKey.currentState!.validate()) {
      return;
    }
    
    final account = _accountController.text.trim();
    setState(() => _isSendingCode = true);
    try {
      await ref
          .read(authControllerProvider.notifier)
          .sendLoginCode(account: account);
      if (!mounted) {
        return;
      }
      _startCountdown();
      await AppFeedback.showMessage(context, message: '验证码已发送');
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && mounted) {
        await AppFeedback.showMessage(context, message: errorMessage);
      }
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
              padding: const EdgeInsets.all(4),
              decoration: BoxDecoration(
                color: AppColors.secondary,
                borderRadius: BorderRadius.circular(AppRadii.lg),
              ),
              child: CupertinoSlidingSegmentedControl<_LoginMode>(
                groupValue: _loginMode,
                thumbColor: AppColors.card,
                backgroundColor: AppColors.secondary,
                children: const {
                  _LoginMode.password: Padding(
                    padding: EdgeInsets.symmetric(vertical: 10),
                    child: Text('密码登录'),
                  ),
                  _LoginMode.otp: Padding(
                    padding: EdgeInsets.symmetric(vertical: 10),
                    child: Text('验证码登录'),
                  ),
                },
                onValueChanged: (selection) {
                  if (selection != null) {
                    setState(() => _loginMode = selection);
                  }
                },
              ),
            ),
            const SizedBox(height: 20),
            AppTextField(
              controller: _accountController,
              placeholder: '账号 / 邮箱 / 手机号',
              prefix: const Icon(
                CupertinoIcons.person_crop_circle,
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
            const SizedBox(height: 16),
            if (_loginMode == _LoginMode.password) ...[
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
              const SizedBox(height: 12),
              Align(
                alignment: Alignment.centerRight,
                child: CupertinoButton(
                  padding: EdgeInsets.zero,
                  minimumSize: Size.zero,
                  onPressed: isSubmitting
                      ? null
                      : () => context.go(AppRoutePaths.forgotPassword),
                  child: const Text(
                    '忘记密码？',
                    style: TextStyle(
                      color: AppColors.primary,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ),
            ] else ...[
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: AppTextField(
                      controller: _otpController,
                      placeholder: '请输入 6 位验证码',
                      keyboardType: TextInputType.number,
                      prefix: const Icon(
                        CupertinoIcons.shield,
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
                    width: 116,
                    child: AppSecondaryButton(
                      onPressed:
                          isSubmitting || _isSendingCode || _countdown > 0
                          ? null
                          : _sendCode,
                      padding: const EdgeInsets.symmetric(
                        vertical: 17,
                        horizontal: 10,
                      ),
                      child: Text(
                        _isSendingCode
                            ? '发送中'
                            : _countdown > 0
                            ? '${_countdown}s'
                            : '发送验证码',
                        textAlign: TextAlign.center,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text('验证码会发送到你填写的邮箱或手机号。', style: AppTextStyles.muted(context)),
            ],
            const SizedBox(height: 22),
            AppPrimaryButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '登录中...' : '登录'),
            ),
          ],
        ),
      ),
    );
  }
}
