import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../components/user_card.dart';
import '../../models/comment.dart';
import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/comment_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

class PostDetailScreen extends ConsumerStatefulWidget {
  const PostDetailScreen({super.key, required this.postId});

  final String postId;

  @override
  ConsumerState<PostDetailScreen> createState() => _PostDetailScreenState();
}

class _PostDetailScreenState extends ConsumerState<PostDetailScreen> {
  final _commentService = CommentService();
  final _commentController = TextEditingController();
  PostDto? _post;
  List<CommentDto> _comments = const [];
  bool _isLoading = true;
  bool _isWorking = false;
  String? _errorMessage;
  String _commentSort = 'recent';
  bool _postLiked = false;
  bool _postDisliked = false;
  final Set<String> _likedComments = {};
  final Set<String> _dislikedComments = {};

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final postService = ref.read(postServiceProvider);
      final results = await Future.wait([
        postService.getPost(widget.postId),
        postService.getPostComments(widget.postId, _commentSort),
      ]);

      final post = (results[0] as dynamic).data as PostDto?;
      final comments = (results[1] as dynamic).data as List<CommentDto>?;
      if (post == null) {
        throw Exception('帖子不存在');
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _post = post;
        _comments = comments ?? const [];
      });
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _errorMessage = error.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _runAction(
    Future<void> Function() action, {
    String? successMessage,
    bool reloadAll = true,
  }) async {
    setState(() => _isWorking = true);

    try {
      await action();
      if (reloadAll) {
        await _loadData();
      }
      if (successMessage != null && mounted) {
        await AppFeedback.showMessage(context, message: successMessage);
      }
    } catch (error) {
      if (!mounted) {
        return;
      }
      await AppFeedback.showMessage(
        context,
        message: error.toString().replaceFirst('Exception: ', ''),
      );
    } finally {
      if (mounted) {
        setState(() => _isWorking = false);
      }
    }
  }

  Future<void> _submitComment() async {
    final user = ref.read(authControllerProvider).asData?.value.user;
    if (user == null) {
      context.go(AppRoutePaths.login);
      return;
    }

    final content = _commentController.text.trim();
    if (content.isEmpty) {
      return;
    }

    await _runAction(() async {
      await _commentService.createComment(
        CommentCreateDto(
          content: content,
          userId: user.id,
          postId: widget.postId,
        ),
      );
      _commentController.clear();
    }, successMessage: '评论已发布');
  }

  Future<void> _replyComment(CommentDto comment) async {
    final user = ref.read(authControllerProvider).asData?.value.user;
    if (user == null) {
      context.go(AppRoutePaths.login);
      return;
    }

    final content = await _openTextComposer(
      title: '回复评论',
      initialValue: '',
      hintText: '输入回复内容',
    );
    if (content == null || content.trim().isEmpty) {
      return;
    }

    await _runAction(() async {
      await _commentService.replyToComment(
        comment.id,
        CommentCreateDto(
          content: content.trim(),
          userId: user.id,
          postId: widget.postId,
          parentCommentId: comment.id,
        ),
      );
    }, successMessage: '回复已发送');
  }

  Future<void> _editComment(CommentDto comment) async {
    final content = await _openTextComposer(
      title: '编辑评论',
      initialValue: comment.content ?? '',
      hintText: '更新评论内容',
    );
    if (content == null || content.trim().isEmpty) {
      return;
    }

    await _runAction(() async {
      await _commentService.updateComment(
        comment.id,
        CommentUpdateDto(content: content.trim()),
      );
    }, successMessage: '评论已更新');
  }

  Future<void> _deleteComment(CommentDto comment) async {
    final confirmed = await _confirm('确定删除这条评论吗？');
    if (confirmed != true) {
      return;
    }

    await _runAction(() async {
      await _commentService.deleteComment(comment.id);
    }, successMessage: '评论已删除');
  }

  Future<void> _editPost() async {
    final post = _post;
    if (post == null) {
      return;
    }

    final values = await _openPostEditor(
      title: post.title ?? '',
      content: post.content ?? '',
    );
    if (values == null) {
      return;
    }

    await _runAction(() async {
      await ref
          .read(postServiceProvider)
          .updatePost(
            post.id,
            PostUpdateDto(
              title: values.$1,
              content: values.$2,
              images: post.images,
            ),
          );
    }, successMessage: '帖子已更新');
  }

  Future<void> _deletePost() async {
    final post = _post;
    if (post == null) {
      return;
    }

    final confirmed = await _confirm('确定删除这篇帖子吗？');
    if (confirmed != true) {
      return;
    }

    await _runAction(
      () async {
        await ref.read(postServiceProvider).deletePost(post.id);
        if (mounted) {
          context.go(AppRoutePaths.home);
        }
      },
      successMessage: '帖子已删除',
      reloadAll: false,
    );
  }

  Future<bool?> _confirm(String message) {
    return AppFeedback.confirm(context, message: message, destructive: true);
  }

  Future<String?> _openTextComposer({
    required String title,
    required String initialValue,
    required String hintText,
  }) async {
    final controller = TextEditingController(text: initialValue);
    final result = await showAppBottomSheet<String>(
      context: context,
      builder: (context) {
        return Padding(
          padding: EdgeInsets.only(
            left: 20,
            right: 20,
            top: 20,
            bottom: MediaQuery.of(context).viewInsets.bottom + 20,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: AppTextStyles.sectionTitle),
              const SizedBox(height: 16),
              AppTextField(
                controller: controller,
                maxLines: 5,
                placeholder: hintText,
              ),
              const SizedBox(height: 16),
              AppPrimaryButton(
                onPressed: () => Navigator.of(context).pop(controller.text),
                child: const Text('保存'),
              ),
            ],
          ),
        );
      },
    );
    controller.dispose();
    return result;
  }

  Future<(String, String)?> _openPostEditor({
    required String title,
    required String content,
  }) async {
    final titleController = TextEditingController(text: title);
    final contentController = TextEditingController(text: content);

    final result = await showAppBottomSheet<(String, String)>(
      context: context,
      builder: (context) {
        return Padding(
          padding: EdgeInsets.only(
            left: 20,
            right: 20,
            top: 20,
            bottom: MediaQuery.of(context).viewInsets.bottom + 20,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('编辑帖子', style: AppTextStyles.sectionTitle),
              const SizedBox(height: 16),
              AppTextField(controller: titleController, placeholder: '标题'),
              const SizedBox(height: 12),
              AppTextField(
                controller: contentController,
                maxLines: 6,
                placeholder: '内容',
              ),
              const SizedBox(height: 16),
              AppPrimaryButton(
                onPressed: () {
                  Navigator.of(context).pop((
                    titleController.text.trim(),
                    contentController.text.trim(),
                  ));
                },
                child: const Text('保存修改'),
              ),
            ],
          ),
        );
      },
    );

    titleController.dispose();
    contentController.dispose();
    return result;
  }

  @override
  Widget build(BuildContext context) {
    final user = ref.watch(authControllerProvider).asData?.value.user;
    final post = _post;
    final isOwner = post != null && user != null && post.userId == user.id;

    return AppPageScaffold(
      title: '帖子详情',
      trailing: isOwner
          ? CupertinoButton(
              padding: EdgeInsets.zero,
              minimumSize: Size.zero,
              onPressed: () {
                showCupertinoModalPopup<void>(
                  context: context,
                  builder: (_) => CupertinoActionSheet(
                    actions: [
                      CupertinoActionSheetAction(
                        onPressed: () {
                          Navigator.of(context).pop();
                          _editPost();
                        },
                        child: const Text('编辑帖子'),
                      ),
                      CupertinoActionSheetAction(
                        isDestructiveAction: true,
                        onPressed: () {
                          Navigator.of(context).pop();
                          _deletePost();
                        },
                        child: const Text('删除帖子'),
                      ),
                    ],
                    cancelButton: CupertinoActionSheetAction(
                      onPressed: () => Navigator.of(context).pop(),
                      child: const Text('取消'),
                    ),
                  ),
                );
              },
              child: const Icon(CupertinoIcons.ellipsis_circle),
            )
          : null,
      bottomBar: AppGlassCard(
        padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Expanded(
              child: AppTextField(
                controller: _commentController,
                placeholder: '写下你的评论...',
                maxLines: 4,
              ),
            ),
            const SizedBox(width: 12),
            AppPrimaryButton(
              onPressed: _isWorking ? null : _submitComment,
              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 16),
              child: const Text('发送'),
            ),
          ],
        ),
      ),
      child: _isLoading
          ? const Center(child: CupertinoActivityIndicator())
          : _errorMessage != null || post == null
          ? _PostDetailErrorView(
              message: _errorMessage ?? '详情加载失败',
              onRetry: _loadData,
            )
          : CustomScrollView(
              physics: const BouncingScrollPhysics(
                parent: AlwaysScrollableScrollPhysics(),
              ),
              slivers: [
                CupertinoSliverRefreshControl(onRefresh: _loadData),
                SliverToBoxAdapter(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _PostHero(
                        post: post,
                        onAuthorTap: post.userId == null
                            ? null
                            : () => context.push(
                                buildPublicProfileLocation(post.userId!),
                              ),
                      ),
                      const SizedBox(height: 16),
                      _ActionBar(
                        likes: post.likes ?? 0,
                        dislikes: post.dislikes ?? 0,
                        watch: post.watch ?? 0,
                        isWorking: _isWorking,
                        isLiked: _postLiked,
                        isDisliked: _postDisliked,
                        onLike: () => _runAction(() async {
                          final res = await ref
                              .read(postServiceProvider)
                              .likePost(post.id);
                          final data = res.data;
                          if (data != null) {
                            setState(() {
                              _postLiked = data.isLiked;
                              _postDisliked = false;
                              if (_post != null) {
                                _post = PostDto(
                                  id: _post!.id,
                                  title: _post!.title,
                                  content: _post!.content,
                                  images: _post!.images,
                                  createdAt: _post!.createdAt,
                                  updatedAt: _post!.updatedAt,
                                  userId: _post!.userId,
                                  author: _post!.author,
                                  likes: data.likeCount,
                                  dislikes: data.dislikeCount,
                                  watch: _post!.watch,
                                  isReview: _post!.isReview,
                                );
                              }
                            });
                          }
                        }, reloadAll: false),
                        onDislike: () => _runAction(() async {
                          final res = await ref
                              .read(postServiceProvider)
                              .dislikePost(post.id);
                          final data = res.data;
                          if (data != null) {
                            setState(() {
                              _postDisliked = data.isDisliked;
                              _postLiked = false;
                              if (_post != null) {
                                _post = PostDto(
                                  id: _post!.id,
                                  title: _post!.title,
                                  content: _post!.content,
                                  images: _post!.images,
                                  createdAt: _post!.createdAt,
                                  updatedAt: _post!.updatedAt,
                                  userId: _post!.userId,
                                  author: _post!.author,
                                  likes: data.likeCount,
                                  dislikes: data.dislikeCount,
                                  watch: _post!.watch,
                                  isReview: _post!.isReview,
                                );
                              }
                            });
                          }
                        }, reloadAll: false),
                      ),
                      const SizedBox(height: 24),
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              '评论 ${_comments.length}',
                              style: AppTextStyles.sectionTitle,
                            ),
                          ),
                          Container(
                            padding: const EdgeInsets.all(4),
                            decoration: BoxDecoration(
                              color: AppColors.secondary,
                              borderRadius: BorderRadius.circular(AppRadii.lg),
                            ),
                            child: CupertinoSlidingSegmentedControl<String>(
                              groupValue: _commentSort,
                              thumbColor: AppColors.card,
                              backgroundColor: AppColors.secondary,
                              children: const {
                                'recent': Padding(
                                  padding: EdgeInsets.symmetric(
                                    horizontal: 12,
                                    vertical: 8,
                                  ),
                                  child: Text('最新'),
                                ),
                                'hot': Padding(
                                  padding: EdgeInsets.symmetric(
                                    horizontal: 12,
                                    vertical: 8,
                                  ),
                                  child: Text('最热'),
                                ),
                              },
                              onValueChanged: (selection) {
                                if (selection == null) {
                                  return;
                                }
                                setState(() => _commentSort = selection);
                                _loadData();
                              },
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      if (_comments.isEmpty)
                        const AppEmptyState(
                          icon: CupertinoIcons.chat_bubble,
                          title: '还没有评论',
                          description: '来抢个沙发，成为第一个参与讨论的人。',
                        )
                      else
                        ..._comments.map(
                          (comment) => Padding(
                            padding: const EdgeInsets.only(bottom: 12),
                            child: _CommentThread(
                              comment: comment,
                              currentUserId: user?.id,
                              onAuthorTapForUser: (userId) => context.push(
                                buildPublicProfileLocation(userId),
                              ),
                              onReply: () => _replyComment(comment),
                              onEdit: user?.id == comment.userId
                                  ? () => _editComment(comment)
                                  : null,
                              onDelete: user?.id == comment.userId
                                  ? () => _deleteComment(comment)
                                  : null,
                              likedComments: _likedComments,
                              dislikedComments: _dislikedComments,
                              onLike: () => _runAction(() async {
                                final res = await _commentService.likeComment(
                                  comment.id,
                                );
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    if (data.isLiked) {
                                      _likedComments.add(comment.id);
                                      _dislikedComments.remove(comment.id);
                                    } else {
                                      _likedComments.remove(comment.id);
                                    }
                                  });
                                }
                              }),
                              onDislike: () => _runAction(() async {
                                final res = await _commentService
                                    .dislikeComment(comment.id);
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    if (data.isDisliked) {
                                      _dislikedComments.add(comment.id);
                                      _likedComments.remove(comment.id);
                                    } else {
                                      _dislikedComments.remove(comment.id);
                                    }
                                  });
                                }
                              }),
                              onReplyChild: _replyComment,
                              onEditChild: _editComment,
                              onDeleteChild: _deleteComment,
                              onLikeChild: (child) => _runAction(() async {
                                final res = await _commentService.likeComment(
                                  child.id,
                                );
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    if (data.isLiked) {
                                      _likedComments.add(child.id);
                                      _dislikedComments.remove(child.id);
                                    } else {
                                      _likedComments.remove(child.id);
                                    }
                                  });
                                }
                              }),
                              onDislikeChild: (child) => _runAction(() async {
                                final res = await _commentService
                                    .dislikeComment(child.id);
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    if (data.isDisliked) {
                                      _dislikedComments.add(child.id);
                                      _likedComments.remove(child.id);
                                    } else {
                                      _dislikedComments.remove(child.id);
                                    }
                                  });
                                }
                              }),
                            ),
                          ),
                        ),
                      const SizedBox(height: 132),
                    ],
                  ),
                ),
              ],
            ),
    );
  }
}

