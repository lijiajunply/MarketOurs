import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import 'auth_scaffold.dart';

class RegisterVerifyScreen extends ConsumerStatefulWidget {
  const RegisterVerifyScreen({
    super.key,
    required this.registrationToken,
    this.account,
  });

  final String registrationToken;
  final String? account;

  @override
  ConsumerState<RegisterVerifyScreen> createState() =>
      _RegisterVerifyScreenState();
}

class _RegisterVerifyScreenState extends ConsumerState<RegisterVerifyScreen> {
  final _formKey = GlobalKey<FormState>();
  final _codeController = TextEditingController();

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _sendCode() async {
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref
          .read(authControllerProvider.notifier)
          .sendRegistrationCode(widget.registrationToken);
      if (mounted) {
        messenger.showSnackBar(const SnackBar(content: Text('验证码已发送')));
      }
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

  Future<void> _submit() async {
    final messenger = ScaffoldMessenger.of(context);
    if (!_formKey.currentState!.validate()) {
      return;
    }

    try {
      await ref
          .read(authControllerProvider.notifier)
          .verifyRegistration(
            registrationToken: widget.registrationToken,
            code: _codeController.text.trim(),
          );

      if (!mounted) {
        return;
      }

      messenger.showSnackBar(const SnackBar(content: Text('注册完成，请使用账号密码登录')));
      context.go(AppRoutePaths.login);
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
      title: '验证注册',
      subtitle: widget.account == null
          ? '输入收到的验证码完成注册。'
          : '我们将为 ${widget.account} 发送验证码，请完成验证。',
      footer: Center(
        child: TextButton(
          onPressed: isSubmitting
              ? null
              : () => context.go(AppRoutePaths.register),
          child: const Text('返回上一步'),
        ),
      ),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            OutlinedButton(
              onPressed: isSubmitting ? null : _sendCode,
              child: const Text('发送验证码'),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _codeController,
              decoration: const InputDecoration(labelText: '验证码'),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入验证码';
                }
                return null;
              },
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '验证中...' : '完成注册'),
            ),
          ],
        ),
      ),
    );
  }
}
