import 'package:flutter/cupertino.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_widgets.dart';
import '../../utils/dto_validation.dart';
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
      await AppFeedback.showSuccess(context, message: '密码修改成功');
      if (!mounted) {
        return;
      }
      context.go(AppRoutePaths.profile);
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
            : AppLocalizations.of(context)!.authChangePasswordFailed,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = authState?.isSubmitting ?? false;

    return AppPageScaffold(
      title: AppLocalizations.of(context)!.profileChangePasswordTitle,
      navigationBarStyle: AppNavigationBarStyle.compact,
      maxContentWidth: AppResponsive.readableMaxWidth(context, fallback: 560),
      child: Form(
        key: _formKey,
        child: Column(
          children: [
            PasswordFormField(
              controller: _oldPasswordController,
              placeholder: '当前密码',
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
              placeholder: '新密码，至少 6 位',
              maxLength: DtoLimits.userPasswordMax,
              validator: (value) {
                return passwordLengthValidator(
                  value,
                  emptyMessage: '请输入新密码',
                  minMessage: '密码至少 ${DtoLimits.userPasswordMin} 位',
                  maxMessage: '密码长度不能超过 ${DtoLimits.userPasswordMax} 位',
                );
              },
            ),
            const SizedBox(height: 16),
            PasswordFormField(
              controller: _confirmPasswordController,
              placeholder: '确认新密码',
              validator: (value) {
                if (value != _newPasswordController.text) {
                  return '两次输入的密码不一致';
                }
                return null;
              },
            ),
            const SizedBox(height: 24),
            AppPrimaryButton(
              onPressed: isSubmitting ? null : _submit,
              child: Text(isSubmitting ? '提交中...' : '保存新密码'),
            ),
          ],
        ),
      ),
    );
  }
}