class _PostHero extends StatelessWidget {
  const _PostHero({required this.post, this.onAuthorTap});

  final PostDto post;
  final VoidCallback? onAuthorTap;

  @override
  Widget build(BuildContext context) {
    return AppSectionCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (post.author != null)
            UserCard(
              user: post.author!,
              showMeta: true,
              meta: _formatDate(post.createdAt, post.updatedAt),
              onTap: onAuthorTap,
            ),
          if (post.author != null) const SizedBox(height: 16),
          Text(
            post.title?.trim().isNotEmpty == true
                ? post.title!.trim()
                : '未命名帖子',
            style: AppTextStyles.title,
          ),
          const SizedBox(height: 12),
          Text(
            post.content?.trim().isNotEmpty == true
                ? post.content!.trim()
                : '这个帖子还没有填写描述。',
            style: AppTextStyles.body,
          ),
          if (post.images?.isNotEmpty == true) ...[
            const SizedBox(height: 16),
            SizedBox(
              height: 220,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: post.images!.length,
                separatorBuilder: (context, index) => const SizedBox(width: 12),
                itemBuilder: (context, index) {
                  return ClipRRect(
                    borderRadius: BorderRadius.circular(AppRadii.lg),
                    child: Image.network(
                      post.images![index],
                      width: 240,
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) => Container(
                        width: 240,
                        color: AppColors.secondary,
                        alignment: Alignment.center,
                        child: const Icon(CupertinoIcons.photo),
                      ),
                    ),
                  );
                },
              ),
            ),
          ],
        ],
      ),
    );
  }

  String _formatDate(DateTime? createdAt, DateTime? updatedAt) {
    final date = updatedAt ?? createdAt;
    if (date == null) {
      return '刚刚发布';
    }
    return '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')} ${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
  }
}

