import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import 'package:mobile_app/l10n/app_localizations.dart';
import '../../components/post_editor_form.dart';
import '../../components/post_tag_selector.dart';
import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../services/error_messages.dart';
import '../../router/app_router.dart';
import '../../services/file_service.dart';
import '../../services/image_compression_service.dart';
import '../../ui/app_feedback.dart';
import '../../utils/dto_validation.dart';

class CreatePostScreen extends ConsumerStatefulWidget {
  const CreatePostScreen({super.key});

  @override
  ConsumerState<CreatePostScreen> createState() => _CreatePostScreenState();
}

class _CreatePostScreenState extends ConsumerState<CreatePostScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _contentController = TextEditingController();
  final _imagePicker = ImagePicker();
  final _fileService = FileService();
  final List<XFile> _images = [];
  List<PostTagDto> _tags = const [];
  PostTagDto? _selectedTag;
  bool _isSubmitting = false;
  double? _uploadProgress;

  @override
  void initState() {
    super.initState();
    _loadTags();
  }

  @override
  void dispose() {
    _titleController.dispose();
    _contentController.dispose();
    super.dispose();
  }

  Future<void> _pickImages() async {
    final picked = await _imagePicker.pickMultiImage();
    if (picked.isEmpty) return;

    setState(() {
      _images.addAll(picked);
    });
  }

  Future<void> _removeImage(int index) async {
    setState(() => _images.removeAt(index));
  }

  Future<void> _loadTags() async {
    try {
      final response = await ref.read(postServiceProvider).getPostTags();
      if (!mounted) return;
      setState(() => _tags = response.data ?? const <PostTagDto>[]);
    } catch (_) {
      if (!mounted) return;
      setState(() => _tags = const <PostTagDto>[]);
    }
  }

  Future<void> _selectTag() async {
    final nextTag = await showPostTagPicker(
      context,
      tags: _tags,
      selectedTag: _selectedTag,
    );
    if (!mounted) return;
    setState(() => _selectedTag = nextTag);
  }

  Future<void> _submit() async {
    final authState = ref.read(authControllerProvider).asData?.value;
    final user = authState?.user;
    if (user == null) {
      context.go(AppRoutePaths.login);
      return;
    }

    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSubmitting = true;
    });

    // Compress images to WebP before upload to reduce file size
    final compressed = <CompressedImage>[];
    try {
      // Fetch upload key and compress images in parallel — they are independent.
      // This saves one network round-trip worth of latency.
      String? uploadKey;
      if (_images.isNotEmpty) {
        final results = await Future.wait([
          _fileService.getUploadKey().then(
            (r) => (r.data?['key'] as String?) ?? '',
          ),
          ImageCompressionService.compressAll(
            _images,
            quality: ImageCompressionService.postImageQuality,
            maxWidth: ImageCompressionService.postMaxWidth,
            maxHeight: ImageCompressionService.postMaxHeight,
          ),
        ]);
        uploadKey = results[0] as String?;
        if (uploadKey?.isEmpty == true) {
          uploadKey = null;
        }
        compressed.addAll(results[1] as List<CompressedImage>);
      }

      final uploadedImages = compressed.isEmpty
          ? <String>[]
          : (await _fileService.uploadStream(
                  compressed.map(ImageCompressionService.toXFile).toList(),
                  key: uploadKey,
                  onProgress: (fraction) {
                    if (mounted) setState(() => _uploadProgress = fraction);
                  },
                )).data ??
                <String>[];

      final response = await ref
          .read(postServiceProvider)
          .createPost(
            PostCreateDto(
              title: _titleController.text.trim(),
              content: _contentController.text.trim(),
              images: uploadedImages,
              userId: user.id,
              uploadKey: uploadKey,
              tagId: _selectedTag?.id,
            ),
          );

      final post = response.data;
      if (post == null)
        throw Exception(
          response.message ?? AppLocalizations.of(context)!.postCreateFailed,
        );

      if (!mounted) return;

      await AppFeedback.showSuccess(
        context,
        message: AppLocalizations.of(context)!.postCreated,
      );
      if (!mounted) return;
      context.pushReplacement(buildPostDetailLocation(post.id));
    } catch (error) {
      if (!mounted) return;
      await AppFeedback.showError(
        context,
        message: extractErrorFromException(error),
      );
    } finally {
      // Clean up temp compressed files regardless of outcome
      ImageCompressionService.cleanup(compressed);
      if (mounted) {
        setState(() {
          _isSubmitting = false;
          _uploadProgress = null;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return CupertinoPageScaffold(
      navigationBar: CupertinoNavigationBar(
        middle: Text(AppLocalizations.of(context)!.postCreate),
        trailing: CupertinoButton(
          padding: EdgeInsets.zero,
          onPressed: _isSubmitting ? null : _submit,
          child: Text(
            _isSubmitting
                ? AppLocalizations.of(context)!.postCreatePublishing
                : AppLocalizations.of(context)!.postCreatePublish,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
        ),
      ),
      child: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: EdgeInsets.zero,
            children: [
              PostEditorForm(
                layout: PostEditorLayout.page,
                titleController: _titleController,
                contentController: _contentController,
                selectedTag: _selectedTag,
                existingImages: const [],
                localImages: _images,
                tagEmptyText: AppLocalizations.of(context)!.postCreateNoTag,
                onPickTag: _isSubmitting ? null : _selectTag,
                onPickImages: _isSubmitting ? null : _pickImages,
                onRemoveLocalImage: _isSubmitting ? null : _removeImage,
                onSubmit: _isSubmitting ? null : _submit,
                submitLabel: _isSubmitting
                    ? AppLocalizations.of(context)!.postCreatePublishing
                    : AppLocalizations.of(context)!.postCreatePublish,
                uploadProgress: _uploadProgress,
                titleValidator: (v) => requiredMaxValidator(
                  v,
                  emptyMessage: AppLocalizations.of(context)!.postCreateTitleEmpty,
                  max: DtoLimits.postTitleMax,
                  maxMessage: '标题长度不能超过 ${DtoLimits.postTitleMax} 位',
                ),
                contentValidator: (v) => requiredMaxValidator(
                  v,
                  emptyMessage: AppLocalizations.of(context)!.postCreateContentEmpty,
                  max: DtoLimits.postContentMax,
                  maxMessage: '内容长度不能超过 ${DtoLimits.postContentMax} 位',
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
