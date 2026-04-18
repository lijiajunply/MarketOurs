import { useEffect, useState } from "react"
import { Search, Eye, Trash2, CheckCircle, XCircle } from "lucide-react"
import { useTranslation } from "react-i18next"
import { Link } from "react-router"
import { adminService } from "../../services/adminService"
import type { CommentDto, PagedResult } from "../../types"

const PAGE_SIZE = 10

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === "object" && error !== null && "message" in error && typeof error.message === "string") {
    return error.message
  }

  return fallback
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(value))
}

export default function AdminCommentsPage() {
  const { t } = useTranslation()
  const [searchTerm, setSearchTerm] = useState("")
  const [page, setPage] = useState(1)
  const [comments, setComments] = useState<PagedResult<CommentDto> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [activeCommentId, setActiveCommentId] = useState<string | null>(null)

  useEffect(() => {
    const fetchComments = async () => {
      try {
        setIsLoading(true)
        setError(null)
        setMessage(null)

        const response = searchTerm.trim()
          ? await adminService.searchComments(page, PAGE_SIZE, searchTerm.trim())
          : await adminService.getComments(page, PAGE_SIZE)

        setComments(response.data)
      } catch (err) {
        setError(getErrorMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchComments()
  }, [page, searchTerm, t])

  const refreshCurrentPage = async (nextPage = page) => {
    const response = searchTerm.trim()
      ? await adminService.searchComments(nextPage, PAGE_SIZE, searchTerm.trim())
      : await adminService.getComments(nextPage, PAGE_SIZE)

    setComments(response.data)
    setPage(nextPage)
  }

  const handleDelete = async (comment: CommentDto) => {
    try {
      setActiveCommentId(comment.id)
      setMessage(null)
      setError(null)
      await adminService.deleteComment(comment.id)

      const shouldStepBack = comments && comments.items.length === 1 && page > 1
      await refreshCurrentPage(shouldStepBack ? page - 1 : page)
      setMessage(t("admin.comments.deleted"))
    } catch (err) {
      setError(getErrorMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveCommentId(null)
    }
  }

  const handleToggleReview = async (comment: CommentDto) => {
    try {
      setActiveCommentId(comment.id)
      setMessage(null)
      setError(null)
      await adminService.updateCommentReview(comment.id, { isReview: !comment.isReview })

      await refreshCurrentPage()
      setMessage(t(comment.isReview ? "admin.comments.review_reverted" : "admin.comments.review_approved"))
    } catch (err) {
      setError(getErrorMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveCommentId(null)
    }
  }

  return (
    <div className="space-y-8">
      <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("admin.comments.title")}</h1>
          <p className="mt-1 text-muted-foreground">{t("admin.comments.subtitle")}</p>
        </div>

        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
          <input
            type="text"
            placeholder={t("admin.comments.search_placeholder")}
            className="w-full rounded-xl border border-border/50 bg-muted/50 py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 sm:w-64"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value)
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

      {message && (
        <div className="rounded-2xl border border-primary/20 bg-primary/10 px-4 py-3 text-sm text-primary">
          {message}
        </div>
      )}

      <div className="overflow-hidden rounded-[2rem] border border-border/50 bg-card">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-muted/30 text-xs uppercase text-muted-foreground">
              <tr>
                <th className="px-6 py-4 font-semibold">{t("admin.comments.table_content")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.comments.table_author")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.comments.table_post")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.comments.table_status")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.comments.table_date")}</th>
                <th className="px-6 py-4 text-right font-semibold">{t("admin.comments.table_actions")}</th>
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
              ) : comments && comments.items.length > 0 ? (
                comments.items.map((comment) => {
                  const isBusy = activeCommentId === comment.id
                  return (
                    <tr key={comment.id} className="transition-colors hover:bg-muted/30">
                      <td className="max-w-md px-6 py-4">
                        <p className="line-clamp-2 font-medium text-foreground">{comment.content}</p>
                      </td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {comment.author?.name || t("common.null")}
                      </td>
                      <td className="px-6 py-4">
                        <Link
                          to={`/post/${comment.postId}`}
                          className="text-primary transition-colors hover:text-primary/80"
                        >
                          {comment.postId.slice(0, 8)}
                        </Link>
                      </td>
                      <td className="px-6 py-4">
                        {comment.isReview ? (
                          <span className="rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-bold text-emerald-500">
                            {t("admin.comments.status_active")}
                          </span>
                        ) : (
                          <span className="rounded-full bg-amber-500/10 px-2.5 py-1 text-xs font-bold text-amber-500">
                            {t("admin.comments.status_pending")}
                          </span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {formatDate(comment.createdAt)}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <button
                            type="button"
                            disabled={isBusy}
                            className={`rounded-lg p-2 transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
                              comment.isReview
                                ? "text-muted-foreground hover:bg-amber-500/10 hover:text-amber-500"
                                : "text-muted-foreground hover:bg-emerald-500/10 hover:text-emerald-500"
                            }`}
                            title={comment.isReview ? t("admin.comments.action_unapprove") : t("admin.comments.action_approve")}
                            onClick={() => void handleToggleReview(comment)}
                          >
                            {comment.isReview ? <XCircle size={18} /> : <CheckCircle size={18} />}
                          </button>
                          <Link
                            to={`/post/${comment.postId}`}
                            className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            title={t("admin.comments.action_view_post")}
                          >
                            <Eye size={18} />
                          </Link>
                          <button
                            type="button"
                            disabled={isBusy}
                            className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive disabled:cursor-not-allowed disabled:opacity-50"
                            title={t("admin.comments.action_delete")}
                            onClick={() => void handleDelete(comment)}
                          >
                            <Trash2 size={18} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })
              ) : (
                <tr>
                  <td className="px-6 py-16 text-center text-muted-foreground" colSpan={6}>
                    {t("admin.comments.no_comments_found", { searchTerm })}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {comments && comments.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            {comments.totalCount} / {comments.totalPages}
          </p>
          <div className="flex items-center gap-2">
            <button
              type="button"
              className="rounded-xl border border-border px-4 py-2 text-sm transition-colors hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
              disabled={!comments.hasPreviousPage}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              Prev
            </button>
            <button
              type="button"
              className="rounded-xl border border-border px-4 py-2 text-sm transition-colors hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
              disabled={!comments.hasNextPage}
              onClick={() => setPage((current) => current + 1)}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
