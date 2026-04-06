import { MessageSquare, Heart, Share2, MoreHorizontal, Search, Loader2, WatchIcon, LucideWatch, Watch, Eye } from "lucide-react"
import { useNavigate } from "react-router"
import { useState, useEffect, useRef } from "react"
import { useTranslation, type i18n } from "react-i18next"
import { useSelector } from "react-redux"
import { type RootState } from "../../stores"
import { postService } from "../../services/postService"
import type { PostDto } from "../../types"
import { formatDistanceToNow } from "date-fns"
import { zhCN, enUS } from "date-fns/locale"

// Format date helper
const formatDate = (dateString: string, i18nInstance: i18n, updatedAtString?: string, t?: any) => {
  try {
    const date = new Date(dateString);
    const display = formatDistanceToNow(date, { 
      addSuffix: true, 
      locale: i18nInstance.language === 'zh' ? zhCN : enUS 
    });
    
    if (updatedAtString && t) {
      const updatedDate = new Date(updatedAtString);
      if (updatedDate.getTime() - date.getTime() > 5000) {
        return `${display} (${t('post.edited')})`;
      }
    }
    
    return display;
  } catch {
    return dateString;
  }
}

export function PostCard({ post, onDelete }: { post: PostDto; onDelete?: (id: string) => void }) {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const { user } = useSelector((state: RootState) => state.auth);
  
  const isMe = user && post.userId.toLowerCase() === user.id.toLowerCase();
  const isAdmin = user && user.role === 'Admin';
  const authorName = post.author?.name || `${t('common.user')} ${post.userId.slice(0, 4)}`;
  const displayName = isMe ? `${authorName} (${t('common.me', { defaultValue: '我' })})` : authorName;
  const authorAvatar = post.author?.avatar || `https://api.dicebear.com/7.x/avataaars/svg?seed=${post.userId}`;

  const handleDelete = async (e: React.MouseEvent) => {
    e.stopPropagation();
    if (window.confirm(t("post.confirm_delete"))) {
      try {
        await postService.deletePost(post.id);
        onDelete?.(post.id);
      } catch (err) {
        console.error(err);
      }
    }
  };

  return (
    <article
      onClick={() => navigate(`/post/${post.id}`)}
      className="group relative bg-card rounded-[2rem] p-6 border border-border/50 transition-all duration-300 hover:border-primary/30 hover:shadow-xl hover:shadow-primary/5 cursor-pointer"
    >
      <div className="flex items-center gap-3 mb-4">
        <img src={authorAvatar} alt={authorName} className="w-10 h-10 rounded-full bg-muted" />
        <div className="flex-1">
          <p className="text-sm font-semibold">{displayName}</p>
          <p className="text-xs text-muted-foreground">{formatDate(post.createdAt, i18n, post.updatedAt, t)}</p>
        </div>
        {(isMe || isAdmin) && (
          <button 
            onClick={handleDelete}
            className="p-2 rounded-full hover:bg-destructive/10 hover:text-destructive transition-colors text-muted-foreground"
            title={t("post.delete")}
          >
            <span className="sr-only">{t("post.delete")}</span>
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/><line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/></svg>
          </button>
        )}
        <button 
          onClick={(e) => { e.stopPropagation(); }} 
          className="p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground"
        >
          <MoreHorizontal size={18} />
        </button>
      </div>

      <div className="space-y-2 mb-6">
        <h2 className="text-2xl font-bold tracking-tight group-hover:text-primary transition-colors">
          {post.title}
        </h2>
        <p className="text-muted-foreground leading-relaxed line-clamp-3 whitespace-pre-wrap">
          {post.content.substring(0, 150)}{post.content.length > 150 ? '...' : ''}
        </p>
        {post.images && post.images.length > 0 && (
          <div className="mt-4 flex gap-2 overflow-hidden h-32 rounded-xl">
             {post.images.slice(0, 3).map((img, i) => (
                <img key={i} src={img} className="object-cover w-1/3 h-full bg-muted" alt="" />
             ))}
          </div>
        )}
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
          <Eye size={18} />
          <span>{post.watch}</span>
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
  const { t } = useTranslation();
  const [posts, setPosts] = useState<PostDto[]>([]);
  const [page, setPage] = useState(1);
  const [keyword, setKeyword] = useState("");
  const [searchInput, setSearchInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const observerTarget = useRef(null);

  const fetchPosts = async (pageNum: number, searchKw: string, append = true) => {
    if (loading) return;
    setLoading(true);
    try {
      const res = await postService.getPosts(pageNum, 10, searchKw);
      const data = res.data;
      if (data && data.items) {
        setPosts(prev => append ? [...prev, ...data.items] : data.items);
        setHasMore(data.hasNextPage);
      } else {
        setHasMore(false);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPosts(1, keyword, false);
  }, [keyword]);

  useEffect(() => {
    if (page > 1) {
      fetchPosts(page, keyword, true);
    }
  }, [page]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    setKeyword(searchInput);
  };

  useEffect(() => {
    const observer = new IntersectionObserver(
      entries => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          setPage(p => p + 1);
        }
      },
      { threshold: 1.0 }
    );
    if (observerTarget.current) {
      observer.observe(observerTarget.current);
    }
    return () => observer.disconnect();
  }, [hasMore, loading]);

  return (
    <div className="max-w-3xl mx-auto space-y-10 pb-20">
      <form onSubmit={handleSearch} className="relative">
        <input 
          type="text" 
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder={t('common.search_placeholder')}
          className="w-full pl-12 pr-4 py-4 rounded-2xl bg-card border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all shadow-sm"
        />
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={20} />
        <button type="submit" className="hidden">{t('common.search')}</button>
      </form>

      <div className="space-y-6">
        {posts.map((post) => (
          <PostCard 
            key={post.id} 
            post={post} 
            onDelete={(id) => setPosts(prev => prev.filter(p => p.id !== id))}
          />
        ))}
      </div>

      <div ref={observerTarget} className="flex justify-center py-8">
        {loading && <Loader2 className="animate-spin text-primary" size={32} />}
        {!hasMore && posts.length > 0 && <p className="text-muted-foreground">{t('common.no_more_posts')}</p>}
        {!hasMore && posts.length === 0 && !loading && <p className="text-muted-foreground">{t('common.no_posts_found')}</p>}
      </div>
    </div>
  )
}
