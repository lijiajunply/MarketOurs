import { Heart, Share2, MoreHorizontal, Search, Loader2, Eye, ArrowLeft } from "lucide-react"
import { useNavigate } from "react-router"
import { useState, useRef, useEffectEvent, useEffect } from "react"
import { useTranslation } from "react-i18next"
import { useSelector } from "react-redux"
import type { RootState } from "@/stores"
import { postService } from "@/services/postService"
import { extractUserMessage } from "@/services/errorCodes"
import { toast } from "@/lib/toast"
import type { PostDto } from "@/types"
import { formatPostRelativeDate, getPostAuthorName, getPostExcerpt } from "@/lib/postDisplay"
import { sharePost } from "@/lib/postShare"
import { PostTagBadge } from "./PostTagBadge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"

export function PostCard({ post, onDelete }: { post: PostDto; onDelete?: (id: string) => void }) {
  const navigate = useNavigate()
  const { t, i18n } = useTranslation()
  const { user } = useSelector((state: RootState) => state.auth)
  const [shareFeedback, setShareFeedback] = useState<string | null>(null)

  const isMe = user && post.userId.toLowerCase() === user.id.toLowerCase()
  const isAdmin = user && user.role === "Admin"
  const authorName = getPostAuthorName(post, t("common.user"))
  const displayName = isMe ? `${authorName} (${t("common.me", { defaultValue: "我" })})` : authorName
  const authorAvatar = post.author?.avatar || `https://api.dicebear.com/7.x/avataaars/svg?seed=${post.userId}`
  const authorInitials = authorName.slice(0, 2).toUpperCase()
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)

  const handleDelete = async () => {
    try {
      await postService.deletePost(post.id)
      onDelete?.(post.id)
    } catch (err) {
      toast.error(extractUserMessage(err, t("post.delete_error")))
    }
  }

  const handleAuthorClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    navigate(`/user/${post.userId}`)
  }

  const handleShare = async (e: React.MouseEvent) => {
    e.stopPropagation()
    try {
      const outcome = await sharePost(post)
      if (outcome === "shared") {
        setShareFeedback("已打开分享面板")
      } else if (outcome === "copied") {
        setShareFeedback("链接已复制")
      }
    } catch (error) {
      console.error(error)
      setShareFeedback(extractUserMessage(error, "分享失败，请稍后重试"))
    } finally {
      window.setTimeout(() => setShareFeedback(null), 2500)
    }
  }

  return (
    <article
      onClick={() => navigate(`/post/${post.id}`)}
      className="group relative cursor-pointer rounded-3xl border border-border/40 bg-card p-5 sm:p-6 transition-all duration-300 hover:border-primary/20 hover:shadow-md hover:shadow-primary/5 hover:-translate-y-0.5"
    >
      {/* Author Row */}
      <div className="mb-4 flex items-center gap-3">
        <button
          type="button"
          onClick={handleAuthorClick}
          className="flex flex-1 items-center gap-3 text-left transition-colors hover:text-primary"
        >
          <Avatar className="h-10 w-10 rounded-full ring-2 ring-border/20">
            <AvatarImage src={authorAvatar} alt={authorName} />
            <AvatarFallback className="bg-primary/10 text-primary text-xs font-medium">
              {authorInitials}
            </AvatarFallback>
          </Avatar>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold truncate">{displayName}</p>
            <p className="text-xs text-muted-foreground">
              {formatPostRelativeDate(post.createdAt, i18n, post.updatedAt, t("post.edited"))}
            </p>
          </div>
        </button>
        {(isMe || isAdmin) && (
          <Button
            variant="ghost"
            size="icon-sm"
            onClick={(e) => { e.stopPropagation(); setShowDeleteDialog(true) }}
            className="rounded-xl text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
            title={t("post.delete")}
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/><line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/></svg>
          </Button>
        )}
        <Button
          variant="ghost"
          size="icon-sm"
          onClick={(e) => { e.stopPropagation() }}
          className="rounded-xl text-muted-foreground"
        >
          <MoreHorizontal size={18} />
        </Button>
      </div>

      {/* Content */}
      <div className="mb-5 space-y-2.5">
        <PostTagBadge tag={post.tag} />
        <h2 className="text-xl font-semibold tracking-tight transition-colors group-hover:text-primary sm:text-2xl">
          {post.title}
        </h2>
        <p className="line-clamp-3 whitespace-pre-wrap leading-relaxed text-muted-foreground text-sm sm:text-base">
          {getPostExcerpt(post.content)}
        </p>
        {post.images && post.images.length > 0 && (
          <div className="mt-4 flex h-36 sm:h-40 gap-2 overflow-hidden rounded-2xl">
            {post.images.slice(0, 3).map((img, i) => (
              <img key={i} src={img} className="h-full flex-1 bg-muted object-cover" alt="" />
            ))}
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="flex items-center gap-4 border-t border-border/20 pt-4">
        <button
          onClick={(e) => { e.stopPropagation() }}
          className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground transition-colors hover:text-primary"
        >
          <Heart size={18} />
          <span>{post.likes}</span>
        </button>
        <button
          onClick={(e) => { e.stopPropagation() }}
          className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground transition-colors hover:text-primary"
        >
          <Eye size={18} />
          <span>{post.watch}</span>
        </button>
        <button
          onClick={handleShare}
          className="ml-auto flex items-center gap-1.5 text-sm font-medium text-muted-foreground transition-colors hover:text-primary"
          title={t("post.share")}
        >
          <Share2 size={18} />
          <span>{t("post.share")}</span>
        </button>
      </div>
      {shareFeedback ? (
        <p className="mt-3 text-right text-xs font-medium text-primary animate-in fade-in">
          {shareFeedback}
        </p>
      ) : null}

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("post.delete")}</AlertDialogTitle>
            <AlertDialogDescription>{t("post.confirm_delete")}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("post.cancel")}</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={handleDelete}>
              {t("post.delete")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </article>
  )
}

