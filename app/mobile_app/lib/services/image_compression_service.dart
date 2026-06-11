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

  /// File extensions that should NOT be converted to WebP.
  static const _skipExtensions = {'.gif'};

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
      final compressed = await FlutterImageCompress.compressAndGetFile(
        image.path,
        _targetPath(image.path),
        format: CompressFormat.webp,
        quality: quality,
        minWidth: maxWidth,
        minHeight: maxHeight,
      );

      if (compressed != null && await File(compressed.path).exists()) {
        return CompressedImage(path: compressed.path, isCompressed: true);
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
    final results = await Future.wait(
      images.map((img) => compress(img, quality: quality, maxWidth: maxWidth, maxHeight: maxHeight)),
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
    final hash = originalPath.hashCode.toRadixString(36).substring(0, 4);
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
