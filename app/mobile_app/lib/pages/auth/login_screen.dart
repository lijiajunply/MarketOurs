import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
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
    final messenger = ScaffoldMessenger.of(context);
    final account = _accountController.text.trim();
    if (account.isEmpty) {
      messenger.showSnackBar(const SnackBar(content: Text('请先输入账号')));
      return;
    }

    setState(() => _isSendingCode = true);
    try {
      await ref
          .read(authControllerProvider.notifier)
          .sendLoginCode(account: account);
      if (!mounted) {
        return;
      }
      _startCountdown();
      messenger.showSnackBar(const SnackBar(content: Text('验证码已发送')));
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      if (errorMessage != null && errorMessage.isNotEmpty && mounted) {
        messenger.showSnackBar(SnackBar(content: Text(errorMessage)));
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
    final messenger = ScaffoldMessenger.of(context);
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
      messenger.showSnackBar(const SnackBar(content: Text('登录成功')));
      context.go(AppRoutePaths.home);
      return;
    }

    if (errorMessage != null && errorMessage.isNotEmpty) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = authState?.isSubmitting ?? false;

    return AuthScaffold(
      title: '登录',
      subtitle: '支持账号密码登录，也支持验证码快捷登录。',
      footer: Center(
        child: TextButton(
          onPressed: isSubmitting
              ? null
              : () => context.go(AppRoutePaths.register),
          child: const Text('没有账号？去注册'),
        ),
      ),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SegmentedButton<_LoginMode>(
              showSelectedIcon: false,
              segments: const [
                ButtonSegment<_LoginMode>(
                  value: _LoginMode.password,
                  label: Text('密码登录'),
                ),
                ButtonSegment<_LoginMode>(
                  value: _LoginMode.otp,
                  label: Text('验证码登录'),
                ),
              ],
              selected: {_loginMode},
              onSelectionChanged: (selection) {
                setState(() => _loginMode = selection.first);
              },
            ),
            const SizedBox(height: 20),
            TextFormField(
              controller: _accountController,
              decoration: const InputDecoration(
                labelText: '账号',
                hintText: '邮箱或手机号',
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
                decoration: const InputDecoration(
                  labelText: '密码',
                  hintText: '请输入密码',
                ),
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
                child: TextButton(
                  onPressed: isSubmitting
                      ? null
                      : () => context.go(AppRoutePaths.forgotPassword),
                  child: const Text('忘记密码？'),
                ),
              ),
            ] else ...[
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _otpController,
                      decoration: const InputDecoration(
                        labelText: '验证码',
                        hintText: '请输入 6 位验证码',
                      ),
                      keyboardType: TextInputType.number,
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
                    height: 52,
                    child: OutlinedButton(
                      onPressed:
                          isSubmitting || _isSendingCode || _countdown > 0
                          ? null
                          : _sendCode,
                      child: Text(
                        _isSendingCode
                            ? '发送中'
                            : _countdown > 0
                            ? '${_countdown}s'
                            : '发送验证码',
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                '验证码会发送到你填写的邮箱或手机号。',
                style: Theme.of(
                  context,
                ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
              ),
            ],
            const SizedBox(height: 20),
            FilledButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '登录中...' : '登录'),
            ),
          ],
        ),
      ),
    );
  }
}
