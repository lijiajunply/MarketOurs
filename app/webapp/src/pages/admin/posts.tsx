import { useEffect, useState } from "react"
import { Search, Eye, Trash2, CheckCircle, XCircle } from "lucide-react"
import { useTranslation } from "react-i18next"
import { Link } from "react-router"
import { adminService } from "../../services/adminService"
import { extractUserMessage } from "../../services/errorCodes"
import { toast } from "../../lib/toast"
import type { PagedResult, PostDto, PostTagDto } from "../../types"
import { formatLocalDate } from "../../lib/dateTime"
import { Button } from "../../components/ui/button"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "../../components/ui/alert-dialog"

const PAGE_SIZE = 10

export default function AdminPostsPage() {
  const { t, i18n } = useTranslation()
  const [searchTerm, setSearchTerm] = useState("")
  const [page, setPage] = useState(1)
  const [posts, setPosts] = useState<PagedResult<PostDto> | null>(null)
  const [tags, setTags] = useState<PostTagDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activePostId, setActivePostId] = useState<string | null>(null)
  const [confirmAction, setConfirmAction] = useState<{ type: "delete" | "review"; post: PostDto } | null>(null)

  useEffect(() => {
    const fetchPosts = async () => {
      try {
        setIsLoading(true)
        setError(null)

        const [response, tagResponse] = await Promise.all([
          searchTerm.trim()
            ? adminService.searchPosts(page, PAGE_SIZE, searchTerm.trim())
            : adminService.getPosts(page, PAGE_SIZE),
          adminService.getPostTags(),
        ])

        setPosts(response.data)
        setTags(tagResponse.data ?? [])
      } catch (err) {
        setError(extractUserMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchPosts()
  }, [page, searchTerm, t])

  const refreshCurrentPage = async (nextPage = page) => {
    try {
      const response = searchTerm.trim()
        ? await adminService.searchPosts(nextPage, PAGE_SIZE, searchTerm.trim())
        : await adminService.getPosts(nextPage, PAGE_SIZE)

      setPosts(response.data)
      setPage(nextPage)
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.load_error")))
    }
  }

  const handleTagChange = async (post: PostDto, tagId: string) => {
    try {
      setActivePostId(post.id)
      await adminService.updatePostTag(post.id, { tagId: tagId || null })
      await refreshCurrentPage()
      toast.success(t("admin.posts.tag_updated"))
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActivePostId(null)
    }
  }

  const handleConfirm = async () => {
    if (!confirmAction) return

    const { type, post } = confirmAction
    setConfirmAction(null)

    try {
      setActivePostId(post.id)

      if (type === "delete") {
        await adminService.deletePost(post.id)
        const shouldStepBack = posts && posts.items.length === 1 && page > 1
        await refreshCurrentPage(shouldStepBack ? page - 1 : page)
        toast.success(t("admin.posts.deleted"))
      } else {
        await adminService.updatePostReview(post.id, { isReview: !post.isReview })
        await refreshCurrentPage()
        toast.success(t(post.isReview ? "admin.posts.review_reverted" : "admin.posts.review_approved"))
      }
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActivePostId(null)
    }
  }

  const getConfirmDialogContent = () => {
    if (!confirmAction) return null
    const { type, post } = confirmAction
    if (type === "delete") {
      return {
        title: t("admin.common.confirm_delete_title"),
        description: t("admin.common.confirm_delete_description", { item: post.title }),
        variant: "destructive" as const,
      }
    } else if (post.isReview) {
      return {
        title: t("admin.common.confirm_unapprove_title"),
        description: t("admin.common.confirm_unapprove_description", { item: post.title }),
        variant: "default" as const,
      }
    } else {
      return {
        title: t("admin.common.confirm_approve_title"),
        description: t("admin.common.confirm_approve_description", { item: post.title }),
        variant: "default" as const,
      }
    }
  }

  const confirmContent = getConfirmDialogContent()

  return (
    <div className="space-y-8">
      <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("admin.posts.title")}</h1>
          <p className="mt-1 text-muted-foreground">{t("admin.posts.subtitle")}</p>
        </div>

        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
          <input
            type="text"
            placeholder={t("admin.posts.search_placeholder")}
            className="w-full rounded-xl border border-border/50 bg-muted/50 py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 sm:w-64"
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              setPage(1)
            }}
          />
        </div>
      </header>

      {error && (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      <div className="overflow-hidden rounded-[2rem] border border-border/50 bg-card">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-muted/30 text-xs uppercase text-muted-foreground">
              <tr>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_title")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_author")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_tag")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_status")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_date")}</th>
                <th className="px-6 py-4 text-right font-semibold">{t("admin.posts.table_actions")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/50">
              {isLoading ? (
                Array.from({ length: 6 }).map((_, index) => (
                  <tr key={index}>
                    <td className="px-6 py-5" colSpan={6}>
                      <div className="h-10 animate-pulse rounded-xl bg-muted/50" />
                    </td>
                  </tr>
                ))
              ) : posts && posts.items.length > 0 ? (
                posts.items.map((post) => {
                  const isBusy = activePostId === post.id
                  return (
                    <tr key={post.id} className="transition-colors hover:bg-muted/30">
                      <td className="px-6 py-4">
                        <div className="flex flex-col">
                          <span className="line-clamp-1 font-bold text-foreground">{post.title}</span>
                          <span className="text-xs text-muted-foreground">{t("admin.posts.comments_unavailable")}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {post.author?.name || t("common.null")}
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex flex-col gap-2">
                          <select
                            value={post.tagId ?? ""}
                            disabled={isBusy}
                            onChange={(event) => void handleTagChange(post, event.target.value)}
                            className="w-36 rounded-lg border border-border/50 bg-muted/30 px-2 py-1 text-xs outline-none focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            <option value="">{t("post.tag_none")}</option>
                            {tags.map((tag) => (
                              <option key={tag.id} value={tag.id}>
                                {tag.name}{tag.isActive ? "" : ` (${t("admin.tags.inactive")})`}
                              </option>
                            ))}
                          </select>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        {post.isReview ? (
                          <span className="rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-bold text-emerald-500">
                            {t("admin.posts.status_active")}
                          </span>
                        ) : (
                          <span className="rounded-full bg-amber-500/10 px-2.5 py-1 text-xs font-bold text-amber-500">
                            {t("admin.posts.status_pending")}
                          </span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {formatLocalDate(post.createdAt, i18n.resolvedLanguage)}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={isBusy}
                            title={post.isReview ? t("admin.posts.action_unapprove") : t("admin.posts.action_approve")}
                            onClick={() => setConfirmAction({ type: "review", post })}
                          >
                            {post.isReview ? <XCircle size={18} /> : <CheckCircle size={18} />}
                          </Button>
                          <Link
                            to={`/post/${post.id}`}
                            className="inline-flex size-8 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            title={t("admin.posts.action_view")}
                          >
                            <Eye size={18} />
                          </Link>
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={isBusy}
                            title={t("admin.posts.action_delete")}
                            onClick={() => setConfirmAction({ type: "delete", post })}
                          >
                            <Trash2 size={18} className="text-destructive" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  )
                })
              ) : (
                <tr>
                  <td className="px-6 py-12 text-center text-muted-foreground" colSpan={6}>
                    {t("admin.posts.no_posts_found", { searchTerm })}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between border-t border-border/50 px-6 py-4 text-sm">
          <span className="text-muted-foreground">
            {posts ? t("admin.common.total_count", { count: posts.totalCount }) : ""}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!posts?.hasPreviousPage || isLoading}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("admin.common.previous")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!posts?.hasNextPage || isLoading}
              onClick={() => setPage((current) => current + 1)}
            >
              {t("admin.common.next")}
            </Button>
          </div>
        </div>
      </div>

      <AlertDialog open={confirmAction !== null} onOpenChange={(open) => { if (!open) setConfirmAction(null) }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirmContent?.title}</AlertDialogTitle>
            <AlertDialogDescription>{confirmContent?.description}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("admin.common.cancel")}</AlertDialogCancel>
            <AlertDialogAction
              variant={confirmContent?.variant}
              onClick={() => void handleConfirm()}
            >
              {t("admin.common.confirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
