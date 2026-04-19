import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import 'auth_scaffold.dart';
import 'password_form_field.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _accountController = TextEditingController();
  final _nameController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _accountController.dispose();
    _nameController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final messenger = ScaffoldMessenger.of(context);
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
          );

      if (!mounted) {
        return;
      }

      messenger.showSnackBar(const SnackBar(content: Text('注册信息已提交，请完成验证码验证')));
      context.goNamed(
        AppRouteNames.registerVerify,
        queryParameters: {
          'registrationToken': registrationToken,
          'account': _accountController.text.trim(),
        },
      );
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

    return AuthScaffold(
      title: '注册账号',
      subtitle: '先填写基础信息，我们会在下一步通过验证码完成注册验证。',
      footer: Center(
        child: TextButton(
          onPressed: isSubmitting
              ? null
              : () => context.go(AppRoutePaths.login),
          child: const Text('已有账号？返回登录'),
        ),
      ),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _accountController,
              decoration: const InputDecoration(
                labelText: '账号',
                hintText: '邮箱或手机号',
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入账号';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _nameController,
              decoration: const InputDecoration(
                labelText: '昵称',
                hintText: '给自己起个名字',
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入昵称';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            PasswordFormField(
              controller: _passwordController,
              decoration: const InputDecoration(
                labelText: '密码',
                hintText: '至少 6 位',
              ),
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return '请输入密码';
                }
                if (value.length < 6) {
                  return '密码至少 6 位';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            PasswordFormField(
              controller: _confirmPasswordController,
              decoration: const InputDecoration(labelText: '确认密码'),
              validator: (value) {
                if (value != _passwordController.text) {
                  return '两次输入的密码不一致';
                }
                return null;
              },
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '提交中...' : '下一步'),
            ),
          ],
        ),
      ),
    );
  }
}
