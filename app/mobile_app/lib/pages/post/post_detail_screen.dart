import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import 'package:mobile_app/l10n/app_localizations.dart';
import '../../components/editable_image_wrap.dart';
import '../../components/post_editor_form.dart';
import '../../components/post_tag_selector.dart';
import '../../models/comment.dart';
import '../../models/post.dart';
import '../../models/user.dart';
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
import 'widgets/post_detail_action_bar.dart';
import 'widgets/post_detail_comment_composer.dart';
import 'widgets/post_detail_comment_widgets.dart';
import 'widgets/post_detail_error_view.dart';
import 'widgets/post_detail_hero.dart';

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

class _PostDraft {
  const _PostDraft({
    required this.title,
    required this.content,
    this.existingImages = const [],
    this.newImages = const [],
    this.tag,
  });

  final String title;
  final String content;
  final List<String> existingImages;
  final List<XFile> newImages;
  final PostTagDto? tag;
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
        throw Exception(AppLocalizations.of(context)!.postNotFound);
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
      final newComment = response.data == null
          ? null
          : _withAuthorFallback(response.data!, user);
      if (newComment != null) {
        _insertCommentLocally(newComment);
        _commentController.clear();
        setState(() {
          _commentImages.clear();
          _commentUploadProgress = null;
        });
        if (mounted)
          await AppFeedback.showSuccess(
            context,
            message: AppLocalizations.of(context)!.postCommentSent,
          );
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
      final newReply = response.data == null
          ? null
          : _withAuthorFallback(response.data!, user);
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
    final remaining = postDetailMaxCommentImages - _commentImages.length;
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

  Future<({List<String> images, String? uploadKey})> _uploadPostImages(
    List<XFile> images, {
    void Function(double fraction)? onProgress,
  }) async {
    if (images.isEmpty) return (images: <String>[], uploadKey: null);

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

      final uploadedImages =
          (await _fileService.uploadStream(
            compressed.map(ImageCompressionService.toXFile).toList(),
            key: uploadKey,
            onProgress: onProgress,
          )).data ??
          <String>[];

      return (images: uploadedImages, uploadKey: uploadKey);
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

  CommentDto _withAuthorFallback(CommentDto comment, UserDto currentUser) {
    final author = comment.author;
    final hasAuthorInfo =
        author?.id?.trim().isNotEmpty == true ||
        author?.name?.trim().isNotEmpty == true ||
        author?.avatar?.trim().isNotEmpty == true;

    if (hasAuthorInfo) {
      return comment;
    }

    return CommentDto(
      id: comment.id,
      content: comment.content,
      images: comment.images,
      likes: comment.likes,
      dislikes: comment.dislikes,
      isLiked: comment.isLiked,
      isDisliked: comment.isDisliked,
      createdAt: comment.createdAt,
      updatedAt: comment.updatedAt,
      userId: comment.userId,
      author: UserSimpleDto(
        id: currentUser.id,
        name: currentUser.name,
        avatar: currentUser.avatar,
      ),
      postId: comment.postId,
      parentCommentId: comment.parentCommentId,
      repliedComments: comment.repliedComments,
      isReview: comment.isReview,
    );
  }

  Future<void> _editPost() async {
    final post = _post;
    if (post == null) return;

    List<PostTagDto> tags = const [];
    try {
      final response = await ref.read(postServiceProvider).getPostTags();
      tags = response.data ?? const <PostTagDto>[];
    } catch (_) {
      tags = const <PostTagDto>[];
    }

    final draft = await _openPostEditor(
      title: post.title ?? '',
      content: post.content ?? '',
      initialImages: post.images ?? const [],
      initialTag: post.tag,
      availableTags: tags,
    );
    if (draft == null) return;
    final validationError = _validatePostDraft(draft.title, draft.content);
    if (validationError != null) {
      if (!mounted) return;
      await AppFeedback.showError(context, message: validationError);
      return;
    }

    setState(() => _isWorking = true);
    try {
      final uploadResult = await _uploadPostImages(draft.newImages);
      final nextImages = [...draft.existingImages, ...uploadResult.images];
      final result = await ref
          .read(postServiceProvider)
          .updatePost(
            post.id,
            PostUpdateDto(
              title: draft.title,
              content: draft.content,
              images: nextImages,
              uploadKey: uploadResult.uploadKey,
              tagId: draft.tag?.id,
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
      successMessage: AppLocalizations.of(context)!.postDeleted,
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
      initialImages.take(postDetailMaxCommentImages),
    );
    final selectedImages = <XFile>[];
    final result = await showAppBottomSheet<_CommentDraft>(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setSheetState) {
            Future<void> pickImages() async {
              final remaining =
                  postDetailMaxCommentImages -
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
                  EditableImageWrap(
                    existingImages: existingImages,
                    localImages: selectedImages,
                    onRemoveExisting: (index) {
                      setSheetState(() => existingImages.removeAt(index));
                    },
                    onRemoveLocal: (index) {
                      setSheetState(() => selectedImages.removeAt(index));
                    },
                    tileSize: 72,
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      CupertinoButton(
                        padding: EdgeInsets.zero,
                        onPressed:
                            existingImages.length + selectedImages.length >=
                                postDetailMaxCommentImages
                            ? null
                            : pickImages,
                        child: const Icon(CupertinoIcons.photo),
                      ),
                      const SizedBox(width: 8),
                      Text(
                        '${existingImages.length + selectedImages.length} / $postDetailMaxCommentImages',
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

  Future<_PostDraft?> _openPostEditor({
    required String title,
    required String content,
    List<String> initialImages = const [],
    PostTagDto? initialTag,
    List<PostTagDto> availableTags = const [],
  }) async {
    final titleController = TextEditingController(text: title);
    final contentController = TextEditingController(text: content);
    final existingImages = List<String>.from(initialImages);
    final selectedImages = <XFile>[];
    PostTagDto? selectedTag = initialTag;

    final result = await showAppBottomSheet<_PostDraft>(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setSheetState) {
            Future<void> pickImages() async {
              final picked = await _imagePicker.pickMultiImage();
              if (picked.isEmpty) return;

              setSheetState(() {
                selectedImages.addAll(picked);
              });
            }

            Future<void> selectTag() async {
              final nextTag = await showPostTagPicker(
                context,
                tags: availableTags,
                selectedTag: selectedTag,
              );
              if (!context.mounted) return;
              setSheetState(() {
                selectedTag = nextTag;
              });
            }

            return Padding(
              padding: EdgeInsets.only(
                left: 20,
                right: 20,
                top: 20,
                bottom: MediaQuery.of(context).viewInsets.bottom + 20,
              ),
              child: PostEditorForm(
                layout: PostEditorLayout.sheet,
                headerText: '编辑帖子',
                titleController: titleController,
                contentController: contentController,
                selectedTag: selectedTag,
                existingImages: existingImages,
                localImages: selectedImages,
                onPickTag: availableTags.isEmpty ? null : selectTag,
                onPickImages: pickImages,
                onRemoveExistingImage: (index) {
                  setSheetState(() => existingImages.removeAt(index));
                },
                onRemoveLocalImage: (index) {
                  setSheetState(() => selectedImages.removeAt(index));
                },
                onSubmit: () {
                  Navigator.of(context).pop(
                    _PostDraft(
                      title: titleController.text.trim(),
                      content: contentController.text.trim(),
                      existingImages: List<String>.from(existingImages),
                      newImages: List<XFile>.from(selectedImages),
                      tag: selectedTag,
                    ),
                  );
                },
                submitLabel: AppLocalizations.of(context)!.notificationSaveSettings,
                tagEmptyText: availableTags.isEmpty
                    ? AppLocalizations.of(context)!.postCreateNoTag
                    : AppLocalizations.of(context)!.postCreateNoTag,
              ),
            );
          },
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
      emptyMessage: AppLocalizations.of(context)!.postCreateTitleEmpty,
      max: DtoLimits.postTitleMax,
      maxMessage: '标题长度不能超过 ${DtoLimits.postTitleMax} 位',
    );
    if (titleError != null) return titleError;

    return requiredMaxValidator(
      content,
      emptyMessage: AppLocalizations.of(context)!.postCreateContentEmpty,
      max: DtoLimits.postContentMax,
      maxMessage: '内容长度不能超过 ${DtoLimits.postContentMax} 位',
    );
  }

  Widget _buildActionBar(
    BuildContext context,
    PostDto post,
    bool isAuthenticated,
  ) {
    return PostDetailActionBar(
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
        title: AppLocalizations.of(context)!.postDetail,
        navigationBarStyle: AppNavigationBarStyle.compact,
        trailing: trailing,
        child: const Center(child: CupertinoActivityIndicator(radius: 14)),
      );
    }

    if (_errorMessage != null || post == null) {
      return AppPageScaffold(
        title: AppLocalizations.of(context)!.postDetail,
        navigationBarStyle: AppNavigationBarStyle.compact,
        trailing: trailing,
        child: PostDetailErrorView(
          message: _errorMessage ?? '详情加载失败',
          onRetry: _loadData,
        ),
      );
    }

    return AppPageScaffold(
      title: AppLocalizations.of(context)!.postDetail,
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
                    PostDetailHero(
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
                          children: {
                            'recent': Padding(
                              padding: EdgeInsets.symmetric(horizontal: 10),
                              child: Text(
                                AppLocalizations.of(context)!.postCommentSortNewest,
                                style: TextStyle(fontSize: 13),
                              ),
                            ),
                            'hot': Padding(
                              padding: EdgeInsets.symmetric(horizontal: 10),
                              child: Text(
                                AppLocalizations.of(context)!.postCommentSortHot,
                                style: TextStyle(fontSize: 13),
                              ),
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
                      AppEmptyState(
                        icon: CupertinoIcons.chat_bubble,
                        title: AppLocalizations.of(context)!.postNoComments,
                        description: '分享你的见解，成为第一个评论的人。',
                      )
                    else
                      ..._comments.map(
                        (c) => Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: PostDetailCommentThread(
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
    return PostDetailCommentComposer(
      controller: _commentController,
      localImages: _commentImages,
      isWorking: _isWorking,
      uploadProgress: _commentUploadProgress,
      onPickImages:
          _isWorking || _commentImages.length >= postDetailMaxCommentImages
          ? null
          : _pickCommentImages,
      onRemoveLocal: _removeCommentImage,
      onSubmit: _submitComment,
    );
  }
}
