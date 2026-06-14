import { Link, useParams, useNavigate } from "react-router"
import { Heart, Share2, ArrowLeft, MoreHorizontal, Send, Loader2, ChevronLeft, ChevronRight, X, ImagePlus } from "lucide-react"
import { useState, useEffect, useRef } from "react"
import { postService } from "../../services/postService"
import { commentService } from "../../services/commentService"
import { fileService } from "../../services/fileService"
import { compressImages } from "../../services/imageCompression"
import type { PostDto, CommentDto } from "../../types"
import { useSelector } from "react-redux"
import type { RootState } from "../../stores"
import { useTranslation } from "react-i18next"
import { extractUserMessage } from "../../services/errorCodes"
import type { i18n, TFunction } from "i18next"
import { formatDistanceToNow } from "date-fns"
import { zhCN, enUS } from "date-fns/locale"
import { cn } from "../../lib/utils"
import { sharePost } from "../../lib/postShare"
import { DTO_LIMITS, requiredMax } from "../../lib/dtoValidation"
import { PostTagBadge } from "../../components/post/PostTagBadge"

const MAX_COMMENT_IMAGES = 3;

const commentLengthError = `评论内容长度不能超过 ${DTO_LIMITS.commentContentMax} 位`;

const formatDate = (dateString: string, i18nInstance: i18n, updatedAtString?: string, t?: TFunction) => {
  try {
    const date = new Date(dateString);
    const display = formatDistanceToNow(date, { 
      addSuffix: true, 
      locale: i18nInstance.language === 'zh' ? zhCN : enUS 
    });
    
    if (updatedAtString && t) {
      const updatedDate = new Date(updatedAtString);
      // If updated more than 5 seconds after creation
      if (updatedDate.getTime() - date.getTime() > 5000) {
        return `${display} (${t('post.edited')})`;
      }
    }
    
    return display;
  } catch {
    return dateString;
  }
}

// 一条被展平的回复：comment 是回复本身，replyTo 是它直接回复的那条评论；
// 当 replyTo 为 null 时表示它是直接回复顶层评论(不显示 @)，否则显示 @对方。
type FlatReply = { comment: CommentDto; replyTo: CommentDto | null };

// 将一条顶层评论下的所有后代回复展平成单层列表(只保留两级:顶层评论 + 其下所有回复)。
// 直接回复顶层评论的 replyTo 记为 null;回复某条回复的则记录被回复者,用于渲染 @对方。
// 列表按创建时间从早到晚排序,读起来像一段对话。
function flattenReplies(root: CommentDto): FlatReply[] {
  const out: FlatReply[] = [];
  const walk = (nodes: CommentDto[] | undefined, parent: CommentDto, parentIsRoot: boolean) => {
    if (!nodes) return;
    for (const child of nodes) {
      out.push({ comment: child, replyTo: parentIsRoot ? null : parent });
      walk(child.repliedComments, child, false);
    }
  };
  walk(root.repliedComments, root, true);
  out.sort(
    (a, b) => new Date(a.comment.createdAt).getTime() - new Date(b.comment.createdAt).getTime()
  );
  return out;
}

async function uploadCommentImageFiles(
  files: File[],
  onProgress?: (fraction: number) => void,
): Promise<string[]> {
  if (files.length === 0) return [];

  const keyResponse = await fileService.getUploadKey();
  const uploadKey = keyResponse.data?.key;
  const compressed = await compressImages(files, {
    quality: 0.75,
    maxWidth: 1920,
    maxHeight: 1920,
  });
  return (await fileService.uploadStream(compressed, uploadKey, onProgress)).data ?? [];
}

function CommentImageGrid({ images, imageLabel }: { images: string[]; imageLabel: string }) {
  const [viewerIndex, setViewerIndex] = useState<number | null>(null);
  if (images.length === 0) return null;

  return (
    <>
      <div className={cn("mt-3 grid gap-2", images.length === 1 ? "max-w-[220px] grid-cols-1" : "grid-cols-3 max-w-[300px]")}>
        {images.map((image, index) => (
          <button
            key={`${image}-${index}`}
            type="button"
            onClick={() => setViewerIndex(index)}
            className="aspect-square overflow-hidden rounded-xl border border-border/50 bg-muted"
          >
            <img src={image} alt={`${imageLabel} ${index + 1}`} className="h-full w-full object-cover" loading="lazy" />
          </button>
        ))}
      </div>

      {viewerIndex !== null && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-background/95 p-4 backdrop-blur-sm animate-in fade-in duration-200"
          role="dialog"
          aria-modal="true"
          onClick={() => setViewerIndex(null)}
        >
          <button
            type="button"
            onClick={() => setViewerIndex(null)}
            className="absolute right-4 top-4 grid size-11 place-items-center rounded-full bg-muted text-foreground transition-colors hover:bg-border"
            aria-label="Close image viewer"
          >
            <X size={22} />
          </button>
          <img
            src={images[viewerIndex]}
            className="max-h-[88vh] max-w-[92vw] rounded-2xl object-contain shadow-2xl"
            alt={`${imageLabel} ${viewerIndex + 1}`}
            onClick={(event) => event.stopPropagation()}
          />
        </div>
      )}
    </>
  );
}

function ImagePreviewStrip({
  previews,
  onRemove,
}: {
  previews: string[];
  onRemove: (index: number) => void;
}) {
  if (previews.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {previews.map((preview, index) => (
        <div key={`${preview}-${index}`} className="relative size-20 overflow-hidden rounded-xl border border-border bg-muted">
          <img src={preview} alt="" className="h-full w-full object-cover" />
          <button
            type="button"
            onClick={() => onRemove(index)}
            className="absolute right-1 top-1 grid size-6 place-items-center rounded-full bg-background/85 text-foreground shadow-sm transition-colors hover:bg-destructive hover:text-destructive-foreground"
            aria-label="Remove image"
          >
            <X size={12} />
          </button>
        </div>
      ))}
    </div>
  );
}

