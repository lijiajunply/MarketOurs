import { useEffect, useState } from "react"
import { Plus, Save, Tag, ToggleLeft, ToggleRight } from "lucide-react"
import { useTranslation } from "react-i18next"
import { adminService } from "../../services/adminService"
import { extractUserMessage } from "../../services/errorCodes"
import type { PostTagDto } from "../../types"
import { PostTagBadge } from "../../components/post/PostTagBadge"

const DEFAULT_COLOR = "#64748b"

export default function AdminTagsPage() {
  const { t } = useTranslation()
  const [tags, setTags] = useState<PostTagDto[]>([])
  const [name, setName] = useState("")
  const [color, setColor] = useState(DEFAULT_COLOR)
  const [isLoading, setIsLoading] = useState(true)
  const [activeId, setActiveId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const loadTags = async () => {
    const response = await adminService.getPostTags()
    setTags(response.data ?? [])
  }

  useEffect(() => {
    const run = async () => {
      try {
        setIsLoading(true)
        setError(null)
        await loadTags()
      } catch (err) {
        setError(extractUserMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void run()
  }, [t])

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!name.trim()) return

    try {
      setActiveId("new")
      setError(null)
      setMessage(null)
      await adminService.createPostTag({ name: name.trim(), color })
      setName("")
      setColor(DEFAULT_COLOR)
      await loadTags()
      setMessage(t("admin.tags.created"))
    } catch (err) {
      setError(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveId(null)
    }
  }

  const handleUpdate = async (tag: PostTagDto, updates: Partial<Pick<PostTagDto, "name" | "color" | "isActive">>) => {
    const next = { ...tag, ...updates }
    try {
      setActiveId(tag.id)
      setError(null)
      setMessage(null)
      await adminService.updatePostTagDefinition(tag.id, {
        name: next.name,
        color: next.color,
        isActive: next.isActive,
      })
      await loadTags()
      setMessage(t("admin.tags.updated"))
    } catch (err) {
      setError(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveId(null)
    }
  }

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t("admin.tags.title")}</h1>
        <p className="mt-1 text-muted-foreground">{t("admin.tags.subtitle")}</p>
      </header>

      {error ? (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : null}
      {message ? (
        <div className="rounded-2xl border border-primary/20 bg-primary/10 px-4 py-3 text-sm text-primary">
          {message}
        </div>
      ) : null}

      <form onSubmit={handleCreate} className="rounded-[2rem] border border-border/50 bg-card p-5">
        <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_160px_auto]">
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder={t("admin.tags.name_placeholder")}
            className="h-11 rounded-xl border border-border/50 bg-muted/40 px-4 text-sm outline-none focus:ring-2 focus:ring-primary/20"
            maxLength={32}
          />
          <input
            value={color}
            onChange={(event) => setColor(event.target.value)}
            placeholder="#64748b"
            className="h-11 rounded-xl border border-border/50 bg-muted/40 px-4 text-sm outline-none focus:ring-2 focus:ring-primary/20"
            maxLength={32}
          />
          <button
            type="submit"
            disabled={activeId === "new" || !name.trim()}
            className="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-primary px-5 text-sm font-bold text-primary-foreground disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Plus size={16} />
            {t("admin.tags.create")}
          </button>
        </div>
      </form>

      <div className="overflow-hidden rounded-[2rem] border border-border/50 bg-card">
        {isLoading ? (
          <div className="p-8 text-center text-muted-foreground">{t("common.loading")}</div>
        ) : tags.length === 0 ? (
          <div className="p-8 text-center text-muted-foreground">{t("admin.tags.empty")}</div>
        ) : (
          <div className="divide-y divide-border/50">
            {tags.map((tag) => {
              const isBusy = activeId === tag.id
              return (
                <div key={tag.id} className="grid gap-3 p-4 md:grid-cols-[180px_minmax(0,1fr)_150px_auto] md:items-center">
                  <PostTagBadge tag={tag} />
                  <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_150px]">
                    <input
                      defaultValue={tag.name}
                      maxLength={32}
                      className="h-10 rounded-xl border border-border/50 bg-muted/30 px-3 text-sm outline-none focus:ring-2 focus:ring-primary/20"
                      onBlur={(event) => {
                        const nextName = event.target.value.trim()
                        if (nextName && nextName !== tag.name) void handleUpdate(tag, { name: nextName })
                      }}
                    />
                    <input
                      defaultValue={tag.color}
                      maxLength={32}
                      className="h-10 rounded-xl border border-border/50 bg-muted/30 px-3 text-sm outline-none focus:ring-2 focus:ring-primary/20"
                      onBlur={(event) => {
                        const nextColor = event.target.value.trim()
                        if (nextColor && nextColor !== tag.color) void handleUpdate(tag, { color: nextColor })
                      }}
                    />
                  </div>
                  <span className={`inline-flex w-fit items-center gap-2 rounded-full px-3 py-1 text-xs font-bold ${
                    tag.isActive ? "bg-emerald-500/10 text-emerald-500" : "bg-muted text-muted-foreground"
                  }`}>
                    <Tag size={14} />
                    {tag.isActive ? t("admin.tags.active") : t("admin.tags.inactive")}
                  </span>
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => void handleUpdate(tag, { isActive: !tag.isActive })}
                    className="inline-flex h-10 items-center justify-center gap-2 rounded-xl border border-border/50 px-4 text-sm font-semibold transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {tag.isActive ? <ToggleRight size={17} /> : <ToggleLeft size={17} />}
                    {tag.isActive ? t("admin.tags.deactivate") : t("admin.tags.activate")}
                  </button>
                </div>
              )
            })}
          </div>
        )}
      </div>

      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <Save size={16} />
        {t("admin.tags.blur_hint")}
      </p>
    </div>
  )
}
