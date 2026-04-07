import { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router";
import { useDispatch } from "react-redux";
import { useTranslation } from "react-i18next";
import { authService } from "../../services/authService";
import { setCredentials } from "../../stores/authSlice";
import { Mail, Lock, Loader2, ArrowRight, GitBranch, ShieldCheck } from "lucide-react";

export default function LoginPage() {
  const { t } = useTranslation();
  const [loginMode, setLoginMode] = useState<"password" | "otp">("password");
  const [account, setAccount] = useState("");
  const [password, setPassword] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [countdown, setCountdown] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [isSendingCode, setIsSendingCode] = useState(false);
  const [error, setError] = useState("");
  
  const navigate = useNavigate();
  const dispatch = useDispatch();

  useEffect(() => {
    let timer: any;
    if (countdown > 0) {
      timer = setInterval(() => {
        setCountdown((prev) => prev - 1);
      }, 1000);
    }
    return () => clearInterval(timer);
  }, [countdown]);

  const handleSendCode = async () => {
    if (!account) {
      setError(t("auth.invalid_account"));
      return;
    }

    setIsSendingCode(true);
    setError("");
    try {
      await authService.sendLoginCode({ account });
      setCountdown(60);
    } catch (err: any) {
      setError(err.message || t("auth.error_failed_to_send_code"));
    } finally {
      setIsSendingCode(false);
    }
  };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      const response = loginMode === "password" 
        ? await authService.login({ account, password })
        : await authService.loginByCode({ account, code: otpCode, deviceType: "Web" });

      if (response.data) {
        const userInfo = await authService.getInfo();
        dispatch(setCredentials({ 
          user: userInfo.data, 
          token: response.data.accessToken 
        }));
        navigate("/");
      }
    } catch (err: any) {
      setError(err.message || (loginMode === "password" ? t("auth.error_failed_to_login") : t("auth.error_invalid_otp")));
    } finally {
      setIsLoading(false);
    }
  };

  const handleThirdPartyLogin = (provider: string) => {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || "https://marketours-api.zeabur.app";
    const returnUrl = window.location.origin + "/login-callback";
    window.location.href = `${baseUrl}/Auth/external-login?provider=${provider}&returnUrl=${encodeURIComponent(returnUrl)}`;
  };

  const thirdPartyLogins = [
    { 
      name: "Ours", 
      id: "Ours",
      icon: (
        <div className="h-5 w-5 rounded-full bg-primary flex items-center justify-center text-[10px] text-white font-bold">
          O
        </div>
      )
    },
    { 
      name: "Google", 
      id: "Google",
      icon: (
        <svg viewBox="0 0 24 24" width="20" height="20" className="fill-current">
          <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
          <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-1 .67-2.28 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
          <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
          <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
        </svg>
      )
    },
    { 
      name: "Github", 
      id: "Github",
      icon: <GitBranch size={18} /> 
    },
    { 
      name: "Weixin", 
      id: "Weixin",
      icon: (
        <svg viewBox="0 0 24 24" width="18" height="18" className="fill-current">
          <path d="M8.2 13.5c-.5 0-1-.4-1-.9s.4-1 .9-1 1 .4 1 .9-.4.9-1 .9zm5.6 0c-.5 0-1-.4-1-.9s.4-1 1-1 .9.4.9.9-.4.9-.9.9zM18 10c0-3.9-3.8-7-8.5-7S2 6.1 2 10c0 2.1 1.1 4 2.9 5.3l-.7 2.2 2.5-1.3c.6.2 1.2.3 1.8.3 4.7 0 8.5-3.1 8.5-7zm3.5 6.5c0-3.3-3.1-6-7-6-.3 0-.6 0-.9.1 2.4 1.8 3.9 4.3 3.9 6.9 0 .5-.1 1-.2 1.5.9.6 1.5 1.4 1.5 2.3 0 .4-.1.8-.3 1.1l1.1.6-.3-1c.8-.7 1.2-1.6 1.2-2.5-1.3 0-2.3-1.1-2.3-2.3z" fill="#07C160"/>
        </svg>
      )
    },
  ];

  return (
    <div className="max-w-md mx-auto py-12 px-4">
      <div className="glass-card rounded-[2.5rem] p-8 space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
        <div className="text-center space-y-2">
          <div className="h-12 w-12 rounded-2xl bg-primary flex items-center justify-center shadow-lg shadow-primary/20 mx-auto mb-4">
            <span className="text-white font-bold text-2xl">M</span>
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t("auth.welcome_back")}</h1>
          <p className="text-muted-foreground">{t("auth.signin_to_account")}</p>
        </div>

        {error && (
          <div className="p-4 rounded-2xl bg-destructive/10 text-destructive text-sm font-medium animate-in fade-in zoom-in duration-300">
            {error}
          </div>
        )}

        <div className="flex p-1 bg-muted/50 rounded-2xl gap-1">
          <button
            onClick={() => setLoginMode("password")}
            className={`flex-1 py-2 rounded-xl text-sm font-bold transition-all ${
              loginMode === "password" ? "bg-card shadow-sm text-primary" : "text-muted-foreground hover:bg-muted"
            }`}
          >
            {t("auth.login_mode_password")}
          </button>
          <button
            onClick={() => setLoginMode("otp")}
            className={`flex-1 py-2 rounded-xl text-sm font-bold transition-all ${
              loginMode === "otp" ? "bg-card shadow-sm text-primary" : "text-muted-foreground hover:bg-muted"
            }`}
          >
            {t("auth.login_mode_otp")}
          </button>
        </div>

        <form onSubmit={handleLogin} className="space-y-6">
          <div className="space-y-4">
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

            {loginMode === "password" ? (
              <div className="space-y-2 animate-in fade-in slide-in-from-top-2 duration-300">
                <div className="flex justify-between items-center ml-1">
                  <label className="text-sm font-semibold">{t("auth.password")}</label>
                  <Link to="/forgot-password" className="text-xs font-bold text-primary hover:underline">
                    {t("auth.forgot_password")}
                  </Link>
                </div>
                <div className="relative">
                  <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                  <input
                    type="password"
                    placeholder={t("auth.password_placeholder")}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                    required
                  />
                </div>
              </div>
            ) : (
              <div className="space-y-2 animate-in fade-in slide-in-from-top-2 duration-300">
                <label className="text-sm font-semibold ml-1">{t("auth.verification_code")}</label>
                <div className="flex gap-3">
                  <div className="relative flex-1">
                    <ShieldCheck className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                    <input
                      type="text"
                      maxLength={6}
                      placeholder={t("auth.verification_code_placeholder")}
                      value={otpCode}
                      onChange={(e) => setOtpCode(e.target.value)}
                      className="w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                      required
                    />
                  </div>
                  <button
                    type="button"
                    disabled={countdown > 0 || isSendingCode}
                    onClick={handleSendCode}
                    className="px-6 rounded-2xl bg-primary/10 text-primary text-sm font-bold hover:bg-primary/20 transition-all disabled:opacity-50 whitespace-nowrap min-w-[120px]"
                  >
                    {isSendingCode ? (
                      <Loader2 className="animate-spin mx-auto" size={18} />
                    ) : countdown > 0 ? (
                      t("auth.resend_code", { count: countdown })
                    ) : (
                      t("auth.send_code")
                    )}
                  </button>
                </div>
              </div>
            )}
          </div>

          <button
            type="submit"
            disabled={isLoading}
            className="w-full py-4 rounded-2xl bg-primary text-primary-foreground font-bold text-lg hover:opacity-90 transition-all shadow-xl shadow-primary/20 flex items-center justify-center gap-2 disabled:opacity-50"
          >
            {isLoading ? (
              <Loader2 className="animate-spin" size={20} />
            ) : (
              <>
                {t("auth.signin")} <ArrowRight size={20} />
              </>
            )}
          </button>
        </form>

        <div className="space-y-6">
          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-border/50"></div>
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-4 text-muted-foreground font-medium tracking-widest">{t("auth.or_continue_with")}</span>
            </div>
          </div>

          <div className="grid grid-cols-4 gap-4">
            {thirdPartyLogins.map((provider) => (
              <button
                key={provider.name}
                onClick={() => handleThirdPartyLogin(provider.id)}
                className="flex items-center justify-center p-3 rounded-2xl border border-border/50 hover:bg-muted transition-all duration-300 group"
                title={t("auth.login_with", { provider: provider.name })}
              >
                <div className="group-hover:scale-110 transition-transform">
                  {provider.icon}
                </div>
              </button>
            ))}
          </div>
        </div>

        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            {t("auth.dont_have_account")}{" "}
            <Link to="/register" className="font-bold text-primary hover:underline">{t("auth.signup")}</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