export function PostCardSkeleton() {
  return (
    <div className="rounded-3xl border border-border/40 bg-card p-5 sm:p-6 space-y-4">
      <div className="flex items-center gap-3">
        <Skeleton className="h-10 w-10 rounded-full" />
        <div className="space-y-1.5">
          <Skeleton className="h-4 w-28" />
          <Skeleton className="h-3 w-20" />
        </div>
      </div>
      <div className="space-y-2">
        <Skeleton className="h-5 w-16 rounded-full" />
        <Skeleton className="h-7 w-3/4" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-2/3" />
      </div>
      <div className="flex gap-4 pt-4 border-t border-border/20">
        <Skeleton className="h-5 w-12" />
        <Skeleton className="h-5 w-12" />
        <Skeleton className="h-5 w-16 ml-auto" />
      </div>
    </div>
  )
}

export function usePostFeed(tagId?: string) {
  const { t } = useTranslation()
  const [posts, setPosts] = useState<PostDto[]>([])
  const [keyword, setKeyword] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const [feedError, setFeedError] = useState<string | null>(null)
  const observerTarget = useRef<HTMLDivElement | null>(null)
  const loadingRef = useRef(false)
  const hasMoreRef = useRef(true)
  const currentPageRef = useRef(0)
  const currentKeywordRef = useRef("")
  const observerInViewRef = useRef(false)
  const requestPhaseRef = useRef<"idle" | "loading" | "cooldown">("idle")
  const feedVersionRef = useRef(0)
  const normalizedTagId = tagId?.trim() || undefined
  const isRefreshingFeed = loading && posts.length === 0
  const isSearching = isRefreshingFeed && keyword.trim().length > 0

  useEffect(() => { loadingRef.current = loading }, [loading])
  useEffect(() => { hasMoreRef.current = hasMore }, [hasMore])
  useEffect(() => { currentKeywordRef.current = keyword }, [keyword])

  const fetchPosts = useEffectEvent(async (pageNum: number, append = true, version = feedVersionRef.current) => {
    if (requestPhaseRef.current === "loading") return

    requestPhaseRef.current = "loading"
    setLoading(true)
    setFeedError(null)
    let nextHasMore = hasMoreRef.current

    try {
      const trimmedKeyword = currentKeywordRef.current.trim()
      const res = trimmedKeyword
        ? await postService.searchPosts(pageNum, 10, trimmedKeyword, normalizedTagId)
        : await postService.getPosts(pageNum, 10, undefined, normalizedTagId)

      if (version !== feedVersionRef.current) return

      const data = res.data
      if (data && data.items) {
        currentPageRef.current = pageNum
        nextHasMore = data.hasNextPage
        setPosts((prev) => (append ? [...prev, ...data.items] : data.items))
        setHasMore(data.hasNextPage)
      } else {
        currentPageRef.current = pageNum
        nextHasMore = false
        if (!append) setPosts([])
        setHasMore(false)
      }
    } catch (err) {
      nextHasMore = hasMoreRef.current
      if (version !== feedVersionRef.current) return
      console.error(err)
      setFeedError(extractUserMessage(err, t("common.error")))
    }

    if (version !== feedVersionRef.current) return

    setLoading(false)
    hasMoreRef.current = nextHasMore
    requestPhaseRef.current = observerInViewRef.current && nextHasMore ? "cooldown" : "idle"
  })

  useEffect(() => {
    const version = feedVersionRef.current + 1
    feedVersionRef.current = version
    currentPageRef.current = 0
    currentKeywordRef.current = keyword
    observerInViewRef.current = false
    requestPhaseRef.current = "idle"
    hasMoreRef.current = true
    setPosts([])
    setHasMore(true)
    void fetchPosts(1, false, version)
  }, [keyword, normalizedTagId])

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[0]
        if (!entry) return
        observerInViewRef.current = entry.isIntersecting
        if (!entry.isIntersecting) {
          if (requestPhaseRef.current === "cooldown") requestPhaseRef.current = "idle"
          return
        }
        if (requestPhaseRef.current !== "idle" || !hasMoreRef.current || loadingRef.current) return
        void fetchPosts(currentPageRef.current + 1, true, feedVersionRef.current)
      },
      { rootMargin: "0px 0px 320px 0px", threshold: 0 },
    )

    const target = observerTarget.current
    if (target) observer.observe(target)
    return () => observer.disconnect()
  }, [])

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    setKeyword(searchInput.trim())
  }

  return {
    posts, setPosts, searchInput, setSearchInput, loading, hasMore,
    feedError, observerTarget, isRefreshingFeed, isSearching, keyword, handleSearch,
  }
}

