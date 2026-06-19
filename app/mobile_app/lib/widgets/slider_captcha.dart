import 'dart:convert';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:dio/dio.dart';
import 'package:flutter/cupertino.dart';

import '../services/auth_service.dart';
import '../ui/app_theme.dart';

class SliderCaptcha extends StatefulWidget {
  const SliderCaptcha({
    super.key,
    required this.onVerify,
    required this.onCancel,
  });

  final void Function(String captchaToken) onVerify;
  final VoidCallback onCancel;

  @override
  State<SliderCaptcha> createState() => _SliderCaptchaState();
}

class _SliderCaptchaState extends State<SliderCaptcha> {
  final _authService = AuthService();
  CaptchaChallenge? _challenge;
  bool _loading = true;
  bool _verifying = false;
  bool _success = false;
  String? _error;
  double _sliderValue = 0;
  final double _trackWidth = 280;

  @override
  void initState() {
    super.initState();
    _fetchChallenge();
  }

  Future<void> _fetchChallenge() async {
    setState(() {
      _loading = true;
      _error = null;
      _sliderValue = 0;
      _success = false;
    });
    try {
      final challenge = await _authService.getCaptchaChallenge();
      if (mounted) {
        setState(() {
          _challenge = challenge;
          _loading = false;
        });
      }
    } on DioException catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = e.response?.data?['message'] ?? '获取验证失败，请重试';
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = '获取验证失败，请重试';
        });
      }
    }
  }

  Future<void> _verify() async {
    if (_challenge == null || _verifying || _success) return;
    if (_sliderValue < 5) {
      setState(() => _sliderValue = 0);
      return;
    }

    setState(() => _verifying = true);
    try {
      final token = await _authService.verifyCaptcha(
        token: _challenge!.token,
        x: _sliderValue.round(),
      );
      if (mounted) {
        setState(() => _success = true);
        Future.delayed(const Duration(milliseconds: 500), () {
          if (mounted) widget.onVerify(token);
        });
      }
    } on DioException catch (e) {
      if (mounted) {
        setState(() {
          _verifying = false;
          _sliderValue = 0;
          _error = e.response?.data?['message'] ?? '验证失败，请重试';
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _verifying = false;
          _sliderValue = 0;
          _error = '验证失败，请重试';
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: widget.onCancel,
      child: Container(
        color: CupertinoColors.black.withValues(alpha: 0.4),
        child: Center(
          child: GestureDetector(
            onTap: () {},
            child: Container(
              width: 340,
              margin: const EdgeInsets.symmetric(horizontal: 16),
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: CupertinoDynamicColor.resolve(AppColors.card, context),
                borderRadius: BorderRadius.circular(24),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text(
                    '请完成验证',
                    style: TextStyle(
                      fontSize: 17,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '拖动滑块使拼图对齐',
                    style: TextStyle(
                      fontSize: 12,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 8,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.destructive.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        _error!,
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w500,
                          color: AppColors.destructive,
                        ),
                      ),
                    ),
                  ],
                  const SizedBox(height: 16),
                  if (_loading)
                    const SizedBox(
                      height: 80,
                      child: Center(
                        child: CupertinoActivityIndicator(),
                      ),
                    )
                  else if (_challenge != null) ...[
                    _CaptchaCanvas(
                      challenge: _challenge!,
                      offset: _sliderValue,
                    ),
                    const SizedBox(height: 16),
                    _buildSlider(),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: CupertinoButton(
                            padding: EdgeInsets.zero,
                            onPressed:
                                _verifying ? null : _fetchChallenge,
                            child: Text(
                              '刷新',
                              style: TextStyle(
                                fontSize: 13,
                                color: _verifying
                                    ? AppColors.mutedForeground
                                        .withValues(alpha: 0.4)
                                    : AppColors.mutedForeground,
                              ),
                            ),
                          ),
                        ),
                        Expanded(
                          child: CupertinoButton(
                            padding: EdgeInsets.zero,
                            onPressed: widget.onCancel,
                            child: Text(
                              '取消',
                              style: TextStyle(
                                fontSize: 13,
                                color: AppColors.mutedForeground,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSlider() {
    final progress = _sliderValue / _trackWidth;
    final knobLeft = (_sliderValue).clamp(0.0, _trackWidth - 44);

    return SizedBox(
      width: _trackWidth,
      height: 48,
      child: Stack(
        children: [
          Container(
            width: _trackWidth,
            height: 48,
            decoration: BoxDecoration(
              color: AppColors.muted.withValues(alpha: 0.6),
              borderRadius: BorderRadius.circular(16),
            ),
            clipBehavior: Clip.hardEdge,
            child: Stack(
              children: [
                Positioned.fill(
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: FractionallySizedBox(
                      widthFactor: progress,
                      child: Container(
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.2),
                          borderRadius: BorderRadius.circular(16),
                        ),
                      ),
                    ),
                  ),
                ),
                if (!_success && !_verifying)
                  const Center(
                    child: Text(
                      '拖动滑块完成拼图',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                        color: Color(0xFF999999),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          Positioned(
            left: knobLeft,
            top: 0,
            child: GestureDetector(
              onHorizontalDragUpdate: _verifying || _success
                  ? null
                  : (details) {
                      setState(() {
                        _sliderValue = (details.localPosition.dx)
                            .clamp(0.0, _trackWidth);
                      });
                    },
              onHorizontalDragEnd: _verifying || _success
                  ? null
                  : (details) => _verify(),
              child: Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(16),
                  boxShadow: [
                    BoxShadow(
                      color: AppColors.primary.withValues(alpha: 0.3),
                      blurRadius: 8,
                      offset: const Offset(0, 2),
                    ),
                  ],
                ),
                child: Center(
                  child: _verifying
                      ? const CupertinoActivityIndicator(
                          color: CupertinoColors.white,
                        )
                      : _success
                          ? const Text(
                              '✓',
                              style: TextStyle(
                                color: CupertinoColors.white,
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                              ),
                            )
                          : const Icon(
                              CupertinoIcons.chevron_right,
                              color: CupertinoColors.white,
                              size: 22,
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

class _CaptchaCanvas extends StatelessWidget {
  const _CaptchaCanvas({
    required this.challenge,
    required this.offset,
  });

  final CaptchaChallenge challenge;
  final double offset;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: challenge.puzzleWidth * 2,
      height: challenge.puzzleHeight + 4 > 60
          ? challenge.puzzleHeight + 4
          : 60,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: CustomPaint(
          painter: _CaptchaPainter(
            bgBytes: base64Decode(challenge.backgroundImage),
            puzzleBytes: base64Decode(challenge.puzzleImage),
            puzzleWidth: challenge.puzzleWidth,
            puzzleHeight: challenge.puzzleHeight,
            offset: offset,
          ),
        ),
      ),
    );
  }
}

class _CaptchaPainter extends CustomPainter {
  final Uint8List bgBytes;
  final Uint8List puzzleBytes;
  final int puzzleWidth;
  final int puzzleHeight;
  final double offset;

  _CaptchaPainter({
    required this.bgBytes,
    required this.puzzleBytes,
    required this.puzzleWidth,
    required this.puzzleHeight,
    required this.offset,
  });

  @override
  void paint(Canvas canvas, Size size) {
    _drawImage(canvas, bgBytes, const Rect.fromLTWH(0, 0, 300, 160),
        Rect.fromLTWH(0, 0, size.width, size.height));

    final scaleX = size.width / 300;
    final puzzleScaledWidth = puzzleWidth * scaleX;
    final puzzleScaledHeight = puzzleHeight * scaleX;
    final puzzleX = offset * scaleX;

    _drawImage(
      canvas,
      puzzleBytes,
      Rect.fromLTWH(0, 0, puzzleWidth.toDouble(), puzzleHeight.toDouble()),
      Rect.fromLTWH(puzzleX, 0, puzzleScaledWidth, puzzleScaledHeight),
    );
  }

  void _drawImage(
    Canvas canvas,
    Uint8List bytes,
    Rect src,
    Rect dst,
  ) {
    final codec = ui.instantiateImageCodec(bytes);
    codec.then((imageCodec) {
      imageCodec.getNextFrame().then((frameInfo) {
        canvas.drawImageRect(frameInfo.image, src, dst, Paint());
        frameInfo.image.dispose();
      });
    });
  }

  @override
  bool shouldRepaint(_CaptchaPainter oldDelegate) {
    return offset != oldDelegate.offset ||
        bgBytes != oldDelegate.bgBytes ||
        puzzleBytes != oldDelegate.puzzleBytes;
  }
}
