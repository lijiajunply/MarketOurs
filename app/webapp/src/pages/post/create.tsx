import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { type RootState } from '../../stores';
import { postService } from '../../services/postService';
import { fileService } from '../../services/fileService';
import { ImagePlus, X, Loader2, Send } from 'lucide-react';

export default function CreatePostPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { user, isAuthenticated } = useSelector((state: RootState) => state.auth);
  
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [images, setImages] = useState<File[]>([]);
  const [imagePreviews, setImagePreviews] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // If not authenticated, redirect to login
  React.useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
    }
  }, [isAuthenticated, navigate]);

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
    if (!title.trim() || !content.trim()) {
      setError(t('post.error_empty'));
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      // 1. Upload images first
      const uploadedImageUrls: string[] = [];
      for (const image of images) {
        const response = await fileService.uploadImage(image);
        if (response.data) {
          uploadedImageUrls.push(response.data);
        }
      }

      // 2. Create post with image URLs
      await postService.createPost({
        title,
        content,
        images: uploadedImageUrls,
        userId: user?.id || '',
      });

      navigate('/');
    } catch (err: any) {
      setError(err.response?.data?.message || t('post.error_create_failed'));
    } finally {
      setIsSubmitting(false);
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
              className="flex min-h-[200px] w-full rounded-2xl border border-input bg-background/50 px-4 py-3 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/20 focus-visible:border-primary transition-all disabled:cursor-not-allowed disabled:opacity-50 resize-none"
              required
            />
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
