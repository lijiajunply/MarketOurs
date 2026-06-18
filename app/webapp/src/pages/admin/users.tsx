import { useEffect, useState } from "react"
import { Search, ShieldAlert, ShieldCheck, Trash2 } from "lucide-react"
import { useTranslation } from "react-i18next"
import { adminService } from "../../services/adminService"
import { extractUserMessage } from "../../services/errorCodes"
import { toast } from "../../lib/toast"
import type { PagedResult, UserDto } from "../../types"
import { formatLocalDate } from "../../lib/dateTime"
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

const PAGE_SIZE = 10

export default function AdminUsersPage() {
  const { t, i18n } = useTranslation()
  const [searchTerm, setSearchTerm] = useState("")
  const [page, setPage] = useState(1)
  const [users, setUsers] = useState<PagedResult<UserDto> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeUserId, setActiveUserId] = useState<string | null>(null)
  const [confirmAction, setConfirmAction] = useState<{ type: "delete" | "toggle"; user: UserDto } | null>(null)

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        setIsLoading(true)
        setError(null)

        const response = searchTerm.trim()
          ? await adminService.searchUsers(page, PAGE_SIZE, searchTerm.trim())
          : await adminService.getUsers(page, PAGE_SIZE)

        setUsers(response.data)
      } catch (err) {
        setError(extractUserMessage(err, t("admin.common.load_error")))
      } finally {
        setIsLoading(false)
      }
    }

    void fetchUsers()
  }, [page, searchTerm, t])

  const refreshCurrentPage = async (nextPage = page) => {
    try {
      const response = searchTerm.trim()
        ? await adminService.searchUsers(nextPage, PAGE_SIZE, searchTerm.trim())
        : await adminService.getUsers(nextPage, PAGE_SIZE)

      setUsers(response.data)
      setPage(nextPage)
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.load_error")))
    }
  }

  const handleConfirm = async () => {
    if (!confirmAction) return

    const { type, user } = confirmAction
    setConfirmAction(null)

    try {
      setActiveUserId(user.id)

      if (type === "delete") {
        await adminService.deleteUser(user.id)
        const shouldStepBack = users && users.items.length === 1 && page > 1
        await refreshCurrentPage(shouldStepBack ? page - 1 : page)
        toast.success(t("admin.users.deleted"))
      } else {
        await adminService.updateUserStatus(user.id, { isActive: !user.isActive })
        await refreshCurrentPage()
        toast.success(user.isActive ? t("admin.users.status_disabled") : t("admin.users.status_enabled"))
      }
    } catch (err) {
      toast.error(extractUserMessage(err, t("admin.common.action_error")))
    } finally {
      setActiveUserId(null)
    }
  }

  const getConfirmDialogContent = () => {
    if (!confirmAction) return null
    const { type, user } = confirmAction

    if (type === "delete") {
      return {
        title: t("admin.common.confirm_delete_title"),
        description: t("admin.common.confirm_delete_description", { item: user.name }),
        variant: "destructive" as const,
      }
    } else if (user.isActive) {
      return {
        title: t("admin.common.confirm_ban_title"),
        description: t("admin.common.confirm_ban_description"),
        variant: "destructive" as const,
      }
    } else {
      return {
        title: t("admin.common.confirm_unban_title"),
        description: t("admin.common.confirm_unban_description"),
        variant: "default" as const,
      }
    }
  }

  const confirmContent = getConfirmDialogContent()

  return (
    <div className="space-y-8">
      <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("admin.users.title")}</h1>
          <p className="mt-1 text-muted-foreground">{t("admin.users.subtitle")}</p>
        </div>

        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
          <input
            type="text"
            placeholder={t("admin.users.search_placeholder")}
            className="w-full rounded-xl border border-border/50 bg-muted/50 py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 sm:w-64"
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              setPage(1)
            }}
          />
        </div>
      </header>

      {error && (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      <div className="overflow-hidden rounded-[2rem] border border-border/50 bg-card">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-muted/30 text-xs uppercase text-muted-foreground">
              <tr>
                <th className="px-6 py-4 font-semibold">{t("admin.users.table_user")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.users.table_role")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.users.table_status")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.users.table_joined")}</th>
                <th className="px-6 py-4 text-right font-semibold">{t("admin.users.table_actions")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/50">
              {isLoading ? (
                Array.from({ length: 6 }).map((_, index) => (
                  <tr key={index}>
                    <td className="px-6 py-5" colSpan={5}>
                      <div className="h-10 animate-pulse rounded-xl bg-muted/50" />
                    </td>
                  </tr>
                ))
              ) : users && users.items.length > 0 ? (
                users.items.map((user) => {
                  const isBusy = activeUserId === user.id
                  return (
                    <tr key={user.id} className="transition-colors hover:bg-muted/30">
                      <td className="px-6 py-4">
                        <div className="flex flex-col">
                          <span className="font-bold text-foreground">{user.name}</span>
                          <span className="text-xs text-muted-foreground">{user.email || user.phone || t("common.null")}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${user.role === "Admin" ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}>
                          {user.role}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${user.isActive ? "bg-emerald-500/10 text-emerald-500" : "bg-destructive/10 text-destructive"}`}>
                          {user.isActive ? t("admin.users.status_active") : t("admin.users.status_disabled")}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {formatLocalDate(user.createdAt, i18n.resolvedLanguage)}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={isBusy}
                            title={user.isActive ? t("admin.users.action_ban") : t("admin.users.action_unban")}
                            onClick={() => setConfirmAction({ type: "toggle", user })}
                          >
                            {user.isActive ? <ShieldAlert size={18} /> : <ShieldCheck size={18} />}
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={isBusy}
                            title={t("admin.users.action_delete")}
                            onClick={() => setConfirmAction({ type: "delete", user })}
                          >
                            <Trash2 size={18} className="text-destructive" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  )
                })
              ) : (
                <tr>
                  <td className="px-6 py-12 text-center text-muted-foreground" colSpan={5}>
                    {t("admin.users.no_users_found", { searchTerm })}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between border-t border-border/50 px-6 py-4 text-sm">
          <span className="text-muted-foreground">
            {users ? t("admin.common.total_count", { count: users.totalCount }) : ""}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!users?.hasPreviousPage || isLoading}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("admin.common.previous")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!users?.hasNextPage || isLoading}
              onClick={() => setPage((current) => current + 1)}
            >
              {t("admin.common.next")}
            </Button>
          </div>
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
