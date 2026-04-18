import { Link, useLocation } from "react-router"
import { cn } from "../../lib/utils"
import { LayoutDashboard, Users, FileText, Settings, Home, LogOut, Menu, Sun, Moon, ScrollText, ShieldBan, MessageSquare } from "lucide-react"
import { useState } from "react"
import { useTheme } from "../theme-provider"
import { useDispatch, useSelector } from "react-redux"
import { logout } from "../../stores/authSlice"
import type { RootState } from "../../stores"
import { useTranslation } from "react-i18next"

interface AdminLayoutProps {
  children: React.ReactNode
}

export function AdminLayout({ children }: AdminLayoutProps) {
  const { t } = useTranslation()
  const location = useLocation()
  const { theme, setTheme } = useTheme()
  const dispatch = useDispatch()
  const user = useSelector((state: RootState) => state.auth.user)
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)

  const navItems = [
    { name: t("admin.sidebar.dashboard"), href: "/admin", icon: LayoutDashboard },
    { name: t("admin.sidebar.users"), href: "/admin/users", icon: Users },
    { name: t("admin.sidebar.posts"), href: "/admin/posts", icon: FileText },
    { name: t("admin.sidebar.comments"), href: "/admin/comments", icon: MessageSquare },
    { name: t("admin.sidebar.logs"), href: "/admin/logs", icon: ScrollText },
    { name: t("admin.sidebar.blacklist"), href: "/admin/blacklist", icon: ShieldBan },
    { name: t("admin.sidebar.settings"), href: "/admin/settings", icon: Settings },
  ]

  const handleLogout = () => {
    dispatch(logout())
  }

  return (
    <div className="min-h-screen bg-background flex">
      {/* Sidebar (Desktop) */}
      <aside className="hidden lg:flex flex-col w-64 border-r border-border/40 bg-card/50 glass">
        <div className="h-16 flex items-center px-6 border-b border-border/40">
          <Link to="/admin" className="flex items-center gap-2">
            <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center shadow-lg shadow-primary/20">
              <span className="text-white font-bold text-lg">A</span>
            </div>
            <span className="font-bold text-lg tracking-tight">{t("admin.panel")}</span>
          </Link>
        </div>

        <nav className="flex-1 px-4 py-6 space-y-1">
          {navItems.map((item) => {
            const isActive = location.pathname === item.href
            return (
              <Link
                key={item.href}
                to={item.href}
                className={cn(
                  "flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium transition-colors",
                  isActive
                    ? "bg-primary/10 text-primary"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground"
                )}
              >
                <item.icon size={18} />
                {item.name}
              </Link>
            )
          })}
        </nav>

        <div className="p-4 border-t border-border/40 space-y-2">
          <Link
            to="/"
            className="flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
          >
            <Home size={18} />
            {t("admin.sidebar.back_to_site")}
          </Link>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Topbar */}
        <header className="h-16 flex items-center justify-between px-4 sm:px-6 lg:px-8 glass border-b border-border/40 sticky top-0 z-40">
          <div className="flex items-center gap-4 lg:hidden">
            <button
              onClick={() => setIsSidebarOpen(true)}
              className="p-2 -ml-2 rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
            >
              <Menu size={24} />
            </button>
            <span className="font-bold text-lg tracking-tight">{t("admin.topbar.admin")}</span>
          </div>

          <div className="flex items-center gap-2 ml-auto">
            <button
              onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
              className="p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground hover:text-foreground"
              aria-label="Toggle theme"
            >
              {theme === "dark" ? <Sun size={20} /> : <Moon size={20} />}
            </button>
            
            <div className="w-px h-6 bg-border/50 mx-2" />

            <div className="flex items-center gap-3 pl-2">
              <div className="flex flex-col items-end hidden sm:flex">
                <span className="text-sm font-bold leading-none">{user?.name}</span>
                <span className="text-xs text-muted-foreground">{t("admin.topbar.admin")}</span>
              </div>
              {user?.avatar ? (
                <img src={user.avatar} alt={user.name} className="w-8 h-8 rounded-full" />
              ) : (
                <div className="w-8 h-8 rounded-full bg-primary/20 text-primary flex items-center justify-center font-bold text-sm">
                  {user?.name?.[0]?.toUpperCase() || 'A'}
                </div>
              )}
              <button
                onClick={handleLogout}
                className="p-2 ml-1 rounded-full hover:bg-destructive/10 text-muted-foreground hover:text-destructive transition-colors"
                title={t("nav.logout")}
              >
                <LogOut size={18} />
              </button>
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8 animate-in fade-in duration-500 overflow-y-auto">
          <div className="max-w-6xl mx-auto">
            {children}
          </div>
        </main>
      </div>

      {/* Mobile Sidebar Overlay */}
      {isSidebarOpen && (
        <div className="fixed inset-0 z-50 lg:hidden flex">
          <div 
            className="fixed inset-0 bg-background/80 backdrop-blur-sm" 
            onClick={() => setIsSidebarOpen(false)}
          />
          <aside className="relative w-64 bg-card/95 glass border-r border-border/40 flex flex-col animate-in slide-in-from-left duration-300">
            <div className="h-16 flex items-center px-6 border-b border-border/40">
              <Link to="/admin" className="flex items-center gap-2" onClick={() => setIsSidebarOpen(false)}>
                <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center shadow-lg shadow-primary/20">
                  <span className="text-white font-bold text-lg">A</span>
                </div>
                <span className="font-bold text-lg tracking-tight">{t("admin.panel")}</span>
              </Link>
            </div>

            <nav className="flex-1 px-4 py-6 space-y-1">
              {navItems.map((item) => {
                const isActive = location.pathname === item.href
                return (
                  <Link
                    key={item.href}
                    to={item.href}
                    onClick={() => setIsSidebarOpen(false)}
                    className={cn(
                      "flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium transition-colors",
                      isActive
                        ? "bg-primary/10 text-primary"
                        : "text-muted-foreground hover:bg-muted hover:text-foreground"
                    )}
                  >
                    <item.icon size={18} />
                    {item.name}
                  </Link>
                )
              })}
            </nav>

            <div className="p-4 border-t border-border/40 space-y-2">
              <Link
                to="/"
                onClick={() => setIsSidebarOpen(false)}
                className="flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
              >
                <Home size={18} />
                {t("admin.sidebar.back_to_site")}
              </Link>
            </div>
          </aside>
        </div>
      )}
    </div>
  )
}
