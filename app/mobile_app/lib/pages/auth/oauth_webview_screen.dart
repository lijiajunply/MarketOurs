import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
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
        (message.contains('绑定成功') || message.contains('Binding successful'));
    if (isBindSuccess) {
      _handleBindSuccess();
      return;
    }

    final accessToken = uri.queryParameters['accessToken'];
    final refreshToken = uri.queryParameters['refreshToken'];

    if (accessToken == null || refreshToken == null) {
      _fail('登录失败，缺少令牌');
      return;
    }

    _processTokens(accessToken, refreshToken);
  }

  Future<void> _handleBindSuccess() async {
    try {
      await ref.read(authControllerProvider.notifier).refreshProfile();
      if (!mounted) return;
      await AppFeedback.showSuccess(context, message: '绑定成功');
      if (!mounted) return;
      context.pop();
    } catch (_) {
      if (!mounted) return;
      _fail('绑定成功，但刷新资料失败，请稍后下拉刷新');
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
        await AppFeedback.showSuccess(context, message: '登录成功');
        if (!mounted) return;
        context.go(AppRoutePaths.home);
      } else {
        final authState = ref.read(authControllerProvider).asData?.value;
        final errorMessage = authState?.errorMessage;
        _fail(errorMessage ?? '登录失败，请稍后重试');
      }
    } catch (_) {
      if (!mounted) return;
      _fail('登录失败，请稍后重试');
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
    final title = widget.purpose == 'bind'
        ? '绑定 ${widget.provider}'
        : '${widget.provider} 登录';

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
                _isLaunching ? '正在打开系统浏览器' : '请在浏览器中完成授权',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                _statusMessage ?? '完成后会自动回到光汇',
                textAlign: TextAlign.center,
                style: AppTextStyles.muted(context),
              ),
              const SizedBox(height: 28),
              AppPrimaryButton(
                onPressed: _isLaunching ? null : _startOAuth,
                child: Text(_isLaunching ? '正在打开' : '重新打开'),
              ),
              const SizedBox(height: 12),
              CupertinoButton(
                onPressed: () => context.pop(),
                child: const Text('取消'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
