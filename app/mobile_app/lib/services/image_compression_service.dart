import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_image_compress/flutter_image_compress.dart';

/// Service for compressing images before upload.
///
/// Converts non-GIF images to WebP format to reduce file size and speed up
/// uploads. GIF images are left untouched to preserve animation.
class ImageCompressionService {
  /// Default quality for post images (0-100).
  static const int postImageQuality = 75;

  /// Quality for avatar images — kept higher since avatars are small and
  /// need to look clean at small sizes.
  static const int avatarQuality = 85;

  /// Maximum resolution for post images. Images larger than this are
  /// downscaled while preserving aspect ratio.
  static const int postMaxWidth = 1920;
  static const int postMaxHeight = 1920;

  /// Maximum resolution for avatar images.
  static const int avatarMaxWidth = 512;
  static const int avatarMaxHeight = 512;

  /// Images smaller than this are uploaded as-is to avoid spending CPU time
  /// on compression that rarely improves the upload enough to justify it.
  static const int minCompressBytes = 1024 * 1024;

  /// File extensions that should NOT be converted to WebP.
  /// GIF preserves animation, WebP is already an optimized format —
  /// re-compressing it wastes CPU and can even increase file size.
  static const _skipExtensions = {'.gif', '.webp'};

  /// Compress a single [image] to WebP.
  ///
  /// Returns a [CompressedImage] with the path to the compressed file.
  /// GIF images are returned as-is (no compression).
  ///
  /// [quality] is 0–100, where higher = better quality / larger file.
  /// Defaults to [postImageQuality].
  static Future<CompressedImage> compress(
    XFile image, {
    int quality = postImageQuality,
    int maxWidth = postMaxWidth,
    int maxHeight = postMaxHeight,
  }) async {
    final ext = _extension(image.path).toLowerCase();

    // GIFs are skipped to preserve animation
    if (_skipExtensions.contains(ext)) {
      return CompressedImage(path: image.path, isCompressed: false);
    }

    try {
      final originalSize = await File(image.path).length();
      if (originalSize < minCompressBytes) {
        debugPrint(
          '[ImageCompression] ${image.name}: smaller than 1MB, using original',
        );
        return CompressedImage(path: image.path, isCompressed: false);
      }

      final targetPath = _targetPath(image.path);
      final compressSw = Stopwatch()..start();
      final compressed = await FlutterImageCompress.compressAndGetFile(
        image.path,
        targetPath,
        format: CompressFormat.webp,
        quality: quality,
        minWidth: maxWidth,
        minHeight: maxHeight,
      );

      if (compressed != null && await File(compressed.path).exists()) {
        final compressedFile = File(compressed.path);
        final compressedSize = await compressedFile.length();
        if (compressedSize > 0 && compressedSize < originalSize) {
          debugPrint(
            '[ImageCompression] ${image.name}: $originalSize -> $compressedSize bytes'
            ' (${compressSw.elapsedMilliseconds}ms)',
          );
          return CompressedImage(path: compressed.path, isCompressed: true);
        }

        await compressedFile.delete();
        debugPrint(
          '[ImageCompression] ${image.name}: compressed file was not smaller, using original',
        );
      }
    } catch (e) {
      debugPrint('[ImageCompression] failed: $e — falling back to original');
    }

    // Fallback to original on failure
    return CompressedImage(path: image.path, isCompressed: false);
  }

  /// Compress multiple images in parallel.
  static Future<List<CompressedImage>> compressAll(
    List<XFile> images, {
    int quality = postImageQuality,
    int maxWidth = postMaxWidth,
    int maxHeight = postMaxHeight,
  }) async {
    final sw = Stopwatch()..start();
    final results = await Future.wait(
      images.map(
        (img) => compress(
          img,
          quality: quality,
          maxWidth: maxWidth,
          maxHeight: maxHeight,
        ),
      ),
    );
    final skipped = results.where((r) => !r.isCompressed).length;
    final compressed = results.length - skipped;
    debugPrint(
      '[ImageCompression] compressAll ${results.length} images: '
      '$compressed compressed, $skipped skipped, ${sw.elapsedMilliseconds}ms',
    );
    return results;
  }

  /// Delete temporary compressed files. Call after upload succeeds/fails to
  /// avoid leaking disk space.
  static Future<void> cleanup(Iterable<CompressedImage> images) async {
    for (final img in images) {
      if (img.isCompressed) {
        try {
          final f = File(img.path);
          if (await f.exists()) await f.delete();
        } catch (_) {
          // best-effort cleanup
        }
      }
    }
  }

  /// Convert a [CompressedImage] back to an [XFile] for use with
  /// [FileService] upload methods.
  static XFile toXFile(CompressedImage image) => XFile(image.path);

  // ---- internal helpers ----

  static String _extension(String path) {
    final lastDot = path.lastIndexOf('.');
    if (lastDot == -1) return '';
    return path.substring(lastDot);
  }

  static String _targetPath(String originalPath) {
    final stamp = DateTime.now().microsecondsSinceEpoch;
    // Keep a unique-enough filename to avoid collisions when compressing
    // multiple images in parallel.
    final hash = originalPath.hashCode.abs().toRadixString(36).padLeft(4, '0');
    return '${Directory.systemTemp.path}/img_${stamp}_$hash.webp';
  }
}

/// Result of image compression.
class CompressedImage {
  const CompressedImage({required this.path, required this.isCompressed});

  /// Path to the (possibly compressed) image file.
  final String path;

  /// Whether the image was actually compressed (vs. skipped or fallback).
  final bool isCompressed;
}
