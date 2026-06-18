import 'package:flutter/cupertino.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_widgets.dart';
import 'auth_scaffold.dart';
import 'password_form_field.dart';

class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({super.key, this.initialToken, this.account});

  final String? initialToken;
  final String? account;

  @override
  ConsumerState<ResetPasswordScreen> createState() =>
      _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _tokenController;
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tokenController = TextEditingController(text: widget.initialToken ?? '');
  }

  @override
  void dispose() {
    _tokenController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    try {
      await ref
          .read(authControllerProvider.notifier)
          .resetPassword(
            token: _tokenController.text.trim(),
            newPassword: _passwordController.text,
          );

      if (!mounted) {
        return;
      }

      await AppFeedback.showSuccess(context, message: '密码已重置，请重新登录');
      if (!mounted) {
        return;
      }
      context.go(AppRoutePaths.login);
    } catch (_) {
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      await AppFeedback.showError(
        context,
        message: (errorMessage != null && errorMessage.isNotEmpty)
            ? errorMessage
            : '重置密码失败，请稍后重试',
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = authState?.isSubmitting ?? false;

    return AuthScaffold(
      title: AppLocalizations.of(context)!.authResetPassword,
      footer: Center(
        child: CupertinoButton(
          onPressed: isSubmitting
              ? null
              : () => context.go(AppRoutePaths.forgotPassword),
          child: const Text('重新获取重置说明'),
        ),
      ),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AppTextField(
              controller: _tokenController,
              placeholder: '验证码 / Token',
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return '请输入验证码或 Token';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            PasswordFormField(
              controller: _passwordController,
              placeholder: '新密码',
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
              placeholder: '确认新密码',
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
              child: Text(isSubmitting ? AppLocalizations.of(context)!.profileSaving : AppLocalizations.of(context)!.authResetPassword),
            ),
          ],
        ),
      ),
    );
  }
}
