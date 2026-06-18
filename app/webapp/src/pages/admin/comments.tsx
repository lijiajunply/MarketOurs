import { useEffect, useState } from "react"
import { Search, Eye, Trash2, CheckCircle, XCircle } from "lucide-react"
import { useTranslation } from "react-i18next"
import { Link } from "react-router"
import { adminService } from "../../services/adminService"
import { extractUserMessage } from "../../services/errorCodes"
import { toast } from "../../lib/toast"
import type { CommentDto, PagedResult } from "../../types"
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

export default function AdminCommentsPage() {
  const { t, i18n } = useTranslation()
  const [searchTerm, setSearchTerm] = useState("")
  const [page, setPage] = useState(1)
  const [comments, setComments] = useState<PagedResult<CommentDto> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeCommentId, setActiveCommentId] = useState<string | null>(null)
  const [confirmAction, setConfirmAction] = useState<{ type: "delete" | "review"; comment: CommentDto } | null>(null)

  useEffect(() => {
    const fetchComments = async () => {
      try {
        setIsLoading(true)
        setError(null)

        const response = searchTerm.trim()
          ? await adminService.searchComments(page, PAGE_SIZE, searchTerm.trim())
          : await adminService.getComments(page, PAGE_SIZE)

        setComments(response.data)
      } catch (err) {
        setError(extractUserMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchComments()
  }, [page, searchTerm, t])

  const refreshCurrentPage = async (nextPage = page) => {
    try {
      const response = searchTerm.trim()
        ? await adminService.searchComments(nextPage, PAGE_SIZE, searchTerm.trim())
        : await adminService.getComments(nextPage, PAGE_SIZE)

      setComments(response.data)
      setPage(nextPage)
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.load_error")))
    }
  }

  const handleConfirm = async () => {
    if (!confirmAction) return

    const { type, comment } = confirmAction
    setConfirmAction(null)

    try {
      setActiveCommentId(comment.id)

      if (type === "delete") {
        await adminService.deleteComment(comment.id)
        const shouldStepBack = comments && comments.items.length === 1 && page > 1
        await refreshCurrentPage(shouldStepBack ? page - 1 : page)
        toast.success(t("admin.comments.deleted"))
      } else {
        await adminService.updateCommentReview(comment.id, { isReview: !comment.isReview })
        await refreshCurrentPage()
        toast.success(t(comment.isReview ? "admin.comments.review_reverted" : "admin.comments.review_approved"))
      }
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveCommentId(null)
    }
  }

  const getConfirmDialogContent = () => {
    if (!confirmAction) return null
    const { type, comment } = confirmAction

    if (type === "delete") {
      return {
        title: t("admin.common.confirm_delete_title"),
        description: t("admin.common.confirm_delete_description", { item: t("admin.comments.table_content") }),
        variant: "destructive" as const,
      }
    } else if (comment.isReview) {
      return {
        title: t("admin.common.confirm_unapprove_title"),
        description: t("admin.common.confirm_unapprove_description", { item: t("admin.comments.table_content") }),
        variant: "default" as const,
      }
    } else {
      return {
        title: t("admin.common.confirm_approve_title"),
        description: t("admin.common.confirm_approve_description", { item: t("admin.comments.table_content") }),
        variant: "default" as const,
      }
    }
  }

  const confirmContent = getConfirmDialogContent()

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
                        {formatLocalDate(comment.createdAt, i18n.resolvedLanguage)}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={isBusy}
                            title={comment.isReview ? t("admin.comments.action_unapprove") : t("admin.comments.action_approve")}
                            onClick={() => setConfirmAction({ type: "review", comment })}
                          >
                            {comment.isReview ? <XCircle size={18} /> : <CheckCircle size={18} />}
                          </Button>
                          <Link
                            to={`/post/${comment.postId}`}
                            className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            title={t("admin.comments.action_view_post")}
                          >
                            <Eye size={18} />
                          </Link>
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={isBusy}
                            title={t("admin.comments.action_delete")}
                            onClick={() => setConfirmAction({ type: "delete", comment })}
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
            <Button
              variant="outline"
              size="sm"
              disabled={!comments.hasPreviousPage}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("admin.common.previous")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!comments.hasNextPage}
              onClick={() => setPage((current) => current + 1)}
            >
              {t("admin.common.next")}
            </Button>
          </div>
        </div>
      )}

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
