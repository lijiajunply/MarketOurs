import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/post.dart';
import 'post_feed_provider.dart';

final postDetailProvider = FutureProvider.autoDispose.family<PostDto, String>((
  ref,
  postId,
) async {
  final service = ref.read(postServiceProvider);
  final response = await service.getPost(postId);
  final post = response.data;

  if (post == null) {
    throw Exception(response.message ?? '帖子详情不存在');
  }

  return post;
});
