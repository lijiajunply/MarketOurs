import 'dart:io';

import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
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
    if (picked.isEmpty) {
      return;
    }

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

    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      final uploadedImages = _images.isEmpty
          ? <String>[]
          : (await _fileService.uploadImages(_images)).data ?? <String>[];

      final response = await ref
          .read(postServiceProvider)
          .createPost(
            PostCreateDto(
              title: _titleController.text.trim(),
              content: _contentController.text.trim(),
              images: uploadedImages.isEmpty ? null : uploadedImages,
              userId: user.id,
            ),
          );

      final post = response.data;
      if (post == null) {
        throw Exception(response.message ?? '帖子创建失败');
      }

      if (!mounted) {
        return;
      }

      await AppFeedback.showMessage(context, message: '帖子已发布');
      context.go(buildPostDetailLocation(post.id));
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _errorMessage = error.toString().replaceFirst('Exception: ', '');
      });
      await AppFeedback.showMessage(
        context,
        message: _errorMessage ?? '帖子创建失败',
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: '发布帖子',
      trailing: CupertinoButton(
        padding: EdgeInsets.zero,
        minimumSize: Size.zero,
        onPressed: _isSubmitting ? null : _submit,
        child: Text(_isSubmitting ? '发布中' : '发布'),
      ),
      child: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.only(bottom: 24),
          children: [
            if (_errorMessage != null) ...[
              _ErrorBanner(message: _errorMessage!),
              const SizedBox(height: 16),
            ],
            AppSectionCard(
              child: Column(
                children: [
                  AppTextField(
                    controller: _titleController,
                    placeholder: '给帖子起个标题',
                    validator: (value) {
                      if (value == null || value.trim().isEmpty) {
                        return '请输入标题';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _contentController,
                    placeholder: '分享点什么吧',
                    maxLines: 8,
                    validator: (value) {
                      if (value == null || value.trim().isEmpty) {
                        return '请输入内容';
                      }
                      return null;
                    },
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            AppSectionCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        '图片',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      AppSecondaryButton(
                        onPressed: _isSubmitting ? null : _pickImages,
                        child: const Text('选择图片'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  if (_images.isEmpty)
                    Container(
                      height: 120,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: const Color(0xFFF7F8FA),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: const Text(
                        '暂未选择图片',
                        style: TextStyle(color: CupertinoColors.systemGrey),
                      ),
                    )
                  else
                    Wrap(
                      spacing: 12,
                      runSpacing: 12,
                      children: [
                        for (var index = 0; index < _images.length; index++)
                          _ImagePreview(
                            image: _images[index],
                            onRemove: () => _removeImage(index),
                          ),
                      ],
                    ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            AppPrimaryButton(
              onPressed: _isSubmitting ? null : _submit,
              child: Text(_isSubmitting ? '发布中...' : '立即发布'),
            ),
          ],
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
          borderRadius: BorderRadius.circular(16),
          child: Image.file(
            File(image.path),
            width: 104,
            height: 104,
            fit: BoxFit.cover,
            errorBuilder: (context, error, stackTrace) => Container(
              width: 104,
              height: 104,
              color: const Color(0xFFF7F8FA),
              alignment: Alignment.center,
              child: const Icon(Icons.image_outlined),
            ),
          ),
        ),
        Positioned(
          right: -6,
          top: -6,
          child: IconButton(
            onPressed: onRemove,
            icon: const Icon(Icons.cancel_rounded),
            color: Colors.black87,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints.tightFor(width: 28, height: 28),
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
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF1F1),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Text(message, style: const TextStyle(color: Color(0xFFB42318))),
    );
  }
}
