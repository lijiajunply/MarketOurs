import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../providers/auth_provider.dart';
import '../../router/app_router.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class BindingScreen extends ConsumerWidget {
  const BindingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authControllerProvider).asData?.value;
    final user = authState?.user;

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: CustomScrollView(
        slivers: [
          const CupertinoSliverNavigationBar(
            largeTitle: Text('第三方绑定'),
            border: null,
          ),
          CupertinoSliverRefreshControl(
            onRefresh: () =>
                ref.read(authControllerProvider.notifier).refreshProfile(),
          ),
          SliverToBoxAdapter(
            child: AppResponsiveCenter(
              padding: AppResponsive.sliverPagePadding(context, bottom: 32),
              child: _BindingSection(user: user),
            ),
          ),
        ],
      ),
    );
  }
}

class _BindingSection extends StatelessWidget {
  const _BindingSection({required this.user});

  final dynamic user;

  @override
  Widget build(BuildContext context) {
    final providers = [
      _ProviderInfo(
        name: 'Ours',
        icon: CupertinoIcons.person_circle,
        isBound: user?.oursId != null,
      ),
      _ProviderInfo(
        name: 'Github',
        icon: CupertinoIcons.cloud,
        isBound: user?.githubId != null,
      ),
      _ProviderInfo(
        name: 'Google',
        icon: CupertinoIcons.globe,
        isBound: user?.googleId != null,
      ),
      _ProviderInfo(
        name: 'Weixin',
        icon: CupertinoIcons.chat_bubble_text,
        isBound: user?.weixinId != null,
      ),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 8, bottom: 12),
          child: Text(
            '将您的账号与第三方平台关联，方便快速登录',
            style: AppTextStyles.muted(context),
          ),
        ),
        AppTappableCard(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          radius: AppRadii.lg,
          child: Column(
            children: [
              for (int i = 0; i < providers.length; i++) ...[
                if (i > 0)
                  Container(
                    height: 1,
                    margin: const EdgeInsets.only(left: 44),
                    color: CupertinoDynamicColor.resolve(
                      AppColors.border,
                      context,
                    ).withValues(alpha: 0.3),
                  ),
                _BindingRow(
                  provider: providers[i],
                  onBind: () => _handleBind(context, providers[i].name),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }

  static void _handleBind(BuildContext context, String provider) {
    final location = Uri(
      path: AppRoutePaths.oauthWebView,
      queryParameters: {
        'provider': provider,
        'purpose': 'bind',
        '_ts': DateTime.now().microsecondsSinceEpoch.toString(),
      },
    ).toString();
    context.push(location);
  }
}

class _ProviderInfo {
  final String name;
  final IconData icon;
  final bool isBound;

  const _ProviderInfo({
    required this.name,
    required this.icon,
    required this.isBound,
  });
}

class _BindingRow extends StatelessWidget {
  const _BindingRow({required this.provider, required this.onBind});

  final _ProviderInfo provider;
  final VoidCallback onBind;

  @override
  Widget build(BuildContext context) {
    return AppListTile(
      padding: const EdgeInsets.symmetric(vertical: 12),
      leading: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: provider.isBound
              ? const Color(0xFF34C759).withValues(alpha: 0.12)
              : AppColors.primary.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Icon(
          provider.icon,
          size: 20,
          color: provider.isBound ? const Color(0xFF34C759) : AppColors.primary,
        ),
      ),
      title: Text(
        provider.name,
        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
      ),
      trailing: provider.isBound
          ? Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: const Color(0xFF34C759).withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Text(
                '已绑定',
                style: TextStyle(
                  color: Color(0xFF34C759),
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
            )
          : CupertinoButton(
              padding: EdgeInsets.zero,
              minimumSize: Size.zero,
              onPressed: onBind,
              child: const Text(
                '去绑定',
                style: TextStyle(
                  color: AppColors.primary,
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
    );
  }
}
