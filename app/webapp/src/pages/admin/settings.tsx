import { useEffect, useMemo, useState } from "react"
import { Save } from "lucide-react"
import { useTranslation } from "react-i18next"
import { adminService } from "../../services/adminService"
import type { AdminSettingsDto } from "../../types"

const DEFAULT_SETTINGS: AdminSettingsDto = {
  siteName: "MarketOurs",
  allowRegistration: true,
  maintenanceMode: false,
  maxPostImages: 9,
  autoApprovePosts: true,
  supportEmail: "",
  announcement: "",
}

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === "object" && error !== null && "message" in error && typeof error.message === "string") {
    return error.message
  }

  return fallback
}

export default function AdminSettingsPage() {
  const { t } = useTranslation()
  const [settings, setSettings] = useState<AdminSettingsDto>(DEFAULT_SETTINGS)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null)

  useEffect(() => {
    const fetchSettings = async () => {
      try {
        setIsLoading(true)
        setMessage(null)
        const response = await adminService.getSettings()
        setSettings(response.data)
      } catch (err) {
        setMessage({ type: "error", text: getErrorMessage(err, t("admin.common.load_error")) })
      } finally {
        setIsLoading(false)
      }
    }

    void fetchSettings()
  }, [t])

  const validationError = useMemo(() => {
    if (!settings.siteName.trim()) {
      return t("admin.settings.validation_site_name")
    }

    if (settings.maxPostImages < 1 || settings.maxPostImages > 20) {
      return t("admin.settings.validation_max_images")
    }

    if (settings.supportEmail.trim()) {
      const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailPattern.test(settings.supportEmail.trim())) {
        return t("admin.settings.validation_support_email")
      }
    }

    return null
  }, [settings, t])

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    if (validationError) {
      setMessage({ type: "error", text: validationError })
      return
    }

    try {
      setIsSaving(true)
      setMessage(null)
      const response = await adminService.updateSettings({
        ...settings,
        siteName: settings.siteName.trim(),
        supportEmail: settings.supportEmail.trim(),
        announcement: settings.announcement.trim(),
      })
      setSettings(response.data)
      setMessage({ type: "success", text: t("admin.settings.success_msg") })
    } catch (err) {
      setMessage({ type: "error", text: getErrorMessage(err, t("admin.common.action_error")) })
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t("admin.settings.title")}</h1>
        <p className="mt-1 text-muted-foreground">{t("admin.settings.subtitle")}</p>
      </header>

      {message && (
        <div className={`rounded-2xl px-4 py-3 text-sm ${message.type === "success" ? "border border-primary/20 bg-primary/10 text-primary" : "border border-destructive/30 bg-destructive/10 text-destructive"}`}>
          {message.text}
        </div>
      )}

      <form onSubmit={handleSave} className="space-y-8">
        <div className="space-y-8 rounded-[2.5rem] border border-border/50 bg-card p-8">
          <div className="space-y-6">
            <h2 className="text-xl font-bold tracking-tight">{t("admin.settings.section_general")}</h2>

            <div className="grid gap-2">
              <label htmlFor="siteName" className="text-sm font-medium">{t("admin.settings.site_name")}</label>
              <input
                id="siteName"
                type="text"
                value={settings.siteName}
                disabled={isLoading}
                onChange={(e) => setSettings({ ...settings, siteName: e.target.value })}
                className="w-full rounded-2xl border border-border/50 bg-muted/50 px-4 py-3 transition-all focus:outline-none focus:ring-2 focus:ring-primary/20 md:w-1/2"
              />
            </div>

            <div className="grid gap-2">
              <label htmlFor="supportEmail" className="text-sm font-medium">{t("admin.settings.support_email")}</label>
              <input
                id="supportEmail"
                type="email"
                value={settings.supportEmail}
                disabled={isLoading}
                onChange={(e) => setSettings({ ...settings, supportEmail: e.target.value })}
                className="w-full rounded-2xl border border-border/50 bg-muted/50 px-4 py-3 transition-all focus:outline-none focus:ring-2 focus:ring-primary/20 md:w-1/2"
              />
            </div>

            <div className="flex items-center justify-between border-b border-border/50 py-4">
              <div>
                <p className="font-medium">{t("admin.settings.allow_registration")}</p>
                <p className="text-sm text-muted-foreground">{t("admin.settings.allow_registration_desc")}</p>
              </div>
              <label className="relative inline-flex cursor-pointer items-center">
                <input
                  type="checkbox"
                  className="peer sr-only"
                  checked={settings.allowRegistration}
                  disabled={isLoading}
                  onChange={(e) => setSettings({ ...settings, allowRegistration: e.target.checked })}
                />
                <div className="h-6 w-11 rounded-full bg-muted peer-checked:bg-primary peer-checked:after:translate-x-full after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-['']" />
              </label>
            </div>

            <div className="flex items-center justify-between py-4">
              <div>
                <p className="font-medium">{t("admin.settings.maintenance_mode")}</p>
                <p className="text-sm text-muted-foreground">{t("admin.settings.maintenance_mode_desc")}</p>
              </div>
              <label className="relative inline-flex cursor-pointer items-center">
                <input
                  type="checkbox"
                  className="peer sr-only"
                  checked={settings.maintenanceMode}
                  disabled={isLoading}
                  onChange={(e) => setSettings({ ...settings, maintenanceMode: e.target.checked })}
                />
                <div className="h-6 w-11 rounded-full bg-muted peer-checked:bg-primary peer-checked:after:translate-x-full after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-['']" />
              </label>
            </div>
          </div>

          <div className="space-y-6 border-t border-border/50 pt-4">
            <h2 className="text-xl font-bold tracking-tight">{t("admin.settings.section_content")}</h2>

            <div className="grid gap-2">
              <label htmlFor="maxPostImages" className="text-sm font-medium">{t("admin.settings.max_images")}</label>
              <input
                id="maxPostImages"
                type="number"
                min="1"
                max="20"
                value={settings.maxPostImages}
                disabled={isLoading}
                onChange={(e) => setSettings({ ...settings, maxPostImages: Number(e.target.value) || 0 })}
                className="w-full rounded-2xl border border-border/50 bg-muted/50 px-4 py-3 transition-all focus:outline-none focus:ring-2 focus:ring-primary/20 md:w-1/4"
              />
            </div>

            <div className="flex items-center justify-between py-4">
              <div>
                <p className="font-medium">{t("admin.settings.auto_approve")}</p>
                <p className="text-sm text-muted-foreground">{t("admin.settings.auto_approve_desc")}</p>
              </div>
              <label className="relative inline-flex cursor-pointer items-center">
                <input
                  type="checkbox"
                  className="peer sr-only"
                  checked={settings.autoApprovePosts}
                  disabled={isLoading}
                  onChange={(e) => setSettings({ ...settings, autoApprovePosts: e.target.checked })}
                />
                <div className="h-6 w-11 rounded-full bg-muted peer-checked:bg-primary peer-checked:after:translate-x-full after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-['']" />
              </label>
            </div>

            <div className="grid gap-2">
              <label htmlFor="announcement" className="text-sm font-medium">{t("admin.settings.announcement")}</label>
              <textarea
                id="announcement"
                rows={5}
                value={settings.announcement}
                disabled={isLoading}
                onChange={(e) => setSettings({ ...settings, announcement: e.target.value })}
                className="w-full rounded-2xl border border-border/50 bg-muted/50 px-4 py-3 transition-all focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>
        </div>

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={isLoading || isSaving}
            className="flex items-center gap-2 rounded-2xl bg-primary px-6 py-3 font-bold text-primary-foreground transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Save size={18} />
            {isSaving ? t("admin.common.saving") : t("admin.settings.save_btn")}
          </button>
        </div>
      </form>
    </div>
  )
}