class _ActionBar extends StatelessWidget {
  const _ActionBar({
    required this.likes,
    required this.dislikes,
    required this.watch,
    required this.isWorking,
    required this.isLiked,
    required this.isDisliked,
    required this.onLike,
    required this.onDislike,
  });

  final int likes;
  final int dislikes;
  final int watch;
  final bool isWorking;
  final bool isLiked;
  final bool isDisliked;
  final VoidCallback onLike;
  final VoidCallback onDislike;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 10,
      runSpacing: 10,
      children: [
        CupertinoButton(
          padding: EdgeInsets.zero,
          minimumSize: Size.zero,
          onPressed: isWorking ? null : onLike,
          child: AppStatChip(
            icon: isLiked ? CupertinoIcons.heart_fill : CupertinoIcons.heart,
            label: '$likes 点赞',
            iconColor: const Color(0xFFFF5A5F),
          ),
        ),
        CupertinoButton(
          padding: EdgeInsets.zero,
          minimumSize: Size.zero,
          onPressed: isWorking ? null : onDislike,
          child: AppStatChip(
            icon: isDisliked
                ? CupertinoIcons.hand_thumbsdown_fill
                : CupertinoIcons.hand_thumbsdown,
            label: '$dislikes 点踩',
            iconColor: AppColors.mutedForeground,
          ),
        ),
        AppStatChip(
          icon: CupertinoIcons.eye,
          label: '$watch 浏览',
          iconColor: const Color(0xFF5AC8FA),
        ),
      ],
    );
  }
}

