import { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router";
import { useDispatch } from "react-redux";
import { useTranslation } from "react-i18next";
import { authService } from "@/services/authService";
import { writeAuthSession } from "@/services/authSession";
import { setCredentials } from "@/stores/authSlice";
import { toast } from "@/lib/toast";
import { Mail, Lock, Loader2, ArrowRight, ShieldCheck, GraduationCap } from "lucide-react";
import { PasswordField } from "@/components/auth/PasswordField";
import { SliderCaptcha } from "@/components/auth/SliderCaptcha";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Separator } from "@/components/ui/separator";

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
  const [showCaptcha, setShowCaptcha] = useState(false);

  const navigate = useNavigate();
  const dispatch = useDispatch();

  useEffect(() => {
    let timer: ReturnType<typeof setInterval>;
    if (countdown > 0) {
      timer = setInterval(() => setCountdown((prev) => prev - 1), 1000);
    }
    return () => clearInterval(timer);
  }, [countdown]);

  const handleSendCode = async () => {
    if (!account) {
      setError(t("auth.invalid_account"));
      return;
    }
    setShowCaptcha(true);
  };

  const handleCaptchaVerified = async (captchaToken: string) => {
    setShowCaptcha(false);
    setIsSendingCode(true);
    setError("");
    try {
      await authService.sendLoginCode({ account, captchaToken });
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
        ? await authService.login({ account, password, deviceType: "Web" })
        : await authService.loginByCode({ account, code: otpCode, deviceType: "Web" });

      if (response.data) {
        writeAuthSession({
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken,
        });
        const userInfo = await authService.getInfo();
        dispatch(setCredentials({
          user: userInfo.data,
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken,
        }));
        toast.success(t("auth.login_success"));
        navigate("/");
      }
    } catch (err: any) {
      setError(err.message || (loginMode === "password"
        ? t("auth.error_failed_to_login")
        : t("auth.error_invalid_otp")));
    } finally {
      setIsLoading(false);
    }
  };

  const handleThirdPartyLogin = (provider: string) => {
    const returnUrl = window.location.origin + "/login-callback";
    window.location.href = authService.getExternalLoginUrl(provider, returnUrl);
  };

  const thirdPartyLogins = [
    {
      name: "Ours",
      id: "Ours",
      icon: <GraduationCap size={20} />,
    },
    {
      name: "Google",
      id: "Google",
      icon: (
        <svg viewBox="0 0 24 24" width="18" height="18" className="fill-current">
          <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
          <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-1 .67-2.28 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
          <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
          <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
        </svg>
      ),
    },
    {
      name: "Github",
      id: "Github",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24">
          <path fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 19c-4.3 1.4-4.3-2.5-6-3m12 5v-3.5c0-1 .1-1.4-.5-2c2.8-.3 5.5-1.4 5.5-6a4.6 4.6 0 0 0-1.3-3.2a4.2 4.2 0 0 0-.1-3.2s-1.1-.3-3.5 1.3a12.3 12.3 0 0 0-6.2 0C6.5 2.8 5.4 3.1 5.4 3.1a4.2 4.2 0 0 0-.1 3.2A4.6 4.6 0 0 0 4 9.5c0 4.6 2.7 5.7 5.5 6c-.6.6-.6 1.2-.5 2V21" />
        </svg>
      ),
    },
    {
      name: "Weixin",
      id: "Weixin",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24">
          <g fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
            <path d="M16.5 10c3.038 0 5.5 2.015 5.5 4.5c0 1.397-.778 2.645-2 3.47V20l-1.964-1.178A6.7 6.7 0 0 1 16.5 19c-3.038 0-5.5-2.015-5.5-4.5s2.462-4.5 5.5-4.5" />
            <path d="M11.197 15.698c-.69.196-1.43.302-2.197.302a8 8 0 0 1-2.612-.432L4 17v-2.801C2.763 13.117 2 11.635 2 10c0-3.314 3.134-6 7-6c3.782 0 6.863 2.57 7 5.785v.233M10 8h.01M7 8h.01M15 14h.01M18 14h.01" />
          </g>
        </svg>
      ),
    },
  ];

  return (
    <div className="mx-auto max-w-md py-8 sm:py-12 px-4">
      <div className="glass-card rounded-3xl p-6 sm:p-8 space-y-7 animate-in fade-in zoom-in-95 duration-500">
        {/* Header */}
        <div className="text-center space-y-2">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary shadow-md shadow-primary/20">
            <span className="text-xl font-bold text-primary-foreground">M</span>
          </div>
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
            {t("auth.welcome_back")}
          </h1>
          <p className="text-sm text-muted-foreground">{t("auth.signin_to_account")}</p>
        </div>

        {/* Error */}
        {error && (
          <div className="rounded-2xl bg-destructive/10 p-4 text-sm font-medium text-destructive animate-in fade-in zoom-in-95 duration-300">
            {error}
          </div>
        )}

        {/* Login Mode Tabs */}
        <Tabs
          value={loginMode}
          onValueChange={(v) => setLoginMode(v as "password" | "otp")}
          className="w-full"
        >
          <TabsList className="w-full rounded-2xl p-1">
            <TabsTrigger value="password" className="flex-1 rounded-xl text-sm font-medium">
              {t("auth.login_mode_password")}
            </TabsTrigger>
            <TabsTrigger value="otp" className="flex-1 rounded-xl text-sm font-medium">
              {t("auth.login_mode_otp")}
            </TabsTrigger>
          </TabsList>
        </Tabs>

        <form onSubmit={handleLogin} className="space-y-5">
          <div className="space-y-4">
            {/* Account */}
            <div className="space-y-1.5">
              <label className="text-sm font-medium ml-1">{t("auth.account")}</label>
              <div className="relative">
                <Mail className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" size={16} />
                <Input
                  type="text"
                  placeholder={t("auth.account_placeholder")}
                  value={account}
                  onChange={(e) => setAccount(e.target.value)}
                  className="h-11 rounded-2xl pl-10"
                  required
                />
              </div>
            </div>

            {loginMode === "password" ? (
              <div className="space-y-1.5 animate-in fade-in slide-in-from-top-2 duration-300">
                <div className="flex justify-between items-center ml-1">
                  <label className="text-sm font-medium">{t("auth.password")}</label>
                  <Link to="/forgot-password" className="text-xs font-medium text-primary hover:underline">
                    {t("auth.forgot_password")}
                  </Link>
                </div>
                <PasswordField
                  icon={<Lock size={16} />}
                  placeholder={t("auth.password_placeholder")}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="h-11 rounded-2xl pl-10 bg-transparent"
                  required
                />
              </div>
            ) : (
              <div className="space-y-1.5 animate-in fade-in slide-in-from-top-2 duration-300">
                <label className="text-sm font-medium ml-1">{t("auth.verification_code")}</label>
                <div className="flex gap-3">
                  <div className="relative flex-1">
                    <ShieldCheck className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" size={16} />
                    <Input
                      type="text"
                      maxLength={6}
                      placeholder={t("auth.verification_code_placeholder")}
                      value={otpCode}
                      onChange={(e) => setOtpCode(e.target.value)}
                      className="h-11 rounded-2xl pl-10"
                      required
                    />
                  </div>
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    disabled={countdown > 0 || isSendingCode}
                    onClick={handleSendCode}
                    className="h-11 rounded-2xl whitespace-nowrap min-w-[110px] font-medium"
                  >
                    {isSendingCode ? (
                      <Loader2 className="animate-spin" size={16} />
                    ) : countdown > 0 ? (
                      t("auth.resend_code", { count: countdown })
                    ) : (
                      t("auth.send_code")
                    )}
                  </Button>
                </div>
              </div>
            )}
          </div>

          <Button
            type="submit"
            disabled={isLoading}
            size="lg"
            className="w-full rounded-2xl font-semibold shadow-md shadow-primary/20 gap-2"
          >
            {isLoading ? (
              <Loader2 className="animate-spin" size={18} />
            ) : (
              <>
                {t("auth.signin")} <ArrowRight size={18} />
              </>
            )}
          </Button>
        </form>

        {/* Third-party login */}
        <div className="space-y-5">
          <div className="relative">
            <Separator />
            <span className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-card px-4 text-xs font-medium text-muted-foreground uppercase tracking-wider">
              {t("auth.or_continue_with")}
            </span>
          </div>

          <div className="grid grid-cols-4 gap-3">
            {thirdPartyLogins.map((provider) => (
              <Button
                key={provider.name}
                variant="outline"
                size="icon"
                onClick={() => handleThirdPartyLogin(provider.id)}
                className="h-11 w-full rounded-2xl hover:bg-muted transition-all"
                title={t("auth.login_with", { provider: provider.name })}
              >
                {provider.icon}
              </Button>
            ))}
          </div>
        </div>

        {/* Sign up link */}
        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            {t("auth.dont_have_account")}{" "}
            <Link to="/register" className="font-semibold text-primary hover:underline">
              {t("auth.signup")}
            </Link>
          </p>
        </div>
      </div>

      {showCaptcha && (
        <SliderCaptcha
          onVerify={handleCaptchaVerified}
          onCancel={() => setShowCaptcha(false)}
        />
      )}
    </div>
  );
}
