import { useState, useEffect, useRef } from "react";
import { useNavigate, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { authService } from "../../services/authService";
import { fileService } from "../../services/fileService";
import { User, Mail, Lock, Loader2, ArrowRight, RefreshCw, Image, Camera } from "lucide-react";
import { PasswordField } from "../../components/auth/PasswordField";

export default function RegisterPage() {
  const { t } = useTranslation();
  const [name, setName] = useState("");
  const [account, setAccount] = useState("");
  const [password, setPassword] = useState("");
  const [avatarUrl, setAvatarUrl] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
  const [showAvatarOptions, setShowAvatarOptions] = useState(false);
  const [error, setError] = useState("");

  // Registration steps
  const [step, setStep] = useState<1 | 2>(1);
  const [regToken, setRegToken] = useState("");
  const [verificationCode, setVerificationCode] = useState("");
  const [countdown, setCountdown] = useState(0);

  const navigate = useNavigate();
  const galleryInputRef = useRef<HTMLInputElement>(null);
  const cameraInputRef = useRef<HTMLInputElement>(null);
  const [accountType, setAccountType] = useState<'email' | 'phone' | 'invalid'>('invalid');
  const [isAccountDirty, setIsAccountDirty] = useState(false);
  const [isPasswordDirty, setIsPasswordDirty] = useState(false);
  const [isPasswordValid, setIsPasswordValid] = useState(false);

  useEffect(() => {
    let timer: ReturnType<typeof setInterval>;
    if (countdown > 0) {
      timer = setInterval(() => setCountdown(c => c - 1), 1000);
    }
    return () => clearInterval(timer);
  }, [countdown]);

  const generateRandomAvatar = () => {
    const randomSeed = Math.random().toString(36).substring(7);
    setAvatarUrl(`https://api.dicebear.com/9.x/avataaars/svg?seed=${randomSeed}`);
    setShowAvatarOptions(false);
  };

  // Generate a random avatar on mount
  useEffect(() => {
    generateRandomAvatar();
  }, []);

  const handleAvatarFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setShowAvatarOptions(false);
    setIsUploadingAvatar(true);
    setError("");

    // Immediate preview
    const previewUrl = URL.createObjectURL(file);
    setAvatarUrl(previewUrl);

    try {
      const response = await fileService.uploadImage(file);
      if (response.data) {
        setAvatarUrl(response.data);
      }
    } catch (err: any) {
      setError(err.message || t("auth.error_registration_failed"));
      // Revert to random on failure
      generateRandomAvatar();
    } finally {
      setIsUploadingAvatar(false);
      URL.revokeObjectURL(previewUrl);
      // Reset input so the same file can be re-selected
      e.target.value = '';
    }
  };

  const validateAccount = (value: string) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const phoneRegex = /^1[3-9]\d{9}$/;

    if (emailRegex.test(value)) {
      setAccountType('email');
      return true;
    } else if (phoneRegex.test(value)) {
      setAccountType('phone');
      return true;
    } else {
      setAccountType('invalid');
      return false;
    }
  };

  const validatePassword = (value: string) => {
    // At least 6 characters, one uppercase, one lowercase, one number
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;
    const isValid = passwordRegex.test(value);
    setIsPasswordValid(isValid);
    return isValid;
  };

  const handleAccountChange = (value: string) => {
    setAccount(value);
    setIsAccountDirty(true);
    validateAccount(value);
  };

  const handlePasswordChange = (value: string) => {
    setPassword(value);
    setIsPasswordDirty(true);
    validatePassword(value);
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      const response = await authService.register({
        name,
        account,
        password,
        avatar: avatarUrl
      });

      if (response.data) {
        setRegToken(response.data);
        // Automatically send code
        await authService.sendRegistrationCode(response.data);
        setStep(2);
        setCountdown(60);
      }
    } catch (err: any) {
      setError(err.message || t("auth.error_registration_failed"));
    } finally {
      setIsLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (countdown > 0 || !regToken) return;
    setIsLoading(true);
    try {
      await authService.sendRegistrationCode(regToken);
      setCountdown(60);
    } catch (err: any) {
      setError(err.message || t("auth.error_registration_failed"));
    } finally {
      setIsLoading(false);
    }
  };

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      await authService.verifyRegistration({
        registrationToken: regToken,
        code: verificationCode
      });
      navigate("/login");
    } catch (err: any) {
      setError(err.message || t("auth.error_registration_failed"));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="max-w-md mx-auto py-12 px-4">
      <div className="glass-card rounded-[2.5rem] p-8 space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
        <div className="text-center space-y-2">
          <div className="h-12 w-12 rounded-2xl bg-primary flex items-center justify-center shadow-lg shadow-primary/20 mx-auto mb-4">
            <span className="text-white font-bold text-2xl">M</span>
          </div>
          <h1 className="text-3xl font-bold tracking-tight">
            {step === 1 ? t("auth.create_account") : t("auth.verification")}
          </h1>
          <p className="text-muted-foreground">
            {step === 1 ? t("auth.join_community") : t("auth.verification_instruction")}
          </p>
        </div>

        {error && (
          <div className="p-4 rounded-2xl bg-destructive/10 text-destructive text-sm font-medium animate-in fade-in zoom-in duration-300">
            {error}
          </div>
        )}

        {step === 1 ? (
          <form onSubmit={handleRegister} className="space-y-6">
            {/* Avatar Selection Area */}
            <div className="flex flex-col items-center space-y-4">
              <div className="relative">
                <button
                  type="button"
                  onClick={() => setShowAvatarOptions(!showAvatarOptions)}
                  disabled={isUploadingAvatar}
                  className="relative group cursor-pointer disabled:cursor-wait"
                >
                  <div className="h-24 w-24 rounded-full overflow-hidden border-4 border-primary/20 shadow-xl transition-transform hover:scale-105">
                    {isUploadingAvatar ? (
                      <div className="h-full w-full flex items-center justify-center bg-muted">
                        <Loader2 className="animate-spin text-primary" size={32} />
                      </div>
                    ) : (
                      <img
                        src={avatarUrl}
                        alt="Avatar Preview"
                        className="h-full w-full object-cover"
                      />
                    )}
                  </div>
                  <div className="absolute -right-2 -bottom-2 p-2 bg-primary text-white rounded-full shadow-lg transition-all duration-500">
                    <RefreshCw size={16} className={isUploadingAvatar ? "animate-spin" : ""} />
                  </div>
                </button>

                {/* Avatar options popover */}
                {showAvatarOptions && (
                  <>
                    <div
                      className="fixed inset-0 z-10"
                      onClick={() => setShowAvatarOptions(false)}
                    />
                    <div className="absolute top-full mt-2 left-1/2 -translate-x-1/2 z-20 bg-card border border-border rounded-2xl shadow-xl p-2 min-w-[170px] animate-in fade-in zoom-in-95 duration-200">
                      <button
                        type="button"
                        onClick={generateRandomAvatar}
                        className="w-full flex items-center gap-3 px-4 py-3 rounded-xl hover:bg-muted text-sm font-semibold transition-colors"
                      >
                        <RefreshCw size={16} className="text-primary" />
                        {t("auth.random_avatar") || "随机生成"}
                      </button>
                      <button
                        type="button"
                        onClick={() => galleryInputRef.current?.click()}
                        className="w-full flex items-center gap-3 px-4 py-3 rounded-xl hover:bg-muted text-sm font-semibold transition-colors"
                      >
                        <Image size={16} className="text-primary" />
                        {t("auth.pick_from_gallery") || "从相册选择"}
                      </button>
                      <button
                        type="button"
                        onClick={() => cameraInputRef.current?.click()}
                        className="w-full flex items-center gap-3 px-4 py-3 rounded-xl hover:bg-muted text-sm font-semibold transition-colors"
                      >
                        <Camera size={16} className="text-primary" />
                        {t("auth.take_photo") || "拍照"}
                      </button>
                    </div>
                  </>
                )}
              </div>
              <p className="text-xs text-muted-foreground font-medium">
                {t("auth.click_to_change_avatar") || "点击头像更换，支持上传和拍照"}
              </p>

              {/* Hidden file inputs */}
              <input
                ref={galleryInputRef}
                type="file"
                accept="image/*"
                onChange={handleAvatarFile}
                className="hidden"
              />
              <input
                ref={cameraInputRef}
                type="file"
                accept="image/*"
                capture="environment"
                onChange={handleAvatarFile}
                className="hidden"
              />
            </div>

            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-semibold ml-1">{t("auth.display_name")}</label>
                <div className="relative">
                  <User className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                  <input
                    type="text"
                    placeholder={t("auth.display_name_placeholder")}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    className="w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                    required
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-semibold ml-1">{t("auth.account")}</label>
                <div className="relative">
                  <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                  <input
                    type="text"
                    placeholder={t("auth.account_placeholder")}
                    value={account}
                    onChange={(e) => handleAccountChange(e.target.value)}
                    className={`w-full pl-12 pr-4 py-3 rounded-2xl bg-muted/50 border outline-none transition-all ${
                      isAccountDirty && accountType === 'invalid'
                        ? 'border-destructive focus:ring-destructive/20'
                        : 'border-border/50 focus:border-primary focus:ring-primary/20'
                    }`}
                    required
                  />
                </div>
                {isAccountDirty && accountType === 'invalid' && (
                  <p className="text-xs text-destructive ml-1">{t("auth.invalid_account")}</p>
                )}
              </div>

              <div className="space-y-2">
                <label className="text-sm font-semibold ml-1">{t("auth.password")}</label>
                <PasswordField
                  icon={<Lock size={18} />}
                  placeholder={t("auth.password_placeholder")}
                  value={password}
                  onChange={(e) => handlePasswordChange(e.target.value)}
                  className={`w-full pl-12 pr-12 py-3 rounded-2xl bg-muted/50 border outline-none transition-all ${
                    isPasswordDirty && !isPasswordValid
                      ? 'border-destructive focus:ring-destructive/20'
                      : 'border-border/50 focus:border-primary focus:ring-primary/20'
                  }`}
                  required
                />
                {isPasswordDirty && !isPasswordValid && (
                  <p className="text-xs text-destructive ml-1">{t("auth.invalid_password")}</p>
                )}
                <p className="text-[10px] text-muted-foreground ml-1 leading-tight">
                  {t("auth.password_requirement")}
                </p>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading || isUploadingAvatar || accountType === 'invalid' || !isPasswordValid}
              className="w-full py-4 rounded-2xl bg-primary text-primary-foreground font-bold text-lg hover:opacity-90 transition-all shadow-xl shadow-primary/20 flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? (
                <Loader2 className="animate-spin" size={20} />
              ) : (
                <>
                  {t("auth.signup")} <ArrowRight size={20} />
                </>
              )}
            </button>
          </form>
        ) : (
          <form onSubmit={handleVerify} className="space-y-6">
            <div className="space-y-2 text-center">
              <p className="text-sm font-medium">{t("auth.verification_code_sent_to")} <span className="text-primary">{account}</span></p>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold ml-1">{t("auth.verification_code")}</label>
              <div className="relative">
                <input
                  type="text"
                  placeholder="------"
                  value={verificationCode}
                  onChange={(e) => setVerificationCode(e.target.value)}
                  className="w-full px-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all text-center text-2xl tracking-[1em] font-bold"
                  maxLength={6}
                  required
                />
              </div>
            </div>

            <div className="text-center">
              <button
                type="button"
                onClick={handleResendCode}
                disabled={isLoading || countdown > 0}
                className="text-sm font-bold text-primary hover:underline disabled:opacity-50 disabled:no-underline"
              >
                {countdown > 0
                  ? t("auth.resend_code_in", { count: countdown })
                  : t("auth.resend_code")}
              </button>
            </div>

            <button
              type="submit"
              disabled={isLoading || verificationCode.length < 4}
              className="w-full py-4 rounded-2xl bg-primary text-primary-foreground font-bold text-lg hover:opacity-90 transition-all shadow-xl shadow-primary/20 flex items-center justify-center gap-2 disabled:opacity-50"
            >
              {isLoading ? (
                <Loader2 className="animate-spin" size={20} />
              ) : (
                <>
                  {t("auth.verify_and_complete")} <ArrowRight size={20} />
                </>
              )}
            </button>

            <button
              type="button"
              onClick={() => setStep(1)}
              className="w-full py-2 text-sm text-muted-foreground hover:text-primary transition-colors"
            >
              {t("auth.back_to_registration")}
            </button>
          </form>
        )}

        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            {t("auth.already_have_account")}{" "}
            <Link to="/login" className="font-bold text-primary hover:underline">{t("auth.signin")}</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