function ExistingImageEditor({
  images,
  onRemove,
}: {
  images: string[];
  onRemove: (index: number) => void;
}) {
  if (images.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {images.map((image, index) => (
        <div key={`${image}-${index}`} className="relative size-20 overflow-hidden rounded-xl border border-border bg-muted">
          <img src={image} alt="" className="h-full w-full object-cover" />
          <button
            type="button"
            onClick={() => onRemove(index)}
            className="absolute right-1 top-1 grid size-6 place-items-center rounded-full bg-background/85 text-foreground shadow-sm transition-colors hover:bg-destructive hover:text-destructive-foreground"
            aria-label="Remove image"
          >
            <X size={12} />
          </button>
        </div>
      ))}
    </div>
  );
}

function PostImageCarousel({ images, imageLabel }: { images: string[]; imageLabel: string }) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [viewerIndex, setViewerIndex] = useState<number | null>(null);
  const hasMultipleImages = images.length > 1;
  const safeCurrentIndex = Math.min(currentIndex, images.length - 1);

  useEffect(() => {
    if (viewerIndex === null) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setViewerIndex(null);
      } else if (event.key === "ArrowLeft") {
        setViewerIndex((index) => (index === null ? index : Math.max(0, index - 1)));
      } else if (event.key === "ArrowRight") {
        setViewerIndex((index) => (index === null ? index : Math.min(images.length - 1, index + 1)));
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [images.length, viewerIndex]);

  const goToPrevious = () => {
    setCurrentIndex((index) => Math.max(0, index - 1));
  };

  const goToNext = () => {
    setCurrentIndex((index) => Math.min(images.length - 1, index + 1));
  };

  const goToViewerPrevious = () => {
    setViewerIndex((index) => (index === null ? index : Math.max(0, index - 1)));
  };

  const goToViewerNext = () => {
    setViewerIndex((index) => (index === null ? index : Math.min(images.length - 1, index + 1)));
  };

  return (
    <>
      <div className="my-8 space-y-4">
        <div className="relative overflow-hidden rounded-[2rem] border border-border/50 bg-muted">
          <div
            className="flex transition-transform duration-500 ease-out"
            style={{ transform: `translateX(-${safeCurrentIndex * 100}%)` }}
          >
            {images.map((img, idx) => (
              <button
                key={`${img}-${idx}`}
                type="button"
                onClick={() => setViewerIndex(idx)}
                className="group relative min-w-full aspect-video overflow-hidden bg-muted text-left"
              >
                <img
                  src={img}
                  className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-[1.02]"
                  alt={`${imageLabel} ${idx + 1}`}
                  loading={idx === 0 ? "eager" : "lazy"}
                />
              </button>
            ))}
          </div>

          {hasMultipleImages && (
            <>
              <button
                type="button"
                onClick={goToPrevious}
                disabled={safeCurrentIndex === 0}
                className="absolute left-3 top-1/2 grid size-10 -translate-y-1/2 place-items-center rounded-full bg-background/85 text-foreground shadow-lg backdrop-blur transition-all hover:bg-background disabled:pointer-events-none disabled:opacity-35"
                aria-label="Previous image"
              >
                <ChevronLeft size={20} />
              </button>
              <button
                type="button"
                onClick={goToNext}
                disabled={safeCurrentIndex === images.length - 1}
                className="absolute right-3 top-1/2 grid size-10 -translate-y-1/2 place-items-center rounded-full bg-background/85 text-foreground shadow-lg backdrop-blur transition-all hover:bg-background disabled:pointer-events-none disabled:opacity-35"
                aria-label="Next image"
              >
                <ChevronRight size={20} />
              </button>
              <div className="absolute bottom-3 left-1/2 flex -translate-x-1/2 items-center gap-2 rounded-full bg-background/85 px-3 py-2 shadow-lg backdrop-blur">
                {images.map((_, idx) => (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => setCurrentIndex(idx)}
                    className={cn(
                      "size-2 rounded-full transition-all",
                      idx === safeCurrentIndex ? "w-5 bg-primary" : "bg-muted-foreground/40 hover:bg-muted-foreground/70"
                    )}
                    aria-label={`Go to image ${idx + 1}`}
                  />
                ))}
              </div>
              <div className="absolute right-3 top-3 rounded-full bg-background/85 px-3 py-1.5 text-xs font-bold text-foreground shadow-lg backdrop-blur">
                {safeCurrentIndex + 1} / {images.length}
              </div>
            </>
          )}
        </div>
      </div>

      {viewerIndex !== null && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-background/95 p-4 backdrop-blur-sm animate-in fade-in duration-200"
          role="dialog"
          aria-modal="true"
          onClick={() => setViewerIndex(null)}
        >
          <button
            type="button"
            onClick={() => setViewerIndex(null)}
            className="absolute right-4 top-4 grid size-11 place-items-center rounded-full bg-muted text-foreground transition-colors hover:bg-border"
            aria-label="Close image viewer"
          >
            <X size={22} />
          </button>
          {hasMultipleImages && (
            <button
              type="button"
              onClick={(event) => {
                event.stopPropagation();
                goToViewerPrevious();
              }}
              disabled={viewerIndex === 0}
              className="absolute left-4 top-1/2 grid size-11 -translate-y-1/2 place-items-center rounded-full bg-muted text-foreground transition-colors hover:bg-border disabled:opacity-35"
              aria-label="Previous image"
            >
              <ChevronLeft size={24} />
            </button>
          )}
          <img
            src={images[viewerIndex]}
            className="max-h-[88vh] max-w-[92vw] rounded-2xl object-contain shadow-2xl"
            alt={`${imageLabel} ${viewerIndex + 1}`}
            onClick={(event) => event.stopPropagation()}
          />
          {hasMultipleImages && (
            <>
              <button
                type="button"
                onClick={(event) => {
                  event.stopPropagation();
                  goToViewerNext();
                }}
                disabled={viewerIndex === images.length - 1}
                className="absolute right-4 top-1/2 grid size-11 -translate-y-1/2 place-items-center rounded-full bg-muted text-foreground transition-colors hover:bg-border disabled:opacity-35"
                aria-label="Next image"
              >
                <ChevronRight size={24} />
              </button>
              <div className="absolute bottom-4 left-1/2 -translate-x-1/2 rounded-full bg-muted px-4 py-2 text-sm font-bold text-foreground">
                {viewerIndex + 1} / {images.length}
              </div>
            </>
          )}
        </div>
      )}
    </>
  );
}

