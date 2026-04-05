import { MessageSquare, Heart, Share2, MoreHorizontal } from "lucide-react"
import { useNavigate } from "react-router"

export const posts = [
  {
    id: 1,
    title: "The Future of Minimalist Web Design in 2026",
    excerpt: "Exploring how blurred backgrounds and bold typography continue to define the modern web experience...",
    author: "Alex Rivera",
    avatar: "https://api.dicebear.com/7.x/avataaars/svg?seed=Alex",
    category: "Design",
    time: "2h ago",
    likes: 124,
    comments: 18,
  },
  {
    id: 2,
    title: "Best Practices for Tailwind CSS 4.x",
    excerpt: "Tailwind 4 brings incredible performance improvements and a simplified configuration. Here's what you need to know.",
    author: "Jordan Smith",
    avatar: "https://api.dicebear.com/7.x/avataaars/svg?seed=Jordan",
    category: "Development",
    time: "5h ago",
    likes: 89,
    comments: 12,
  },
  {
    id: 3,
    title: "MarketOurs Version 2.0 is now live!",
    excerpt: "We've completely redesigned the experience from the ground up to be faster and more intuitive.",
    author: "Team MarketOurs",
    avatar: "https://api.dicebear.com/7.x/avataaars/svg?seed=Market",
    category: "Updates",
    time: "1d ago",
    likes: 256,
    comments: 45,
  },
]

export function PostCard({ post }: { post: typeof posts[0] }) {
  const navigate = useNavigate();
  
  return (
    <article
      onClick={() => navigate(`/post/${post.id}`)}
      className="group relative bg-card rounded-[2rem] p-6 border border-border/50 transition-all duration-300 hover:border-primary/30 hover:shadow-xl hover:shadow-primary/5 cursor-pointer"
    >
      <div className="flex items-center gap-3 mb-4">
        <img src={post.avatar} alt={post.author} className="w-10 h-10 rounded-full bg-muted" />
        <div>
          <p className="text-sm font-semibold">{post.author}</p>
          <p className="text-xs text-muted-foreground">{post.time} • {post.category}</p>
        </div>
        <button 
          onClick={(e) => { e.stopPropagation(); }} 
          className="ml-auto p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground"
        >
          <MoreHorizontal size={18} />
        </button>
      </div>

      <div className="space-y-2 mb-6">
        <h2 className="text-2xl font-bold tracking-tight group-hover:text-primary transition-colors">
          {post.title}
        </h2>
        <p className="text-muted-foreground leading-relaxed line-clamp-3">
          {post.excerpt}
        </p>
      </div>

      <div className="flex items-center gap-6 pt-4 border-t border-border/30">
        <button 
          onClick={(e) => { e.stopPropagation(); }}
          className="flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-primary transition-colors"
        >
          <Heart size={18} />
          <span>{post.likes}</span>
        </button>
        <button 
          onClick={(e) => { e.stopPropagation(); }}
          className="flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-primary transition-colors"
        >
          <MessageSquare size={18} />
          <span>{post.comments}</span>
        </button>
        <button 
          onClick={(e) => { e.stopPropagation(); }}
          className="flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-primary transition-colors ml-auto"
        >
          <Share2 size={18} />
        </button>
      </div>
    </article>
  )
}

export default function HomePage() {
  return (
    <div className="max-w-3xl mx-auto space-y-10">
      <div className="space-y-6">
        {posts.map((post) => (
          <PostCard key={post.id} post={post} />
        ))}
      </div>
    </div>
  )
}
