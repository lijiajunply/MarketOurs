import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { type RootState } from '../../stores';
import { postService } from '../../services/postService';
import { fileService } from '../../services/fileService';
import { compressImages } from '../../services/imageCompression';
import { extractUserMessage } from '../../services/errorCodes';
import { ImagePlus, X, Loader2, Send } from 'lucide-react';
import { DTO_LIMITS, requiredMax } from '../../lib/dtoValidation';
import type { PostTagDto } from '../../types';
import { PostTagBadge } from '../../components/post/PostTagBadge';

export default function CreatePostPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { user, isAuthenticated } = useSelector((state: RootState) => state.auth);
  
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [tagId, setTagId] = useState('');
  const [tags, setTags] = useState<PostTagDto[]>([]);
  const [images, setImages] = useState<File[]>([]);
  const [imagePreviews, setImagePreviews] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  // If not authenticated, redirect to login
  React.useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
    }
  }, [isAuthenticated, navigate]);

  React.useEffect(() => {
    const loadTags = async () => {
      try {
        const response = await postService.getPostTags();
        setTags(response.data ?? []);
      } catch (err) {
        console.error('Failed to load post tags', err);
      }
    };

    void loadTags();
  }, []);

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const newFiles = Array.from(e.target.files);
      setImages((prev) => [...prev, ...newFiles]);
      
      const newPreviews = newFiles.map((file) => URL.createObjectURL(file));
      setImagePreviews((prev) => [...prev, ...newPreviews]);
    }
  };

  const removeImage = (index: number) => {
    setImages((prev) => prev.filter((_, i) => i !== index));
    URL.revokeObjectURL(imagePreviews[index]);
    setImagePreviews((prev) => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const titleError = requiredMax(
      title,
      DTO_LIMITS.postTitleMax,
      t('post.error_empty'),
      `标题长度不能超过 ${DTO_LIMITS.postTitleMax} 位`,
    );
    const contentError = requiredMax(
      content,
      DTO_LIMITS.postContentMax,
      t('post.error_empty'),
      `内容长度不能超过 ${DTO_LIMITS.postContentMax} 位`,
    );
    if (titleError || contentError) {
      setError(titleError || contentError);
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      // 1. Request an upload key to bind images to the post
      let uploadKey: string | undefined;
      if (images.length > 0) {
        const keyResponse = await fileService.getUploadKey();
        uploadKey = keyResponse.data?.key;
      }

      // 2. Compress images to WebP, then upload with the key
      const compressed = await compressImages(images, {
        quality: 0.75,
        maxWidth: 1920,
        maxHeight: 1920,
      });
      const uploadedImageUrls = compressed.length > 0
        ? (await fileService.uploadStream(compressed, uploadKey, setUploadProgress)).data ?? []
        : [];

      // 3. Create post with image URLs and upload key
      await postService.createPost({
        title,
        content,
        images: uploadedImageUrls,
        userId: user?.id || '',
        uploadKey,
        tagId: tagId || null,
      });

      navigate('/');
    } catch (err: unknown) {
      setError(extractUserMessage(err, t('post.error_create_failed')));
    } finally {
      setIsSubmitting(false);
      setUploadProgress(null);
    }
  };

  return (
    <div className="container max-w-2xl mx-auto px-4 py-8">
      <div className="glass rounded-3xl p-6 md:p-8 space-y-6">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">{t('post.create_title')}</h1>
          <p className="text-muted-foreground">{t('post.create_desc')}</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="space-y-2">
            <label htmlFor="title" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
              {t('post.title_label')}
            </label>
            <input
              id="title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder={t('post.title_placeholder')}
              maxLength={DTO_LIMITS.postTitleMax}
              className="flex h-12 w-full rounded-2xl border border-input bg-background/50 px-4 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/20 focus-visible:border-primary transition-all disabled:cursor-not-allowed disabled:opacity-50"
              required
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="content" className="text-sm font-medium leading-none">
              {t('post.content_label')}
            </label>
            <textarea
              id="content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder={t('post.content_placeholder')}
              maxLength={DTO_LIMITS.postContentMax}
              className="flex min-h-[200px] w-full rounded-2xl border border-input bg-background/50 px-4 py-3 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/20 focus-visible:border-primary transition-all disabled:cursor-not-allowed disabled:opacity-50 resize-none"
              required
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="tag" className="text-sm font-medium leading-none">
              {t('post.tag_label')}
            </label>
            <select
              id="tag"
              value={tagId}
              onChange={(e) => setTagId(e.target.value)}
              className="flex h-12 w-full rounded-2xl border border-input bg-background/50 px-4 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/20 focus-visible:border-primary transition-all"
            >
              <option value="">{t('post.tag_none')}</option>
              {tags.map((tag) => (
                <option key={tag.id} value={tag.id}>
                  {tag.name}
                </option>
              ))}
            </select>
            {tagId ? (
              <PostTagBadge tag={tags.find((tag) => tag.id === tagId)} fallback={t('post.tag_label')} />
            ) : null}
          </div>

          <div className="space-y-3">
            <label className="text-sm font-medium leading-none">{t('post.add_images')}</label>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
              {imagePreviews.map((preview, index) => (
                <div key={index} className="relative aspect-square rounded-2xl overflow-hidden border border-border group">
                  <img src={preview} alt="" className="w-full h-full object-cover" />
                  <button
                    type="button"
                    onClick={() => removeImage(index)}
                    className="absolute top-2 right-2 p-1.5 rounded-full bg-background/80 text-foreground opacity-0 group-hover:opacity-100 transition-opacity hover:bg-destructive hover:text-destructive-foreground"
                  >
                    <X size={14} />
                  </button>
                </div>
              ))}
              <label className="flex flex-col items-center justify-center aspect-square rounded-2xl border-2 border-dashed border-muted-foreground/25 hover:border-primary/50 hover:bg-primary/5 transition-all cursor-pointer">
                <ImagePlus size={24} className="text-muted-foreground mb-2" />
                <span className="text-xs text-muted-foreground">{t('post.upload_image')}</span>
                <input
                  type="file"
                  accept="image/*"
                  multiple
                  onChange={handleImageChange}
                  className="hidden"
                />
              </label>
            </div>
          </div>

          {uploadProgress !== null && (
            <div className="space-y-2 animate-in fade-in slide-in-from-top-2 duration-200">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">{t('post.uploading')}</span>
                <span className="font-medium tabular-nums">{Math.round(uploadProgress * 100)}%</span>
              </div>
              <div className="w-full h-2 rounded-full bg-secondary overflow-hidden">
                <div
                  className="h-full rounded-full bg-primary transition-all duration-300 ease-out"
                  style={{ width: `${uploadProgress * 100}%` }}
                />
              </div>
            </div>
          )}

          {error && (
            <div className="p-4 rounded-2xl bg-destructive/10 text-destructive text-sm font-medium animate-in fade-in slide-in-from-top-2 duration-200">
              {error}
            </div>
          )}

          <div className="flex gap-4 pt-4">
            <button
              type="button"
              onClick={() => navigate(-1)}
              className="flex-1 h-12 rounded-2xl border border-input bg-background hover:bg-muted font-medium transition-colors"
              disabled={isSubmitting}
            >
              {t('post.cancel')}
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex-[2] h-12 rounded-2xl bg-primary text-primary-foreground font-bold shadow-lg shadow-primary/20 hover:opacity-90 transition-all disabled:opacity-50 flex items-center justify-center gap-2"
            >
              {isSubmitting ? (
                <>
                  <Loader2 size={20} className="animate-spin" />
                  <span>{t('post.submitting')}</span>
                </>
              ) : (
                <>
                  <Send size={18} />
                  <span>{t('post.submit')}</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
