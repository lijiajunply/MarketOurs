import { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { authService } from "../../services/authService";
import { Mail, Lock, Loader2, ArrowRight, Key, CheckCircle2, AlertCircle } from "lucide-react";

export default function ForgotPasswordPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  
  const [account, setAccount] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [isPasswordDirty, setIsPasswordDirty] = useState(false);
  const [isPasswordValid, setIsPasswordValid] = useState(false);
  const [step, setStep] = useState(1); // 1: Send Code, 2: Reset

  const validatePassword = (value: string) => {
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;
    const isValid = passwordRegex.test(value);
    setIsPasswordValid(isValid);
    return isValid;
  };

  const handlePasswordChange = (value: string) => {
    setNewPassword(value);
    setIsPasswordDirty(true);
    validatePassword(value);
  };
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [countdown, setCountdown] = useState(0);

  useEffect(() => {
    let timer: any;
    if (countdown > 0) {
      timer = setInterval(() => {
        setCountdown((prev) => prev - 1);
      }, 1000);
    }
    return () => clearInterval(timer);
  }, [countdown]);

  const handleSendCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      await authService.forgotPassword({ account });
      setStep(2);
      setCountdown(60);
    } catch (err: any) {
      setError(err.message || t("common.error"));
    } finally {
      setIsLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      await authService.resetPassword({ 
        token: code, // Assuming code is used as token
        newPassword 
      });
      setSuccess(true);
      setTimeout(() => navigate("/login"), 3000);
    } catch (err: any) {
      setError(err.message || t("common.error"));
    } finally {
      setIsLoading(false);
    }
  };

  const isAccountValid = () => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const phoneRegex = /^1[3-9]\d{9}$/;
    return emailRegex.test(account) || phoneRegex.test(account);
  };

  return (
    <div className="max-w-md mx-auto py-12 px-4">
      <div className="glass-card rounded-[2.5rem] p-8 space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
        <div className="text-center space-y-2">
          <div className="h-12 w-12 rounded-2xl bg-primary flex items-center justify-center shadow-lg shadow-primary/20 mx-auto mb-4">
            <Key className="text-white" size={24} />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t("auth.forgot_password_title")}</h1>
          <p className="text-muted-foreground">{t("auth.forgot_password_desc")}</p>
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
            {t("auth.reset_success")}
          </div>
        )}

        {!success && (
          <form onSubmit={step === 1 ? handleSendCode : handleResetPassword} className="space-y-6">
            <div className="space-y-4">
              {step === 1 ? (
                <div className="space-y-2">
                  <label className="text-sm font-semibold ml-1">{t("auth.account")}</label>
                  <div className="relative">
                    <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                    <input
                      type="text"
                      placeholder={t("auth.account_placeholder")}
                      value={account}
                      onChange={(e) => setAccount(e.target.value)}
                      className="w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                      required
                    />
                  </div>
                </div>
              ) : (
                <>
                  <div className="space-y-2">
                    <label className="text-sm font-semibold ml-1">{t("auth.verification_code")}</label>
                    <div className="relative">
                      <Key className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                      <input
                        type="text"
                        placeholder={t("auth.verification_code_placeholder")}
                        value={code}
                        onChange={(e) => setCode(e.target.value)}
                        className="w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                        required
                      />
                      {countdown > 0 && (
                        <div className="absolute right-4 top-1/2 -translate-y-1/2 text-xs font-bold text-primary">
                          {t("auth.resend_code", { count: countdown })}
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="space-y-2">
                    <label className="text-sm font-semibold ml-1">{t("auth.new_password")}</label>
                    <div className="relative">
                      <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                      <input
                        type="password"
                        placeholder={t("auth.password_placeholder")}
                        value={newPassword}
                        onChange={(e) => handlePasswordChange(e.target.value)}
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
                </>
              )}
            </div>

            <button
              type="submit"
              disabled={isLoading || (step === 1 && !isAccountValid()) || (step === 2 && !isPasswordValid)}
              className="w-full py-4 rounded-2xl bg-primary text-primary-foreground font-bold text-lg hover:opacity-90 transition-all shadow-xl shadow-primary/20 flex items-center justify-center gap-2 disabled:opacity-50"
            >
              {isLoading ? (
                <Loader2 className="animate-spin" size={20} />
              ) : (
                <>
                  {step === 1 ? t("auth.send_code") : t("auth.reset_password_btn")} <ArrowRight size={20} />
                </>
              )}
            </button>
            
            {step === 2 && countdown === 0 && (
              <button
                type="button"
                onClick={handleSendCode}
                className="w-full text-sm font-bold text-primary hover:underline"
              >
                {t("auth.resend_code_action")}
              </button>
            )}
          </form>
        )}

        <div className="text-center">
          <Link to="/login" className="text-sm font-bold text-muted-foreground hover:text-primary transition-colors">
            {t("auth.back_to_login")}
          </Link>
        </div>
      </div>
    </div>
  );
}
