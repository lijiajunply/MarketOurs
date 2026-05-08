import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../auth/password_form_field.dart';

class ChangePasswordScreen extends ConsumerStatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  ConsumerState<ChangePasswordScreen> createState() =>
      _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends ConsumerState<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _oldPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _oldPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final messenger = ScaffoldMessenger.of(context);
    if (!_formKey.currentState!.validate()) {
      return;
    }

    try {
      await ref
          .read(authControllerProvider.notifier)
          .changePassword(
            oldPassword: _oldPasswordController.text,
            newPassword: _newPasswordController.text,
          );
      if (!mounted) {
        return;
      }
      messenger.showSnackBar(const SnackBar(content: Text('密码修改成功')));
      context.go(AppRoutePaths.profile);
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

    return Scaffold(
      appBar: AppBar(title: const Text('修改密码')),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              PasswordFormField(
                controller: _oldPasswordController,
                decoration: const InputDecoration(
                  labelText: '当前密码',
                  hintText: '请输入当前密码',
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return '请输入当前密码';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              PasswordFormField(
                controller: _newPasswordController,
                decoration: const InputDecoration(
                  labelText: '新密码',
                  hintText: '至少 6 位，建议包含大小写字母和数字',
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return '请输入新密码';
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
                decoration: const InputDecoration(labelText: '确认新密码'),
                validator: (value) {
                  if (value != _newPasswordController.text) {
                    return '两次输入的密码不一致';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: isSubmitting ? null : _submit,
                child: Text(isSubmitting ? '提交中...' : '保存新密码'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
