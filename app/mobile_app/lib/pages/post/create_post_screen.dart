import 'dart:io';

import 'package:flutter/cupertino.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../models/post.dart';
import '../../providers/auth_provider.dart';
import '../../providers/post_feed_provider.dart';
import '../../router/app_router.dart';
import '../../services/file_service.dart';
import '../../ui/app_feedback.dart';
import '../../ui/app_fields.dart';
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
  String? _errorMessage;

  @override
  void dispose() {
    _titleController.dispose();
    _contentController.dispose();
    super.dispose();
  }

  Future<void> _pickImages() async {
    final picked = await _imagePicker.pickMultiImage(imageQuality: 90);
    if (picked.isEmpty) return;

    setState(() {
      _images.addAll(picked);
      _errorMessage = null;
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
      _errorMessage = null;
    });

    try {
      final uploadedImages = _images.isEmpty
          ? <String>[]
          : (await _fileService.uploadImages(_images)).data ?? <String>[];

      final response = await ref.read(postServiceProvider).createPost(
            PostCreateDto(
              title: _titleController.text.trim(),
              content: _contentController.text.trim(),
              images: uploadedImages.isEmpty ? null : uploadedImages,
              userId: user.id,
            ),
          );

      final post = response.data;
      if (post == null) throw Exception(response.message ?? '帖子创建失败');

      if (!mounted) return;

      await AppFeedback.showMessage(context, message: '帖子已发布');
      context.go(buildPostDetailLocation(post.id));
    } catch (error) {
      if (!mounted) return;
      setState(() => _errorMessage = error.toString().replaceFirst('Exception: ', ''));
      await AppFeedback.showMessage(context, message: _errorMessage ?? '帖子创建失败');
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return CupertinoPageScaffold(
      backgroundColor: AppColors.background,
      navigationBar: CupertinoNavigationBar(
        middle: const Text('发布帖子'),
        backgroundColor: AppColors.background.withValues(alpha: 0.94),
        border: Border(bottom: BorderSide(color: AppColors.border.withValues(alpha: 0.3))),
        trailing: CupertinoButton(
          padding: EdgeInsets.zero,
          onPressed: _isSubmitting ? null : _submit,
          child: Text(_isSubmitting ? '正在发布' : '发布', style: const TextStyle(fontWeight: FontWeight.w700)),
        ),
      ),
      child: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: const EdgeInsets.all(20),
            children: [
              if (_errorMessage != null) ...[
                _ErrorBanner(message: _errorMessage!),
                const SizedBox(height: 16),
              ],
              AppTappableCard(
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
                      maxLines: 8,
                      validator: (v) => v?.trim().isEmpty == true ? '请输入内容' : null,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              AppTappableCard(
                padding: const EdgeInsets.all(20),
                radius: AppRadii.lg,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('图片', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                        CupertinoButton(
                          padding: EdgeInsets.zero,
                          onPressed: _isSubmitting ? null : _pickImages,
                          child: const Text('添加图片', style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600)),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    if (_images.isEmpty)
                      Container(
                        height: 120,
                        alignment: Alignment.center,
                        decoration: BoxDecoration(color: AppColors.secondary, borderRadius: BorderRadius.circular(AppRadii.md)),
                        child: const Text('还没选择图片', style: TextStyle(color: AppColors.mutedForeground)),
                      )
                    else
                      Wrap(
                        spacing: 12,
                        runSpacing: 12,
                        children: [
                          for (var i = 0; i < _images.length; i++)
                            _ImagePreview(image: _images[i], onRemove: () => _removeImage(i)),
                        ],
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 32),
              AppPrimaryButton(
                onPressed: _isSubmitting ? null : _submit,
                child: Text(_isSubmitting ? '发布中...' : '立即发布'),
              ),
            ],
          ),
        ),
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
          child: Image.file(File(image.path), width: 90, height: 90, fit: BoxFit.cover),
        ),
        Positioned(
          right: -8,
          top: -8,
          child: CupertinoButton(
            padding: EdgeInsets.zero,
            onPressed: onRemove,
            child: const Icon(CupertinoIcons.xmark_circle_fill, color: AppColors.destructive, size: 24),
          ),
        ),
      ],
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  const _ErrorBanner({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(color: const Color(0xFFFFE5E5), borderRadius: BorderRadius.circular(AppRadii.md)),
      child: Text(message, style: const TextStyle(color: AppColors.destructive, fontWeight: FontWeight.w600)),
    );
  }
}
