import { Link, useLocation } from "react-router"
import { cn } from "../../lib/utils"
import { useTheme } from "../theme-provider"
import { Sun, Moon, MessageSquare, User, Menu, LogIn, LogOut, PlusSquare, Languages, Bell } from "lucide-react"
import { useState } from "react"
import { useSelector, useDispatch } from "react-redux"
import { useTranslation } from "react-i18next"
import type { RootState } from "../../stores"
import { logout } from "../../stores/authSlice"
import { NotificationBell } from "./NotificationBell"

export function Navbar() {
  const { theme, setTheme } = useTheme()
  const { t, i18n } = useTranslation()
  const location = useLocation()
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const { isAuthenticated, user } = useSelector((state: RootState) => state.auth)
  const dispatch = useDispatch()

  const navItems = [
    { name: t("nav.home"), href: "/", icon: MessageSquare },
  ]

  const handleLogout = () => {
    dispatch(logout())
  }

  const toggleLanguage = () => {
    const newLang = i18n.language === 'en' ? 'zh' : 'en'
    i18n.changeLanguage(newLang)
  }

  return (
    <header className="sticky top-0 z-50 w-full glass border-b border-border/40">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <div className="flex items-center gap-8">
            <Link to="/" className="flex items-center space-x-2">
              <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center shadow-lg shadow-primary/20">
                <span className="text-white font-bold text-lg">M</span>
              </div>
              <span className="hidden font-semibold sm:inline-block text-lg tracking-tight">MarketOurs</span>
            </Link>

            <nav className="hidden md:flex items-center space-x-1">
              {navItems.map((item) => (
                <Link
                  key={item.href}
                  to={item.href}
                  className={cn(
                    "px-4 py-2 rounded-full text-sm font-medium transition-colors",
                    location.pathname === item.href || (item.href !== "/" && location.pathname.startsWith(item.href))
                      ? "bg-primary/10 text-primary"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground"
                  )}
                >
                  {item.name}
                </Link>
              ))}
              {isAuthenticated && (
                <Link
                  to="/post/create"
                  className={cn(
                    "px-4 py-2 rounded-full text-sm font-medium transition-colors flex items-center gap-1.5",
                    location.pathname === "/post/create"
                      ? "bg-primary/10 text-primary"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground"
                  )}
                >
                  <PlusSquare size={16} />
                  <span>{t('nav.home') === 'Home' ? 'Post' : '发布'}</span>
                </Link>
              )}
            </nav>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={toggleLanguage}
              className="p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground hover:text-foreground"
              aria-label="Toggle language"
              title={i18n.language === 'en' ? 'Switch to Chinese' : '切换为英文'}
            >
              <Languages size={20} />
            </button>

            <button
              onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
              className="p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground hover:text-foreground"
              aria-label="Toggle theme"
            >
              {theme === "dark" ? <Sun size={20} /> : <Moon size={20} />}
            </button>

            <NotificationBell />

            {isAuthenticated ? (
              <div className="flex items-center gap-2">
                <Link
                  to="/profile"
                  className="flex items-center gap-2 px-3 py-1.5 rounded-full hover:bg-muted transition-colors text-muted-foreground hover:text-foreground"
                >
                  {user?.avatar ? (
                    <img src={user.avatar} className="w-6 h-6 rounded-full" alt="" />
                  ) : (
                    <User size={20} />
                  )}
                  <span className="text-sm font-medium hidden sm:inline-block">{user?.name}</span>
                </Link>
                <button
                  onClick={handleLogout}
                  className="p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground hover:text-destructive"
                  title="Logout"
                >
                  <LogOut size={20} />
                </button>
              </div>
            ) : (
              <Link
                to="/login"
                className="flex items-center gap-2 px-4 py-2 rounded-full bg-primary text-primary-foreground text-sm font-bold hover:opacity-90 transition-opacity"
              >
                <LogIn size={18} />
                <span>{t('nav.login')}</span>
              </Link>
            )}

            <button
              className="md:hidden p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground"
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            >
              <Menu size={20} />
            </button>
          </div>
        </div>
      </div>

      {/* Mobile Menu */}
      {isMobileMenuOpen && (
        <div className="md:hidden glass border-b border-border/40 animate-in slide-in-from-top-4 duration-200">
          <div className="space-y-1 px-4 py-4">
            {navItems.map((item) => (
              <Link
                key={item.href}
                to={item.href}
                onClick={() => setIsMobileMenuOpen(false)}
                className={cn(
                  "flex items-center gap-3 px-4 py-3 rounded-2xl text-base font-medium transition-colors",
                  location.pathname === item.href
                    ? "bg-primary/10 text-primary"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground"
                )}
              >
                <item.icon size={20} />
                {item.name}
              </Link>
            ))}
            {isAuthenticated && (
              <Link
                to="/post/create"
                onClick={() => setIsMobileMenuOpen(false)}
                className={cn(
                  "flex items-center gap-3 px-4 py-3 rounded-2xl text-base font-medium transition-colors",
                  location.pathname === "/post/create"
                    ? "bg-primary/10 text-primary"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground"
                )}
              >
                <PlusSquare size={20} />
                <span>{t('nav.home') === 'Home' ? 'Post' : '发布贴子'}</span>
              </Link>
            )}
          </div>
        </div>
      )}
    </header>
  )
}
