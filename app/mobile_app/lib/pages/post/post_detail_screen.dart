import 'dart:io';
import 'dart:ui';

import 'package:carousel_slider/carousel_slider.dart';
import 'package:flutter/cupertino.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import 'image_viewer_screen.dart';

import '../../models/comment.dart';
import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/comment_service.dart';
import '../../services/file_service.dart';
import '../../services/follow_service.dart';
import '../../services/image_compression_service.dart';
import '../../services/share_service.dart';
import '../../services/user_service.dart';
import '../../services/error_messages.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';
import '../../utils/dto_validation.dart';

const int _maxCommentImages = 3;

class _CommentDraft {
  const _CommentDraft({
    required this.content,
    this.existingImages = const [],
    this.newImages = const [],
  });

  final String content;
  final List<String> existingImages;
  final List<XFile> newImages;
}

class PostDetailScreen extends ConsumerStatefulWidget {
  const PostDetailScreen({super.key, required this.postId});

  final String postId;

  @override
  ConsumerState<PostDetailScreen> createState() => _PostDetailScreenState();
}

class _PostDetailScreenState extends ConsumerState<PostDetailScreen> {
  final _commentService = CommentService();
  final _commentController = TextEditingController();
  final _imagePicker = ImagePicker();
  final _fileService = FileService();
  final _shareService = const ShareService();
  final _followService = FollowService();
  final _userService = UserService();
  PostDto? _post;
  List<CommentDto> _comments = const [];
  bool _isLoading = true;
  bool _isCommentsLoading = false;
  bool _isWorking = false;
  final List<XFile> _commentImages = [];
  double? _commentUploadProgress;
  String? _errorMessage;
  String _commentSort = 'recent';
  bool _postLiked = false;
  bool _postDisliked = false;
  int _commentsRequestId = 0;
  final Set<String> _likedComments = {};
  final Set<String> _dislikedComments = {};

