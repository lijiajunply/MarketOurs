import { useParams, useNavigate } from "react-router"
import { MessageSquare, Heart, Share2, ArrowLeft, MoreHorizontal, Send } from "lucide-react"
import { posts } from "../home"
import { useState } from "react"
import { cn } from "../../lib/utils"

const mockComments = [
  {
    id: 1,
    author: "Sarah Connor",
    avatar: "https://api.dicebear.com/7.x/avataaars/svg?seed=Sarah",
    content: "This is exactly what the industry needs right now. Great insights on the blurred background trends!",
    time: "45m ago",
    likes: 12,
  },
  {
    id: 2,
    author: "David Miller",
    avatar: "https://api.dicebear.com/7.x/avataaars/svg?seed=David",
    content: "I've been experimenting with Tailwind 4.0 and the performance gains are definitely noticeable.",
    time: "1h ago",
    likes: 8,
  },
]

export default function PostDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const post = posts.find(p => p.id.toString() === id) || posts[0]
  const [comment, setComment] = useState("")

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
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 text-primary text-xs font-bold uppercase tracking-wider">
            {post.category}
          </div>
          <h1 className="text-4xl sm:text-5xl font-black tracking-tight leading-[1.1]">
            {post.title}
          </h1>
          
          <div className="flex items-center gap-4 py-6 border-y border-border/30">
            <img src={post.avatar} alt={post.author} className="w-12 h-12 rounded-full bg-muted shadow-inner" />
            <div className="flex-1">
              <p className="font-bold text-lg">{post.author}</p>
              <p className="text-sm text-muted-foreground">{post.time} • 8 min read</p>
            </div>
            <button className="p-2 rounded-full hover:bg-muted transition-colors">
              <MoreHorizontal size={20} />
            </button>
          </div>
        </header>

        <div className="prose prose-lg dark:prose-invert max-w-none">
          <p className="text-xl leading-relaxed text-foreground/90 font-medium italic mb-8">
            "{post.excerpt}"
          </p>
          <div className="space-y-6 text-lg leading-relaxed text-muted-foreground">
            <p>
              In the rapidly evolving landscape of digital experiences, minimalism isn't just about what you remove—it's about what you choose to highlight. The aesthetic we're seeing in 2026 is a sophisticated evolution of the glassmorphism and neomorphism of previous years.
            </p>
            <div className="aspect-video rounded-[2.5rem] bg-muted overflow-hidden my-10 border border-border/50">
              <img 
                src="https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=1200&q=80" 
                className="w-full h-full object-cover"
                alt="Workspace"
              />
            </div>
            <p>
              By leveraging Tailwind CSS 4.x's new engine, developers can now achieve complex backdrop filters and dynamic lighting effects with a fraction of the CSS overhead. This allows for interfaces that feel alive, responding to user movement and environmental changes while maintaining that crisp, clean Apple-inspired look.
            </p>
            <p>
              The key takeaway for designers this year is balance. While the tools allow for more complexity, the most successful interfaces will be those that use these effects to guide attention, not distract from it.
            </p>
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
          <h3 className="text-2xl font-bold tracking-tight">Comments ({mockComments.length})</h3>
        </div>

        <div className="flex gap-4 p-2 pl-4 rounded-[2rem] bg-muted/50 border border-border/50 focus-within:border-primary/30 focus-within:ring-4 focus-within:ring-primary/5 transition-all">
          <input
            type="text"
            placeholder="Write a thoughtful reply..."
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            className="flex-1 bg-transparent border-none outline-none py-3 text-sm"
          />
          <button 
            disabled={!comment.trim()}
            className="p-3 rounded-2xl bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50 transition-all shadow-lg shadow-primary/20"
          >
            <Send size={18} />
          </button>
        </div>

        <div className="space-y-6">
          {mockComments.map((c) => (
            <div key={c.id} className="flex gap-4 group">
              <img src={c.avatar} alt={c.author} className="w-10 h-10 rounded-full bg-muted shadow-sm flex-shrink-0" />
              <div className="flex-1 space-y-2">
                <div className="p-5 rounded-[1.5rem] bg-card border border-border/40 shadow-sm group-hover:border-primary/20 transition-colors">
                  <div className="flex items-center justify-between mb-1">
                    <p className="font-bold text-sm">{c.author}</p>
                    <p className="text-xs text-muted-foreground">{c.time}</p>
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
        </div>
      </section>
    </div>
  )
}
