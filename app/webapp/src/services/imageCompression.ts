/**
 * Client-side image compression — converts images to WebP and downscales
 * large images before upload.  GIFs are returned as-is to preserve animation.
 *
 * Uses the Canvas API (available in all modern browsers) so no extra
 * dependencies are needed.
 */

const DEFAULT_QUALITY = 0.75; // 0–1, maps to WebP quality
const AVATAR_QUALITY = 0.85;
const POST_MAX_WIDTH = 1920;
const POST_MAX_HEIGHT = 1920;
const AVATAR_MAX_WIDTH = 512;
const AVATAR_MAX_HEIGHT = 512;

const SKIP_EXTENSIONS = new Set(['.gif']);

export interface CompressOptions {
  /** WebP quality 0–1 (default 0.75) */
  quality?: number;
  /** Maximum width in px — larger images are downscaled proportionally */
  maxWidth?: number;
  /** Maximum height in px — larger images are downscaled proportionally */
  maxHeight?: number;
}

/**
 * Compress a single image File to WebP.
 *
 * Returns the original file if it's a GIF or if compression fails for any
 * reason (graceful fallback).
 */
export async function compressImage(
  file: File,
  options: CompressOptions = {},
): Promise<File> {
  const {
    quality = DEFAULT_QUALITY,
    maxWidth = POST_MAX_WIDTH,
    maxHeight = POST_MAX_HEIGHT,
  } = options;

  const ext = extension(file.name).toLowerCase();
  if (SKIP_EXTENSIONS.has(ext)) return file;

  try {
    const bitmap = await createImageBitmap(file);
    const { width, height } = calcDimensions(
      bitmap.width,
      bitmap.height,
      maxWidth,
      maxHeight,
    );

    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;

    const ctx = canvas.getContext('2d');
    if (!ctx) return file;

    // Use higher-quality image smoothing for downscaling
    ctx.imageSmoothingEnabled = true;
    ctx.imageSmoothingQuality = 'medium';
    ctx.drawImage(bitmap, 0, 0, width, height);
    bitmap.close(); // free GPU memory

    const blob = await new Promise<Blob>((resolve, reject) => {
      canvas.toBlob(
        (b) => (b ? resolve(b) : reject(new Error('toBlob returned null'))),
        'image/webp',
        quality,
      );
    });

    // Only use the compressed result if it actually shrank
    if (blob.size > 0 && blob.size < file.size) {
      const name = replaceExtension(file.name, '.webp');
      return new File([blob], name, { type: 'image/webp' });
    }

    // Compressed file is larger — keep original
    return file;
  } catch (e) {
    console.warn('[imageCompression] failed, using original:', e);
    return file;
  }
}

/**
 * Compress multiple images in parallel.
 */
export function compressImages(
  files: File[],
  options: CompressOptions = {},
): Promise<File[]> {
  return Promise.all(files.map((f) => compressImage(f, options)));
}

// ---- internal helpers ----

function extension(name: string): string {
  const i = name.lastIndexOf('.');
  return i === -1 ? '' : name.substring(i);
}

function replaceExtension(name: string, newExt: string): string {
  const i = name.lastIndexOf('.');
  return i === -1 ? name + newExt : name.substring(0, i) + newExt;
}

function calcDimensions(
  w: number,
  h: number,
  maxW: number,
  maxH: number,
): { width: number; height: number } {
  if (w <= maxW && h <= maxH) return { width: w, height: h };
  const ratio = Math.min(maxW / w, maxH / h);
  return {
    width: Math.round(w * ratio),
    height: Math.round(h * ratio),
  };
}
