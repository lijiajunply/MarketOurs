import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../l10n/app_localizations.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../services/auth_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class OAuthWebViewScreen extends ConsumerStatefulWidget {
  const OAuthWebViewScreen({
    super.key,
    required this.provider,
    this.purpose = 'login',
  });

  final String provider;
  final String purpose;

  @override
  ConsumerState<OAuthWebViewScreen> createState() => _OAuthWebViewScreenState();
}

class _OAuthWebViewScreenState extends ConsumerState<OAuthWebViewScreen> {
  final AppLinks _appLinks = AppLinks();
  StreamSubscription<Uri>? _linkSubscription;
  bool _isLaunching = true;
  bool _callbackHandled = false;
  String? _statusMessage;

  @override
  void initState() {
    super.initState();
    _listenForCallback();
    WidgetsBinding.instance.addPostFrameCallback((_) => _startOAuth());
  }

  @override
  void dispose() {
    _linkSubscription?.cancel();
    super.dispose();
  }

  void _listenForCallback() {
    _linkSubscription = _appLinks.uriLinkStream.listen(
      _handleIncomingUri,
      onError: (_) {
        if (mounted && !_callbackHandled) {
          setState(() => _statusMessage = '无法接收登录回调，请重试');
        }
      },
    );
  }

  Future<void> _startOAuth() async {
    setState(() {
      _isLaunching = true;
      _statusMessage = null;
    });

    try {
      final initialUri = await _appLinks.getInitialLink();
      if (initialUri != null && AuthService.isOAuthCallback(initialUri)) {
        _handleIncomingUri(initialUri);
        return;
      }

      final loginUrl = await _buildLoginUrl();
      final launched = await launchUrl(
        Uri.parse(loginUrl),
        mode: LaunchMode.externalApplication,
      );

      if (!mounted || _callbackHandled) return;

      setState(() {
        _isLaunching = false;
        _statusMessage = launched ? null : '无法打开系统浏览器，请检查系统设置';
      });
    } catch (_) {
      if (!mounted || _callbackHandled) return;
      setState(() {
        _isLaunching = false;
        _statusMessage = '无法发起第三方认证，请稍后重试';
      });
    }
  }

  Future<String> _buildLoginUrl() async {
    String? accessToken;
    if (widget.purpose == 'bind') {
      accessToken = await ref.read(authStorageProvider).readAccessToken();
    }

    return AuthService().buildExternalLoginUrl(
      provider: widget.provider,
      returnUrl: AuthService.mobileOAuthCallbackUrl,
      purpose: widget.purpose,
      accessToken: accessToken,
    );
  }

  void _handleIncomingUri(Uri uri) {
    if (_callbackHandled || !AuthService.isOAuthCallback(uri)) {
      return;
    }
    _callbackHandled = true;
    _handleCallback(uri);
  }

  void _handleCallback(Uri uri) {
    final error = uri.queryParameters['error'];
    if (error != null) {
      _fail(error);
      return;
    }

    final message = uri.queryParameters['message'];
    final isBindSuccess =
        message != null &&
        (message.contains(AppLocalizations.of(context).commentBindingSuccess) || message.contains('Binding successful'));
    if (isBindSuccess) {
      _handleBindSuccess();
      return;
    }

    final accessToken = uri.queryParameters['accessToken'];
    final refreshToken = uri.queryParameters['refreshToken'];

    if (accessToken == null || refreshToken == null) {
      _fail(AppLocalizations.of(context).oauthWebViewLoginFailed);
      return;
    }

    _processTokens(accessToken, refreshToken);
  }

  Future<void> _handleBindSuccess() async {
    try {
      await ref.read(authControllerProvider.notifier).refreshProfile();
      if (!mounted) return;
      await AppFeedback.showSuccess(context, message: AppLocalizations.of(context).commentBindingSuccess);
      if (!mounted) return;
      context.pop();
    } catch (_) {
      if (!mounted) return;
      _fail(AppLocalizations.of(context).commentBindingSuccessRefreshFailed);
    }
  }

  Future<void> _processTokens(String accessToken, String refreshToken) async {
    try {
      final success = await ref
          .read(authControllerProvider.notifier)
          .handleOAuthTokens(
            accessToken: accessToken,
            refreshToken: refreshToken,
          );

      if (!mounted) return;

      if (success) {
        await AppFeedback.showSuccess(context, message: AppLocalizations.of(context).authLoginSuccess);
        if (!mounted) return;
        context.go(AppRoutePaths.home);
      } else {
        final authState = ref.read(authControllerProvider).asData?.value;
        final errorMessage = authState?.errorMessage;
        _fail(errorMessage ?? AppLocalizations.of(context).authLoginFailed);
      }
    } catch (_) {
      if (!mounted) return;
      _fail(AppLocalizations.of(context).authLoginFailed);
    }
  }

  void _fail(String message) {
    AppFeedback.showError(context, message: message);
    if (mounted) {
      context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final title = widget.purpose == 'bind'
        ? l10n.oauthBindProvider(widget.provider)
        : l10n.oauthLoginProvider(widget.provider);

    return CupertinoPageScaffold(
      navigationBar: CupertinoNavigationBar(
        middle: Text(title),
        leading: CupertinoButton(
          padding: EdgeInsets.zero,
          onPressed: () => context.pop(),
          child: const Icon(CupertinoIcons.xmark),
        ),
      ),
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const CupertinoActivityIndicator(radius: 16),
              const SizedBox(height: 20),
              Text(
                _isLaunching ? l10n.oauthWebViewOpenBrowser : l10n.oauthWebViewAuthorizeInBrowser,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                _statusMessage ?? l10n.oauthWebViewAutoReturn,
                textAlign: TextAlign.center,
                style: AppTextStyles.muted(context),
              ),
              const SizedBox(height: 28),
              AppPrimaryButton(
                onPressed: _isLaunching ? null : _startOAuth,
                child: Text(_isLaunching ? AppLocalizations.of(context).oauthWebViewOpening : AppLocalizations.of(context).oauthWebViewReopen),
              ),
              const SizedBox(height: 12),
              CupertinoButton(
                onPressed: () => context.pop(),
                child: Text(AppLocalizations.of(context).cancel),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