function CommentItem({
  comment,
  replyTo,
  replies,
  user,
  i18n,
  t,
  onUpdate,
  onReply,
  onDelete,
  onLike,
  likedComments,
}: {
  comment: CommentDto;
  // 该评论直接回复的对象;有值时在内容前显示 @对方(仅楼中楼的回复-回复场景)
  replyTo?: CommentDto | null;
  // 仅顶层评论传入:其下被展平的所有回复
  replies?: FlatReply[];
  user: RootState["auth"]["user"];
  i18n: i18n;
  t: TFunction;
  onUpdate: (id: string, content: string, images: string[]) => Promise<void>;
  onReply: (parentId: string, content: string, images: string[]) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  onLike: (id: string) => Promise<void>;
  likedComments: Set<string>;
}) {
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(comment.content);
  const [editExistingImages, setEditExistingImages] = useState<string[]>(comment.images || []);
  const [editImageFiles, setEditImageFiles] = useState<File[]>([]);
  const [editImagePreviews, setEditImagePreviews] = useState<string[]>([]);
  const [editUploadProgress, setEditUploadProgress] = useState<number | null>(null);
  const [isReplying, setIsReplying] = useState(false);
  const [replyContent, setReplyContent] = useState("");
  const [replyImageFiles, setReplyImageFiles] = useState<File[]>([]);
  const [replyImagePreviews, setReplyImagePreviews] = useState<string[]>([]);
  const [replyUploadProgress, setReplyUploadProgress] = useState<number | null>(null);
  const [replySubmitting, setReplySubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const editPreviewRef = useRef<string[]>([]);
  const replyPreviewRef = useRef<string[]>([]);

  useEffect(() => {
    editPreviewRef.current = editImagePreviews;
  }, [editImagePreviews]);

  useEffect(() => {
    replyPreviewRef.current = replyImagePreviews;
  }, [replyImagePreviews]);

  useEffect(() => {
    return () => {
      editPreviewRef.current.forEach((preview) => URL.revokeObjectURL(preview));
      replyPreviewRef.current.forEach((preview) => URL.revokeObjectURL(preview));
    };
  }, []);

  const isMe = user && comment.userId.toLowerCase() === user.id.toLowerCase();
  const isAdmin = user && user.role === 'Admin';
  
  const authorName = comment.author?.name || `${t("common.user")} ${comment.userId.slice(0, 4)}`;
  const displayName = isMe ? `${authorName} (${t("common.me")})` : authorName;
  const authorAvatar = comment.author?.avatar || `https://api.dicebear.com/7.x/avataaars/svg?seed=${comment.userId}`;

  const handleSave = async () => {
    if (!editContent.trim() && editExistingImages.length === 0 && editImageFiles.length === 0) return;
    if (editContent.trim().length > DTO_LIMITS.commentContentMax) return;
    try {
      setEditUploadProgress(editImageFiles.length > 0 ? 0 : null);
      const uploadedImages = await uploadCommentImageFiles(editImageFiles, setEditUploadProgress);
      await onUpdate(comment.id, editContent, [...editExistingImages, ...uploadedImages]);
      editImagePreviews.forEach((preview) => URL.revokeObjectURL(preview));
      setEditImageFiles([]);
      setEditImagePreviews([]);
      setIsEditing(false);
    } finally {
      setEditUploadProgress(null);
    }
  };

  const handleDelete = async () => {
    if (window.confirm(t("post.confirm_delete"))) {
      setIsDeleting(true);
      try {
        await onDelete(comment.id);
      } finally {
        setIsDeleting(false);
      }
    }
  };

  const handleSubmitReply = async () => {
    if (!replyContent.trim() && replyImageFiles.length === 0) return;
    if (replyContent.trim().length > DTO_LIMITS.commentContentMax) return;
    setReplySubmitting(true);
    try {
      setReplyUploadProgress(replyImageFiles.length > 0 ? 0 : null);
      const uploadedImages = await uploadCommentImageFiles(replyImageFiles, setReplyUploadProgress);
      await onReply(comment.id, replyContent, uploadedImages);
      setReplyContent("");
      replyImagePreviews.forEach((preview) => URL.revokeObjectURL(preview));
      setReplyImageFiles([]);
      setReplyImagePreviews([]);
      setIsReplying(false);
    } finally {
      setReplySubmitting(false);
      setReplyUploadProgress(null);
    }
  };

  const addEditImages = (files: FileList | null) => {
    if (!files) return;
    const remaining = MAX_COMMENT_IMAGES - editExistingImages.length - editImageFiles.length;
    const nextFiles = Array.from(files).slice(0, Math.max(0, remaining));
    if (nextFiles.length === 0) return;
    setEditImageFiles((prev) => [...prev, ...nextFiles]);
    setEditImagePreviews((prev) => [...prev, ...nextFiles.map((file) => URL.createObjectURL(file))]);
  };

  const addReplyImages = (files: FileList | null) => {
    if (!files) return;
    const remaining = MAX_COMMENT_IMAGES - replyImageFiles.length;
    const nextFiles = Array.from(files).slice(0, Math.max(0, remaining));
    if (nextFiles.length === 0) return;
    setReplyImageFiles((prev) => [...prev, ...nextFiles]);
    setReplyImagePreviews((prev) => [...prev, ...nextFiles.map((file) => URL.createObjectURL(file))]);
  };

  const removeEditFile = (index: number) => {
    URL.revokeObjectURL(editImagePreviews[index]);
    setEditImageFiles((prev) => prev.filter((_, i) => i !== index));
    setEditImagePreviews((prev) => prev.filter((_, i) => i !== index));
  };

  const removeReplyFile = (index: number) => {
    URL.revokeObjectURL(replyImagePreviews[index]);
    setReplyImageFiles((prev) => prev.filter((_, i) => i !== index));
    setReplyImagePreviews((prev) => prev.filter((_, i) => i !== index));
  };

  return (
    <div className={cn("flex gap-4 group transition-opacity", isDeleting && "opacity-50 pointer-events-none")}>
      <Link to={`/user/${comment.userId}`} className="flex-shrink-0">
        <img src={authorAvatar} alt={authorName} className="w-10 h-10 rounded-full bg-muted shadow-sm" />
      </Link>
      <div className="flex-1 space-y-2">
        <div className="p-5 rounded-[1.5rem] bg-card border border-border/40 shadow-sm group-hover:border-primary/20 transition-colors">
          <div className="flex items-center justify-between mb-1">
            <Link to={`/user/${comment.userId}`} className="font-bold text-sm transition-colors hover:text-primary">
              {displayName}
            </Link>
            <p className="text-xs text-muted-foreground">{formatDate(comment.createdAt, i18n, comment.updatedAt, t)}</p>
          </div>
          
          {isEditing ? (
            <div className="space-y-4 mt-2">
              <textarea
                value={editContent}
                onChange={(e) => setEditContent(e.target.value)}
                maxLength={DTO_LIMITS.commentContentMax}
                className="w-full min-h-[100px] bg-transparent border border-border/50 rounded-xl p-3 outline-none focus:border-primary transition-colors resize-none text-sm"
              />
              <div className="space-y-3">
                <ExistingImageEditor
                  images={editExistingImages}
                  onRemove={(index) => setEditExistingImages((prev) => prev.filter((_, i) => i !== index))}
                />
                <ImagePreviewStrip previews={editImagePreviews} onRemove={removeEditFile} />
                <div className="flex items-center gap-3">
                  <label className={cn(
                    "grid size-9 place-items-center rounded-xl border border-border bg-muted transition-colors",
                    editExistingImages.length + editImageFiles.length < MAX_COMMENT_IMAGES ? "cursor-pointer hover:border-primary hover:text-primary" : "opacity-40"
                  )}>
                    <ImagePlus size={16} />
                    <input
                      type="file"
                      accept="image/*"
                      multiple
                      disabled={editExistingImages.length + editImageFiles.length >= MAX_COMMENT_IMAGES}
                      onChange={(event) => {
                        addEditImages(event.target.files);
                        event.target.value = "";
                      }}
                      className="hidden"
                    />
                  </label>
                  <span className="text-xs text-muted-foreground">{editExistingImages.length + editImageFiles.length} / {MAX_COMMENT_IMAGES}</span>
                </div>
                {editUploadProgress !== null && (
                  <div className="h-1.5 overflow-hidden rounded-full bg-secondary">
                    <div className="h-full bg-primary transition-all" style={{ width: `${editUploadProgress * 100}%` }} />
                  </div>
                )}
              </div>
              <div className="flex gap-2">
                <button
                  onClick={handleSave}
                  disabled={!editContent.trim() && editExistingImages.length === 0 && editImageFiles.length === 0}
                  className="text-xs font-bold px-3 py-1.5 rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
                >
                  {t("post.save")}
                </button>
                <button
                  onClick={() => {
                    setIsEditing(false);
                    setEditContent(comment.content);
                    setEditExistingImages(comment.images || []);
                    editImagePreviews.forEach((preview) => URL.revokeObjectURL(preview));
                    setEditImageFiles([]);
                    setEditImagePreviews([]);
                  }}
                  className="text-xs font-bold px-3 py-1.5 rounded-lg bg-muted hover:bg-border transition-colors"
                >
                  {t("post.cancel")}
                </button>
              </div>
            </div>
          ) : (
            <p className="text-muted-foreground leading-relaxed text-sm whitespace-pre-wrap">
              {replyTo && (
                <Link
                  to={`/user/${replyTo.userId}`}
                  className="font-bold text-primary hover:underline mr-1"
                >
                  @{replyTo.author?.name || `${t("common.user")} ${replyTo.userId.slice(0, 4)}`}
                </Link>
              )}
              {comment.content}
            </p>
          )}
          {!isEditing && <CommentImageGrid images={comment.images || []} imageLabel="Comment image" />}
        </div>
        
        <div className="flex items-center gap-4 ml-2">
          <button 
            onClick={() => onLike(comment.id)}
            disabled={!user}
            className={cn(
              "text-xs font-bold transition-colors flex items-center gap-1.5 px-2 py-1 rounded-md",
              user ? "hover:bg-primary/10 hover:text-primary text-muted-foreground" : "text-muted-foreground/50 cursor-not-allowed"
            )}
          >
            <Heart size={14} className={cn(likedComments.has(comment.id) && "fill-primary text-primary")} />
            {comment.likes}
          </button>
          
          {user && (
            <button 
              onClick={() => setIsReplying(!isReplying)}
              className={cn("text-xs font-bold transition-colors", isReplying ? "text-primary" : "text-muted-foreground hover:text-primary")}
            >
              {t("post.reply")}
            </button>
          )}
          
          {(isMe || isAdmin) && !isEditing && (
            <div className="flex gap-4">
              {isMe && (
                <button 
                  onClick={() => setIsEditing(true)}
                  className="text-xs font-bold text-primary/70 hover:text-primary transition-colors"
                >
                  {t("post.edit")}
                </button>
              )}
              <button 
                onClick={handleDelete}
                className="text-xs font-bold text-destructive/70 hover:text-destructive transition-colors"
              >
                {t("post.delete")}
              </button>
            </div>
          )}
        </div>

        {isReplying && (
          <div className="mt-4 space-y-3 animate-in slide-in-from-top-2 duration-300">
            <textarea
              placeholder={`${t("post.reply")} @${authorName}...`}
              value={replyContent}
              onChange={(e) => setReplyContent(e.target.value)}
              maxLength={DTO_LIMITS.commentContentMax}
              className="w-full min-h-[80px] bg-muted/30 border border-border/50 rounded-2xl p-3 outline-none focus:border-primary transition-all text-sm resize-none"
              autoFocus
            />
            <ImagePreviewStrip previews={replyImagePreviews} onRemove={removeReplyFile} />
            <div className="flex items-center gap-3">
              <label className={cn(
                "grid size-9 place-items-center rounded-xl border border-border bg-muted transition-colors",
                replyImageFiles.length < MAX_COMMENT_IMAGES ? "cursor-pointer hover:border-primary hover:text-primary" : "opacity-40"
              )}>
                <ImagePlus size={16} />
                <input
                  type="file"
                  accept="image/*"
                  multiple
                  disabled={replyImageFiles.length >= MAX_COMMENT_IMAGES}
                  onChange={(event) => {
                    addReplyImages(event.target.files);
                    event.target.value = "";
                  }}
                  className="hidden"
                />
              </label>
              <span className="text-xs text-muted-foreground">{replyImageFiles.length} / {MAX_COMMENT_IMAGES}</span>
            </div>
            {replyUploadProgress !== null && (
              <div className="h-1.5 overflow-hidden rounded-full bg-secondary">
                <div className="h-full bg-primary transition-all" style={{ width: `${replyUploadProgress * 100}%` }} />
              </div>
            )}
            <div className="flex gap-2">
              <button
                onClick={handleSubmitReply}
                disabled={(!replyContent.trim() && replyImageFiles.length === 0) || replySubmitting}
                className="text-xs font-bold px-4 py-2 rounded-xl bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50 transition-all flex items-center gap-2"
              >
                {replySubmitting ? <Loader2 size={14} className="animate-spin" /> : <Send size={14} />}
                {t("post.submit")}
              </button>
              <button
                onClick={() => {
                  setIsReplying(false);
                  replyImagePreviews.forEach((preview) => URL.revokeObjectURL(preview));
                  setReplyImageFiles([]);
                  setReplyImagePreviews([]);
                }}
                className="text-xs font-bold px-4 py-2 rounded-xl bg-muted hover:bg-border transition-all"
              >
                {t("post.cancel")}
              </button>
            </div>
          </div>
        )}

        {/* 渲染回复:只展开到两级,更深的回复被展平到这里并用 @对方 标注 */}
        {replies && replies.length > 0 && (
          <div className="mt-4 space-y-6 pl-4 border-l-2 border-border/20">
            {replies.map(({ comment: reply, replyTo: target }) => (
              <CommentItem
                key={reply.id}
                comment={reply}
                replyTo={target}
                user={user}
                i18n={i18n}
                t={t}
                onUpdate={onUpdate}
                onReply={onReply}
                onDelete={onDelete}
                onLike={onLike}
                likedComments={likedComments}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default function PostDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { t, i18n } = useTranslation()
  const { user } = useSelector((state: RootState) => state.auth)

  const [post, setPost] = useState<PostDto | null>(null)
  const [comments, setComments] = useState<CommentDto[]>([])
  const [commentContent, setCommentContent] = useState("")
  const [commentImageFiles, setCommentImageFiles] = useState<File[]>([])
  const [commentImagePreviews, setCommentImagePreviews] = useState<string[]>([])
  const [commentUploadProgress, setCommentUploadProgress] = useState<number | null>(null)
  const commentPreviewRef = useRef<string[]>([])
  const [loading, setLoading] = useState(true)
  const [commentsLoading, setCommentsLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [commentSort, setCommentSort] = useState<"recent" | "hot">("recent")
  const [postLiked, setPostLiked] = useState(false)
  const [likedComments, setLikedComments] = useState<Set<string>>(new Set())
  const [shareFeedback, setShareFeedback] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  // Editing state for post
  const [isEditingPost, setIsEditingPost] = useState(false)
  const [editTitle, setEditTitle] = useState("")
  const [editContent, setEditContent] = useState("")

  useEffect(() => {
    if (!id) return;
    const controller = new AbortController();
    const fetchPostData = async () => {
      setLoading(true)
      try {
        const postRes = await postService.getPost(id, { signal: controller.signal })
        setPost(postRes.data)
        setPostLiked(postRes.data.isLiked ?? false)
        setEditTitle(postRes.data.title)
        setEditContent(postRes.data.content)
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      console.error(err)
      setActionError(extractUserMessage(err, t("common.error")));
    } finally {
      if (!controller.signal.aborted) setLoading(false)
      }
    }
    fetchPostData()
    return () => controller.abort()
  }, [id, t])

  useEffect(() => {
    if (!id) return;
    const controller = new AbortController();
    const fetchComments = async () => {
      setCommentsLoading(true)
      try {
        const commentsRes = await postService.getPostComments(id, commentSort, { signal: controller.signal })
        const nextComments = Array.isArray(commentsRes.data) ? commentsRes.data : [];
        setComments(nextComments)
        const nextLikedComments = new Set<string>();
        const collectLiked = (items: CommentDto[]) => {
          items.forEach((comment) => {
            if (comment.isLiked) nextLikedComments.add(comment.id);
            if (comment.repliedComments?.length) collectLiked(comment.repliedComments);
          });
        };
        collectLiked(nextComments);
        setLikedComments(nextLikedComments);
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') return;
        console.error("Failed to fetch comments", err)
        setActionError(extractUserMessage(err, t("common.error")));
      } finally {
        if (!controller.signal.aborted) setCommentsLoading(false)
      }
    }
    fetchComments()
    return () => controller.abort()
  }, [id, commentSort, t])

  useEffect(() => {
    commentPreviewRef.current = commentImagePreviews;
  }, [commentImagePreviews]);

  useEffect(() => {
    return () => {
      commentPreviewRef.current.forEach((preview) => URL.revokeObjectURL(preview));
    };
  }, []);

  const addCommentImages = (files: FileList | null) => {
    if (!files) return;
    const remaining = MAX_COMMENT_IMAGES - commentImageFiles.length;
    const nextFiles = Array.from(files).slice(0, Math.max(0, remaining));
    if (nextFiles.length === 0) return;
    setCommentImageFiles((prev) => [...prev, ...nextFiles]);
    setCommentImagePreviews((prev) => [...prev, ...nextFiles.map((file) => URL.createObjectURL(file))]);
  };

  const removeCommentImage = (index: number) => {
    URL.revokeObjectURL(commentImagePreviews[index]);
    setCommentImageFiles((prev) => prev.filter((_, i) => i !== index));
    setCommentImagePreviews((prev) => prev.filter((_, i) => i !== index));
  };

  const handlePostUpdate = async () => {
    if (!id) return;
    const titleError = requiredMax(
      editTitle,
      DTO_LIMITS.postTitleMax,
      "标题不能为空",
      `标题长度不能超过 ${DTO_LIMITS.postTitleMax} 位`,
    );
    const contentError = requiredMax(
      editContent,
      DTO_LIMITS.postContentMax,
      "内容不能为空",
      `内容长度不能超过 ${DTO_LIMITS.postContentMax} 位`,
    );
    if (titleError || contentError) {
      setActionError(titleError || contentError);
      return;
    }
    setSubmitting(true)
    try {
      const res = await postService.updatePost(id, {
        title: editTitle,
        content: editContent
      });
      if (res.data) {
        setPost(res.data);
        setIsEditingPost(false);
      }
    } catch (err) {
      console.error(err);
      setActionError(extractUserMessage(err, t("common.error")));
    } finally {
      setSubmitting(false);
    }
  }

  const handlePostDelete = async () => {
    if (!id || !window.confirm(t("post.confirm_delete"))) return;
    setSubmitting(true);
    try {
      await postService.deletePost(id);
      navigate("/");
    } catch (err) {
      console.error(err);
      setActionError(extractUserMessage(err, t("common.error")));
    } finally {
      setSubmitting(false);
    }
  }

  const handleShare = async () => {
    if (!post) return;

    try {
      const outcome = await sharePost(post);
      if (outcome === "shared") {
        setShareFeedback("已打开分享面板");
      } else if (outcome === "copied") {
        setShareFeedback("链接已复制");
      }
    } catch (error) {
      console.error(error);
      setShareFeedback(extractUserMessage(error, "分享失败，请稍后重试"));
    } finally {
      window.setTimeout(() => setShareFeedback(null), 2500);
    }
  }

  const handlePostLike = async () => {
    if (!id || !user || !post) return;
    try {
      const res = await postService.likePost(id);
      const { likeCount, dislikeCount, isLiked } = res.data;
      setPost({ ...post, likes: likeCount, dislikes: dislikeCount });
      setPostLiked(isLiked);
    } catch (err) {
      console.error("Failed to like post", err);
      setActionError(extractUserMessage(err, t("common.error")));
    }
  }

  const handleCommentLike = async (commentId: string) => {
    if (!user) return;
    try {
      const res = await commentService.likeComment(commentId);
      const { likeCount, dislikeCount, isLiked } = res.data;
      const updateInTree = (list: CommentDto[]): CommentDto[] => {
        return list.map(c => {
          if (c.id === commentId) {
            return { ...c, likes: likeCount, dislikes: dislikeCount };
          }
          if (c.repliedComments && c.repliedComments.length > 0) {
            return { ...c, repliedComments: updateInTree(c.repliedComments) };
          }
          return c;
        });
      };
      setComments(updateInTree(comments));
      setLikedComments(prev => {
        const next = new Set(prev);
        if (isLiked) next.add(commentId);
        else next.delete(commentId);
        return next;
      });
    } catch (err) {
      console.error("Failed to like comment", err);
      setActionError(extractUserMessage(err, t("common.error")));
    }
  }

  const handleCommentDelete = async (commentId: string) => {
    try {
      await commentService.deleteComment(commentId);
      // Remove the comment from the local state tree
      const removeFromTree = (list: CommentDto[]): CommentDto[] => {
        return list.filter(c => c.id !== commentId).map(c => {
          if (c.repliedComments && c.repliedComments.length > 0) {
            return { ...c, repliedComments: removeFromTree(c.repliedComments) };
          }
          return c;
        });
      };
      setComments(removeFromTree(comments));
    } catch (err) {
      console.error("Failed to delete comment", err);
      setActionError(extractUserMessage(err, t("common.error")));
    }
  }

  const handleCommentUpdate = async (commentId: string, content: string, images: string[]) => {
    if (content.trim().length > DTO_LIMITS.commentContentMax) {
      setActionError(commentLengthError);
      return;
    }
    try {
      const res = await commentService.updateComment(commentId, { content, images });
      if (res.data) {
        // Update the comment in the local state tree
        const updateInTree = (list: CommentDto[]): CommentDto[] => {
          return list.map(c => {
            if (c.id === commentId) {
              return { ...res.data, author: c.author, repliedComments: c.repliedComments };
            }
            if (c.repliedComments && c.repliedComments.length > 0) {
              return { ...c, repliedComments: updateInTree(c.repliedComments) };
            }
            return c;
          });
        };
        setComments(updateInTree(comments));
      }
    } catch (err) {
      console.error("Failed to update comment", err);
      setActionError(extractUserMessage(err, t("common.error")));
    }
  }

  const handleCommentReply = async (parentId: string, content: string, images: string[]) => {
    if (!id || !user) return;
    if (content.trim().length > DTO_LIMITS.commentContentMax) {
      setActionError(commentLengthError);
      return;
    }
    try {
      const res = await commentService.createComment({
        content,
        images,
        userId: user.id,
        postId: id,
        parentCommentId: parentId
      });
      if (res.data) {
        const newReply = { 
          ...res.data, 
          author: { id: user.id, name: user.name, avatar: user.avatar },
          repliedComments: [] 
        };
        
        const insertInTree = (list: CommentDto[]): CommentDto[] => {
          return list.map(c => {
            if (c.id === parentId) {
              return { 
                ...c, 
                repliedComments: [newReply, ...(c.repliedComments || [])] 
              };
            }
            if (c.repliedComments && c.repliedComments.length > 0) {
              return { ...c, repliedComments: insertInTree(c.repliedComments) };
            }
            return c;
          });
        };
        setComments(insertInTree(comments));
      }
    } catch (err) {
      console.error("Failed to reply to comment", err);
      setActionError(extractUserMessage(err, t("common.error")));
    }
  }

  const handleCommentSubmit = async () => {
    if ((!commentContent.trim() && commentImageFiles.length === 0) || !user || !id) return;
    if (commentContent.trim().length > DTO_LIMITS.commentContentMax) {
      setActionError(commentLengthError);
      return;
    }
    setSubmitting(true)
    try {
      setCommentUploadProgress(commentImageFiles.length > 0 ? 0 : null);
      const uploadedImages = await uploadCommentImageFiles(commentImageFiles, setCommentUploadProgress);
      const res = await commentService.createComment({
        content: commentContent,
        images: uploadedImages,
        userId: user.id,
        postId: id
      });
      if (res.data) {
        setComments([{ ...res.data, author: { id: user.id, name: user.name, avatar: user.avatar } }, ...comments]);
        setCommentContent("");
        commentImagePreviews.forEach((preview) => URL.revokeObjectURL(preview));
        setCommentImageFiles([]);
        setCommentImagePreviews([]);
      }
    } catch (err) {
      console.error(err)
      setActionError(extractUserMessage(err, t("common.error")));
    } finally {
      setSubmitting(false)
      setCommentUploadProgress(null)
    }
  }

  if (loading) {
    return <div className="flex justify-center items-center h-64"><Loader2 className="animate-spin text-primary" size={48} /></div>
  }

  if (!post) {
    return <div className="text-center py-20 text-muted-foreground">{t("post.not_found")}</div>
  }

  const isMe = user && post.userId.toLowerCase() === user.id.toLowerCase();
  const isAdmin = user && user.role === 'Admin';
  const authorName = post.author?.name || `${t("common.user")} ${post.userId.slice(0, 4)}`;
  const displayName = isMe ? `${authorName} (${t("common.me")})` : authorName;
  const authorAvatar = post.author?.avatar || `https://api.dicebear.com/7.x/avataaars/svg?seed=${post.userId}`;

  return (
    <div className="max-w-3xl mx-auto space-y-8 pb-20">
      <button
        onClick={() => navigate(-1)}
        className="flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors group"
      >
        <ArrowLeft size={18} className="group-hover:-translate-x-1 transition-transform" />
        {t("post.back_to_feed")}
      </button>

      <article className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
        <header className="space-y-6">
          {isEditingPost ? (
            <input
              type="text"
              value={editTitle}
              onChange={(e) => setEditTitle(e.target.value)}
              maxLength={DTO_LIMITS.postTitleMax}
              className="w-full text-4xl sm:text-5xl font-black tracking-tight leading-[1.1] bg-transparent border-b border-primary/30 outline-none focus:border-primary transition-colors"
            />
          ) : (
            <h1 className="text-4xl sm:text-5xl font-black tracking-tight leading-[1.1]">
              {post.title}
            </h1>
          )}
          
          <div className="flex items-center gap-4 py-6 border-y border-border/30">
            <Link to={`/user/${post.userId}`} className="shrink-0">
              <img src={authorAvatar} alt={authorName} className="w-12 h-12 rounded-full bg-muted shadow-inner" />
            </Link>
            <div className="flex-1">
              <Link to={`/user/${post.userId}`} className="font-bold text-lg transition-colors hover:text-primary">
                {displayName}
              </Link>
              <p className="text-sm text-muted-foreground">{formatDate(post.createdAt, i18n, post.updatedAt, t)}</p>
            </div>
            {(isMe || isAdmin) && !isEditingPost && (
              <div className="flex gap-2">
                {isMe && (
                  <button 
                    onClick={() => setIsEditingPost(true)}
                    className="px-4 py-1.5 rounded-full bg-muted hover:bg-primary/10 hover:text-primary transition-all text-sm font-bold"
                  >
                    {t("post.edit")}
                  </button>
                )}
                <button 
                  onClick={handlePostDelete}
                  className="px-4 py-1.5 rounded-full bg-muted hover:bg-destructive/10 hover:text-destructive transition-all text-sm font-bold"
                >
                  {t("post.delete")}
                </button>
              </div>
            )}
            <button className="p-2 rounded-full hover:bg-muted transition-colors">
              <MoreHorizontal size={20} />
            </button>
          </div>
        </header>

        {post.images && post.images.length > 0 && (
          <PostImageCarousel key={post.id} images={post.images} imageLabel={`${t("nav.post")} image`} />
        )}

        <PostTagBadge tag={post.tag} />

        <div className="prose prose-lg dark:prose-invert max-w-none">
          {isEditingPost ? (
            <textarea
              value={editContent}
              onChange={(e) => setEditContent(e.target.value)}
              maxLength={DTO_LIMITS.postContentMax}
              className="w-full min-h-[300px] text-lg leading-relaxed bg-transparent border border-border/50 rounded-2xl p-4 outline-none focus:border-primary transition-colors resize-none"
            />
          ) : (
            <div className="space-y-6 text-lg leading-relaxed text-foreground whitespace-pre-wrap">
              {post.content}
            </div>
          )}
        </div>

        {isEditingPost && (
          <div className="flex gap-4">
            <button
              onClick={handlePostUpdate}
              disabled={submitting}
              className="flex-1 py-3 rounded-2xl bg-primary text-primary-foreground font-bold hover:opacity-90 transition-all shadow-lg shadow-primary/20 flex items-center justify-center gap-2"
            >
              {submitting ? <Loader2 className="animate-spin" size={20} /> : t("post.save")}
            </button>
            <button
              onClick={() => {
                setIsEditingPost(false);
                setEditTitle(post.title);
                setEditContent(post.content);
              }}
              className="flex-1 py-3 rounded-2xl bg-muted font-bold hover:bg-border transition-all"
            >
              {t("post.cancel")}
            </button>
          </div>
        )}

        <footer className="flex items-center gap-6 py-8 border-t border-border/30">
          <button 
            onClick={handlePostLike}
            disabled={!user}
            className={cn(
              "flex items-center gap-2 px-6 py-2.5 rounded-full transition-all font-bold group",
              user ? "bg-primary/10 text-primary hover:bg-primary/20" : "bg-muted text-muted-foreground/50 cursor-not-allowed"
            )}
          >
            <Heart size={20} className={cn("group-hover:scale-110 transition-transform", postLiked && "fill-primary")} />
            <span>{post.likes} {t("post.likes")}</span>
          </button>
          <button
            onClick={handleShare}
            className="flex items-center gap-2 px-6 py-2.5 rounded-full hover:bg-muted transition-all font-bold text-muted-foreground group"
          >
            <Share2 size={20} className="group-hover:scale-110 transition-transform" />
            <span>{t("post.share")}</span>
          </button>
        </footer>
        {shareFeedback ? <p className="text-sm font-medium text-primary">{shareFeedback}</p> : null}
      </article>

      {actionError && (
        <div className="p-4 rounded-2xl bg-destructive/10 text-destructive text-sm font-medium text-center animate-in fade-in duration-300">
          {actionError}
        </div>
      )}

      <section className="space-y-8 animate-in fade-in slide-in-from-bottom-6 duration-700 delay-200">
        <div className="flex items-center justify-between">
          <h3 className="flex items-center gap-3 text-2xl font-bold tracking-tight">
            {t("post.comments_count", { count: comments.length })}
            {commentsLoading ? <Loader2 className="animate-spin text-primary" size={18} /> : null}
          </h3>
          <div className="flex items-center gap-2 p-1 rounded-xl bg-muted/50 border border-border/50">
            <button
              onClick={() => setCommentSort("recent")}
              disabled={commentsLoading && commentSort === "recent"}
              className={cn(
                "px-3 py-1.5 rounded-lg text-xs font-bold transition-all disabled:opacity-70",
                commentSort === "recent" ? "bg-background shadow-sm text-foreground" : "text-muted-foreground hover:text-foreground"
              )}
            >
              {t("post.sort_recent")}
            </button>
            <button
              onClick={() => setCommentSort("hot")}
              disabled={commentsLoading && commentSort === "hot"}
              className={cn(
                "px-3 py-1.5 rounded-lg text-xs font-bold transition-all disabled:opacity-70",
                commentSort === "hot" ? "bg-background shadow-sm text-foreground" : "text-muted-foreground hover:text-foreground"
              )}
            >
              {t("post.sort_hot")}
            </button>
          </div>
        </div>

        {user ? (
          <div className="space-y-3 rounded-3xl bg-muted/50 border border-border/50 p-3 focus-within:border-primary/30 focus-within:ring-4 focus-within:ring-primary/5 transition-all">
            <textarea
              placeholder={t("post.comment_placeholder")}
              value={commentContent}
              onChange={(e) => setCommentContent(e.target.value)}
              maxLength={DTO_LIMITS.commentContentMax}
              className="min-h-[76px] w-full resize-none bg-transparent border-none outline-none px-2 py-2 text-sm"
            />
            <ImagePreviewStrip previews={commentImagePreviews} onRemove={removeCommentImage} />
            {commentUploadProgress !== null && (
              <div className="h-1.5 overflow-hidden rounded-full bg-secondary">
                <div className="h-full bg-primary transition-all" style={{ width: `${commentUploadProgress * 100}%` }} />
              </div>
            )}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <label className={cn(
                  "grid size-10 place-items-center rounded-2xl border border-border bg-background/70 transition-colors",
                  commentImageFiles.length < MAX_COMMENT_IMAGES ? "cursor-pointer hover:border-primary hover:text-primary" : "opacity-40"
                )}>
                  <ImagePlus size={18} />
                  <input
                    type="file"
                    accept="image/*"
                    multiple
                    disabled={commentImageFiles.length >= MAX_COMMENT_IMAGES}
                    onChange={(event) => {
                      addCommentImages(event.target.files);
                      event.target.value = "";
                    }}
                    className="hidden"
                  />
                </label>
                <span className="text-xs text-muted-foreground">{commentImageFiles.length} / {MAX_COMMENT_IMAGES}</span>
              </div>
              <button 
                onClick={handleCommentSubmit}
                disabled={(!commentContent.trim() && commentImageFiles.length === 0) || submitting}
                className="p-3 rounded-2xl bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50 transition-all shadow-lg shadow-primary/20"
              >
                {submitting ? <Loader2 size={18} className="animate-spin" /> : <Send size={18} />}
              </button>
            </div>
          </div>
        ) : (
          <div className="p-4 rounded-[2rem] bg-muted/50 border border-border/50 text-center">
            <p className="text-muted-foreground text-sm">{t("post.login_to_comment")}</p>
          </div>
        )}

        <div className="space-y-10">
          {comments.map((c) => (
            <CommentItem
              key={c.id}
              comment={c}
              replies={flattenReplies(c)}
              user={user}
              i18n={i18n}
              t={t}
              onUpdate={handleCommentUpdate}
              onReply={handleCommentReply}
              onDelete={handleCommentDelete}
              onLike={handleCommentLike}
              likedComments={likedComments}
            />
          ))}
          {comments.length === 0 && !commentsLoading && (
            <p className="text-center text-muted-foreground py-8">{t("post.no_comments")}</p>
          )}
        </div>
      </section>
    </div>
  )
}
