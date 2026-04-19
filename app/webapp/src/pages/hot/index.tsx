import { AlertCircle, Eye, Flame, Heart, Loader2, RefreshCw } from "lucide-react"
import { useEffect, useEffectEvent, useState } from "react"
import { useTranslation } from "react-i18next"
import { useNavigate } from "react-router"
import { postService } from "../../services/postService"
import type { PostDto } from "../../types"
import { cn } from "../../lib/utils"
import { formatPostRelativeDate, getPostAuthorName, getPostExcerpt } from "../../lib/postDisplay"

const hotRanks = ["01", "02", "03"]

export default function HotPage() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const [posts, setPosts] = useState<PostDto[]>([])
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadHotPosts = useEffectEvent(async (isRefresh = false) => {
    if (isRefresh) {
      setRefreshing(true)
    } else {
      setLoading(true)
    }

    setError(null)

    try {
      const response = await postService.getHotPosts(10)
      setPosts(response.data ?? [])
    } catch (err) {
      console.error(err)
      setError(t("hot.fetch_error"))
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  })

  useEffect(() => {
    void loadHotPosts()
  }, [])

  return (
    <div className="mx-auto max-w-4xl space-y-8 pb-20">
      <section className="overflow-hidden rounded-[2rem] border border-orange-200/70 bg-[radial-gradient(circle_at_top_left,_rgba(251,146,60,0.32),_transparent_45%),linear-gradient(135deg,_rgba(255,247,237,1),_rgba(255,255,255,1))] p-8 shadow-lg shadow-orange-100/60">
        <div className="flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
          <div className="space-y-4">
            <span className="inline-flex items-center gap-2 rounded-full bg-white/85 px-4 py-2 text-sm font-semibold text-orange-600 shadow-sm">
              <Flame size={16} />
              {t("hot.badge")}
            </span>
            <div className="space-y-2">
              <h1 className="text-4xl font-black tracking-tight text-slate-900">
                {t("hot.title")}
              </h1>
              <p className="max-w-2xl text-sm leading-6 text-slate-600 sm:text-base">
                {t("hot.subtitle")}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={() => void loadHotPosts(true)}
            disabled={refreshing}
            className="inline-flex items-center justify-center gap-2 rounded-full bg-slate-950 px-5 py-3 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-70"
          >
            {refreshing ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
            {refreshing ? t("hot.refreshing") : t("hot.refresh")}
          </button>
        </div>
      </section>

      {loading ? (
        <div className="flex min-h-72 items-center justify-center rounded-[2rem] border border-border/50 bg-card">
          <div className="flex items-center gap-3 text-muted-foreground">
            <Loader2 size={20} className="animate-spin" />
            <span>{t("common.loading")}</span>
          </div>
        </div>
      ) : error ? (
        <div className="rounded-[2rem] border border-destructive/20 bg-destructive/5 p-8 text-center">
          <div className="mx-auto flex max-w-md flex-col items-center gap-4">
            <div className="rounded-full bg-white p-3 text-destructive shadow-sm">
              <AlertCircle size={24} />
            </div>
            <div className="space-y-2">
              <h2 className="text-xl font-bold">{t("hot.error_title")}</h2>
              <p className="text-sm text-muted-foreground">{error}</p>
            </div>
            <button
              type="button"
              onClick={() => void loadHotPosts()}
              className="inline-flex items-center gap-2 rounded-full bg-foreground px-5 py-3 text-sm font-semibold text-background transition hover:opacity-90"
            >
              <RefreshCw size={16} />
              {t("hot.retry")}
            </button>
          </div>
        </div>
      ) : posts.length === 0 ? (
        <div className="rounded-[2rem] border border-dashed border-border bg-card/70 p-10 text-center">
          <div className="mx-auto max-w-md space-y-3">
            <p className="text-xl font-bold text-foreground">{t("hot.empty_title")}</p>
            <p className="text-sm text-muted-foreground">{t("hot.empty_desc")}</p>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {posts.map((post, index) => {
            const isTopThree = index < 3
            const authorName = getPostAuthorName(post, t("common.user"))
            const coverImage = post.images?.[0]

            return (
              <article
                key={post.id}
                onClick={() => navigate(`/post/${post.id}`)}
                className={cn(
                  "group cursor-pointer overflow-hidden rounded-[2rem] border transition-all duration-300 hover:-translate-y-0.5 hover:shadow-xl",
                  isTopThree
                    ? "border-orange-200/80 bg-[linear-gradient(135deg,_rgba(255,247,237,0.95),_rgba(255,255,255,1))] shadow-orange-100/70"
                    : "border-border/60 bg-card hover:border-orange-200/80"
                )}
              >
                <div className="grid gap-0 md:grid-cols-[minmax(0,1fr)_220px]">
                  <div className="p-6 sm:p-7">
                    <div className="mb-5 flex items-start justify-between gap-4">
                      <div className="flex items-center gap-3">
                        <div
                          className={cn(
                            "flex h-14 w-14 items-center justify-center rounded-2xl text-lg font-black tracking-[0.2em]",
                            isTopThree
                              ? "bg-orange-500 text-white shadow-lg shadow-orange-200"
                              : "bg-slate-100 text-slate-600"
                          )}
                        >
                          {isTopThree ? hotRanks[index] : String(index + 1).padStart(2, "0")}
                        </div>
                        <div className="space-y-1">
                          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-orange-500">
                            {t("hot.rank_label", { rank: index + 1 })}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            {authorName} · {formatPostRelativeDate(post.createdAt, i18n, post.updatedAt, t("post.edited"))}
                          </p>
                        </div>
                      </div>
                      <div className="rounded-full bg-white/80 px-3 py-1 text-xs font-semibold text-orange-600 shadow-sm">
                        {t("hot.heat_label")}
                      </div>
                    </div>

                    <div className="space-y-3">
                      <h2 className="text-2xl font-black tracking-tight text-slate-900 transition-colors group-hover:text-orange-600">
                        {post.title}
                      </h2>
                      <p className="line-clamp-3 whitespace-pre-wrap text-sm leading-6 text-slate-600 sm:text-base">
                        {getPostExcerpt(post.content, 180)}
                      </p>
                    </div>

                    <div className="mt-6 flex flex-wrap items-center gap-3 text-sm">
                      <div className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-2 text-slate-700 shadow-sm">
                        <Heart size={16} className="text-rose-500" />
                        <span className="font-semibold">{post.likes}</span>
                      </div>
                      <div className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-2 text-slate-700 shadow-sm">
                        <Eye size={16} className="text-sky-500" />
                        <span className="font-semibold">{post.watch}</span>
                      </div>
                    </div>
                  </div>

                  <div className="relative min-h-48 bg-slate-100">
                    {coverImage ? (
                      <img
                        src={coverImage}
                        alt={post.title}
                        className="h-full w-full object-cover transition duration-500 group-hover:scale-[1.04]"
                      />
                    ) : (
                      <div className="flex h-full min-h-48 items-center justify-center bg-[radial-gradient(circle_at_top_left,_rgba(251,146,60,0.3),_transparent_45%),linear-gradient(135deg,_rgba(15,23,42,0.96),_rgba(30,41,59,0.92))] p-6 text-left text-white">
                        <div className="space-y-3">
                          <Flame size={26} className="text-orange-300" />
                          <p className="text-lg font-bold">{t("hot.cover_fallback")}</p>
                          <p className="text-sm text-slate-300">{t("hot.cover_hint")}</p>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </article>
            )
          })}
        </div>
      )}
    </div>
  )
}
