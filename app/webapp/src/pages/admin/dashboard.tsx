import { useEffect, useState } from "react"
import { AreaChart } from "../../components/ui/chart"
import { AlertCircle, Activity, FileText, MessageSquareWarning, Users } from "lucide-react"
import { useTranslation } from "react-i18next"
import { adminService } from "../../services/adminService"
import type { AdminOverviewDto } from "../../types"

const DATE_FORMATTER = new Intl.DateTimeFormat(undefined, {
  month: "short",
  day: "numeric",
})

const RELATIVE_TIME_FORMATTER = new Intl.RelativeTimeFormat(undefined, {
  numeric: "auto",
})

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === "object" && error !== null && "message" in error && typeof error.message === "string") {
    return error.message
  }

  return fallback
}

function formatRelativeTime(timestamp: string) {
  const target = new Date(timestamp).getTime()
  const deltaSeconds = Math.round((target - Date.now()) / 1000)

  const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
    ["day", 86400],
    ["hour", 3600],
    ["minute", 60],
  ]

  for (const [unit, secondsPerUnit] of units) {
    if (Math.abs(deltaSeconds) >= secondsPerUnit || unit === "minute") {
      return RELATIVE_TIME_FORMATTER.format(Math.round(deltaSeconds / secondsPerUnit), unit)
    }
  }

  return RELATIVE_TIME_FORMATTER.format(deltaSeconds, "second")
}

export default function AdminDashboard() {
  const { t } = useTranslation()
  const [overview, setOverview] = useState<AdminOverviewDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchOverview = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const response = await adminService.getOverview()
        setOverview(response.data)
      } catch (err) {
        setError(getErrorMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchOverview()
  }, [t])

  const chartData = overview?.postTrend.map((point) => ({
    date: new Date(point.date),
    value: point.posts,
  })) ?? []

  const stats = overview ? [
    { name: t("admin.dashboard.total_users"), value: overview.totalUsers.toLocaleString(), icon: Users },
    { name: t("admin.dashboard.active_users"), value: overview.activeUsers.toLocaleString(), icon: Activity },
    { name: t("admin.dashboard.total_posts"), value: overview.totalPosts.toLocaleString(), icon: FileText },
    { name: t("admin.dashboard.last_7_days_posts"), value: overview.postsCreatedInLast7Days.toLocaleString(), icon: MessageSquareWarning },
  ] : []

  return (
    <div className="space-y-10">
      <header className="space-y-2">
        <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">{t("admin.dashboard.title")}</h1>
        <p className="text-lg text-muted-foreground">{t("admin.dashboard.subtitle")}</p>
      </header>

      {error && (
        <div className="rounded-3xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive">
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <div key={index} className="h-36 animate-pulse rounded-3xl border border-border/50 bg-card" />
          ))}
        </div>
      ) : overview ? (
        <>
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {stats.map((stat) => (
              <div key={stat.name} className="space-y-4 rounded-3xl border border-border/50 bg-card p-6">
                <div className="flex items-center justify-between">
                  <div className="rounded-xl bg-primary/10 p-2 text-primary">
                    <stat.icon size={20} />
                  </div>
                </div>
                <div>
                  <p className="text-sm font-medium text-muted-foreground">{stat.name}</p>
                  <p className="text-3xl font-black tracking-tight">{stat.value}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
            <div className="space-y-6 rounded-[2.5rem] border border-border/50 bg-card p-8 lg:col-span-2">
              <div className="flex items-center justify-between">
                <h3 className="text-2xl font-bold tracking-tight">{t("admin.dashboard.activity_overview")}</h3>
                <select
                  disabled
                  className="cursor-not-allowed rounded-full bg-muted px-4 py-1.5 text-sm font-medium opacity-70 outline-none"
                >
                  <option>{t("admin.dashboard.last_7_days")}</option>
                  <option>{t("admin.dashboard.last_30_days")}</option>
                </select>
              </div>

              {chartData.length > 0 ? (
                <AreaChart data={chartData} />
              ) : (
                <div className="flex h-[300px] items-center justify-center rounded-3xl bg-muted/40 text-sm text-muted-foreground">
                  {t("admin.dashboard.no_activity")}
                </div>
              )}
            </div>

            <div className="space-y-6 rounded-[2.5rem] border border-border/50 bg-card p-8">
              <h3 className="text-2xl font-bold tracking-tight">{t("admin.dashboard.recent_activity")}</h3>
              {overview.recentActivities.length > 0 ? (
                <div className="space-y-6">
                  {overview.recentActivities.map((activity) => (
                    <div key={activity.id} className="flex items-start gap-4">
                      <div className="mt-1 h-10 w-10 rounded-full bg-muted" />
                      <div className="space-y-1">
                        <p className="text-sm font-bold">{activity.title}</p>
                        <p className="text-sm text-muted-foreground">{activity.description}</p>
                        <p className="text-xs text-muted-foreground">
                          {formatRelativeTime(activity.timestamp)}
                          {" · "}
                          {DATE_FORMATTER.format(new Date(activity.timestamp))}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="rounded-3xl bg-muted/40 px-4 py-8 text-center text-sm text-muted-foreground">
                  {t("admin.dashboard.no_recent_activity")}
                </div>
              )}
            </div>
          </div>
        </>
      ) : (
        <div className="rounded-[2.5rem] border border-border/50 bg-card px-8 py-16 text-center text-muted-foreground">
          <AlertCircle className="mx-auto mb-4" size={28} />
          {t("admin.common.empty")}
        </div>
      )}
    </div>
  )
}