  bool _isFollowingAuthor = false;
  bool _followLoading = false;

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
      _isCommentsLoading = true;
      _errorMessage = null;
    });

    final commentsRequestId = ++_commentsRequestId;
    final postService = ref.read(postServiceProvider);
    final postFuture = postService.getPost(widget.postId);
    final commentsFuture = postService.getPostComments(
      widget.postId,
      _commentSort,
    );

    try {
      final response = await postFuture;
      final post = response.data;
      if (post == null) {
        throw Exception('帖子不存在');
      }

      if (!mounted) return;

      setState(() {
        _post = post;
        _postLiked = post.isLiked ?? false;
        _postDisliked = post.isDisliked ?? false;
        _isLoading = false;
      });

      _loadAuthorFollowState(post.userId);
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _errorMessage = extractErrorFromException(error);
        _isLoading = false;
      });
    }

    await _consumeCommentsFuture(commentsFuture, commentsRequestId);
  }

  Future<void> _loadCommentsInternal() async {
    final requestId = ++_commentsRequestId;
    final commentsFuture = ref
        .read(postServiceProvider)
        .getPostComments(widget.postId, _commentSort);
    await _consumeCommentsFuture(commentsFuture, requestId);
  }

  Future<void> _consumeCommentsFuture(
    Future<dynamic> commentsFuture,
    int requestId,
  ) async {
    try {
      final res = await commentsFuture;
      final comments = res.data;

      if (!mounted || requestId != _commentsRequestId) return;
      setState(() {
        _comments = comments ?? const [];
        _syncCommentReactionState(_comments);
        _isCommentsLoading = false;
      });
    } catch (error) {
      if (!mounted || requestId != _commentsRequestId) return;
      setState(() => _isCommentsLoading = false);
    }
  }

  Future<void> _loadAuthorFollowState(String? authorId) async {
    if (authorId == null || authorId.isEmpty) return;
    final user = ref.read(authControllerProvider).asData?.value.user;
    if (user == null || user.id == authorId) return;

    try {
      final response = await _userService.getPublicProfile(authorId);
      final status = response.data?.relationshipStatus;
      if (mounted && status != null) {
        setState(() => _isFollowingAuthor = status.isFollowing);
      }
    } catch (_) {}
  }

  Future<void> _toggleFollowAuthor() async {
    final authorId = _post?.userId;
    if (authorId == null || authorId.isEmpty || _followLoading) return;

    final user = ref.read(authControllerProvider).asData?.value.user;
    if (user == null) {
      context.go(AppRoutePaths.login);
      return;
    }

    setState(() => _followLoading = true);
    try {
      final result = await _followService.toggleFollow(authorId);
      if (mounted && result.data != null) {
        setState(() => _isFollowingAuthor = result.data!.isFollowing);
      }
    } catch (_) {}
    if (mounted) setState(() => _followLoading = false);
  }

  Future<void> _loadComments() async {
    setState(() => _isCommentsLoading = true);
    await _loadCommentsInternal();
  }

  Future<void> _runAction(
    Future<void> Function() action, {
    String? successMessage,
    bool reloadAll = true,
    bool reloadComments = false,
  }) async {
    setState(() => _isWorking = true);
    try {
      await action();
      if (reloadAll) await _loadData();
      if (!reloadAll && reloadComments) await _loadComments();
      if (successMessage != null && mounted) {
        await AppFeedback.showSuccess(context, message: successMessage);
      }
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: extractErrorFromException(error),
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
    if (content.isEmpty && _commentImages.isEmpty) return;
    if (content.length > DtoLimits.commentContentMax) {
      await AppFeedback.showError(
        context,
        message: '评论内容长度不能超过 ${DtoLimits.commentContentMax} 位',
      );
      return;
    }

    setState(() => _isWorking = true);
    try {
      final uploadedImages = await _uploadCommentImages(
        _commentImages,
        onProgress: (fraction) {
          if (mounted) setState(() => _commentUploadProgress = fraction);
        },
      );
      final response = await _commentService.createComment(
        CommentCreateDto(
          content: content,
          images: uploadedImages,
          userId: user.id,
          postId: widget.postId,
        ),
      );
      final newComment = response.data;
      if (newComment != null) {
        _insertCommentLocally(newComment);
        _commentController.clear();
        setState(() {
          _commentImages.clear();
          _commentUploadProgress = null;
        });
        if (mounted) await AppFeedback.showSuccess(context, message: '评论已发布');
      }
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: extractErrorFromException(error),
      );
    } finally {
      if (mounted) {
        setState(() {
          _isWorking = false;
          _commentUploadProgress = null;
        });
      }
    }
  }

  Future<void> _shareCurrentPost() async {
    final post = _post;
    if (post == null) return;

    try {
      await _shareService.sharePost(post);
      if (!mounted) return;
      await AppFeedback.showSuccess(context, message: '帖子分享面板已打开');
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(context, message: '分享失败，请稍后重试');
    }
  }

  Future<void> _replyComment(CommentDto comment) async {
    final user = ref.read(authControllerProvider).asData?.value.user;
    if (user == null) {
      context.go(AppRoutePaths.login);
      return;
    }

    final draft = await _openCommentComposer(
      title: '回复评论',
      initialValue: '',
      hintText: '输入回复内容',
    );
    if (draft == null ||
        (draft.content.trim().isEmpty && draft.newImages.isEmpty)) {
      return;
    }
    if (draft.content.trim().length > DtoLimits.commentContentMax) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: '评论内容长度不能超过 ${DtoLimits.commentContentMax} 位',
      );
      return;
    }

    setState(() => _isWorking = true);
    try {
      final uploadedImages = await _uploadCommentImages(draft.newImages);
      final response = await _commentService.replyToComment(
        comment.id,
        CommentCreateDto(
          content: draft.content.trim(),
          images: uploadedImages,
          userId: user.id,
          postId: widget.postId,
          parentCommentId: comment.id,
        ),
      );
      final newReply = response.data;
      if (newReply != null) {
        _insertReplyLocally(comment.id, newReply);
        if (mounted) await AppFeedback.showSuccess(context, message: '回复已发送');
      }
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: extractErrorFromException(error),
      );
    } finally {
      if (mounted) setState(() => _isWorking = false);
    }
  }

  Future<void> _editComment(CommentDto comment) async {
    final draft = await _openCommentComposer(
      title: '编辑评论',
      initialValue: comment.content ?? '',
      hintText: '更新评论内容',
      initialImages: comment.images ?? const [],
    );
    if (draft == null ||
        (draft.content.trim().isEmpty &&
            draft.existingImages.isEmpty &&
            draft.newImages.isEmpty)) {
      return;
    }
    if (draft.content.trim().length > DtoLimits.commentContentMax) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: '评论内容长度不能超过 ${DtoLimits.commentContentMax} 位',
      );
      return;
    }

    setState(() => _isWorking = true);
    try {
      final uploadedImages = await _uploadCommentImages(draft.newImages);
      final nextImages = [...draft.existingImages, ...uploadedImages];
      await _commentService.updateComment(
        comment.id,
        CommentUpdateDto(content: draft.content.trim(), images: nextImages),
      );
      _updateCommentLocally(comment.id, draft.content.trim(), nextImages);
      if (mounted) await AppFeedback.showSuccess(context, message: '评论已更新');
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: extractErrorFromException(error),
      );
    } finally {
      if (mounted) setState(() => _isWorking = false);
    }
  }

  Future<void> _pickCommentImages() async {
    final remaining = _maxCommentImages - _commentImages.length;
    if (remaining <= 0) return;

    final picked = await _imagePicker.pickMultiImage();
    if (picked.isEmpty) return;

    setState(() {
      _commentImages.addAll(picked.take(remaining));
    });
  }

  void _removeCommentImage(int index) {
    setState(() => _commentImages.removeAt(index));
  }

  Future<List<String>> _uploadCommentImages(
    List<XFile> images, {
    void Function(double fraction)? onProgress,
  }) async {
    if (images.isEmpty) return <String>[];

    final compressed = <CompressedImage>[];
    try {
      final results = await Future.wait([
        _fileService.getUploadKey().then(
          (r) => (r.data?['key'] as String?) ?? '',
        ),
        ImageCompressionService.compressAll(
          images,
          quality: ImageCompressionService.postImageQuality,
          maxWidth: ImageCompressionService.postMaxWidth,
          maxHeight: ImageCompressionService.postMaxHeight,
        ),
      ]);

      var uploadKey = results[0] as String?;
      if (uploadKey?.isEmpty == true) uploadKey = null;
      compressed.addAll(results[1] as List<CompressedImage>);

      return (await _fileService.uploadStream(
            compressed.map(ImageCompressionService.toXFile).toList(),
            key: uploadKey,
            onProgress: onProgress,
          )).data ??
          <String>[];
    } finally {
      ImageCompressionService.cleanup(compressed);
    }
  }

  Future<void> _deleteComment(CommentDto comment) async {
    final confirmed = await _confirm('确定删除这条评论吗？');
    if (confirmed != true) return;

    setState(() => _isWorking = true);
    try {
      await _commentService.deleteComment(comment.id);
      _removeCommentLocally(comment.id);
      if (mounted) await AppFeedback.showSuccess(context, message: '评论已删除');
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: extractErrorFromException(error),
      );
    } finally {
      if (mounted) setState(() => _isWorking = false);
    }
  }

  void _insertCommentLocally(CommentDto comment) {
    setState(() {
      _comments = [comment, ..._comments];
      _syncCommentReactionState(_comments);
    });
  }

  void _insertReplyLocally(String parentCommentId, CommentDto reply) {
    setState(() {
      _comments = _comments
          .map((c) => _insertReplyInTree(c, parentCommentId, reply))
          .toList();
    });
  }

  CommentDto _insertReplyInTree(
    CommentDto comment,
    String parentId,
    CommentDto reply,
  ) {
    if (comment.id == parentId) {
      return _copyComment(
        comment,
        repliedComments: [...(comment.repliedComments ?? const []), reply],
      );
    }
    if (comment.repliedComments == null || comment.repliedComments!.isEmpty) {
      return comment;
    }
    return _copyComment(
      comment,
      repliedComments: comment.repliedComments!
          .map((r) => _insertReplyInTree(r, parentId, reply))
          .toList(),
    );
  }

  void _updateCommentLocally(
    String commentId,
    String newContent,
    List<String> images,
  ) {
    setState(() {
      _comments = _comments
          .map((c) => _updateCommentInTree(c, commentId, newContent, images))
          .toList();
    });
  }

  CommentDto _updateCommentInTree(
    CommentDto comment,
    String commentId,
    String newContent,
    List<String> images,
  ) {
    if (comment.id == commentId) {
      return _copyComment(comment, content: newContent, images: images);
    }
    if (comment.repliedComments == null || comment.repliedComments!.isEmpty) {
      return comment;
    }
    return _copyComment(
      comment,
      repliedComments: comment.repliedComments!
          .map((r) => _updateCommentInTree(r, commentId, newContent, images))
          .toList(),
    );
  }

  void _removeCommentLocally(String commentId) {
    setState(() {
      _comments = _comments
          .where((c) => c.id != commentId)
          .map((c) => _removeCommentInTree(c, commentId))
          .toList();
      _likedComments.remove(commentId);
      _dislikedComments.remove(commentId);
    });
  }

  CommentDto _removeCommentInTree(CommentDto comment, String commentId) {
    if (comment.repliedComments == null || comment.repliedComments!.isEmpty) {
      return comment;
    }
    return _copyComment(
      comment,
      repliedComments: comment.repliedComments!
          .where((r) => r.id != commentId)
          .map((r) => _removeCommentInTree(r, commentId))
          .toList(),
    );
  }

  CommentDto _copyComment(
    CommentDto c, {
    String? content,
    List<String>? images,
    List<CommentDto>? repliedComments,
  }) {
    return CommentDto(
      id: c.id,
      content: content ?? c.content,
      images: images ?? c.images,
      likes: c.likes,
      dislikes: c.dislikes,
      isLiked: c.isLiked,
      isDisliked: c.isDisliked,
      createdAt: c.createdAt,
      updatedAt: c.updatedAt,
      userId: c.userId,
      author: c.author,
      postId: c.postId,
      parentCommentId: c.parentCommentId,
      repliedComments: repliedComments ?? c.repliedComments,
      isReview: c.isReview,
    );
  }

  Future<void> _editPost() async {
    final post = _post;
    if (post == null) return;

    final values = await _openPostEditor(
      title: post.title ?? '',
      content: post.content ?? '',
    );
    if (values == null) return;
    final validationError = _validatePostDraft(values.$1, values.$2);
    if (validationError != null) {
      if (!mounted) return;
      await AppFeedback.showError(context, message: validationError);
      return;
    }

    setState(() => _isWorking = true);
    try {
      final result = await ref
          .read(postServiceProvider)
          .updatePost(
            post.id,
            PostUpdateDto(
              title: values.$1,
              content: values.$2,
              images: post.images,
            ),
          );
      if (mounted && result.data != null) {
        setState(() => _post = result.data);
        await AppFeedback.showSuccess(context, message: '帖子已更新');
      }
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: extractErrorFromException(error),
      );
    } finally {
      if (mounted) setState(() => _isWorking = false);
    }
  }

  Future<void> _deletePost() async {
    final post = _post;
    if (post == null) return;

    final confirmed = await _confirm('确定删除这篇帖子吗？');
    if (confirmed != true) return;

    await _runAction(
      () async {
        await ref.read(postServiceProvider).deletePost(post.id);
        if (mounted) context.pop();
      },
      successMessage: '帖子已删除',
      reloadAll: false,
    );
  }

  Future<bool?> _confirm(String message) {
    return AppFeedback.confirm(context, message: message, destructive: true);
  }

  Future<_CommentDraft?> _openCommentComposer({
    required String title,
    required String initialValue,
    required String hintText,
    List<String> initialImages = const [],
  }) async {
    final controller = TextEditingController(text: initialValue);
    final existingImages = List<String>.from(
      initialImages.take(_maxCommentImages),
    );
    final selectedImages = <XFile>[];
    final result = await showAppBottomSheet<_CommentDraft>(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setSheetState) {
            Future<void> pickImages() async {
              final remaining =
                  _maxCommentImages -
                  existingImages.length -
                  selectedImages.length;
              if (remaining <= 0) return;

              final picked = await _imagePicker.pickMultiImage();
              if (picked.isEmpty) return;

              setSheetState(() {
                selectedImages.addAll(picked.take(remaining));
              });
            }

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
                    maxLength: DtoLimits.commentContentMax,
                    validator: (v) => optionalMaxValidator(
                      v,
                      max: DtoLimits.commentContentMax,
                      maxMessage: '评论内容长度不能超过 ${DtoLimits.commentContentMax} 位',
                    ),
                  ),
                  const SizedBox(height: 12),
                  _CommentComposerImages(
                    existingImages: existingImages,
                    localImages: selectedImages,
                    onRemoveExisting: (index) {
                      setSheetState(() => existingImages.removeAt(index));
                    },
                    onRemoveLocal: (index) {
                      setSheetState(() => selectedImages.removeAt(index));
                    },
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      CupertinoButton(
                        padding: EdgeInsets.zero,
                        onPressed:
                            existingImages.length + selectedImages.length >=
                                _maxCommentImages
                            ? null
                            : pickImages,
                        child: const Icon(CupertinoIcons.photo),
                      ),
                      const SizedBox(width: 8),
                      Text(
                        '${existingImages.length + selectedImages.length} / $_maxCommentImages',
                        style: AppTextStyles.label(context),
                      ),
                      const Spacer(),
                    ],
                  ),
                  const SizedBox(height: 20),
                  AppPrimaryButton(
                    onPressed: () => Navigator.of(context).pop(
                      _CommentDraft(
                        content: controller.text,
                        existingImages: List<String>.from(existingImages),
                        newImages: List<XFile>.from(selectedImages),
                      ),
                    ),
                    child: const Text('保存'),
                  ),
                ],
              ),
            );
          },
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
              AppTextField(
                controller: titleController,
                placeholder: '标题',
                maxLength: DtoLimits.postTitleMax,
              ),
              const SizedBox(height: 12),
              AppTextField(
                controller: contentController,
                maxLines: 6,
                placeholder: '内容',
                maxLength: DtoLimits.postContentMax,
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

  String? _validatePostDraft(String title, String content) {
    final titleError = requiredMaxValidator(
      title,
      emptyMessage: '请输入标题',
      max: DtoLimits.postTitleMax,
      maxMessage: '标题长度不能超过 ${DtoLimits.postTitleMax} 位',
    );
    if (titleError != null) return titleError;

    return requiredMaxValidator(
      content,
      emptyMessage: '请输入内容',
      max: DtoLimits.postContentMax,
      maxMessage: '内容长度不能超过 ${DtoLimits.postContentMax} 位',
    );
  }

  Widget _buildActionBar(
    BuildContext context,
    PostDto post,
    bool isAuthenticated,
  ) {
    return _ActionBar(
      likes: post.likes ?? 0,
      dislikes: post.dislikes ?? 0,
      watch: post.watch ?? 0,
      isWorking: _isWorking,
      isLiked: _postLiked,
      isDisliked: _postDisliked,
      onShare: _shareCurrentPost,
      onLike: () {
        if (!isAuthenticated) {
          context.go(AppRoutePaths.login);
          return;
        }
        _runAction(() async {
          final res = await ref.read(postServiceProvider).likePost(post.id);
          final data = res.data;
          if (data != null) {
            setState(() {
              _postLiked = data.isLiked;
              _postDisliked = false;
              _replacePostCounts(data.likeCount, data.dislikeCount);
            });
          }
        }, reloadAll: false);
      },
      onDislike: () {
        if (!isAuthenticated) {
          context.go(AppRoutePaths.login);
          return;
        }
        _runAction(() async {
          final res = await ref.read(postServiceProvider).dislikePost(post.id);
          final data = res.data;
          if (data != null) {
            setState(() {
              _postDisliked = data.isDisliked;
              _postLiked = false;
              _replacePostCounts(data.likeCount, data.dislikeCount);
            });
          }
        }, reloadAll: false);
      },
    );
  }

  void _replacePostCounts(int likes, int dislikes) {
    final current = _post;
    if (current == null) return;
    _post = PostDto(
      id: current.id,
      title: current.title,
      content: current.content,
      images: current.images,
      createdAt: current.createdAt,
      updatedAt: current.updatedAt,
      userId: current.userId,
      author: current.author,
      likes: likes,
      dislikes: dislikes,
      isLiked: _postLiked,
      isDisliked: _postDisliked,
      watch: current.watch,
      commentsCount: current.commentsCount,
      isReview: current.isReview,
    );
  }

  void _syncCommentReactionState(List<CommentDto> comments) {
    _likedComments.clear();
    _dislikedComments.clear();

    void visit(CommentDto comment) {
      if (comment.isLiked ?? false) {
        _likedComments.add(comment.id);
      }
      if (comment.isDisliked ?? false) {
        _dislikedComments.add(comment.id);
      }
      for (final reply in comment.repliedComments ?? const <CommentDto>[]) {
        visit(reply);
      }
    }

    for (final comment in comments) {
      visit(comment);
    }
  }

  void _applyCommentReaction(
    String commentId, {
    required bool isLiked,
    required bool isDisliked,
    required int likeCount,
    required int dislikeCount,
  }) {
    if (isLiked) {
      _likedComments.add(commentId);
    } else {
      _likedComments.remove(commentId);
    }

    if (isDisliked) {
      _dislikedComments.add(commentId);
    } else {
      _dislikedComments.remove(commentId);
    }

    _comments = _comments
        .map(
          (comment) => _replaceCommentCounts(
            comment,
            commentId,
            likeCount,
            dislikeCount,
          ),
        )
        .toList();
  }

  CommentDto _replaceCommentCounts(
    CommentDto comment,
    String commentId,
    int likeCount,
    int dislikeCount,
  ) {
    final replies = comment.repliedComments
        ?.map(
          (reply) =>
              _replaceCommentCounts(reply, commentId, likeCount, dislikeCount),
        )
        .toList();

    return CommentDto(
      id: comment.id,
      content: comment.content,
      images: comment.images,
      likes: comment.id == commentId ? likeCount : comment.likes,
      dislikes: comment.id == commentId ? dislikeCount : comment.dislikes,
      isLiked: comment.id == commentId
          ? _likedComments.contains(commentId)
          : comment.isLiked,
      isDisliked: comment.id == commentId
          ? _dislikedComments.contains(commentId)
          : comment.isDisliked,
      createdAt: comment.createdAt,
      updatedAt: comment.updatedAt,
      userId: comment.userId,
      author: comment.author,
      postId: comment.postId,
      parentCommentId: comment.parentCommentId,
      repliedComments: replies,
      isReview: comment.isReview,
    );
  }

  @override
  Widget build(BuildContext context) {
    final user = ref.watch(authControllerProvider).asData?.value.user;
    final post = _post;
    final isOwner = post != null && user != null && post.userId == user.id;

    final trailing = isOwner
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
        : null;

    if (_isLoading) {
      return AppPageScaffold(
        title: '详情',
        navigationBarStyle: AppNavigationBarStyle.compact,
        trailing: trailing,
        child: const Center(child: CupertinoActivityIndicator(radius: 14)),
      );
    }

    if (_errorMessage != null || post == null) {
      return AppPageScaffold(
        title: '详情',
        navigationBarStyle: AppNavigationBarStyle.compact,
        trailing: trailing,
        child: _PostDetailErrorView(
          message: _errorMessage ?? '详情加载失败',
          onRetry: _loadData,
        ),
      );
    }

    return AppPageScaffold(
      title: '详情',
      navigationBarStyle: AppNavigationBarStyle.compact,
      trailing: trailing,
      bottomBar: _buildCommentComposer(context),
      slivers: [
        CupertinoSliverRefreshControl(onRefresh: _loadData),
        SliverToBoxAdapter(
          child: AppResponsiveCenter(
            padding: AppResponsive.sliverPagePadding(context),
            child: Builder(
              builder: (context) {
                final isWide = AppResponsive.isWideTwoPane(context);
                final actionBar = _buildActionBar(context, post, user != null);
                final content = Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _PostHero(
                      post: post,
                      isFollowingAuthor: _isFollowingAuthor,
                      isMe: isOwner,
                      onFollowToggle: _toggleFollowAuthor,
                      onAuthorTap: post.userId == null
                          ? null
                          : () => context.push(
                              buildPublicProfileLocation(post.userId!),
                            ),
                    ),
                    if (!isWide) ...[const SizedBox(height: 24), actionBar],
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
                            if (v != null && v != _commentSort) {
                              setState(() => _commentSort = v);
                              _loadComments();
                            }
                          },
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    if (_isCommentsLoading)
                      const Padding(
                        padding: EdgeInsets.only(bottom: 16),
                        child: Center(
                          child: CupertinoActivityIndicator(radius: 10),
                        ),
                      ),
                    if (_comments.isEmpty && !_isCommentsLoading)
                      const AppEmptyState(
                        icon: CupertinoIcons.chat_bubble,
                        title: '暂无评论',
                        description: '分享你的见解，成为第一个评论的人。',
                      )
                    else
                      ..._comments.map(
                        (c) => Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: _CommentThread(
                            comment: c,
                            currentUserId: user?.id,
                            onAuthorTapForUser: (id) =>
                                context.push(buildPublicProfileLocation(id)),
                            onReply: () => _replyComment(c),
                            onEdit: user?.id == c.userId
                                ? () => _editComment(c)
                                : null,
                            onDelete: user?.id == c.userId
                                ? () => _deleteComment(c)
                                : null,
                            likedComments: _likedComments,
                            dislikedComments: _dislikedComments,
                            onLike: () {
                              if (user == null) {
                                context.go(AppRoutePaths.login);
                                return;
                              }
                              _runAction(() async {
                                final res = await _commentService.likeComment(
                                  c.id,
                                );
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    _applyCommentReaction(
                                      c.id,
                                      isLiked: data.isLiked,
                                      isDisliked: data.isDisliked,
                                      likeCount: data.likeCount,
                                      dislikeCount: data.dislikeCount,
                                    );
                                  });
                                }
                              }, reloadAll: false);
                            },
                            onDislike: () {
                              if (user == null) {
                                context.go(AppRoutePaths.login);
                                return;
                              }
                              _runAction(() async {
                                final res = await _commentService
                                    .dislikeComment(c.id);
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    _applyCommentReaction(
                                      c.id,
                                      isLiked: data.isLiked,
                                      isDisliked: data.isDisliked,
                                      likeCount: data.likeCount,
                                      dislikeCount: data.dislikeCount,
                                    );
                                  });
                                }
                              }, reloadAll: false);
                            },
                            onReplyChild: _replyComment,
                            onEditChild: _editComment,
                            onDeleteChild: _deleteComment,
                            onLikeChild: (child) {
                              if (user == null) {
                                context.go(AppRoutePaths.login);
                                return;
                              }
                              _runAction(() async {
                                final res = await _commentService.likeComment(
                                  child.id,
                                );
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    _applyCommentReaction(
                                      child.id,
                                      isLiked: data.isLiked,
                                      isDisliked: data.isDisliked,
                                      likeCount: data.likeCount,
                                      dislikeCount: data.dislikeCount,
                                    );
                                  });
                                }
                              }, reloadAll: false);
                            },
                            onDislikeChild: (child) {
                              if (user == null) {
                                context.go(AppRoutePaths.login);
                                return;
                              }
                              _runAction(() async {
                                final res = await _commentService
                                    .dislikeComment(child.id);
                                final data = res.data;
                                if (data != null) {
                                  setState(() {
                                    _applyCommentReaction(
                                      child.id,
                                      isLiked: data.isLiked,
                                      isDisliked: data.isDisliked,
                                      likeCount: data.likeCount,
                                      dislikeCount: data.dislikeCount,
                                    );
                                  });
                                }
                              }, reloadAll: false);
                            },
                          ),
                        ),
                      ),
                  ],
                );

                if (!isWide) {
                  return content;
                }

                return AppTwoPane(
                  key: const ValueKey('post-detail-responsive-two-pane'),
                  primary: content,
                  secondary: actionBar,
                );
              },
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildCommentComposer(BuildContext context) {
    return Align(
      alignment: Alignment.bottomCenter,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: AppResponsive.readableMaxWidth(context, fallback: 820),
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(AppRadii.xl),
          child: BackdropFilter(
            filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
            child: Container(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
              decoration: BoxDecoration(
                color: CupertinoDynamicColor.resolve(
                  AppColors.background,
                  context,
                ).withValues(alpha: 0.8),
                border: Border.all(
                  color: CupertinoDynamicColor.resolve(
                    AppColors.border,
                    context,
                  ).withValues(alpha: 0.3),
                ),
                borderRadius: BorderRadius.circular(AppRadii.xl),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  _CommentComposerImages(
                    localImages: _commentImages,
                    onRemoveLocal: _removeCommentImage,
                  ),
                  if (_commentImages.isNotEmpty) const SizedBox(height: 10),
                  if (_commentUploadProgress != null) ...[
                    ClipRRect(
                      borderRadius: BorderRadius.circular(AppRadii.sm),
                      child: Container(
                        height: 5,
                        color: AppColors.secondary,
                        alignment: Alignment.centerLeft,
                        child: FractionallySizedBox(
                          widthFactor: _commentUploadProgress!.clamp(0.0, 1.0),
                          child: Container(height: 5, color: AppColors.primary),
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                  ],
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      CupertinoButton(
                        padding: EdgeInsets.zero,
                        onPressed:
                            _isWorking ||
                                _commentImages.length >= _maxCommentImages
                            ? null
                            : _pickCommentImages,
                        child: const Icon(CupertinoIcons.photo, size: 22),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Container(
                          height: 44,
                          decoration: BoxDecoration(
                            color: CupertinoDynamicColor.resolve(
                              AppColors.secondary,
                              context,
                            ),
                            borderRadius: BorderRadius.circular(AppRadii.pill),
                          ),
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          alignment: Alignment.centerLeft,
                          child: CupertinoTextField(
                            controller: _commentController,
                            placeholder: '写下你的评论...',
                            placeholderStyle: AppTextStyles.muted(context),
                            decoration: null,
                            style: AppTextStyles.body(
                              context,
                            ).copyWith(fontSize: 15),
                            cursorColor: AppColors.primary,
                            inputFormatters: [
                              LengthLimitingTextInputFormatter(
                                DtoLimits.commentContentMax,
                              ),
                            ],
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
                            color: AppColors.primary.withValues(
                              alpha: _isWorking ? 0.5 : 1.0,
                            ),
                            fontWeight: FontWeight.w700,
                            fontSize: 16,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _PostHero extends StatelessWidget {
  const _PostHero({
    required this.post,
    this.onAuthorTap,
    this.isFollowingAuthor = false,
    this.isMe = false,
    this.onFollowToggle,
  });

  final PostDto post;
  final VoidCallback? onAuthorTap;
  final bool isFollowingAuthor;
  final bool isMe;
  final VoidCallback? onFollowToggle;

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
                Expanded(
                  child: GestureDetector(
                    onTap: () {
                      if (post.userId != null && post.userId!.isNotEmpty) {
                        context.push(buildPublicProfileLocation(post.userId!));
                      }
                    },
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
                      ],
                    ),
                  ),
                ),
                AppSecondaryButton(
                  onPressed: isMe ? onAuthorTap : onFollowToggle,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 6,
                  ),
                  child: Text(
                    isMe
                        ? '我的主页'
                        : isFollowingAuthor
                        ? '已关注'
                        : '关注',
                    style: TextStyle(
                      fontSize: 13,
                      color: isFollowingAuthor && !isMe
                          ? AppColors.mutedForeground
                          : null,
                    ),
                  ),
                ),
              ],
            ),
          ),
        Text(
          post.title?.trim().isNotEmpty == true ? post.title!.trim() : '未命名帖子',
          style: AppTextStyles.title(
            context,
          ).copyWith(fontSize: 22, height: 1.3),
        ),
        const SizedBox(height: 16),
        Text(
          post.content?.trim().isNotEmpty == true
              ? post.content!.trim()
              : '这个帖子还没有填写描述。',
          style: AppTextStyles.body(context).copyWith(
            fontSize: 17,
            height: 1.6,
            color: CupertinoDynamicColor.resolve(
              AppColors.foreground,
              context,
            ).withValues(alpha: 0.9),
          ),
        ),
        if (post.images?.isNotEmpty == true) ...[
          const SizedBox(height: 20),
          CarouselSlider.builder(
            itemCount: post.images?.length,
            itemBuilder:
                (BuildContext context, int itemIndex, int pageViewIndex) =>
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: GestureDetector(
                        onTap: () {
                          Navigator.of(context).push(
                            CupertinoPageRoute(
                              fullscreenDialog: true,
                              builder: (_) => ImageViewerScreen(
                                images: post.images!,
                                initialIndex: itemIndex,
                              ),
                            ),
                          );
                        },
                        child: Hero(
                          tag: 'image_${post.images?[itemIndex]}',
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(AppRadii.lg),
                            child: Image.network(
                              post.images?[itemIndex] ?? '',
                              width: double.infinity,
                              fit: BoxFit.cover,
                              gaplessPlayback: true,
                              errorBuilder: (context, error, stackTrace) =>
                                  Container(
                                    height: 200,
                                    width: double.infinity,
                                    color: AppColors.muted,
                                    child: const Icon(
                                      CupertinoIcons.photo,
                                      color: AppColors.mutedForeground,
                                    ),
                                  ),
                            ),
                          ),
                        ),
                      ),
                    ),
            options: CarouselOptions(
              aspectRatio: 16 / 9,
              viewportFraction: 0.8,
              initialPage: 0,
              enableInfiniteScroll: false,
              reverse: false,
              enlargeCenterPage: true,
              enlargeFactor: 0.3,
              scrollDirection: Axis.horizontal,
            ),
          ),
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
    required this.onShare,
    required this.onLike,
    required this.onDislike,
  });

  final int likes;
  final int dislikes;
  final int watch;
  final bool isWorking;
  final bool isLiked;
  final bool isDisliked;
  final VoidCallback onShare;
  final VoidCallback onLike;
  final VoidCallback onDislike;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16),
      decoration: BoxDecoration(
        border: Border(
          top: BorderSide(
            color: CupertinoDynamicColor.resolve(
              AppColors.border,
              context,
            ).withValues(alpha: 0.3),
          ),
          bottom: BorderSide(
            color: CupertinoDynamicColor.resolve(
              AppColors.border,
              context,
            ).withValues(alpha: 0.3),
          ),
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
            icon: isDisliked
                ? CupertinoIcons.hand_thumbsdown_fill
                : CupertinoIcons.hand_thumbsdown,
            label: '$dislikes',
            onTap: isWorking ? null : onDislike,
            color: CupertinoDynamicColor.resolve(
              AppColors.mutedForeground,
              context,
            ),
            active: isDisliked,
          ),
          const Spacer(),
          Text('$watch 次浏览', style: AppTextStyles.label(context)),
          const SizedBox(width: 12),
          CupertinoButton(
            padding: EdgeInsets.zero,
            minimumSize: Size.zero,
            onPressed: onShare,
            child: const Icon(
              CupertinoIcons.share,
              size: 18,
              color: AppColors.mutedForeground,
            ),
          ),
        ],
      ),
    );
  }
}

