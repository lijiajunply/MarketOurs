import { useEffect, useMemo, useState } from "react"
import { Search, RotateCcw, Trash2 } from "lucide-react"
import { useTranslation } from "react-i18next"
import { adminService } from "../../services/adminService"
import type { LogDistribution, LogEntry, LogStatistics, PaginatedResponse } from "../../types"

const PAGE_SIZE = 20

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === "object" && error !== null && "message" in error && typeof error.message === "string") {
    return error.message
  }

  return fallback
}

function formatTimestamp(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  }).format(new Date(value))
}

const LEVEL_OPTIONS = ["Information", "Warning", "Error", "Fatal"]
export default function AdminLogsPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [searchTerm, setSearchTerm] = useState("")
  const [levelFilter, setLevelFilter] = useState("")
  const [timeRange, setTimeRange] = useState<"today" | "7" | "30">("today")
  const [logs, setLogs] = useState<PaginatedResponse<LogEntry> | null>(null)
  const [statistics, setStatistics] = useState<LogStatistics | null>(null)
  const [distribution, setDistribution] = useState<LogDistribution[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isCleaning, setIsCleaning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const levelSummary = useMemo(() => {
    const counts = statistics?.levelCounts ?? {}
    return LEVEL_OPTIONS.map((level) => ({
      level,
      count: counts[level] ?? 0,
    }))
  }, [statistics])

  useEffect(() => {
    const fetchLogsData = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const [logsResponse, statsResponse, distributionResponse] = await Promise.all([
          adminService.getLogs(page, PAGE_SIZE, searchTerm.trim() || undefined, levelFilter || undefined, timeRange),
          adminService.getLogStatistics(),
          adminService.getLogDistribution(timeRange),
        ])

        setLogs(logsResponse.data)
        setStatistics(statsResponse.data)
        setDistribution(distributionResponse.data)
      } catch (err) {
        setError(getErrorMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchLogsData()
  }, [levelFilter, page, searchTerm, t, timeRange])

  const handleCleanup = async () => {
    try {
      setIsCleaning(true)
      setError(null)
      setMessage(null)
      const days = Number(timeRange) > 0 ? Number(timeRange) : 7
      const response = await adminService.cleanupLogs(days)
      setMessage(response.message || t("admin.logs.cleanup_success"))

      const [logsResponse, statsResponse, distributionResponse] = await Promise.all([
        adminService.getLogs(page, PAGE_SIZE, searchTerm.trim() || undefined, levelFilter || undefined, timeRange),
        adminService.getLogStatistics(),
        adminService.getLogDistribution(timeRange),
      ])

      setLogs(logsResponse.data)
      setStatistics(statsResponse.data)
      setDistribution(distributionResponse.data)
    } catch (err) {
      setError(getErrorMessage(err, t("admin.common.action_error")))
    } finally {
      setIsCleaning(false)
    }
  }

  return (
    <div className="space-y-8">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("admin.logs.title")}</h1>
          <p className="mt-1 text-muted-foreground">{t("admin.logs.subtitle")}</p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
            <input
              type="text"
              placeholder={t("admin.logs.search_placeholder")}
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value)
                setPage(1)
              }}
              className="w-full rounded-xl border border-border/50 bg-muted/50 py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 sm:w-72"
            />
          </div>

          <select
            value={levelFilter}
            onChange={(e) => {
              setLevelFilter(e.target.value)
              setPage(1)
            }}
            className="rounded-xl border border-border/50 bg-muted/50 px-4 py-2 focus:outline-none focus:ring-2 focus:ring-primary/20"
          >
            <option value="">{t("admin.logs.all_levels")}</option>
            {LEVEL_OPTIONS.map((level) => (
              <option key={level} value={level}>{level}</option>
            ))}
          </select>

          <select
            value={timeRange}
            onChange={(e) => {
              setTimeRange(e.target.value as "today" | "7" | "30")
              setPage(1)
            }}
            className="rounded-xl border border-border/50 bg-muted/50 px-4 py-2 focus:outline-none focus:ring-2 focus:ring-primary/20"
          >
            <option value="today">{t("admin.logs.today")}</option>
            <option value="7">{t("admin.logs.last_7_days")}</option>
            <option value="30">{t("admin.logs.last_30_days")}</option>
          </select>

          <button
            type="button"
            onClick={() => {
              setSearchTerm("")
              setLevelFilter("")
              setTimeRange("today")
              setPage(1)
            }}
            className="inline-flex items-center gap-2 rounded-xl border border-border/50 px-4 py-2"
          >
            <RotateCcw size={16} />
            {t("admin.logs.reset_filters")}
          </button>

          <button
            type="button"
            disabled={isCleaning}
            onClick={() => void handleCleanup()}
            className="inline-flex items-center gap-2 rounded-xl bg-destructive px-4 py-2 font-medium text-destructive-foreground disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Trash2 size={16} />
            {isCleaning ? t("admin.logs.cleaning") : t("admin.logs.cleanup")}
          </button>
        </div>
      </header>

      {error && <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>}
      {message && <div className="rounded-2xl border border-primary/20 bg-primary/10 px-4 py-3 text-sm text-primary">{message}</div>}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
        <div className="rounded-3xl border border-border/50 bg-card p-5">
          <p className="text-sm text-muted-foreground">{t("admin.logs.total_logs")}</p>
          <p className="mt-2 text-3xl font-black">{statistics?.totalCount?.toLocaleString() ?? "-"}</p>
        </div>
        {levelSummary.map((item) => (
          <div key={item.level} className="rounded-3xl border border-border/50 bg-card p-5">
            <p className="text-sm text-muted-foreground">{item.level}</p>
            <p className="mt-2 text-3xl font-black">{item.count.toLocaleString()}</p>
          </div>
        ))}
      </div>

      <div className="rounded-[2rem] border border-border/50 bg-card p-6">
        <h2 className="text-lg font-bold">{t("admin.logs.distribution_title")}</h2>
        <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {distribution.length > 0 ? distribution.map((item) => (
            <div key={item.timePoint} className="rounded-2xl bg-muted/40 p-4">
              <p className="text-sm font-medium">{item.timePoint}</p>
              <p className="mt-2 text-2xl font-black">{item.totalCount}</p>
              <p className="mt-2 text-xs text-muted-foreground">
                {t("admin.logs.distribution_breakdown", {
                  errors: item.errorCount,
                  warnings: item.warningCount,
                  info: item.infoCount,
                })}
              </p>
            </div>
          )) : (
            <div className="rounded-2xl bg-muted/40 p-6 text-sm text-muted-foreground">
              {t("admin.logs.no_distribution")}
            </div>
          )}
        </div>
      </div>

      <div className="overflow-hidden rounded-[2rem] border border-border/50 bg-card">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-muted/30 text-xs uppercase text-muted-foreground">
              <tr>
                <th className="px-6 py-4 font-semibold">{t("admin.logs.table_time")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.logs.table_level")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.logs.table_message")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.logs.table_exception")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/50">
              {isLoading ? (
                Array.from({ length: 8 }).map((_, index) => (
                  <tr key={index}>
                    <td colSpan={4} className="px-6 py-5">
                      <div className="h-10 animate-pulse rounded-xl bg-muted/50" />
                    </td>
                  </tr>
                ))
              ) : logs && logs.data.length > 0 ? (
                logs.data.map((log, index) => (
                  <tr key={`${log.timestamp}-${index}`} className="align-top transition-colors hover:bg-muted/30">
                    <td className="px-6 py-4 text-muted-foreground">{formatTimestamp(log.timestamp)}</td>
                    <td className="px-6 py-4">
                      <span className="rounded-full bg-muted px-2.5 py-1 text-xs font-bold">{log.level}</span>
                    </td>
                    <td className="max-w-xl px-6 py-4 text-foreground">{log.message || t("common.null")}</td>
                    <td className="max-w-xl px-6 py-4 text-xs text-muted-foreground">{log.exception || t("common.null")}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={4} className="px-6 py-12 text-center text-muted-foreground">
                    {t("admin.logs.no_logs")}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between border-t border-border/50 px-6 py-4 text-sm">
          <span className="text-muted-foreground">
            {logs ? t("admin.common.total_count", { count: logs.totalCount }) : ""}
          </span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={!logs || page <= 1 || isLoading}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              className="rounded-xl border border-border/50 px-4 py-2 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {t("admin.common.previous")}
            </button>
            <button
              type="button"
              disabled={!logs || page >= logs.totalPages || isLoading}
              onClick={() => setPage((current) => current + 1)}
              className="rounded-xl border border-border/50 px-4 py-2 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {t("admin.common.next")}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
