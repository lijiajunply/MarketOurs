import { useEffect, useState } from "react"
import { useSelector, useDispatch } from "react-redux"
import { RootState, AppDispatch } from "../../stores"
import { fetchNotifications, markReadLocal, markAllReadLocal } from "../../stores/notificationSlice"
import { notificationService } from "../../services/notificationService"
import { NotificationType, PushSettingsDto } from "../../types"
import { Bell, MessageSquare, Reply, Flame, Check, Settings, Save, Loader2 } from "lucide-react"
import { formatDistanceToNow } from "date-fns"
import { zhCN } from "date-fns/locale"
import { Link } from "react-router"
import { cn } from "../../lib/utils"

export default function NotificationsPage() {
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
      console.error("Failed to load settings:", err)
    } finally {
      setSettingsLoading(false)
    }
  }

  const handleUpdateSettings = async () => {
    setSaving(true)
    try {
      await notificationService.updateSettings(settings)
    } catch (err) {
      console.error("Failed to update settings:", err)
    } finally {
      setSaving(false)
    }
  }

  const handleMarkAsRead = async (id: string) => {
    try {
      await notificationService.markAsRead(id)
      dispatch(markReadLocal(id))
    } catch (err) {
      console.error("Failed to mark as read:", err)
    }
  }

  const handleMarkAllAsRead = async () => {
    try {
      await notificationService.markAllAsRead()
      dispatch(markAllReadLocal())
    } catch (err) {
      console.error("Failed to mark all as read:", err)
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
        return <Bell size={18} className="text-gray-500" />
    }
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
          通知中心
        </h1>
        <div className="flex items-center gap-2">
          <div className="bg-muted p-1 rounded-full flex">
            <button
              onClick={() => setActiveTab("list")}
              className={cn(
                "px-4 py-1.5 rounded-full text-sm font-medium transition-all",
                activeTab === "list" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground"
              )}
            >
              通知列表
            </button>
            <button
              onClick={() => setActiveTab("settings")}
              className={cn(
                "px-4 py-1.5 rounded-full text-sm font-medium transition-all",
                activeTab === "settings" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground"
              )}
            >
              推送设置
            </button>
          </div>
          {activeTab === "list" && notifications.length > 0 && (
            <button
              onClick={handleMarkAllAsRead}
              className="p-2 text-muted-foreground hover:text-primary transition-colors bg-muted/50 rounded-full"
              title="全部标记为已读"
            >
              <Check size={18} />
            </button>
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
              <p className="text-muted-foreground">暂无通知</p>
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
                      <div className="mt-1 p-2 bg-white dark:bg-zinc-800 rounded-xl shadow-sm">
                        {getIcon(n.type)}
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center justify-between mb-1">
                          <h3 className="font-semibold">{n.title}</h3>
                          <span className="text-xs text-muted-foreground">
                            {formatDistanceToNow(new Date(n.createdAt), { addSuffix: true, locale: zhCN })}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                          {n.content}
                        </p>
                        <div className="flex items-center justify-between">
                          {link ? (
                            <Link
                              to={link}
                              onClick={() => handleMarkAsRead(n.id)}
                              className="text-xs font-medium text-primary hover:underline"
                            >
                              查看详情 →
                            </Link>
                          ) : (
                            <div />
                          )}
                          {!n.isRead && (
                            <button
                              onClick={() => handleMarkAsRead(n.id)}
                              className="text-xs text-muted-foreground hover:text-primary"
                            >
                              标记已读
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
                <h3 className="text-lg font-bold">邮件通知</h3>
                <p className="text-sm text-muted-foreground">当收到新回复或系统通知时向您的邮箱发送提醒</p>
              </div>
              <button
                onClick={() => setSettings({ ...settings, enableEmailNotifications: !settings.enableEmailNotifications })}
                className={cn(
                  "relative inline-flex h-6 w-11 items-center rounded-full transition-colors",
                  settings.enableEmailNotifications ? "bg-primary" : "bg-muted"
                )}
              >
                <span className={cn(
                  "inline-block h-4 w-4 transform rounded-full bg-white transition-transform shadow-sm",
                  settings.enableEmailNotifications ? "translate-x-6" : "translate-x-1"
                )} />
              </button>
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <h3 className="text-lg font-bold">评论回复推送</h3>
                <p className="text-sm text-muted-foreground">当有人回复您的贴子或评论时实时推送通知</p>
              </div>
              <button
                onClick={() => setSettings({ ...settings, enableCommentReplyPush: !settings.enableCommentReplyPush })}
                className={cn(
                  "relative inline-flex h-6 w-11 items-center rounded-full transition-colors",
                  settings.enableCommentReplyPush ? "bg-primary" : "bg-muted"
                )}
              >
                <span className={cn(
                  "inline-block h-4 w-4 transform rounded-full bg-white transition-transform shadow-sm",
                  settings.enableCommentReplyPush ? "translate-x-6" : "translate-x-1"
                )} />
              </button>
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <h3 className="text-lg font-bold">每日热榜推送</h3>
                <p className="text-sm text-muted-foreground">每天早晨 8 点接收校园内最热门的贴子精选</p>
              </div>
              <button
                onClick={() => setSettings({ ...settings, enableHotListPush: !settings.enableHotListPush })}
                className={cn(
                  "relative inline-flex h-6 w-11 items-center rounded-full transition-colors",
                  settings.enableHotListPush ? "bg-primary" : "bg-muted"
                )}
              >
                <span className={cn(
                  "inline-block h-4 w-4 transform rounded-full bg-white transition-transform shadow-sm",
                  settings.enableHotListPush ? "translate-x-6" : "translate-x-1"
                )} />
              </button>
            </div>
          </div>

          <button
            onClick={handleUpdateSettings}
            disabled={saving || settingsLoading}
            className="w-full flex items-center justify-center gap-2 bg-primary text-primary-foreground py-4 rounded-2xl font-bold hover:opacity-90 disabled:opacity-50 transition-all shadow-lg shadow-primary/20"
          >
            {saving ? <Loader2 className="animate-spin" size={20} /> : <Save size={20} />}
            保存设置
          </button>
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
