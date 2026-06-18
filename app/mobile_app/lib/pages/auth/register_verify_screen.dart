import 'dart:async';

import 'package:flutter/cupertino.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';
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
  Timer? _countdownTimer;
  int _countdown = 60;
  bool _isCodeValid = false;

  @override
  void initState() {
    super.initState();
    _codeController.addListener(_onCodeChanged);
    _startCountdown();
  }

  void _onCodeChanged() {
    final valid = _codeController.text.trim().length >= 4;
    if (valid != _isCodeValid && mounted) {
      setState(() => _isCodeValid = valid);
    }
  }

  @override
  void dispose() {
    _countdownTimer?.cancel();
    _codeController.dispose();
    super.dispose();
  }

  void _startCountdown() {
    _countdownTimer?.cancel();
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_countdown <= 1) {
        timer.cancel();
        if (mounted) setState(() => _countdown = 0);
        return;
      }
      if (mounted) setState(() => _countdown -= 1);
    });
  }

  Future<void> _resendCode() async {
    if (_countdown > 0) return;

    try {
      await ref
          .read(authControllerProvider.notifier)
          .sendRegistrationCode(widget.registrationToken);
      if (mounted) {
        setState(() => _countdown = 60);
        _startCountdown();
      }
    } catch (_) {
      if (!mounted) return;
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      await AppFeedback.showError(
        context,
        message: (errorMessage != null && errorMessage.isNotEmpty)
            ? errorMessage
            : AppLocalizations.of(context).authSendCodeFailed,
      );
    }
  }

  Future<void> _submit() async {
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

      if (!mounted) return;

      await AppFeedback.showSuccess(context, message: AppLocalizations.of(context).registerComplete);
      if (!mounted) return;
      context.go(AppRoutePaths.login);
    } catch (_) {
      if (!mounted) return;
      final errorMessage = ref
          .read(authControllerProvider)
          .asData
          ?.value
          .errorMessage;
      await AppFeedback.showError(
        context,
        message: (errorMessage != null && errorMessage.isNotEmpty)
            ? errorMessage
            : AppLocalizations.of(context).authVerifyFailed,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final isSubmitting = authState?.isSubmitting ?? false;

    return AuthScaffold(
      title: AppLocalizations.of(context).authRegisterVerifyTitle,
      footer: Center(
        child: CupertinoButton(
          onPressed: isSubmitting
              ? null
              : () => context.go(AppRoutePaths.register),
          child: Text(
            AppLocalizations.of(context).goBack,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
        ),
      ),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Account confirmation
            if (widget.account != null && widget.account!.isNotEmpty) ...[
              Center(
                child: Text(
                  '验证码已发送至 ${widget.account}',
                  style: AppTextStyles.label(context),
                  textAlign: TextAlign.center,
                ),
              ),
              const SizedBox(height: 20),
            ],

            // Code input
            AppTextField(
              controller: _codeController,
              placeholder: '------',
              keyboardType: TextInputType.number,
              prefix: Icon(
                CupertinoIcons.shield_fill,
                color: AppColors.mutedForeground,
                size: 18,
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return AppLocalizations.of(context).validatorCodeRequired;
                }
                if (value.trim().length < 4) {
                  return AppLocalizations.of(context).codeMinLength;
                }
                return null;
              },
            ),
            const SizedBox(height: 20),

            // Resend button
            Center(
              child: CupertinoButton(
                onPressed: isSubmitting || _countdown > 0 ? null : _resendCode,
                padding: EdgeInsets.zero,
                child: Text(
                  _countdown > 0 ? '${_countdown}s 后重新发送' : AppLocalizations.of(context).authResendCode,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: _countdown > 0
                        ? AppColors.mutedForeground
                        : AppColors.primary,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 24),

            AppPrimaryButton(
              onPressed: isSubmitting || !_isCodeValid ? null : _submit,
              child: Text(isSubmitting ? AppLocalizations.of(context).profileSaving : AppLocalizations.of(context).submit),
            ),
          ],
        ),
      ),
    );
  }
}
