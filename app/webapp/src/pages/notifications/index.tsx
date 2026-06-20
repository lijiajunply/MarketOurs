import { useEffect, useState } from "react"
import { useSelector, useDispatch } from "react-redux"
import type { RootState, AppDispatch } from "@/stores"
import { fetchNotifications, markReadLocal, markAllReadLocal } from "@/stores/notificationSlice"
import { notificationService } from "@/services/notificationService"
import { NotificationType, type PushSettingsDto, type NotificationParams } from "@/types"
import { Bell, MessageSquare, Reply, Flame, Check, Save, Loader2 } from "lucide-react"
import { Link } from "react-router"
import { cn } from "@/lib/utils"
import { toast } from "@/lib/toast"
import { useTranslation } from "react-i18next"
import { formatRelativeDateFromNow } from "@/lib/dateTime"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"

export default function NotificationsPage() {
  const { t, i18n } = useTranslation()
  const { notifications, loading, error } = useSelector((state: RootState) => state.notification)
  const dispatch = useDispatch<AppDispatch>()
  
  const [activeTab, setActiveTab] = useState<"list" | "settings">("list")
  const [settings, setSettings] = useState<PushSettingsDto>({
    enableEmailNotifications: true,
    enableHotListPush: true,
    enableCommentReplyPush: true
  })
  const [settingsLoading, setSettingsLoading] = useState(false)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (activeTab === "list") {
      dispatch(fetchNotifications({ pageIndex: 1, pageSize: 50 }))
    } else {
      loadSettings()
    }
  }, [dispatch, activeTab])

  const loadSettings = async () => {
    setSettingsLoading(true)
    try {
      const res = await notificationService.getSettings()
      if (res.data) setSettings(res.data)
    } catch (err) {
      toast.error(t("notifications.settings_load_error"))
    } finally {
      setSettingsLoading(false)
    }
  }

  const handleUpdateSettings = async () => {
    setSaving(true)
    try {
      await notificationService.updateSettings(settings)
      toast.success(t("notifications.settings_saved"))
    } catch (err) {
      toast.error(t("notifications.settings_save_error"))
    } finally {
      setSaving(false)
    }
  }

  const handleMarkAsRead = async (id: string) => {
    try {
      await notificationService.markAsRead(id)
      dispatch(markReadLocal(id))
    } catch (err) {
      toast.error(t("notifications.mark_read_error"))
    }
  }

  const handleMarkAllAsRead = async () => {
    try {
      await notificationService.markAllAsRead()
      dispatch(markAllReadLocal())
    } catch (err) {
      toast.error(t("notifications.mark_read_error"))
    }
  }

  const getIcon = (type: NotificationType) => {
    switch (type) {
      case NotificationType.CommentReply:
        return <Reply size={18} className="text-blue-500" />
      case NotificationType.PostReply:
        return <MessageSquare size={18} className="text-green-500" />
      case NotificationType.HotList:
        return <Flame size={18} className="text-orange-500" />
      default:
        return <Bell size={18} className="text-muted-foreground" />
    }
  }

  const formatTitle = (type: NotificationType) => {
    switch (type) {
      case NotificationType.CommentReply:
        return t("notifications.types.comment_reply.title")
      case NotificationType.PostReply:
        return t("notifications.types.post_reply.title")
      case NotificationType.HotList:
        return t("notifications.types.hot_list.title")
      case NotificationType.Review:
        return t("notifications.types.review.title")
      case NotificationType.System:
        return t("notifications.types.system.title")
      default:
        return t("notifications.title")
    }
  }

  const formatContent = (type: NotificationType, p?: NotificationParams) => {
    switch (type) {
      case NotificationType.CommentReply:
        if (p?.$type === "commentReply") {
          return t("notifications.types.comment_reply.content", {
            commenterName: p.commenterName || "",
            bodySnippet: p.bodySnippet || "",
          })
        }
        break
      case NotificationType.PostReply:
        if (p?.$type === "postReply") {
          return t("notifications.types.post_reply.content", {
            commenterName: p.commenterName || "",
            bodySnippet: p.bodySnippet || "",
          })
        }
        break
      case NotificationType.Review:
        if (p?.$type === "review") {
          const entityType = p.entityType || "post"
          if (p.approved) {
            return t("notifications.types.review.approved", {
              entity: t(`notifications.types.review.entity_${entityType}`),
              name: p.name || "",
            })
          }
          return t("notifications.types.review.rejected", {
            entity: t(`notifications.types.review.entity_${entityType}`),
            name: p.name || "",
            reason: p.reason || "",
          })
        }
        break
      case NotificationType.HotList:
        if (p?.$type === "hotList") {
          const header = t("notifications.types.hot_list.header")
          const list = (p.posts || []).map((post) => post.title).join(" / ")
          return `${header} ${list}`
        }
        break
      default:
        if (p?.$type === "system" && p.message) return p.message
        return ""
    }
    return ""
  }

  const getTargetLink = (notification: any) => {
    if (!notification.targetId) return null
    return `/post/${notification.targetId}`
  }

  return (
    <div className="max-w-3xl mx-auto py-8 px-4 animate-in fade-in slide-in-from-bottom-4 duration-700">
      <div className="flex items-center justify-between mb-8">
        <h1 className="text-3xl font-bold flex items-center gap-3">
          <Bell className="text-primary" />
          {t("notifications.title")}
        </h1>
        <div className="flex items-center gap-2">
          <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as "list" | "settings")}>
            <TabsList className="rounded-full">
              <TabsTrigger value="list" className="rounded-full text-sm">
                {t("notifications.list_tab")}
              </TabsTrigger>
              <TabsTrigger value="settings" className="rounded-full text-sm">
                {t("notifications.settings_tab")}
              </TabsTrigger>
            </TabsList>
          </Tabs>
          {activeTab === "list" && notifications.length > 0 && (
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={handleMarkAllAsRead}
              className="rounded-full text-muted-foreground hover:text-foreground"
              title={t("notifications.mark_all_read")}
            >
              <Check size={18} />
            </Button>
          )}
        </div>
      </div>

      {activeTab === "list" ? (
        <>
          {loading && notifications.length === 0 ? (
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-24 w-full bg-muted animate-pulse rounded-2xl" />
              ))}
            </div>
          ) : notifications.length === 0 ? (
            <div className="text-center py-20 glass rounded-3xl">
              <div className="h-16 w-16 bg-muted rounded-full flex items-center justify-center mx-auto mb-4">
                <Bell size={32} className="text-muted-foreground" />
              </div>
              <p className="text-muted-foreground">{t("notifications.empty")}</p>
            </div>
          ) : (
            <div className="space-y-4">
              {notifications.map((n) => {
                const link = getTargetLink(n)
                return (
                  <div
                    key={n.id}
                    className={cn(
                      "p-4 rounded-2xl border transition-all hover:shadow-md glass",
                      n.isRead ? "opacity-75 border-border/40" : "border-primary/20 bg-primary/5 shadow-sm"
                    )}
                  >
                    <div className="flex items-start gap-4">
                      <div className="mt-1 p-2 bg-card rounded-xl shadow-sm">
                        {getIcon(n.type)}
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center justify-between mb-1">
                          <h3 className="font-semibold">{formatTitle(n.type)}</h3>
                          <span className="text-xs text-muted-foreground">
                            {formatRelativeDateFromNow(n.createdAt, i18n.resolvedLanguage)}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                          {formatContent(n.type, n.params) || n.content}
                        </p>
                        <div className="flex items-center justify-between">
                          {link ? (
                            <Link
                              to={link}
                              onClick={() => handleMarkAsRead(n.id)}
                              className="text-xs font-medium text-primary hover:underline"
                            >
                              {t("notifications.view_detail")} →
                            </Link>
                          ) : (
                            <div />
                          )}
                          {!n.isRead && (
                            <button
                              onClick={() => handleMarkAsRead(n.id)}
                              className="text-xs text-muted-foreground hover:text-primary"
                            >
                              {t("notifications.mark_as_read")}
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </>
      ) : (
        <div className="glass rounded-[2rem] p-8 space-y-8">
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <h3 className="text-lg font-bold">{t("notifications.email_notifications")}</h3>
                <p className="text-sm text-muted-foreground">{t("notifications.email_notifications_desc")}</p>
              </div>
              <Switch
                checked={settings.enableEmailNotifications}
                onCheckedChange={(checked) => setSettings({ ...settings, enableEmailNotifications: checked })}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <h3 className="text-lg font-bold">{t("notifications.comment_push")}</h3>
                <p className="text-sm text-muted-foreground">{t("notifications.comment_push_desc")}</p>
              </div>
              <Switch
                checked={settings.enableCommentReplyPush}
                onCheckedChange={(checked) => setSettings({ ...settings, enableCommentReplyPush: checked })}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <h3 className="text-lg font-bold">{t("notifications.hotlist_push")}</h3>
                <p className="text-sm text-muted-foreground">{t("notifications.hotlist_push_desc")}</p>
              </div>
              <Switch
                checked={settings.enableHotListPush}
                onCheckedChange={(checked) => setSettings({ ...settings, enableHotListPush: checked })}
              />
            </div>
          </div>

          <Button
            onClick={handleUpdateSettings}
            disabled={saving || settingsLoading}
            size="lg"
            className="w-full rounded-2xl font-semibold shadow-md shadow-primary/20 gap-2"
          >
            {saving ? <Loader2 className="animate-spin" size={20} /> : <Save size={20} />}
            {saving ? t("notifications.saving") : t("notifications.save_settings")}
          </Button>
        </div>
      )}

      {error && activeTab === "list" && (
        <div className="mt-8 p-4 bg-destructive/10 text-destructive rounded-xl text-center text-sm">
          {error}
        </div>
      )}
    </div>
  )
}
