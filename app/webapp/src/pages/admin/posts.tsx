import { useEffect, useState } from "react"
import { Search, Eye, Trash2 } from "lucide-react"
import { useTranslation } from "react-i18next"
import { Link } from "react-router"
import { adminService } from "../../services/adminService"
import type { PagedResult, PostDto } from "../../types"

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

export default function AdminPostsPage() {
  const { t } = useTranslation()
  const [searchTerm, setSearchTerm] = useState("")
  const [page, setPage] = useState(1)
  const [posts, setPosts] = useState<PagedResult<PostDto> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [activePostId, setActivePostId] = useState<string | null>(null)

  useEffect(() => {
    const fetchPosts = async () => {
      try {
        setIsLoading(true)
        setError(null)
        setMessage(null)

        const response = searchTerm.trim()
          ? await adminService.searchPosts(page, PAGE_SIZE, searchTerm.trim())
          : await adminService.getPosts(page, PAGE_SIZE)

        setPosts(response.data)
      } catch (err) {
        setError(getErrorMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchPosts()
  }, [page, searchTerm, t])

  const refreshCurrentPage = async (nextPage = page) => {
    const response = searchTerm.trim()
      ? await adminService.searchPosts(nextPage, PAGE_SIZE, searchTerm.trim())
      : await adminService.getPosts(nextPage, PAGE_SIZE)

    setPosts(response.data)
    setPage(nextPage)
  }

  const handleDelete = async (post: PostDto) => {
    try {
      setActivePostId(post.id)
      setMessage(null)
      setError(null)
      await adminService.deletePost(post.id)

      const shouldStepBack = posts && posts.items.length === 1 && page > 1
      await refreshCurrentPage(shouldStepBack ? page - 1 : page)
      setMessage(t("admin.posts.deleted"))
    } catch (err) {
      setError(getErrorMessage(err, t("admin.common.action_error")))
    } finally {
      setActivePostId(null)
    }
  }

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
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_title")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_author")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_status")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_date")}</th>
                <th className="px-6 py-4 text-right font-semibold">{t("admin.posts.table_actions")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/50">
              {isLoading ? (
                Array.from({ length: 6 }).map((_, index) => (
                  <tr key={index}>
                    <td className="px-6 py-5" colSpan={5}>
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
                        <span className="rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-bold text-emerald-500">
                          {t("admin.posts.status_active")}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {formatDate(post.createdAt)}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Link
                            to={`/post/${post.id}`}
                            className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            title={t("admin.posts.action_view")}
                          >
                            <Eye size={18} />
                          </Link>
                          <button
                            type="button"
                            disabled={isBusy}
                            className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive disabled:cursor-not-allowed disabled:opacity-50"
                            title={t("admin.posts.action_delete")}
                            onClick={() => void handleDelete(post)}
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
                  <td className="px-6 py-12 text-center text-muted-foreground" colSpan={5}>
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
            <button
              type="button"
              disabled={!posts?.hasPreviousPage || isLoading}
              className="rounded-xl border border-border/50 px-4 py-2 disabled:cursor-not-allowed disabled:opacity-50"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("admin.common.previous")}
            </button>
            <button
              type="button"
              disabled={!posts?.hasNextPage || isLoading}
              className="rounded-xl border border-border/50 px-4 py-2 disabled:cursor-not-allowed disabled:opacity-50"
              onClick={() => setPage((current) => current + 1)}
            >
              {t("admin.common.next")}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
