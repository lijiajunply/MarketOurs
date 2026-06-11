import 'dart:io';

import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../services/error_messages.dart';
import '../../router/app_router.dart';
import '../../services/file_service.dart';
import '../../services/image_compression_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
import '../../ui/app_responsive.dart';
import '../../ui/app_theme.dart';
import '../../ui/app_widgets.dart';

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
  bool _isSubmitting = false;
  double? _uploadProgress;

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
            ),
          );

      final post = response.data;
      if (post == null) throw Exception(response.message ?? '帖子创建失败');

      if (!mounted) return;

      await AppFeedback.showSuccess(context, message: '帖子已发布');
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

  Widget _buildUploadProgress() {
    final fraction = _uploadProgress ?? 0;
    final percent = (fraction * 100).round();
    return AppTappableCard(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
      radius: AppRadii.lg,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                '正在上传图片',
                style: TextStyle(fontSize: 14, color: AppColors.mutedForeground),
              ),
              Text(
                '$percent%',
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ClipRRect(
            borderRadius: BorderRadius.circular(AppRadii.sm),
            child: Container(
              height: 6,
              color: AppColors.secondary,
              alignment: Alignment.centerLeft,
              child: FractionallySizedBox(
                widthFactor: fraction,
                child: Container(
                  height: 6,
                  color: AppColors.primary,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      navigationBar: CupertinoNavigationBar(
        middle: const Text('发布帖子'),
        backgroundColor: CupertinoDynamicColor.resolve(
          AppColors.background,
          context,
        ).withValues(alpha: 0.94),
        border: Border(
          bottom: BorderSide(
            color: CupertinoDynamicColor.resolve(
              AppColors.border,
              context,
            ).withValues(alpha: 0.3),
          ),
        ),
        trailing: CupertinoButton(
          padding: EdgeInsets.zero,
          onPressed: _isSubmitting ? null : _submit,
          child: Text(
            _isSubmitting ? '正在发布' : '发布',
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
              AppResponsiveCenter(
                maxWidth: AppResponsive.formMaxWidth(context),
                padding: AppResponsive.pagePadding(context, narrow: 20),
                child: AppTwoPane(
                  primary: _buildEditorCard(),
                  secondary: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _buildImageCard(),
                      if (_uploadProgress != null) ...[
                        const SizedBox(height: 12),
                        _buildUploadProgress(),
                      ],
                      const SizedBox(height: 20),
                      AppPrimaryButton(
                        onPressed: _isSubmitting ? null : _submit,
                        child: Text(_isSubmitting ? '发布中...' : '立即发布'),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildEditorCard() {
    return AppTappableCard(
      padding: const EdgeInsets.all(20),
      radius: AppRadii.lg,
      child: Column(
        children: [
          AppTextField(
            controller: _titleController,
            placeholder: '帖子标题',
            validator: (v) => v?.trim().isEmpty == true ? '请输入标题' : null,
          ),
          const SizedBox(height: 16),
          AppTextField(
            controller: _contentController,
            placeholder: '分享此刻的新鲜事...',
            maxLines: AppResponsive.isDesktop(context) ? 12 : 8,
            validator: (v) => v?.trim().isEmpty == true ? '请输入内容' : null,
          ),
        ],
      ),
    );
  }

  Widget _buildImageCard() {
    return AppTappableCard(
      padding: const EdgeInsets.all(20),
      radius: AppRadii.lg,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                '图片',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
              ),
              CupertinoButton(
                padding: EdgeInsets.zero,
                onPressed: _isSubmitting ? null : _pickImages,
                child: const Text(
                  '添加图片',
                  style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (_images.isEmpty)
            Container(
              height: 120,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: AppColors.secondary,
                borderRadius: BorderRadius.circular(AppRadii.md),
              ),
              child: const Text(
                '还没选择图片',
                style: TextStyle(color: AppColors.mutedForeground),
              ),
            )
          else
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                for (var i = 0; i < _images.length; i++)
                  _ImagePreview(
                    image: _images[i],
                    onRemove: () => _removeImage(i),
                  ),
              ],
            ),
        ],
      ),
    );
  }
}

class _ImagePreview extends StatelessWidget {
  const _ImagePreview({required this.image, required this.onRemove});
  final XFile image;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(AppRadii.md),
          child: Image.file(
            File(image.path),
            width: 90,
            height: 90,
            fit: BoxFit.cover,
          ),
        ),
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
