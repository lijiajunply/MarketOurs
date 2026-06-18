import { Link, useLocation } from "react-router"
import { cn } from "@/lib/utils"
import { useTheme } from "@/components/theme-provider"
import {
  Sun, Moon, MessageSquare, User, Menu, LogIn, LogOut,
  PlusSquare, Languages, Flame, ChevronDown, Settings
} from "lucide-react"
import { useState } from "react"
import { useSelector, useDispatch } from "react-redux"
import { useTranslation } from "react-i18next"
import type { RootState } from "@/stores"
import { logout } from "@/stores/authSlice"
import { NotificationBell } from "./NotificationBell"
import { useScrollPosition } from "@/hooks/useScrollPosition"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"
import { Button } from "@/components/ui/button"

export function Navbar() {
  const { theme, setTheme } = useTheme()
  const { t, i18n } = useTranslation()
  const location = useLocation()
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const { isAuthenticated, user } = useSelector((state: RootState) => state.auth)
  const dispatch = useDispatch()
  const { isScrolled } = useScrollPosition()

  const navItems = [
    { name: t("nav.home"), href: "/", icon: MessageSquare },
    { name: t("nav.hot"), href: "/hot", icon: Flame },
  ]

  const handleLogout = () => {
    dispatch(logout())
  }

  const toggleLanguage = () => {
    const newLang = i18n.language === 'en' ? 'zh' : 'en'
    i18n.changeLanguage(newLang)
  }

  const isActiveRoute = (href: string) => {
    if (href === "/") return location.pathname === "/"
    return location.pathname.startsWith(href)
  }

  const userInitials = user?.name
    ? user.name.slice(0, 2).toUpperCase()
    : "U"

  return (
    <header
      className={cn(
        "sticky top-0 z-50 w-full transition-all duration-300",
        "border-b",
        isScrolled
          ? "bg-background/80 backdrop-blur-xl border-border/20 shadow-sm"
          : "bg-background/50 backdrop-blur-md border-transparent"
      )}
    >
      <div className="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-14 items-center justify-between">
          {/* Left: Logo + Nav */}
          <div className="flex items-center gap-6">
            <Link to="/" className="flex items-center gap-2.5 shrink-0">
              <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-primary shadow-sm">
                <span className="text-sm font-bold text-primary-foreground">L</span>
              </div>
              <span className="hidden font-semibold text-foreground sm:inline-block text-base tracking-tight">
                光汇
              </span>
            </Link>

            <nav className="hidden md:flex items-center gap-0.5">
              {navItems.map((item) => (
                <Link
                  key={item.href}
                  to={item.href}
                  className={cn(
                    "relative px-3.5 py-2 rounded-xl text-sm font-medium transition-colors",
                    isActiveRoute(item.href)
                      ? "text-foreground"
                      : "text-muted-foreground hover:text-foreground hover:bg-muted/80"
                  )}
                >
                  {item.name}
                  {isActiveRoute(item.href) && (
                    <span className="absolute inset-x-3 -bottom-px h-0.5 rounded-full bg-primary" />
                  )}
                </Link>
              ))}
              {isAuthenticated && (
                <Link
                  to="/post/create"
                  className={cn(
                    "relative px-3.5 py-2 rounded-xl text-sm font-medium transition-colors flex items-center gap-1.5",
                    isActiveRoute("/post/create")
                      ? "text-foreground"
                      : "text-muted-foreground hover:text-foreground hover:bg-muted/80"
                  )}
                >
                  <PlusSquare size={16} />
                  <span>{t("nav.post")}</span>
                </Link>
              )}
            </nav>
          </div>

          {/* Right: Actions */}
          <div className="flex items-center gap-1">
            {/* Language Toggle */}
            <Button
              variant="ghost"
              size="icon"
              onClick={toggleLanguage}
              aria-label={t("nav.toggle_language")}
              className="text-muted-foreground hover:text-foreground rounded-xl"
            >
              <Languages size={18} />
            </Button>

            {/* Theme Toggle */}
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
              aria-label="Toggle theme"
              className="text-muted-foreground hover:text-foreground rounded-xl"
            >
              {theme === "dark" ? <Sun size={18} /> : <Moon size={18} />}
            </Button>

            {/* Notification Bell */}
            <NotificationBell />

            {/* Auth Section */}
            {isAuthenticated ? (
              <DropdownMenu>
                <DropdownMenuTrigger
                  render={
                    <button className="flex items-center gap-2 rounded-xl px-1.5 py-1 hover:bg-muted/80 transition-colors ml-1">
                      <Avatar className="h-7 w-7 rounded-full ring-2 ring-border/30">
                        <AvatarImage src={user?.avatar} alt={user?.name} />
                        <AvatarFallback className="text-[10px] bg-primary/10 text-primary font-medium">
                          {userInitials}
                        </AvatarFallback>
                      </Avatar>
                      <ChevronDown size={14} className="text-muted-foreground hidden sm:block" />
                    </button>
                  }
                />
                <DropdownMenuContent align="end" className="w-56 mt-2">
                  <div className="px-2 py-2">
                    <div className="flex flex-col gap-0.5">
                      <p className="text-sm font-medium">{user?.name}</p>
                      <p className="text-xs text-muted-foreground truncate">{user?.email}</p>
                    </div>
                  </div>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="rounded-xl"
                    render={<Link to="/profile" className="cursor-pointer inline-flex items-center w-full">
                      <User size={14} className="mr-2" />
                      {t("nav.profile")}
                    </Link>}
                  />
                  <DropdownMenuItem
                    onClick={handleLogout}
                    className="text-destructive focus:text-destructive rounded-xl"
                  >
                    <LogOut size={14} className="mr-2" />
                    {t("nav.logout")}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : (
              <Button
                size="sm"
                className="rounded-xl font-medium ml-1"
                render={<Link to="/login" className="inline-flex items-center gap-1.5">
                  <LogIn size={16} />
                  {t('nav.login')}
                </Link>}
              />
            )}

            {/* Mobile Menu Trigger */}
            <Sheet open={isMobileMenuOpen} onOpenChange={setIsMobileMenuOpen}>
              <SheetTrigger
                render={
                  <Button variant="ghost" size="icon" className="md:hidden rounded-xl text-muted-foreground">
                    <Menu size={20} />
                  </Button>
                }
              />
              <SheetContent side="right" className="w-72 rounded-l-3xl pt-12">
                <SheetHeader className="text-left mb-4">
                  <SheetTitle className="flex items-center gap-2.5">
                    <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-primary shadow-sm">
                      <span className="text-sm font-bold text-primary-foreground">L</span>
                    </div>
                    光汇
                  </SheetTitle>
                </SheetHeader>
                <nav className="flex flex-col gap-1">
                  {navItems.map((item) => (
                    <Link
                      key={item.href}
                      to={item.href}
                      onClick={() => setIsMobileMenuOpen(false)}
                      className={cn(
                        "flex items-center gap-3 px-4 py-3 rounded-xl text-base font-medium transition-colors",
                        isActiveRoute(item.href)
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
                        "flex items-center gap-3 px-4 py-3 rounded-xl text-base font-medium transition-colors",
                        isActiveRoute("/post/create")
                          ? "bg-primary/10 text-primary"
                          : "text-muted-foreground hover:bg-muted hover:text-foreground"
                      )}
                    >
                      <PlusSquare size={20} />
                      <span>{t("nav.post")}</span>
                    </Link>
                  )}
                </nav>
              </SheetContent>
            </Sheet>
          </div>
        </div>
      </div>
    </header>
  )
}
