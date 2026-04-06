import { useState } from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { userService } from "../../services/userService";
import { Lock, Loader2, ArrowRight, ShieldCheck, AlertCircle, CheckCircle2 } from "lucide-react";

export default function ResetPasswordPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);

  const [isPasswordValid, setIsPasswordValid] = useState(false);
  const [isPasswordDirty, setIsPasswordDirty] = useState(false);

  const validatePassword = (value: string) => {
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;
    const isValid = passwordRegex.test(value);
    setIsPasswordValid(isValid);
    return isValid;
  };

  const handleNewPasswordChange = (value: string) => {
    setNewPassword(value);
    setIsPasswordDirty(true);
    validatePassword(value);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      setError(t("profile.passwords_not_match"));
      return;
    }

    setIsLoading(true);
    setError("");

    try {
      await userService.changePassword({ oldPassword, newPassword });
      setSuccess(true);
      setTimeout(() => navigate("/profile"), 2000);
    } catch (err: any) {
      setError(err.message || t("auth.error_failed_to_login"));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="max-w-md mx-auto py-12 px-4">
      <div className="glass-card rounded-[2.5rem] p-8 space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
        <div className="text-center space-y-2">
          <div className="h-12 w-12 rounded-2xl bg-primary flex items-center justify-center shadow-lg shadow-primary/20 mx-auto mb-4">
            <ShieldCheck className="text-white" size={24} />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t("profile.change_password")}</h1>
        </div>

        {error && (
          <div className="p-4 rounded-2xl bg-destructive/10 text-destructive text-sm font-medium flex items-center gap-2 animate-in fade-in zoom-in duration-300">
            <AlertCircle size={18} />
            {error}
          </div>
        )}

        {success && (
          <div className="p-4 rounded-2xl bg-primary/10 text-primary text-sm font-medium flex items-center gap-2 animate-in fade-in zoom-in duration-300">
            <CheckCircle2 size={18} />
            {t("profile.success_update")}
          </div>
        )}

        {!success && (
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-semibold ml-1">{t("profile.old_password")}</label>
                <div className="relative">
                  <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                  <input
                    type="password"
                    placeholder={t("profile.old_password_placeholder")}
                    value={oldPassword}
                    onChange={(e) => setOldPassword(e.target.value)}
                    className="w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                    required
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-semibold ml-1">{t("auth.new_password")}</label>
                <div className="relative">
                  <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                  <input
                    type="password"
                    placeholder={t("auth.new_password_placeholder")}
                    value={newPassword}
                    onChange={(e) => handleNewPasswordChange(e.target.value)}
                    className={`w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border outline-none transition-all ${
                      isPasswordDirty && !isPasswordValid 
                        ? 'border-destructive focus:ring-destructive/20' 
                        : 'border-border/50 focus:border-primary focus:ring-primary/20'
                    }`}
                    required
                  />
                </div>
                {isPasswordDirty && !isPasswordValid && (
                  <p className="text-xs text-destructive ml-1">{t("auth.invalid_password")}</p>
                )}
                <p className="text-[10px] text-muted-foreground ml-1 leading-tight">
                  {t("auth.password_requirement")}
                </p>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-semibold ml-1">{t("profile.confirm_password")}</label>
                <div className="relative">
                  <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                  <input
                    type="password"
                    placeholder={t("profile.confirm_password")}
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className={`w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border outline-none transition-all ${
                      confirmPassword && newPassword !== confirmPassword
                        ? 'border-destructive focus:ring-destructive/20' 
                        : 'border-border/50 focus:border-primary focus:ring-primary/20'
                    }`}
                    required
                  />
                </div>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading || !isPasswordValid || newPassword !== confirmPassword}
              className="w-full py-4 rounded-2xl bg-primary text-primary-foreground font-bold text-lg hover:opacity-90 transition-all shadow-xl shadow-primary/20 flex items-center justify-center gap-2 disabled:opacity-50"
            >
              {isLoading ? (
                <Loader2 className="animate-spin" size={20} />
              ) : (
                <>
                  {t("profile.save_changes")} <ArrowRight size={20} />
                </>
              )}
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