class _CommentThread extends StatelessWidget {
  const _CommentThread({
    required this.comment,
    required this.currentUserId,
    required this.onAuthorTapForUser,
    required this.onReply,
    required this.onEdit,
    required this.onDelete,
    required this.onLike,
    required this.onDislike,
    required this.onReplyChild,
    required this.onEditChild,
    required this.onDeleteChild,
    required this.onLikeChild,
    required this.onDislikeChild,
    required this.likedComments,
    required this.dislikedComments,
  });

  final CommentDto comment;
  final String? currentUserId;
  final ValueChanged<String> onAuthorTapForUser;
  final VoidCallback onReply;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;
  final VoidCallback onLike;
  final VoidCallback onDislike;
  final ValueChanged<CommentDto> onReplyChild;
  final ValueChanged<CommentDto>? onEditChild;
  final ValueChanged<CommentDto>? onDeleteChild;
  final ValueChanged<CommentDto> onLikeChild;
  final ValueChanged<CommentDto> onDislikeChild;
  final Set<String> likedComments;
  final Set<String> dislikedComments;

  @override
  Widget build(BuildContext context) {
    return AppSectionCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _CommentCard(
            comment: comment,
            onAuthorTap: comment.userId == null
                ? null
                : () => onAuthorTapForUser(comment.userId!),
            onReply: onReply,
            onEdit: onEdit,
            onDelete: onDelete,
            onLike: onLike,
            onDislike: onDislike,
            isLiked: likedComments.contains(comment.id),
            isDisliked: dislikedComments.contains(comment.id),
          ),
          if (comment.repliedComments?.isNotEmpty == true) ...[
            const SizedBox(height: 14),
            Container(
              margin: const EdgeInsets.only(left: 12),
              padding: const EdgeInsets.only(left: 14),
              decoration: BoxDecoration(
                border: Border(
                  left: BorderSide(
                    color: AppColors.border.withValues(alpha: 0.6),
                    width: 2,
                  ),
                ),
              ),
              child: Column(
                children: [
                  for (final reply in comment.repliedComments!) ...[
                    _CommentCard(
                      comment: reply,
                      onAuthorTap: reply.userId == null
                          ? null
                          : () => onAuthorTapForUser(reply.userId!),
                      onReply: () => onReplyChild(reply),
                      onEdit: currentUserId == reply.userId
                          ? () => onEditChild?.call(reply)
                          : null,
                      onDelete: currentUserId == reply.userId
                          ? () => onDeleteChild?.call(reply)
                          : null,
                      onLike: () => onLikeChild(reply),
                      onDislike: () => onDislikeChild(reply),
                      isLiked: likedComments.contains(reply.id),
                      isDisliked: dislikedComments.contains(reply.id),
                    ),
                    if (reply != comment.repliedComments!.last)
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        child: Container(
                          height: 1,
                          color: AppColors.border.withValues(alpha: 0.45),
                        ),
                      ),
                  ],
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _CommentCard extends StatelessWidget {
  const _CommentCard({
    required this.comment,
    this.onAuthorTap,
    required this.onReply,
    this.onEdit,
    this.onDelete,
    required this.onLike,
    required this.onDislike,
    this.isLiked = false,
    this.isDisliked = false,
  });

  final CommentDto comment;
  final VoidCallback? onAuthorTap;
  final VoidCallback onReply;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;
  final VoidCallback onLike;
  final VoidCallback onDislike;
  final bool isLiked;
  final bool isDisliked;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (comment.author != null)
          UserCard(
            user: comment.author!,
            onTap: onAuthorTap,
            showMeta: true,
            meta: _formatDate(comment.createdAt),
          ),
        const SizedBox(height: 10),
        Text(
          comment.content?.trim().isNotEmpty == true
              ? comment.content!.trim()
              : '评论内容为空',
          style: AppTextStyles.body,
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: [
            _TextAction(label: '回复', onTap: onReply),
            _TextAction(
              label: '点赞 ${comment.likes ?? 0}',
              onTap: onLike,
              active: isLiked,
            ),
            _TextAction(
              label: '点踩 ${comment.dislikes ?? 0}',
              onTap: onDislike,
              active: isDisliked,
            ),
            if (onEdit != null) _TextAction(label: '编辑', onTap: onEdit!),
            if (onDelete != null)
              _TextAction(
                label: '删除',
                onTap: onDelete!,
                activeColor: AppColors.destructive,
              ),
          ],
        ),
      ],
    );
  }

  String _formatDate(DateTime? date) {
    if (date == null) {
      return '刚刚';
    }
    final diff = DateTime.now().difference(date);
    if (diff.inMinutes < 1) {
      return '刚刚';
    }
    if (diff.inHours < 1) {
      return '${diff.inMinutes} 分钟前';
    }
    if (diff.inDays < 1) {
      return '${diff.inHours} 小时前';
    }
    return '${diff.inDays} 天前';
  }
}

class _TextAction extends StatelessWidget {
  const _TextAction({
    required this.label,
    required this.onTap,
    this.active = false,
    this.activeColor = AppColors.primary,
  });

  final String label;
  final VoidCallback onTap;
  final bool active;
  final Color activeColor;

  @override
  Widget build(BuildContext context) {
    return CupertinoButton(
      padding: EdgeInsets.zero,
      minimumSize: Size.zero,
      onPressed: onTap,
      child: Text(
        label,
        style: TextStyle(
          color: active ? activeColor : AppColors.mutedForeground,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _PostDetailErrorView extends StatelessWidget {
  const _PostDetailErrorView({required this.message, required this.onRetry});

  final String message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: AppEmptyState(
        icon: CupertinoIcons.doc_text,
        title: '详情加载失败',
        description: message,
        action: AppPrimaryButton(onPressed: onRetry, child: const Text('重新加载')),
      ),
    );
  }
}
