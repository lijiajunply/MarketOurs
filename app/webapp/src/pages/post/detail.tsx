import { useParams, useNavigate } from "react-router"
import { Heart, Share2, ArrowLeft, MoreHorizontal, Send, Loader2 } from "lucide-react"
import { useState, useEffect } from "react"
import { postService } from "../../services/postService"
import { commentService } from "../../services/commentService"
import type { PostDto, CommentDto } from "../../types"
import { useSelector } from "react-redux"
import type { RootState } from "../../stores"

const formatDate = (dateString: string) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export default function PostDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useSelector((state: RootState) => state.auth)

  const [post, setPost] = useState<PostDto | null>(null)
  const [comments, setComments] = useState<CommentDto[]>([])
  const [commentContent, setCommentContent] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!id) return;
    const fetchPostData = async () => {
      setLoading(true)
      try {
        const [postRes, commentsRes] = await Promise.all([
          postService.getPost(id),
          postService.getPostComments(id, "recent").catch(() => ({ data: [] }))
        ])
        setPost(postRes.data)
        setComments(Array.isArray(commentsRes.data) ? commentsRes.data : [])
      } catch (err) {
        console.error(err)
      } finally {
        setLoading(false)
      }
    }
    fetchPostData()
  }, [id])

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
        setComments([res.data, ...comments]);
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
    return <div className="text-center py-20 text-muted-foreground">Post not found.</div>
  }

  return (
    <div className="max-w-3xl mx-auto space-y-8 pb-20">
      <button
        onClick={() => navigate(-1)}
        className="flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors group"
      >
        <ArrowLeft size={18} className="group-hover:-translate-x-1 transition-transform" />
        Back to Feed
      </button>

      <article className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
        <header className="space-y-6">
          <h1 className="text-4xl sm:text-5xl font-black tracking-tight leading-[1.1]">
            {post.title}
          </h1>
          
          <div className="flex items-center gap-4 py-6 border-y border-border/30">
            <img src={`https://api.dicebear.com/7.x/avataaars/svg?seed=${post.userId}`} alt="Author" className="w-12 h-12 rounded-full bg-muted shadow-inner" />
            <div className="flex-1">
              <p className="font-bold text-lg">User {post.userId.slice(0, 4)}</p>
              <p className="text-sm text-muted-foreground">{formatDate(post.createdAt)}</p>
            </div>
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
                alt={`Post image ${idx + 1}`}
              />
            ))}
          </div>
        )}

        <div className="prose prose-lg dark:prose-invert max-w-none">
          <div className="space-y-6 text-lg leading-relaxed text-foreground whitespace-pre-wrap">
            {post.content}
          </div>
        </div>

        <footer className="flex items-center gap-6 py-8 border-t border-border/30">
          <button className="flex items-center gap-2 px-6 py-2.5 rounded-full bg-primary/10 text-primary hover:bg-primary/20 transition-all font-bold group">
            <Heart size={20} className="group-hover:scale-110 transition-transform" />
            <span>{post.likes} Likes</span>
          </button>
          <button className="flex items-center gap-2 px-6 py-2.5 rounded-full hover:bg-muted transition-all font-bold text-muted-foreground group">
            <Share2 size={20} className="group-hover:scale-110 transition-transform" />
            <span>Share</span>
          </button>
        </footer>
      </article>

      <section className="space-y-8 animate-in fade-in slide-in-from-bottom-6 duration-700 delay-200">
        <div className="flex items-center justify-between">
          <h3 className="text-2xl font-bold tracking-tight">Comments ({comments.length})</h3>
        </div>

        {user ? (
          <div className="flex gap-4 p-2 pl-4 rounded-[2rem] bg-muted/50 border border-border/50 focus-within:border-primary/30 focus-within:ring-4 focus-within:ring-primary/5 transition-all">
            <input
              type="text"
              placeholder="Write a thoughtful reply..."
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
            <p className="text-muted-foreground text-sm">Please log in to leave a comment.</p>
          </div>
        )}

        <div className="space-y-6">
          {comments.map((c) => (
            <div key={c.id} className="flex gap-4 group">
              <img src={`https://api.dicebear.com/7.x/avataaars/svg?seed=${c.userId}`} alt="User" className="w-10 h-10 rounded-full bg-muted shadow-sm flex-shrink-0" />
              <div className="flex-1 space-y-2">
                <div className="p-5 rounded-[1.5rem] bg-card border border-border/40 shadow-sm group-hover:border-primary/20 transition-colors">
                  <div className="flex items-center justify-between mb-1">
                    <p className="font-bold text-sm">User {c.userId.slice(0, 4)}</p>
                    <p className="text-xs text-muted-foreground">{formatDate(c.createdAt)}</p>
                  </div>
                  <p className="text-muted-foreground leading-relaxed">{c.content}</p>
                </div>
                <div className="flex items-center gap-4 ml-2">
                  <button className="text-xs font-bold text-muted-foreground hover:text-primary transition-colors flex items-center gap-1">
                    <Heart size={12} /> {c.likes}
                  </button>
                  <button className="text-xs font-bold text-muted-foreground hover:text-primary transition-colors">Reply</button>
                </div>
              </div>
            </div>
          ))}
          {comments.length === 0 && (
            <p className="text-center text-muted-foreground py-8">No comments yet. Be the first to share your thoughts!</p>
          )}
        </div>
      </section>
    </div>
  )
}
