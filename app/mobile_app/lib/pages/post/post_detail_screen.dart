import 'dart:ui';

import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

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

      if (!mounted) return;

      setState(() {
        _post = post;
        _comments = comments ?? const [];
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _errorMessage = error.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) setState(() => _isLoading = false);
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
      if (reloadAll) await _loadData();
      if (successMessage != null && mounted) {
        await AppFeedback.showMessage(context, message: successMessage);
      }
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showMessage(
        context,
        message: error.toString().replaceFirst('Exception: ', ''),
      );
    } finally {
      if (mounted) setState(() => _isWorking = false);
    }
  }

  Future<void> _submitComment() async {
    final user = ref.read(authControllerProvider).asData?.value.user;
    if (user == null) {
      context.go(AppRoutePaths.login);
      return;
    }

    final content = _commentController.text.trim();
    if (content.isEmpty) return;

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
    if (content == null || content.trim().isEmpty) return;

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
    if (content == null || content.trim().isEmpty) return;

    await _runAction(() async {
      await _commentService.updateComment(
        comment.id,
        CommentUpdateDto(content: content.trim()),
      );
    }, successMessage: '评论已更新');
  }

  Future<void> _deleteComment(CommentDto comment) async {
    final confirmed = await _confirm('确定删除这条评论吗？');
    if (confirmed != true) return;

    await _runAction(() async {
      await _commentService.deleteComment(comment.id);
    }, successMessage: '评论已删除');
  }

  Future<void> _editPost() async {
    final post = _post;
    if (post == null) return;

    final values = await _openPostEditor(
      title: post.title ?? '',
      content: post.content ?? '',
    );
    if (values == null) return;

    await _runAction(() async {
      await ref.read(postServiceProvider).updatePost(
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
    if (post == null) return;

    final confirmed = await _confirm('确定删除这篇帖子吗？');
    if (confirmed != true) return;

    await _runAction(
      () async {
        await ref.read(postServiceProvider).deletePost(post.id);
        if (mounted) context.go(AppRoutePaths.home);
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
              Text(title, style: AppTextStyles.sectionTitle(context)),
              const SizedBox(height: 16),
              AppTextField(
                controller: controller,
                maxLines: 5,
                placeholder: hintText,
              ),
              const SizedBox(height: 20),
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
              Text('编辑帖子', style: AppTextStyles.sectionTitle(context)),
              const SizedBox(height: 16),
              AppTextField(controller: titleController, placeholder: '标题'),
              const SizedBox(height: 12),
              AppTextField(
                controller: contentController,
                maxLines: 6,
                placeholder: '内容',
              ),
              const SizedBox(height: 20),
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

    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      child: Stack(
        children: [
          _isLoading
              ? const Center(child: CupertinoActivityIndicator(radius: 14))
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
                        CupertinoSliverNavigationBar(
                          backgroundColor: CupertinoDynamicColor.resolve(
                            AppColors.background,
                            context,
                          ).withValues(alpha: 0.8),
                          border: Border(
                            bottom: BorderSide(
                              color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3),
                              width: 0.5,
                            ),
                          ),
                          largeTitle: const Text('详情'),
                          middle: const Text('详情', style: TextStyle(fontWeight: FontWeight.w700)),
                          trailing: isOwner
                              ? CupertinoButton(
                                  padding: EdgeInsets.zero,
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
                                  child: const Icon(CupertinoIcons.ellipsis, size: 22),
                                )
                              : null,
                        ),
                        CupertinoSliverRefreshControl(onRefresh: _loadData),
                        SliverPadding(
                          padding: const EdgeInsets.fromLTRB(16, 12, 16, 160),
                          sliver: SliverToBoxAdapter(
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
                                const SizedBox(height: 24),
                                _ActionBar(
                                  likes: post.likes ?? 0,
                                  dislikes: post.dislikes ?? 0,
                                  watch: post.watch ?? 0,
                                  isWorking: _isWorking,
                                  isLiked: _postLiked,
                                  isDisliked: _postDisliked,
                                  onLike: () => _runAction(() async {
                                    final res = await ref.read(postServiceProvider).likePost(post.id);
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
                                    final res = await ref.read(postServiceProvider).dislikePost(post.id);
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
                                const SizedBox(height: 32),
                                Row(
                                  children: [
                                    Expanded(
                                      child: Text(
                                        '评论 ${_comments.length}',
                                        style: AppTextStyles.sectionTitle(context),
                                      ),
                                    ),
                                    CupertinoSlidingSegmentedControl<String>(
                                      groupValue: _commentSort,
                                      backgroundColor: CupertinoDynamicColor.resolve(
                                        AppColors.secondary,
                                        context,
                                      ),
                                      children: const {
                                        'recent': Padding(
                                          padding: EdgeInsets.symmetric(horizontal: 10),
                                          child: Text('最新', style: TextStyle(fontSize: 13)),
                                        ),
                                        'hot': Padding(
                                          padding: EdgeInsets.symmetric(horizontal: 10),
                                          child: Text('最热', style: TextStyle(fontSize: 13)),
                                        ),
                                      },
                                      onValueChanged: (v) {
                                        if (v != null) {
                                          setState(() => _commentSort = v);
                                          _loadData();
                                        }
                                      },
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 16),
                                if (_comments.isEmpty)
                                  const AppEmptyState(
                                    icon: CupertinoIcons.chat_bubble,
                                    title: '暂无评论',
                                    description: '分享你的见解，成为第一个评论的人。',
                                  )
                                else
                                  ..._comments.map((c) => Padding(
                                        padding: const EdgeInsets.only(bottom: 12),
                                        child: _CommentThread(
                                          comment: c,
                                          currentUserId: user?.id,
                                          onAuthorTapForUser: (id) => context.push(buildPublicProfileLocation(id)),
                                          onReply: () => _replyComment(c),
                                          onEdit: user?.id == c.userId ? () => _editComment(c) : null,
                                          onDelete: user?.id == c.userId ? () => _deleteComment(c) : null,
                                          likedComments: _likedComments,
                                          dislikedComments: _dislikedComments,
                                          onLike: () => _runAction(() async {
                                            final res = await _commentService.likeComment(c.id);
                                            final data = res.data;
                                            if (data != null) {
                                              setState(() {
                                                if (data.isLiked) {
                                                  _likedComments.add(c.id);
                                                  _dislikedComments.remove(c.id);
                                                } else {
                                                  _likedComments.remove(c.id);
                                                }
                                              });
                                            }
                                          }),
                                          onDislike: () => _runAction(() async {
                                            final res = await _commentService.dislikeComment(c.id);
                                            final data = res.data;
                                            if (data != null) {
                                              setState(() {
                                                if (data.isDisliked) {
                                                  _dislikedComments.add(c.id);
                                                  _likedComments.remove(c.id);
                                                } else {
                                                  _dislikedComments.remove(c.id);
                                                }
                                              });
                                            }
                                          }),
                                          onReplyChild: _replyComment,
                                          onEditChild: _editComment,
                                          onDeleteChild: _deleteComment,
                                          onLikeChild: (child) => _runAction(() async {
                                            final res = await _commentService.likeComment(child.id);
                                            if (res.data?.isLiked == true) {
                                              setState(() {
                                                _likedComments.add(child.id);
                                                _dislikedComments.remove(child.id);
                                              });
                                            }
                                          }),
                                          onDislikeChild: (child) => _runAction(() async {
                                            final res = await _commentService.dislikeComment(child.id);
                                            if (res.data?.isDisliked == true) {
                                              setState(() {
                                                _dislikedComments.add(child.id);
                                                _likedComments.remove(child.id);
                                              });
                                            }
                                          }),
                                        ),
                                      )),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
          if (!_isLoading && post != null)
            Positioned(
              left: 0,
              right: 0,
              bottom: 0,
              child: ClipRect(
                child: BackdropFilter(
                  filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
                  child: Container(
                    padding: EdgeInsets.fromLTRB(16, 12, 16, MediaQuery.of(context).padding.bottom + 12),
                    decoration: BoxDecoration(
                      color: CupertinoDynamicColor.resolve(AppColors.background, context).withValues(alpha: 0.8),
                      border: Border(
                        top: BorderSide(
                          color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3),
                        ),
                      ),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        Expanded(
                          child: Container(
                            height: 44,
                            decoration: BoxDecoration(
                              color: CupertinoDynamicColor.resolve(AppColors.secondary, context),
                              borderRadius: BorderRadius.circular(AppRadii.pill),
                            ),
                            padding: const EdgeInsets.symmetric(horizontal: 16),
                            alignment: Alignment.centerLeft,
                            child: CupertinoTextField(
                              controller: _commentController,
                              placeholder: '写下你的评论...',
                              placeholderStyle: AppTextStyles.muted(context),
                              decoration: null,
                              style: AppTextStyles.body(context).copyWith(fontSize: 15),
                              cursorColor: AppColors.primary,
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        CupertinoButton(
                          padding: EdgeInsets.zero,
                          onPressed: _isWorking ? null : _submitComment,
                          child: Text(
                            '发布',
                            style: TextStyle(
                              color: AppColors.primary.withValues(alpha: _isWorking ? 0.5 : 1.0),
                              fontWeight: FontWeight.w700,
                              fontSize: 16,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
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
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (post.author != null)
          Padding(
            padding: const EdgeInsets.only(bottom: 16),
            child: Row(
              children: [
                AppAvatar(
                  url: post.author?.avatar,
                  name: post.author?.name,
                  size: 40,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        post.author?.name ?? '匿名用户',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      Text(
                        _formatDate(post.createdAt, post.updatedAt),
                        style: AppTextStyles.label(context),
                      ),
                    ],
                  ),
                ),
                AppSecondaryButton(
                  onPressed: onAuthorTap,
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
                  child: const Text('关注', style: TextStyle(fontSize: 13)),
                ),
              ],
            ),
          ),
        Text(
          post.title?.trim().isNotEmpty == true ? post.title!.trim() : '未命名帖子',
          style: AppTextStyles.title(context).copyWith(
            fontSize: 22,
            height: 1.3,
          ),
        ),
        const SizedBox(height: 16),
        Text(
          post.content?.trim().isNotEmpty == true ? post.content!.trim() : '这个帖子还没有填写描述。',
          style: AppTextStyles.body(context).copyWith(
            fontSize: 17,
            height: 1.6,
            color: CupertinoDynamicColor.resolve(AppColors.foreground, context).withValues(alpha: 0.9),
          ),
        ),
        if (post.images?.isNotEmpty == true) ...[
          const SizedBox(height: 20),
          ...post.images!.map((imageUrl) => Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(AppRadii.lg),
              child: Image.network(
                imageUrl,
                width: double.infinity,
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => Container(
                  height: 200,
                  width: double.infinity,
                  color: AppColors.muted,
                  child: const Icon(CupertinoIcons.photo, color: AppColors.mutedForeground),
                ),
              ),
            ),
          )),
        ],
      ],
    );
  }

  String _formatDate(DateTime? createdAt, DateTime? updatedAt) {
    final date = updatedAt ?? createdAt;
    if (date == null) return '刚刚';
    return '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
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
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16),
      decoration: BoxDecoration(
        border: Border(
          top: BorderSide(color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3)),
          bottom: BorderSide(color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3)),
        ),
      ),
      child: Row(
        children: [
          _ActionChip(
            icon: isLiked ? CupertinoIcons.heart_fill : CupertinoIcons.heart,
            label: '$likes',
            onTap: isWorking ? null : onLike,
            color: const Color(0xFFFF5A5F),
            active: isLiked,
          ),
          const SizedBox(width: 12),
          _ActionChip(
            icon: isDisliked ? CupertinoIcons.hand_thumbsdown_fill : CupertinoIcons.hand_thumbsdown,
            label: '$dislikes',
            onTap: isWorking ? null : onDislike,
            color: CupertinoDynamicColor.resolve(AppColors.mutedForeground, context),
            active: isDisliked,
          ),
          const Spacer(),
          Text(
            '$watch 次浏览',
            style: AppTextStyles.label(context),
          ),
        ],
      ),
    );
  }
}

class _ActionChip extends StatelessWidget {
  const _ActionChip({required this.icon, required this.label, this.onTap, required this.color, this.active = false});
  final IconData icon;
  final String label;
  final VoidCallback? onTap;
  final Color color;
  final bool active;

  @override
  Widget build(BuildContext context) {
    return CupertinoButton(
      padding: EdgeInsets.zero,
      onPressed: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: active ? color.withValues(alpha: 0.1) : AppColors.secondary,
          borderRadius: BorderRadius.circular(AppRadii.pill),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 18,
              color: active ? color : CupertinoDynamicColor.resolve(AppColors.mutedForeground, context),
            ),
            const SizedBox(width: 8),
            Text(
              label,
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w700,
                color: active ? color : CupertinoDynamicColor.resolve(AppColors.foreground, context),
              ),
            ),
          ],
        ),
      ),
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
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _CommentCard(
          comment: comment,
          onAuthorTap: comment.userId == null ? null : () => onAuthorTapForUser(comment.userId!),
          onReply: onReply,
          onEdit: onEdit,
          onDelete: onDelete,
          onLike: onLike,
          onDislike: onDislike,
          isLiked: likedComments.contains(comment.id),
          isDisliked: dislikedComments.contains(comment.id),
        ),
        if (comment.repliedComments?.isNotEmpty == true) ...[
          Container(
            margin: const EdgeInsets.only(left: 44, top: 12),
            padding: const EdgeInsets.only(left: 12),
            decoration: BoxDecoration(
              border: Border(
                left: BorderSide(
                  color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3),
                  width: 2,
                ),
              ),
            ),
            child: Column(
              children: [
                for (final reply in comment.repliedComments!) ...[
                  _CommentCard(
                    comment: reply,
                    isReply: true,
                    onAuthorTap: reply.userId == null ? null : () => onAuthorTapForUser(reply.userId!),
                    onReply: () => onReplyChild(reply),
                    onEdit: currentUserId == reply.userId ? () => onEditChild?.call(reply) : null,
                    onDelete: currentUserId == reply.userId ? () => onDeleteChild?.call(reply) : null,
                    onLike: () => onLikeChild(reply),
                    onDislike: () => onDislikeChild(reply),
                    isLiked: likedComments.contains(reply.id),
                    isDisliked: dislikedComments.contains(reply.id),
                  ),
                  if (reply != comment.repliedComments!.last) const SizedBox(height: 16),
                ],
              ],
            ),
          ),
        ],
        const SizedBox(height: 16),
        Container(
          height: 1,
          color: CupertinoDynamicColor.resolve(AppColors.border, context).withValues(alpha: 0.3),
        ),
        const SizedBox(height: 16),
      ],
    );
  }
}

class _CommentCard extends StatelessWidget {
  const _CommentCard({required this.comment, this.onAuthorTap, required this.onReply, this.onEdit, this.onDelete, required this.onLike, required this.onDislike, this.isLiked = false, this.isDisliked = false, this.isReply = false});
  final CommentDto comment;
  final VoidCallback? onAuthorTap;
  final VoidCallback onReply;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;
  final VoidCallback onLike;
  final VoidCallback onDislike;
  final bool isLiked;
  final bool isDisliked;
  final bool isReply;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            AppAvatar(
              url: comment.author?.avatar,
              name: comment.author?.name,
              size: isReply ? 28 : 32,
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    comment.author?.name ?? '匿名用户',
                    style: TextStyle(
                      fontSize: isReply ? 13 : 14,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  Text(
                    _formatDate(comment.createdAt),
                    style: AppTextStyles.label(context).copyWith(fontSize: 11),
                  ),
                ],
              ),
            ),
            _CommentActionIcon(
              icon: isLiked ? CupertinoIcons.heart_fill : CupertinoIcons.heart,
              label: '${comment.likes ?? 0}',
              onTap: onLike,
              active: isLiked,
              activeColor: const Color(0xFFFF5A5F),
            ),
          ],
        ),
        Padding(
          padding: EdgeInsets.only(left: isReply ? 38 : 42, top: 4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                comment.content ?? '',
                style: AppTextStyles.body(context).copyWith(fontSize: 15, height: 1.5),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  _TextAction(label: '回复', onTap: onReply),
                  if (onEdit != null) ...[
                    const SizedBox(width: 16),
                    _TextAction(label: '编辑', onTap: onEdit!),
                  ],
                  if (onDelete != null) ...[
                    const SizedBox(width: 16),
                    _TextAction(label: '删除', onTap: onDelete!, activeColor: AppColors.destructive, active: true),
                  ],
                  const Spacer(),
                  _CommentActionIcon(
                    icon: isDisliked ? CupertinoIcons.hand_thumbsdown_fill : CupertinoIcons.hand_thumbsdown,
                    onTap: onDislike,
                    active: isDisliked,
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }

  String _formatDate(DateTime? date) {
    if (date == null) return '刚刚';
    final diff = DateTime.now().difference(date);
    if (diff.inMinutes < 1) return '刚刚';
    if (diff.inHours < 1) return '${diff.inMinutes}分钟前';
    if (diff.inDays < 1) return '${diff.inHours}小时前';
    return '${date.year}-${date.month}-${date.day}';
  }
}

class _CommentActionIcon extends StatelessWidget {
  const _CommentActionIcon({required this.icon, this.label, required this.onTap, this.active = false, this.activeColor});
  final IconData icon;
  final String? label;
  final VoidCallback onTap;
  final bool active;
  final Color? activeColor;

  @override
  Widget build(BuildContext context) {
    final color = active 
        ? (activeColor ?? CupertinoDynamicColor.resolve(AppColors.primary, context))
        : CupertinoDynamicColor.resolve(AppColors.mutedForeground, context);

    return CupertinoButton(
      padding: EdgeInsets.zero,
      minimumSize: Size.zero,
      onPressed: onTap,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: color),
          if (label != null) ...[
            const SizedBox(width: 4),
            Text(label!, style: TextStyle(fontSize: 12, color: color, fontWeight: FontWeight.w600)),
          ],
        ],
      ),
    );
  }
}

class _TextAction extends StatelessWidget {
  const _TextAction({required this.label, required this.onTap, this.active = false, this.activeColor = AppColors.primary});
  final String label;
  final VoidCallback onTap;
  final bool active;
  final Color activeColor;

  @override
  Widget build(BuildContext context) {
    final resolvedMuted = CupertinoDynamicColor.resolve(AppColors.mutedForeground, context);
    final resolvedActive = CupertinoDynamicColor.resolve(activeColor, context);

    return CupertinoButton(
      padding: EdgeInsets.zero,
      minimumSize: Size.zero,
      onPressed: onTap,
      child: Text(
        label,
        style: TextStyle(
          color: active ? resolvedActive : resolvedMuted,
          fontSize: 13,
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
