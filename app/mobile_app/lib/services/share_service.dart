import 'package:flutter/widgets.dart';
import 'package:share_plus/share_plus.dart';

import '../models/post.dart';

const _defaultPublicWebBaseUrl = String.fromEnvironment(
  'PUBLIC_WEB_BASE_URL',
  defaultValue: 'https://lumalis.luckyfishes.site',
);

class ShareService {
  const ShareService();

  String buildPostShareUrl(String postId) {
    final baseUri = Uri.parse(_normalizeBaseUrl(_defaultPublicWebBaseUrl));
    return baseUri.resolve('/post/$postId').toString();
  }

  Future<void> sharePost(PostDto post, {Rect? sharePositionOrigin}) {
    return SharePlus.instance.share(
      ShareParams(
        uri: Uri.parse(buildPostShareUrl(post.id)),
        sharePositionOrigin: sharePositionOrigin,
      ),
    );
  }

  String _normalizeBaseUrl(String value) {
    final trimmed = value.trim();
    if (trimmed.endsWith('/')) {
      return trimmed;
    }
    return '$trimmed/';
  }
}
