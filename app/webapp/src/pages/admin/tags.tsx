import { useEffect, useState } from "react"
import { Pencil, Plus, Tag, ToggleLeft, ToggleRight } from "lucide-react"
import { useTranslation } from "react-i18next"
import { adminService } from "../../services/adminService"
import { extractUserMessage } from "../../services/errorCodes"
import { toast } from "../../lib/toast"
import type { PostTagDto } from "../../types"
import { PostTagBadge } from "../../components/post/PostTagBadge"
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "../../components/ui/dialog"

export default function AdminTagsPage() {
  const { t } = useTranslation()
  const [tags, setTags] = useState<PostTagDto[]>([])
  const [name, setName] = useState("")
  const [isLoading, setIsLoading] = useState(true)
  const [activeId, setActiveId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [confirmToggle, setConfirmToggle] = useState<PostTagDto | null>(null)
  const [editingTag, setEditingTag] = useState<PostTagDto | null>(null)
  const [editName, setEditName] = useState("")

  const loadTags = async () => {
    try {
      const response = await adminService.getPostTags()
      setTags(response.data ?? [])
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.load_error")))
    }
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
      await adminService.createPostTag({ name: name.trim() })
      setName("")
      await loadTags()
      toast.success(t("admin.tags.created"))
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveId(null)
    }
  }

  const handleUpdate = async (tag: PostTagDto, updates: Partial<Pick<PostTagDto, "name" | "isActive">>) => {
    const next = { ...tag, ...updates }
    try {
      setActiveId(tag.id)
      setError(null)
      await adminService.updatePostTagDefinition(tag.id, {
        name: next.name,
        isActive: next.isActive,
      })
      await loadTags()
      toast.success(t("admin.tags.updated"))
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveId(null)
    }
  }

  const handleConfirmToggle = async () => {
    if (!confirmToggle) return

    const tag = confirmToggle
    setConfirmToggle(null)

    await handleUpdate(tag, { isActive: !tag.isActive })
  }

  const handleOpenEdit = (tag: PostTagDto) => {
    setEditingTag(tag)
    setEditName(tag.name)
  }

  const handleSaveEdit = async () => {
    if (!editingTag || !editName.trim()) return

    const tag = editingTag
    const nextName = editName.trim()
    setEditingTag(null)

    if (nextName !== tag.name) {
      await handleUpdate(tag, { name: nextName })
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

      <form onSubmit={handleCreate} className="rounded-[2rem] border border-border/50 bg-card p-5">
        <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_auto]">
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder={t("admin.tags.name_placeholder")}
            className="h-11 rounded-xl border border-border/50 bg-muted/40 px-4 text-sm outline-none focus:ring-2 focus:ring-primary/20"
            maxLength={32}
          />
          <Button
            type="submit"
            disabled={activeId === "new" || !name.trim()}
          >
            <Plus size={16} />
            {t("admin.tags.create")}
          </Button>
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
                  <PostTagBadge tag={tag} clickable={false} />
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">{tag.name}</span>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      disabled={isBusy}
                      onClick={() => handleOpenEdit(tag)}
                      title={t("admin.tags.edit_name")}
                    >
                      <Pencil size={14} />
                    </Button>
                  </div>
                  <span className={`inline-flex w-fit items-center gap-2 rounded-full px-3 py-1 text-xs font-bold ${
                    tag.isActive ? "bg-emerald-500/10 text-emerald-500" : "bg-muted text-muted-foreground"
                  }`}>
                    <Tag size={14} />
                    {tag.isActive ? t("admin.tags.active") : t("admin.tags.inactive")}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={isBusy}
                    onClick={() => setConfirmToggle(tag)}
                    className="inline-flex h-10 items-center justify-center gap-2"
                  >
                    {tag.isActive ? <ToggleRight size={17} /> : <ToggleLeft size={17} />}
                    {tag.isActive ? t("admin.tags.deactivate") : t("admin.tags.activate")}
                  </Button>
                </div>
              )
            })}
          </div>
        )}
      </div>

      {/* Edit Tag Name Dialog */}
      <Dialog open={editingTag !== null} onOpenChange={(open) => { if (!open) setEditingTag(null) }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("admin.tags.edit_title")}</DialogTitle>
            <DialogDescription>{t("admin.tags.edit_description")}</DialogDescription>
          </DialogHeader>
          <input
            value={editName}
            onChange={(event) => setEditName(event.target.value)}
            placeholder={t("admin.tags.name_placeholder")}
            className="h-11 rounded-xl border border-border/50 bg-muted/40 px-4 text-sm outline-none focus:ring-2 focus:ring-primary/20"
            maxLength={32}
            onKeyDown={(event) => {
              if (event.key === "Enter") void handleSaveEdit()
            }}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditingTag(null)}>
              {t("admin.common.cancel")}
            </Button>
            <Button
              disabled={!editName.trim() || editName.trim() === editingTag?.name}
              onClick={() => void handleSaveEdit()}
            >
              {t("admin.tags.edit_save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Toggle Active/Inactive Confirmation Dialog */}
      <AlertDialog open={confirmToggle !== null} onOpenChange={(open) => { if (!open) setConfirmToggle(null) }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("admin.common.confirm_toggle_title")}</AlertDialogTitle>
            <AlertDialogDescription>
              {confirmToggle?.isActive
                ? t("admin.common.confirm_deactivate_description", { item: confirmToggle?.name })
                : t("admin.common.confirm_activate_description", { item: confirmToggle?.name })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("admin.common.cancel")}</AlertDialogCancel>
            <AlertDialogAction
              variant={confirmToggle?.isActive ? "destructive" : "default"}
              onClick={() => void handleConfirmToggle()}
            >
              {t("admin.common.confirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
