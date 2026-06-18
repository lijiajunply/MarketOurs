import { useEffect, useState } from "react"
import { Ban, RefreshCw, Search, ShieldCheck, Trash2 } from "lucide-react"
import { useTranslation } from "react-i18next"
import { adminService } from "../../services/adminService"
import { extractUserMessage } from "../../services/errorCodes"
import { toast } from "../../lib/toast"
import type { BlacklistStats } from "../../types"
import { formatLocalDateTime } from "../../lib/dateTime"
import { Button } from "../../components/ui/button"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "../../components/ui/alert-dialog"

export default function AdminBlacklistPage() {
  const { t, i18n } = useTranslation()
  const [stats, setStats] = useState<BlacklistStats | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isWorking, setIsWorking] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [ipInput, setIpInput] = useState("")
  const [reasonInput, setReasonInput] = useState("")
  const [checkIp, setCheckIp] = useState("")
  const [checkResult, setCheckResult] = useState<{ ip: string; isBlacklisted: boolean; checkTime: string } | null>(null)
  const [confirmAction, setConfirmAction] = useState<"add" | "remove" | "clear_cache" | null>(null)

  const loadStats = async () => {
    const response = await adminService.getBlacklistStats()
    setStats(response.data)
  }

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setIsLoading(true)
        setError(null)
        await loadStats()
      } catch (err) {
        setError(extractUserMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchStats()
  }, [t])

  const runAction = async (action: () => Promise<{ message: string } | void>, successMessage: string) => {
    try {
      setIsWorking(true)
      setError(null)
      const response = await action()
      toast.success(response?.message || successMessage)
      await loadStats()
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setIsWorking(false)
    }
  }

  const handleAdd = async () => {
    if (!ipInput.trim()) {
      toast.error(t("admin.blacklist.validation_ip"))
      return
    }

    setConfirmAction(null)

    await runAction(
      () => adminService.addIpToBlacklist({ ip: ipInput.trim(), reason: reasonInput.trim() || null }),
      t("admin.blacklist.added"),
    )
    setIpInput("")
    setReasonInput("")
  }

  const handleRemove = async () => {
    if (!ipInput.trim()) {
      toast.error(t("admin.blacklist.validation_ip"))
      return
    }

    setConfirmAction(null)

    await runAction(
      () => adminService.removeIpFromBlacklist({ ip: ipInput.trim(), reason: reasonInput.trim() || null }),
      t("admin.blacklist.removed"),
    )
    setIpInput("")
    setReasonInput("")
  }

  const handleRefresh = async () => {
    await runAction(() => adminService.refreshBlacklist(), t("admin.blacklist.refreshed"))
  }

  const handleCleanCache = async () => {
    setConfirmAction(null)
    await runAction(() => adminService.cleanCache(), t("admin.blacklist.cache_cleared"))
  }

  const handleCheck = async () => {
    if (!checkIp.trim()) {
      toast.error(t("admin.blacklist.validation_check_ip"))
      return
    }

    try {
      setIsWorking(true)
      const response = await adminService.checkIp(checkIp.trim())
      setCheckResult(response.data)
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setIsWorking(false)
    }
  }

  const getConfirmDialogContent = () => {
    switch (confirmAction) {
      case "add":
        return {
          title: t("admin.common.confirm_add_blacklist_title"),
          description: t("admin.common.confirm_add_blacklist_description", { ip: ipInput.trim() }),
          variant: "destructive" as const,
        }
      case "remove":
        return {
          title: t("admin.common.confirm_remove_blacklist_title"),
          description: t("admin.common.confirm_remove_blacklist_description", { ip: ipInput.trim() }),
          variant: "default" as const,
        }
      case "clear_cache":
        return {
          title: t("admin.common.confirm_clear_cache_title"),
          description: t("admin.common.confirm_clear_cache_description"),
          variant: "destructive" as const,
        }
      default:
        return null
    }
  }

  const confirmContent = getConfirmDialogContent()

  const handleConfirm = async () => {
    if (confirmAction === "add") {
      await handleAdd()
    } else if (confirmAction === "remove") {
      await handleRemove()
    } else if (confirmAction === "clear_cache") {
      await handleCleanCache()
    }
  }

  const openConfirm = (action: "add" | "remove" | "clear_cache") => {
    if ((action === "add" || action === "remove") && !ipInput.trim()) {
      toast.error(t("admin.blacklist.validation_ip"))
      return
    }
    setConfirmAction(action)
  }

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t("admin.blacklist.title")}</h1>
        <p className="mt-1 text-muted-foreground">{t("admin.blacklist.subtitle")}</p>
      </header>

      {error && <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
        <div className="rounded-3xl border border-border/50 bg-card p-5">
          <p className="text-sm text-muted-foreground">{t("admin.blacklist.total_ips")}</p>
          <p className="mt-2 text-3xl font-black">{isLoading ? "-" : stats?.totalIps.toLocaleString() ?? "-"}</p>
        </div>
        <div className="rounded-3xl border border-border/50 bg-card p-5">
          <p className="text-sm text-muted-foreground">{t("admin.blacklist.total_ranges")}</p>
          <p className="mt-2 text-3xl font-black">{isLoading ? "-" : stats?.totalCidrRanges.toLocaleString() ?? "-"}</p>
        </div>
        <div className="rounded-3xl border border-border/50 bg-card p-5">
          <p className="text-sm text-muted-foreground">{t("admin.blacklist.blacklist_hits")}</p>
          <p className="mt-2 text-3xl font-black">{isLoading ? "-" : stats?.blacklistHits.toLocaleString() ?? "-"}</p>
        </div>
        <div className="rounded-3xl border border-border/50 bg-card p-5">
          <p className="text-sm text-muted-foreground">{t("admin.blacklist.cache_hits")}</p>
          <p className="mt-2 text-3xl font-black">{isLoading ? "-" : stats?.cacheHits.toLocaleString() ?? "-"}</p>
        </div>
        <div className="rounded-3xl border border-border/50 bg-card p-5">
          <p className="text-sm text-muted-foreground">{t("admin.blacklist.cache_misses")}</p>
          <p className="mt-2 text-3xl font-black">{isLoading ? "-" : stats?.cacheMisses.toLocaleString() ?? "-"}</p>
        </div>
        <div className="rounded-3xl border border-border/50 bg-card p-5">
          <p className="text-sm text-muted-foreground">{t("admin.blacklist.last_refresh")}</p>
          <p className="mt-2 text-sm font-medium">
            {stats?.lastRefreshTime
              ? formatLocalDateTime(stats.lastRefreshTime, i18n.resolvedLanguage, { includeSeconds: false })
              : "-"}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-8 xl:grid-cols-2">
        <div className="space-y-6 rounded-[2rem] border border-border/50 bg-card p-6">
          <h2 className="text-xl font-bold">{t("admin.blacklist.manage_title")}</h2>

          <div className="grid gap-2">
            <label className="text-sm font-medium">{t("admin.blacklist.ip_label")}</label>
            <input
              type="text"
              value={ipInput}
              onChange={(e) => setIpInput(e.target.value)}
              placeholder={t("admin.blacklist.ip_placeholder")}
              className="rounded-2xl border border-border/50 bg-muted/50 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-primary/20"
            />
          </div>

          <div className="grid gap-2">
            <label className="text-sm font-medium">{t("admin.blacklist.reason_label")}</label>
            <textarea
              rows={3}
              value={reasonInput}
              onChange={(e) => setReasonInput(e.target.value)}
              placeholder={t("admin.blacklist.reason_placeholder")}
              className="rounded-2xl border border-border/50 bg-muted/50 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-primary/20"
            />
          </div>

          <div className="flex flex-wrap gap-3">
            <Button
              variant="destructive"
              size="sm"
              disabled={isWorking}
              onClick={() => openConfirm("add")}
            >
              <Ban size={16} />
              {t("admin.blacklist.add_action")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={isWorking}
              onClick={() => openConfirm("remove")}
            >
              <Trash2 size={16} />
              {t("admin.blacklist.remove_action")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={isWorking}
              onClick={() => void handleRefresh()}
            >
              <RefreshCw size={16} />
              {t("admin.blacklist.refresh_action")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={isWorking}
              onClick={() => openConfirm("clear_cache")}
            >
              <ShieldCheck size={16} />
              {t("admin.blacklist.clean_cache_action")}
            </Button>
          </div>
        </div>

        <div className="space-y-6 rounded-[2rem] border border-border/50 bg-card p-6">
          <h2 className="text-xl font-bold">{t("admin.blacklist.check_title")}</h2>

          <div className="flex flex-col gap-3 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
              <input
                type="text"
                value={checkIp}
                onChange={(e) => setCheckIp(e.target.value)}
                placeholder={t("admin.blacklist.check_placeholder")}
                className="w-full rounded-2xl border border-border/50 bg-muted/50 py-3 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>
            <Button
              disabled={isWorking}
              onClick={() => void handleCheck()}
            >
              {t("admin.blacklist.check_action")}
            </Button>
          </div>

          {checkResult ? (
            <div className={`rounded-2xl px-4 py-4 ${checkResult.isBlacklisted ? "bg-destructive/10 text-destructive" : "bg-emerald-500/10 text-emerald-600"}`}>
              <p className="font-bold">{checkResult.ip}</p>
              <p className="mt-1 text-sm">
                {checkResult.isBlacklisted ? t("admin.blacklist.check_blocked") : t("admin.blacklist.check_allowed")}
              </p>
              <p className="mt-2 text-xs opacity-80">
                {formatLocalDateTime(checkResult.checkTime, i18n.resolvedLanguage, { includeSeconds: false })}
              </p>
            </div>
          ) : (
            <div className="rounded-2xl bg-muted/40 px-4 py-10 text-center text-sm text-muted-foreground">
              {t("admin.blacklist.no_check_result")}
            </div>
          )}
        </div>
      </div>

      <AlertDialog open={confirmAction !== null} onOpenChange={(open) => { if (!open) setConfirmAction(null) }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirmContent?.title}</AlertDialogTitle>
            <AlertDialogDescription>{confirmContent?.description}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("admin.common.cancel")}</AlertDialogCancel>
            <AlertDialogAction
              variant={confirmContent?.variant}
              onClick={() => void handleConfirm()}
            >
              {t("admin.common.confirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