class _ActionChip extends StatelessWidget {
  const _ActionChip({
    required this.icon,
    required this.label,
    this.onTap,
    required this.color,
    this.active = false,
  });

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
              color: active
                  ? color
                  : CupertinoDynamicColor.resolve(
                      AppColors.mutedForeground,
                      context,
                    ),
            ),
            const SizedBox(width: 8),
            Text(
              label,
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w700,
                color: active
                    ? color
                    : CupertinoDynamicColor.resolve(
                        AppColors.foreground,
                        context,
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// 一条被展平的回复:[comment] 是回复本身,[replyTo] 是它直接回复的那条评论。
/// [replyTo] 为 null 表示它直接回复顶层评论(不显示 @);否则显示 @对方。
class _FlatReply {
  const _FlatReply({required this.comment, this.replyTo});

  final CommentDto comment;
  final CommentDto? replyTo;
}

/// 将一条顶层评论下的所有后代回复展平成单层列表(只保留两级:顶层评论 + 其下所有回复)。
/// 直接回复顶层评论的 replyTo 记为 null;回复某条回复的则记录被回复者,用于渲染 @对方。
/// 列表按创建时间从早到晚排序,读起来像一段对话。
List<_FlatReply> _flattenReplies(CommentDto root) {
  final out = <_FlatReply>[];

  void walk(List<CommentDto>? nodes, CommentDto parent, bool parentIsRoot) {
    if (nodes == null) return;
    for (final child in nodes) {
      out.add(
        _FlatReply(comment: child, replyTo: parentIsRoot ? null : parent),
      );
      walk(child.repliedComments, child, false);
    }
  }

  walk(root.repliedComments, root, true);
  out.sort((a, b) {
    final at = a.comment.createdAt;
    final bt = b.comment.createdAt;
    if (at == null || bt == null) return 0;
    return at.compareTo(bt);
  });
  return out;
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
    // 展平为两级:顶层评论 + 其下所有回复(含原本的三级及更深),更深的回复用 @对方 标注。
    final flatReplies = _flattenReplies(comment);
    return Column(
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
        if (flatReplies.isNotEmpty) ...[
          Container(
            margin: const EdgeInsets.only(left: 44, top: 12),
            padding: const EdgeInsets.only(left: 12),
            decoration: BoxDecoration(
              border: Border(
                left: BorderSide(
                  color: CupertinoDynamicColor.resolve(
                    AppColors.border,
                    context,
                  ).withValues(alpha: 0.3),
                  width: 2,
                ),
              ),
            ),
            child: Column(
              children: [
                for (final flat in flatReplies) ...[
                  _CommentCard(
                    comment: flat.comment,
                    isReply: true,
                    replyToName: flat.replyTo?.author?.name,
                    onAuthorTap: flat.comment.userId == null
                        ? null
                        : () => onAuthorTapForUser(flat.comment.userId!),
                    onReply: () => onReplyChild(flat.comment),
                    onEdit: currentUserId == flat.comment.userId
                        ? () => onEditChild?.call(flat.comment)
                        : null,
                    onDelete: currentUserId == flat.comment.userId
                        ? () => onDeleteChild?.call(flat.comment)
                        : null,
                    onLike: () => onLikeChild(flat.comment),
                    onDislike: () => onDislikeChild(flat.comment),
                    isLiked: likedComments.contains(flat.comment.id),
                    isDisliked: dislikedComments.contains(flat.comment.id),
                  ),
                  if (flat != flatReplies.last) const SizedBox(height: 16),
                ],
              ],
            ),
          ),
        ],
        const SizedBox(height: 16),
        Container(
          height: 1,
          color: CupertinoDynamicColor.resolve(
            AppColors.border,
            context,
          ).withValues(alpha: 0.3),
        ),
        const SizedBox(height: 16),
      ],
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
    this.isReply = false,
    this.replyToName,
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
  final bool isReply;
  // 被回复者名称;仅楼中楼(回复某条回复)时有值,在内容前显示 @对方。
  final String? replyToName;

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
              Text.rich(
                TextSpan(
                  children: [
                    if (replyToName != null && replyToName!.isNotEmpty)
                      TextSpan(
                        text: '@$replyToName ',
                        style: const TextStyle(
                          fontWeight: FontWeight.w600,
                          color: AppColors.primary,
                        ),
                      ),
                    TextSpan(text: comment.content ?? ''),
                  ],
                ),
                style: AppTextStyles.body(
                  context,
                ).copyWith(fontSize: 15, height: 1.5),
              ),
              _CommentImageGrid(images: comment.images ?? const []),
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
                    _TextAction(
                      label: '删除',
                      onTap: onDelete!,
                      activeColor: AppColors.destructive,
                      active: true,
                    ),
                  ],
                  const Spacer(),
                  _CommentActionIcon(
                    icon: isDisliked
                        ? CupertinoIcons.hand_thumbsdown_fill
                        : CupertinoIcons.hand_thumbsdown,
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

class _CommentImageGrid extends StatelessWidget {
  const _CommentImageGrid({required this.images});

  final List<String> images;

  @override
  Widget build(BuildContext context) {
    if (images.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(top: 10),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          for (var i = 0; i < images.length && i < _maxCommentImages; i++)
            GestureDetector(
              onTap: () {
                Navigator.of(context).push(
                  CupertinoPageRoute<void>(
                    builder: (_) =>
                        ImageViewerScreen(images: images, initialIndex: i),
                  ),
                );
              },
              child: Hero(
                tag: 'image_${images[i]}',
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(AppRadii.md),
                  child: Image.network(
                    images[i],
                    width: images.length == 1 ? 132 : 82,
                    height: images.length == 1 ? 132 : 82,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) => Container(
                      width: images.length == 1 ? 132 : 82,
                      height: images.length == 1 ? 132 : 82,
                      color: AppColors.secondary,
                      alignment: Alignment.center,
                      child: const Icon(CupertinoIcons.photo),
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

class _CommentComposerImages extends StatelessWidget {
  const _CommentComposerImages({
    this.existingImages = const [],
    this.localImages = const [],
    this.onRemoveExisting,
    this.onRemoveLocal,
  });

  final List<String> existingImages;
  final List<XFile> localImages;
  final ValueChanged<int>? onRemoveExisting;
  final ValueChanged<int>? onRemoveLocal;

  @override
  Widget build(BuildContext context) {
    if (existingImages.isEmpty && localImages.isEmpty) {
      return const SizedBox.shrink();
    }

    return Wrap(
      spacing: 10,
      runSpacing: 10,
      children: [
        for (var i = 0; i < existingImages.length; i++)
          _ComposerImageTile(
            image: Image.network(existingImages[i], fit: BoxFit.cover),
            onRemove: onRemoveExisting == null
                ? null
                : () => onRemoveExisting!(i),
          ),
        for (var i = 0; i < localImages.length; i++)
          _ComposerImageTile(
            image: Image.file(File(localImages[i].path), fit: BoxFit.cover),
            onRemove: onRemoveLocal == null ? null : () => onRemoveLocal!(i),
          ),
      ],
    );
  }
}

class _ComposerImageTile extends StatelessWidget {
  const _ComposerImageTile({required this.image, this.onRemove});

  final Widget image;
  final VoidCallback? onRemove;

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(AppRadii.md),
          child: SizedBox(width: 72, height: 72, child: image),
        ),
        if (onRemove != null)
          Positioned(
            right: -8,
            top: -8,
            child: CupertinoButton(
              padding: EdgeInsets.zero,
              onPressed: onRemove,
              child: const Icon(
                CupertinoIcons.xmark_circle_fill,
                color: AppColors.destructive,
                size: 24,
              ),
            ),
          ),
      ],
    );
  }
}

class _CommentActionIcon extends StatelessWidget {
  const _CommentActionIcon({
    required this.icon,
    this.label,
    required this.onTap,
    this.active = false,
    this.activeColor,
  });

  final IconData icon;
  final String? label;
  final VoidCallback onTap;
  final bool active;
  final Color? activeColor;

  @override
  Widget build(BuildContext context) {
    final color = active
        ? (activeColor ??
              CupertinoDynamicColor.resolve(AppColors.primary, context))
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
            Text(
              label!,
              style: TextStyle(
                fontSize: 12,
                color: color,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ],
      ),
    );
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
    final resolvedMuted = CupertinoDynamicColor.resolve(
      AppColors.mutedForeground,
      context,
    );
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
