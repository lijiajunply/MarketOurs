import { useEffect } from "react"
import { useSelector, useDispatch } from "react-redux"
import { RootState, AppDispatch } from "../../stores"
import { fetchNotifications, markReadLocal, markAllReadLocal } from "../../stores/notificationSlice"
import { notificationService } from "../../services/notificationService"
import { NotificationType } from "../../types"
import { Bell, MessageSquare, Reply, Flame, Check, Mail } from "lucide-react"
import { formatDistanceToNow } from "date-fns"
import { zhCN } from "date-fns/locale"
import { Link } from "react-router"
import { cn } from "../../lib/utils"

export default function NotificationsPage() {
  const { notifications, loading, error } = useSelector((state: RootState) => state.notification)
  const dispatch = useDispatch<AppDispatch>()

  useEffect(() => {
    dispatch(fetchNotifications({ pageIndex: 1, pageSize: 50 }))
  }, [dispatch])

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
        {notifications.length > 0 && (
          <button
            onClick={handleMarkAllAsRead}
            className="flex items-center gap-2 text-sm text-muted-foreground hover:text-primary transition-colors bg-muted/50 px-4 py-2 rounded-full"
          >
            <Check size={16} />
            全部标记为已读
          </button>
        )}
      </div>

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

      {error && (
        <div className="mt-8 p-4 bg-destructive/10 text-destructive rounded-xl text-center text-sm">
          {error}
        </div>
      )}
    </div>
  )
}