export function PostFeed({
  tagId,
  searchPlaceholder,
  emptyMessage,
  header,
}: {
  tagId?: string
  searchPlaceholder?: string
  emptyMessage?: string
  header?: React.ReactNode
}) {
  const { t } = useTranslation()
  const {
    posts, setPosts, searchInput, setSearchInput, loading, hasMore,
    feedError, observerTarget, isRefreshingFeed, isSearching, handleSearch,
  } = usePostFeed(tagId)

  return (
    <div className="mx-auto max-w-2xl space-y-8 pb-20">
      {header}
      <form onSubmit={handleSearch} className="relative">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" size={18} />
        <Input
          type="text"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder={searchPlaceholder ?? t("common.search_placeholder")}
          className="h-12 rounded-2xl border-border/50 bg-card pl-11 pr-12 text-sm shadow-sm transition-all focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-primary/20"
          aria-busy={isSearching}
        />
        {isSearching ? (
          <Loader2
            className="absolute right-4 top-1/2 -translate-y-1/2 animate-spin text-primary"
            size={18}
            aria-hidden="true"
          />
        ) : null}
        <button type="submit" className="hidden">{t("common.search")}</button>
      </form>

      <div className="space-y-5">
        {feedError && (
          <div className="animate-in rounded-2xl bg-destructive/10 p-4 text-center text-sm font-medium text-destructive fade-in duration-300">
            {feedError}
          </div>
        )}
        {isRefreshingFeed && (
          <div className="space-y-5">
            {Array.from({ length: 3 }).map((_, i) => (
              <PostCardSkeleton key={i} />
            ))}
          </div>
        )}
        {posts.map((post) => (
          <PostCard
            key={post.id}
            post={post}
            onDelete={(id) => setPosts((prev) => prev.filter((p) => p.id !== id))}
          />
        ))}
      </div>

      <div ref={observerTarget} className="flex justify-center py-8">
        {loading && posts.length > 0 && <Loader2 className="animate-spin text-primary" size={28} />}
        {!hasMore && posts.length > 0 && (
          <p className="text-sm text-muted-foreground">{t("common.no_more_posts")}</p>
        )}
        {!hasMore && posts.length === 0 && !loading && (
          <div className="flex flex-col items-center gap-3 py-12 text-center">
            <div className="rounded-2xl bg-muted/50 p-4">
              <Search size={24} className="text-muted-foreground/60" />
            </div>
            <p className="text-sm text-muted-foreground">
              {emptyMessage ?? t("common.no_posts_found")}
            </p>
          </div>
        )}
      </div>
    </div>
  )
}

export function BackLink({ to, label }: { to: string; label: string }) {
  const navigate = useNavigate()

  return (
    <Button
      variant="ghost"
      size="sm"
      onClick={() => navigate(to)}
      className="rounded-xl gap-2 text-muted-foreground hover:text-foreground"
    >
      <ArrowLeft size={16} />
      {label}
    </Button>
  )
}
