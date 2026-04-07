import { AreaChart } from "../../components/ui/chart"
import { Users, MessageSquare, TrendingUp, ShoppingBag } from "lucide-react"
import { useTranslation } from "react-i18next"

export default function AdminDashboard() {
  const { t } = useTranslation();

  const chartData = [
    { date: new Date(2026, 3, 1), value: 45 },
    { date: new Date(2026, 3, 2), value: 52 },
    { date: new Date(2026, 3, 3), value: 48 },
    { date: new Date(2026, 3, 4), value: 70 },
    { date: new Date(2026, 3, 5), value: 65 },
    { date: new Date(2026, 3, 6), value: 85 },
    { date: new Date(2026, 3, 7), value: 95 },
  ]

  const stats = [
    { name: t("admin.dashboard.total_users"), value: "12,453", change: "+12%", icon: Users },
    { name: t("admin.dashboard.active_listings"), value: "3,241", change: "+5%", icon: ShoppingBag },
    { name: t("admin.dashboard.daily_posts"), value: "842", change: "+18%", icon: MessageSquare },
    { name: t("admin.dashboard.revenue"), value: "$45,231", change: "+24%", icon: TrendingUp },
  ]

  return (
    <div className="space-y-10">
      <header className="space-y-2">
        <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">{t("admin.dashboard.title")}</h1>
        <p className="text-lg text-muted-foreground">{t("admin.dashboard.subtitle")}</p>
      </header>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {stats.map((stat, i) => (
          <div key={i} className="p-6 rounded-3xl bg-card border border-border/50 space-y-4">
            <div className="flex items-center justify-between">
              <div className="p-2 rounded-xl bg-primary/10 text-primary">
                <stat.icon size={20} />
              </div>
              <span className="text-xs font-bold text-emerald-500 bg-emerald-500/10 px-2 py-1 rounded-full">
                {stat.change}
              </span>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">{stat.name}</p>
              <p className="text-3xl font-black tracking-tight">{stat.value}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 p-8 rounded-[2.5rem] bg-card border border-border/50 space-y-6">
          <div className="flex items-center justify-between">
            <h3 className="text-2xl font-bold tracking-tight">{t("admin.dashboard.activity_overview")}</h3>
            <select className="bg-muted border-none rounded-full px-4 py-1.5 text-sm font-medium focus:ring-2 focus:ring-primary/20 outline-none cursor-pointer">
              <option>{t("admin.dashboard.last_7_days")}</option>
              <option>{t("admin.dashboard.last_30_days")}</option>
            </select>
          </div>
          <AreaChart data={chartData} />
        </div>

        <div className="p-8 rounded-[2.5rem] bg-card border border-border/50 space-y-6">
          <h3 className="text-2xl font-bold tracking-tight">{t("admin.dashboard.recent_activity")}</h3>
          <div className="space-y-6">
            {[1, 2, 3, 4, 5].map((item) => (
              <div key={item} className="flex items-center gap-4">
                <div className="w-10 h-10 rounded-full bg-muted flex-shrink-0" />
                <div className="space-y-1">
                  <p className="text-sm font-bold">{t("admin.dashboard.new_user_registered")}</p>
                  <p className="text-xs text-muted-foreground">2 minutes ago</p>
                </div>
              </div>
            ))}
          </div>
          <button className="w-full py-3 rounded-2xl bg-muted hover:bg-muted/80 transition-colors text-sm font-bold mt-4">
            {t("admin.dashboard.view_all_activity")}
          </button>
        </div>
      </div>
    </div>
  )
}
