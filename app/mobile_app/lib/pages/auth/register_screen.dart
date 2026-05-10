import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
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

      await AppFeedback.showMessage(context, message: '注册信息已提交，请完成验证码验证');
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
        await AppFeedback.showMessage(context, message: errorMessage);
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
        child: CupertinoButton(
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
            AppTextField(
              controller: _accountController,
              placeholder: '账号 / 邮箱 / 手机号',
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入账号';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            AppTextField(
              controller: _nameController,
              placeholder: '给自己起个名字',
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
              placeholder: '密码，至少 6 位',
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
              placeholder: '再次输入密码',
              validator: (value) {
                if (value != _passwordController.text) {
                  return '两次输入的密码不一致';
                }
                return null;
              },
            ),
            const SizedBox(height: 24),
            AppPrimaryButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '提交中...' : '下一步'),
            ),
          ],
        ),
      ),
    );
  }
}
