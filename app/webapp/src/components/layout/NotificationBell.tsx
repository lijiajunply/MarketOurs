import { Bell } from "lucide-react"
import { useSelector, useDispatch } from "react-redux"
import { Link } from "react-router"
import { useEffect } from "react"
import type { RootState, AppDispatch } from "../../stores"
import { fetchUnreadCount } from "../../stores/notificationSlice"
import { cn } from "../../lib/utils"

export function NotificationBell() {
  const { unreadCount } = useSelector((state: RootState) => state.notification)
  const { isAuthenticated } = useSelector((state: RootState) => state.auth)
  const dispatch = useDispatch<AppDispatch>()

  useEffect(() => {
    if (isAuthenticated) {
      dispatch(fetchUnreadCount())
      
      // Optional: Set up polling or WebSocket for real-time notifications
      const interval = setInterval(() => {
        dispatch(fetchUnreadCount())
      }, 60000) // Every minute

      return () => clearInterval(interval)
    }
  }, [isAuthenticated, dispatch])

  if (!isAuthenticated) return null

  return (
    <Link
      to="/notifications"
      className="relative p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground hover:text-foreground"
      aria-label="Notifications"
    >
      <Bell size={20} />
      {unreadCount > 0 && (
        <span className={cn(
          "absolute top-1 right-1 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-white",
          unreadCount > 9 && "w-5 px-1"
        )}>
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      )}
    </Link>
  )
}
