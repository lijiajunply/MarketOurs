import { Link, useParams, useNavigate } from "react-router"
import { Heart, Share2, ArrowLeft, MoreHorizontal, Send, Loader2 } from "lucide-react"
import { useState, useEffect } from "react"
import { postService } from "../../services/postService"
import { commentService } from "../../services/commentService"
import type { PostDto, CommentDto } from "../../types"
import { useSelector } from "react-redux"
import type { RootState } from "../../stores"
import { useTranslation } from "react-i18next"
import type { i18n } from "i18next"
import { formatDistanceToNow } from "date-fns"
import { zhCN, enUS } from "date-fns/locale"
import { cn } from "../../lib/utils"

const formatDate = (dateString: string, i18nInstance: i18n, updatedAtString?: string, t?: any) => {
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

function CommentItem({ 
  comment, 
  user, 
  i18n, 
  t, 
  onUpdate,
  onReply,
  onDelete,
  onLike
}: { 
  comment: CommentDto; 
  user: any; 
  i18n: any; 
  t: any;
  onUpdate: (id: string, content: string) => Promise<void>;
  onReply: (parentId: string, content: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  onLike: (id: string) => Promise<void>;
}) {
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(comment.content);
  const [isReplying, setIsReplying] = useState(false);
  const [replyContent, setReplyContent] = useState("");
  const [replySubmitting, setReplySubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const isMe = user && comment.userId.toLowerCase() === user.id.toLowerCase();
  const isAdmin = user && user.role === 'Admin';
  
  const authorName = comment.author?.name || `${t("common.user")} ${comment.userId.slice(0, 4)}`;
  const displayName = isMe ? `${authorName} (${t("common.me")})` : authorName;
  const authorAvatar = comment.author?.avatar || `https://api.dicebear.com/7.x/avataaars/svg?seed=${comment.userId}`;

  const handleSave = async () => {
    if (!editContent.trim()) return;
    await onUpdate(comment.id, editContent);
    setIsEditing(false);
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
    if (!replyContent.trim()) return;
    setReplySubmitting(true);
    try {
      await onReply(comment.id, replyContent);
      setReplyContent("");
      setIsReplying(false);
    } finally {
      setReplySubmitting(false);
    }
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
                className="w-full min-h-[100px] bg-transparent border border-border/50 rounded-xl p-3 outline-none focus:border-primary transition-colors resize-none text-sm"
              />
              <div className="flex gap-2">
                <button
                  onClick={handleSave}
                  className="text-xs font-bold px-3 py-1.5 rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
                >
                  {t("post.save")}
                </button>
                <button
                  onClick={() => {
                    setIsEditing(false);
                    setEditContent(comment.content);
                  }}
                  className="text-xs font-bold px-3 py-1.5 rounded-lg bg-muted hover:bg-border transition-colors"
                >
                  {t("post.cancel")}
                </button>
              </div>
            </div>
          ) : (
            <p className="text-muted-foreground leading-relaxed text-sm whitespace-pre-wrap">{comment.content}</p>
          )}
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
            <Heart size={14} className={cn(comment.likes > 0 && "fill-primary text-primary")} /> 
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
              className="w-full min-h-[80px] bg-muted/30 border border-border/50 rounded-2xl p-3 outline-none focus:border-primary transition-all text-sm resize-none"
              autoFocus
            />
            <div className="flex gap-2">
              <button
                onClick={handleSubmitReply}
                disabled={!replyContent.trim() || replySubmitting}
                className="text-xs font-bold px-4 py-2 rounded-xl bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50 transition-all flex items-center gap-2"
              >
                {replySubmitting ? <Loader2 size={14} className="animate-spin" /> : <Send size={14} />}
                {t("post.submit")}
              </button>
              <button
                onClick={() => setIsReplying(false)}
                className="text-xs font-bold px-4 py-2 rounded-xl bg-muted hover:bg-border transition-all"
              >
                {t("post.cancel")}
              </button>
            </div>
          </div>
        )}

        {/* Render Replies */}
        {comment.repliedComments && comment.repliedComments.length > 0 && (
          <div className="mt-4 space-y-6 pl-4 border-l-2 border-border/20">
            {comment.repliedComments.map((reply) => (
              <CommentItem 
                key={reply.id} 
                comment={reply} 
                user={user} 
                i18n={i18n} 
                t={t} 
                onUpdate={onUpdate}
                onReply={onReply}
                onDelete={onDelete}
                onLike={onLike}
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
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [commentSort, setCommentSort] = useState<"recent" | "hot">("recent")

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
        const [postRes, commentsRes] = await Promise.all([
          postService.getPost(id, { signal: controller.signal }),
          postService.getPostComments(id, commentSort).catch(() => ({ data: [] }))
        ])
        setPost(postRes.data)
        setEditTitle(postRes.data.title)
        setEditContent(postRes.data.content)
        setComments(Array.isArray(commentsRes.data) ? commentsRes.data : [])
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') return;
        console.error(err)
      } finally {
        if (!controller.signal.aborted) setLoading(false)
      }
    }
    fetchPostData()
    return () => controller.abort()
  }, [id, commentSort])

  const handlePostUpdate = async () => {
    if (!id || !editTitle.trim() || !editContent.trim()) return;
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
    } finally {
      setSubmitting(false);
    }
  }

  const handlePostLike = async () => {
    if (!id || !user || !post) return;
    try {
      await postService.likePost(id);
      setPost({ ...post, likes: post.likes + 1 });
    } catch (err) {
      console.error("Failed to like post", err);
    }
  }

  const handleCommentLike = async (commentId: string) => {
    if (!user) return;
    try {
      await commentService.likeComment(commentId);
      const updateInTree = (list: CommentDto[]): CommentDto[] => {
        return list.map(c => {
          if (c.id === commentId) {
            return { ...c, likes: c.likes + 1 };
          }
          if (c.repliedComments && c.repliedComments.length > 0) {
            return { ...c, repliedComments: updateInTree(c.repliedComments) };
          }
          return c;
        });
      };
      setComments(updateInTree(comments));
    } catch (err) {
      console.error("Failed to like comment", err);
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
    }
  }

  const handleCommentUpdate = async (commentId: string, content: string) => {
    try {
      const res = await commentService.updateComment(commentId, { content });
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
    }
  }

  const handleCommentReply = async (parentId: string, content: string) => {
    if (!id || !user) return;
    try {
      const res = await commentService.createComment({
        content,
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
    }
  }

  const handleCommentSubmit = async () => {
    if (!commentContent.trim() || !user || !id) return;
    setSubmitting(true)
    try {
      const res = await commentService.createComment({
        content: commentContent,
        userId: user.id,
        postId: id
      });
      if (res.data) {
        setComments([{ ...res.data, author: { id: user.id, name: user.name, avatar: user.avatar } }, ...comments]);
        setCommentContent("");
      }
    } catch (err) {
      console.error(err)
    } finally {
      setSubmitting(false)
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
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 my-8">
            {post.images.map((img, idx) => (
              <img 
                key={idx}
                src={img} 
                className="w-full h-auto rounded-[2rem] object-cover bg-muted border border-border/50"
                alt={`${t("nav.post")} image ${idx + 1}`}
              />
            ))}
          </div>
        )}

        <div className="prose prose-lg dark:prose-invert max-w-none">
          {isEditingPost ? (
            <textarea
              value={editContent}
              onChange={(e) => setEditContent(e.target.value)}
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
            <Heart size={20} className={cn("group-hover:scale-110 transition-transform", post.likes > 0 && "fill-primary")} />
            <span>{post.likes} {t("post.likes")}</span>
          </button>
          <button className="flex items-center gap-2 px-6 py-2.5 rounded-full hover:bg-muted transition-all font-bold text-muted-foreground group">
            <Share2 size={20} className="group-hover:scale-110 transition-transform" />
            <span>{t("post.share")}</span>
          </button>
        </footer>
      </article>

      <section className="space-y-8 animate-in fade-in slide-in-from-bottom-6 duration-700 delay-200">
        <div className="flex items-center justify-between">
          <h3 className="text-2xl font-bold tracking-tight">{t("post.comments_count", { count: comments.length })}</h3>
          <div className="flex items-center gap-2 p-1 rounded-xl bg-muted/50 border border-border/50">
            <button
              onClick={() => setCommentSort("recent")}
              className={cn(
                "px-3 py-1.5 rounded-lg text-xs font-bold transition-all",
                commentSort === "recent" ? "bg-background shadow-sm text-foreground" : "text-muted-foreground hover:text-foreground"
              )}
            >
              {t("post.sort_recent")}
            </button>
            <button
              onClick={() => setCommentSort("hot")}
              className={cn(
                "px-3 py-1.5 rounded-lg text-xs font-bold transition-all",
                commentSort === "hot" ? "bg-background shadow-sm text-foreground" : "text-muted-foreground hover:text-foreground"
              )}
            >
              {t("post.sort_hot")}
            </button>
          </div>
        </div>

        {user ? (
          <div className="flex gap-4 p-2 pl-4 rounded-4xl bg-muted/50 border border-border/50 focus-within:border-primary/30 focus-within:ring-4 focus-within:ring-primary/5 transition-all">
            <input
              type="text"
              placeholder={t("post.comment_placeholder")}
              value={commentContent}
              onChange={(e) => setCommentContent(e.target.value)}
              className="flex-1 bg-transparent border-none outline-none py-3 text-sm"
              onKeyDown={(e) => e.key === 'Enter' && handleCommentSubmit()}
            />
            <button 
              onClick={handleCommentSubmit}
              disabled={!commentContent.trim() || submitting}
              className="p-3 rounded-2xl bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50 transition-all shadow-lg shadow-primary/20"
            >
              {submitting ? <Loader2 size={18} className="animate-spin" /> : <Send size={18} />}
            </button>
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
              user={user} 
              i18n={i18n} 
              t={t} 
              onUpdate={handleCommentUpdate} 
              onReply={handleCommentReply}
              onDelete={handleCommentDelete}
              onLike={handleCommentLike}
            />
          ))}
          {comments.length === 0 && (
            <p className="text-center text-muted-foreground py-8">{t("post.no_comments")}</p>
          )}
        </div>
      </section>
    </div>
  )
}
