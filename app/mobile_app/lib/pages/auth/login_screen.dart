import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import 'auth_scaffold.dart';
import 'password_form_field.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _accountController = TextEditingController();
  final _passwordController = TextEditingController();

  @override
  void dispose() {
    _accountController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final messenger = ScaffoldMessenger.of(context);
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final success = await ref
        .read(authControllerProvider.notifier)
        .login(
          account: _accountController.text.trim(),
          password: _passwordController.text,
        );

    if (!mounted) {
      return;
    }

    final authState = ref.read(authControllerProvider).asData?.value;
    final errorMessage = authState?.errorMessage;
    if (success) {
      messenger.showSnackBar(const SnackBar(content: Text('登录成功')));
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
      subtitle: '输入账号和密码进入 MarketOurs。首次移动端接入已默认启用登录态持久化。',
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
            TextFormField(
              controller: _accountController,
              decoration: const InputDecoration(
                labelText: '账号',
                hintText: '邮箱或手机号',
              ),
              textInputAction: TextInputAction.next,
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入账号';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
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
            const SizedBox(height: 12),
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
