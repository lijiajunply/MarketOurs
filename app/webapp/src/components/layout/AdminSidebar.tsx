import { Link, useLocation } from "react-router"
import {
  LayoutDashboard, Users, FileText, Home,
  ScrollText, ShieldBan, MessageSquare, Tags
} from "lucide-react"
import { useTranslation } from "react-i18next"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  useSidebar,
} from "@/components/ui/sidebar"

export function AdminSidebar() {
  const { t } = useTranslation()
  const { state } = useSidebar()
  const location = useLocation()

  const navItems = [
    { name: t("admin.sidebar.dashboard"), href: "/admin", icon: LayoutDashboard },
    { name: t("admin.sidebar.users"), href: "/admin/users", icon: Users },
    { name: t("admin.sidebar.posts"), href: "/admin/posts", icon: FileText },
    { name: t("admin.sidebar.tags"), href: "/admin/tags", icon: Tags },
    { name: t("admin.sidebar.comments"), href: "/admin/comments", icon: MessageSquare },
    { name: t("admin.sidebar.logs"), href: "/admin/logs", icon: ScrollText },
    { name: t("admin.sidebar.blacklist"), href: "/admin/blacklist", icon: ShieldBan },
  ]

  const isActiveRoute = (href: string) => {
    if (href === "/admin") return location.pathname === "/admin"
    return location.pathname.startsWith(href)
  }

  return (
    <Sidebar collapsible="icon" variant="sidebar">
      {/* Logo Header */}
      <SidebarHeader>
        <div className="flex items-center gap-2.5">
          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-primary shadow-sm">
            <span className="text-sm font-bold text-primary-foreground">A</span>
          </div>
          {state === "expanded" && (
            <span className="font-semibold text-foreground tracking-tight">
              {t("admin.panel")}
            </span>
          )}
        </div>
      </SidebarHeader>

      {/* Nav Items */}
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              {navItems.map((item) => (
                <SidebarMenuItem key={item.href}>
                  <SidebarMenuButton
                    render={<Link to={item.href} />}
                    isActive={isActiveRoute(item.href)}
                    tooltip={item.name}
                  >
                    <item.icon />
                    <span>{item.name}</span>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      {/* Footer */}
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              render={<Link to="/" />}
              tooltip={t("admin.sidebar.back_to_site")}
            >
              <Home />
              <span>{t("admin.sidebar.back_to_site")}</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  )
}
