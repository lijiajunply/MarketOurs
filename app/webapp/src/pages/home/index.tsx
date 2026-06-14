import { Heart, Share2, MoreHorizontal, Search, Loader2, Eye } from "lucide-react"
import { useNavigate } from "react-router"
import { useState, useEffect, useRef, useEffectEvent } from "react"
import { useTranslation } from "react-i18next"
import { useSelector } from "react-redux"
import { type RootState } from "../../stores"
import { postService } from "../../services/postService"
import { extractUserMessage } from "../../services/errorCodes"
import type { PostDto } from "../../types"
import { formatPostRelativeDate, getPostAuthorName, getPostExcerpt } from "../../lib/postDisplay"
import { sharePost } from "../../lib/postShare"
import { PostTagBadge } from "../../components/post/PostTagBadge"

export function PostCard({ post, onDelete }: { post: PostDto; onDelete?: (id: string) => void }) {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const { user } = useSelector((state: RootState) => state.auth);
  const [shareFeedback, setShareFeedback] = useState<string | null>(null);
  
  const isMe = user && post.userId.toLowerCase() === user.id.toLowerCase();
  const isAdmin = user && user.role === 'Admin';
  const authorName = getPostAuthorName(post, t("common.user"));
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

  const handleAuthorClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigate(`/user/${post.userId}`);
  };

  const handleShare = async (e: React.MouseEvent) => {
    e.stopPropagation();
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
  };

  return (
    <article
      onClick={() => navigate(`/post/${post.id}`)}
      className="group relative bg-card rounded-[2rem] p-6 border border-border/50 transition-all duration-300 hover:border-primary/30 hover:shadow-xl hover:shadow-primary/5 cursor-pointer"
    >
      <div className="flex items-center gap-3 mb-4">
        <button
          type="button"
          onClick={handleAuthorClick}
          className="flex flex-1 items-center gap-3 rounded-2xl text-left transition-colors hover:text-primary"
        >
          <img src={authorAvatar} alt={authorName} className="w-10 h-10 rounded-full bg-muted" />
          <div className="flex-1">
          <p className="text-sm font-semibold">{displayName}</p>
          <p className="text-xs text-muted-foreground">{formatPostRelativeDate(post.createdAt, i18n, post.updatedAt, t("post.edited"))}</p>
          </div>
        </button>
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
        <PostTagBadge tag={post.tag} />
        <h2 className="text-2xl font-bold tracking-tight group-hover:text-primary transition-colors">
          {post.title}
        </h2>
        <p className="text-muted-foreground leading-relaxed line-clamp-3 whitespace-pre-wrap">
          {getPostExcerpt(post.content)}
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
          onClick={handleShare}
          className="flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-primary transition-colors ml-auto"
          title={t("post.share")}
        >
          <Share2 size={18} />
          <span>{t("post.share")}</span>
        </button>
      </div>
      {shareFeedback ? (
        <p className="mt-3 text-right text-xs font-medium text-primary">{shareFeedback}</p>
      ) : null}
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
  const [feedError, setFeedError] = useState<string | null>(null);
  const observerTarget = useRef(null);
  const isRefreshingFeed = loading && page === 1;
  const isSearching = isRefreshingFeed && keyword.trim().length > 0;

  const fetchPosts = useEffectEvent(async (pageNum: number, searchKw: string, append = true) => {
    if (loading) return;
    setLoading(true);
    setFeedError(null);
    try {
      const trimmedKeyword = searchKw.trim();
      const res = trimmedKeyword
        ? await postService.searchPosts(pageNum, 10, trimmedKeyword)
        : await postService.getPosts(pageNum, 10);
      const data = res.data;
      if (data && data.items) {
        setPosts(prev => append ? [...prev, ...data.items] : data.items);
        setHasMore(data.hasNextPage);
      } else {
        setHasMore(false);
      }
    } catch (err) {
      console.error(err);
      setFeedError(extractUserMessage(err, t("common.error")));
    } finally {
      setLoading(false);
    }
  });

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
    setKeyword(searchInput.trim());
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
          className="w-full pl-12 pr-12 py-4 rounded-2xl bg-card border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all shadow-sm"
          aria-busy={isSearching}
        />
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={20} />
        {isSearching ? (
          <Loader2
            className="absolute right-4 top-1/2 -translate-y-1/2 animate-spin text-primary"
            size={20}
            aria-hidden="true"
          />
        ) : null}
        <button type="submit" className="hidden">{t('common.search')}</button>
      </form>

      <div className="space-y-6">
        {feedError && (
          <div className="p-4 rounded-2xl bg-destructive/10 text-destructive text-sm font-medium text-center animate-in fade-in duration-300">
            {feedError}
          </div>
        )}
        {isRefreshingFeed ? (
          <div className="flex items-center justify-center gap-3 rounded-2xl border border-border/50 bg-card/70 px-4 py-5 text-sm font-medium text-muted-foreground">
            <Loader2 className="animate-spin text-primary" size={20} />
            <span>{isSearching ? t('common.search') : t('common.loading', { defaultValue: 'Loading...' })}</span>
          </div>
        ) : null}
        {posts.map((post) => (
          <PostCard 
            key={post.id} 
            post={post} 
            onDelete={(id) => setPosts(prev => prev.filter(p => p.id !== id))}
          />
        ))}
      </div>

      <div ref={observerTarget} className="flex justify-center py-8">
        {loading && page > 1 && <Loader2 className="animate-spin text-primary" size={32} />}
        {!hasMore && posts.length > 0 && <p className="text-muted-foreground">{t('common.no_more_posts')}</p>}
        {!hasMore && posts.length === 0 && !loading && <p className="text-muted-foreground">{t('common.no_posts_found')}</p>}
      </div>
    </div>
  )
}
