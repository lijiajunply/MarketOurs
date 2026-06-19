import { useState, useEffect, useRef } from "react";
import { useNavigate, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { authService } from "@/services/authService";
import { fileService } from "@/services/fileService";
import { compressImage } from "@/services/imageCompression";
import { toast } from "@/lib/toast";
import { User, Mail, Lock, Loader2, ArrowRight, RefreshCw, Image, Camera } from "lucide-react";
import { PasswordField } from "@/components/auth/PasswordField";
import { SliderCaptcha } from "@/components/auth/SliderCaptcha";
import { DTO_LIMITS, passwordLength, requiredMax } from "@/lib/dtoValidation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

export default function RegisterPage() {
  const { t } = useTranslation();
  const [name, setName] = useState("");
  const [account, setAccount] = useState("");
  const [password, setPassword] = useState("");
  const [avatarUrl, setAvatarUrl] = useState("");
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [showAvatarOptions, setShowAvatarOptions] = useState(false);
  const [error, setError] = useState("");

  // Registration steps
  const [step, setStep] = useState<1 | 2>(1);
  const [regToken, setRegToken] = useState("");
  const [verificationCode, setVerificationCode] = useState("");
  const [countdown, setCountdown] = useState(0);
  const [showCaptcha, setShowCaptcha] = useState(false);

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
    setAvatarFile(null);
    if (avatarPreview) URL.revokeObjectURL(avatarPreview);
    setAvatarPreview("");
    setShowAvatarOptions(false);
  };

  useEffect(() => {
    generateRandomAvatar();
  }, []);

  const handleAvatarFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setShowAvatarOptions(false);
    setError("");
    if (avatarPreview) URL.revokeObjectURL(avatarPreview);
    const previewUrl = URL.createObjectURL(file);
    setAvatarFile(file);
    setAvatarPreview(previewUrl);
    setAvatarUrl("");
    e.target.value = '';
  };

  const validateAccount = (value: string) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const phoneRegex = /^1[3-9]\d{9}$/;
    if (value.length > DTO_LIMITS.userAccountMax) {
      setAccountType('invalid');
      return false;
    }
    if (emailRegex.test(value)) { setAccountType('email'); return true; }
    else if (phoneRegex.test(value)) { setAccountType('phone'); return true; }
    else { setAccountType('invalid'); return false; }
  };

  const validatePassword = (value: string) => {
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;
    const isValid = value.length <= DTO_LIMITS.userPasswordMax && passwordRegex.test(value);
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
    const nameError = requiredMax(name, DTO_LIMITS.userNameMax, t("validation.user_name_required"), t("validation.user_name_too_long", { max: DTO_LIMITS.userNameMax }));
    const accountError = requiredMax(account, DTO_LIMITS.userAccountMax, t("validation.account_required"), t("validation.account_too_long", { max: DTO_LIMITS.userAccountMax }));
    const passwordError = passwordLength(password, t("validation.password_required"), t("validation.password_too_short", { min: DTO_LIMITS.userPasswordMin }), t("validation.password_too_long", { max: DTO_LIMITS.userPasswordMax }));
    if (nameError || accountError || passwordError || accountType === 'invalid' || !isPasswordValid) {
      setError(nameError || accountError || passwordError || t("auth.error_registration_failed"));
      setIsLoading(false);
      return;
    }
    try {
      let avatar = avatarUrl;
      if (avatarFile) {
        const compressed = await compressImage(avatarFile, { quality: 0.85, maxWidth: 512, maxHeight: 512 });
        const uploadResponse = await fileService.uploadAvatar(compressed);
        if (uploadResponse.data) avatar = uploadResponse.data;
      }
      const response = await authService.register({ name, account, password, avatar });
      if (response.data) {
        setRegToken(response.data);
        setShowCaptcha(true);
      }
    } catch (err: any) {
      setError(err.message || t("auth.error_registration_failed"));
    } finally {
      setIsLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (countdown > 0 || !regToken) return;
    setShowCaptcha(true);
  };

  const handleCaptchaVerified = async (captchaToken: string) => {
    setShowCaptcha(false);
    setIsLoading(true);
    try {
      await authService.sendRegistrationCode(regToken, captchaToken);
      setStep(2);
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
      await authService.verifyRegistration({ registrationToken: regToken, code: verificationCode });
      toast.success(t("auth.register_success"));
      navigate("/login");
    } catch (err: any) {
      setError(err.message || t("auth.error_registration_failed"));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-md py-8 sm:py-12 px-4">
      <div className="glass-card rounded-3xl p-6 sm:p-8 space-y-7 animate-in fade-in zoom-in-95 duration-500">
        {/* Header */}
        <div className="text-center space-y-2">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary shadow-md shadow-primary/20">
            <span className="text-xl font-bold text-primary-foreground">M</span>
          </div>
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
            {step === 1 ? t("auth.create_account") : t("auth.verification")}
          </h1>
          <p className="text-sm text-muted-foreground">
            {step === 1 ? t("auth.join_community") : t("auth.verification_instruction")}
          </p>
        </div>

        {error && (
          <div className="rounded-2xl bg-destructive/10 p-4 text-sm font-medium text-destructive animate-in fade-in zoom-in-95 duration-300">
            {error}
          </div>
        )}

        {step === 1 ? (
          <form onSubmit={handleRegister} className="space-y-5">
            {/* Avatar */}
            <div className="flex flex-col items-center space-y-3">
              <Popover open={showAvatarOptions} onOpenChange={setShowAvatarOptions}>
                <PopoverTrigger
                  render={
                    <button type="button" className="group relative cursor-pointer">
                      <Avatar className="h-24 w-24 rounded-full ring-4 ring-primary/20 shadow-xl transition-transform group-hover:scale-105">
                        <AvatarImage src={avatarPreview || avatarUrl} />
                        <AvatarFallback className="text-2xl bg-primary/10 text-primary">
                          {name ? name.slice(0, 2).toUpperCase() : "?"}
                        </AvatarFallback>
                      </Avatar>
                      <span className="absolute -right-1 -bottom-1 flex h-8 w-8 items-center justify-center rounded-full bg-primary text-white shadow-lg transition-all">
                        <RefreshCw size={14} />
                      </span>
                    </button>
                  }
                />
                <PopoverContent className="w-48 rounded-2xl p-1.5" align="center">
                  <button
                    type="button"
                    onClick={generateRandomAvatar}
                    className="w-full flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium hover:bg-muted transition-colors"
                  >
                    <RefreshCw size={16} className="text-primary" />
                    {t("auth.random_avatar") || "随机生成"}
                  </button>
                  <button
                    type="button"
                    onClick={() => galleryInputRef.current?.click()}
                    className="w-full flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium hover:bg-muted transition-colors"
                  >
                    <Image size={16} className="text-primary" />
                    {t("auth.pick_from_gallery") || "从相册选择"}
                  </button>
                  <button
                    type="button"
                    onClick={() => cameraInputRef.current?.click()}
                    className="w-full flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium hover:bg-muted transition-colors"
                  >
                    <Camera size={16} className="text-primary" />
                    {t("auth.take_photo") || "拍照"}
                  </button>
                </PopoverContent>
              </Popover>
              <p className="text-xs text-muted-foreground font-medium">
                {t("auth.click_to_change_avatar") || "点击头像更换"}
              </p>
              <input ref={galleryInputRef} type="file" accept="image/*" onChange={handleAvatarFile} className="hidden" />
              <input ref={cameraInputRef} type="file" accept="image/*" capture="environment" onChange={handleAvatarFile} className="hidden" />
            </div>

            <div className="space-y-4">
              {/* Name */}
              <div className="space-y-1.5">
                <label className="text-sm font-medium ml-1">{t("auth.display_name")}</label>
                <div className="relative">
                  <User className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" size={16} />
                  <Input
                    type="text"
                    placeholder={t("auth.display_name_placeholder")}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    maxLength={DTO_LIMITS.userNameMax}
                    className="h-11 rounded-2xl pl-10"
                    required
                  />
                </div>
              </div>

              {/* Account */}
              <div className="space-y-1.5">
                <label className="text-sm font-medium ml-1">{t("auth.account")}</label>
                <div className="relative">
                  <Mail className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" size={16} />
                  <Input
                    type="text"
                    placeholder={t("auth.account_placeholder")}
                    value={account}
                    onChange={(e) => handleAccountChange(e.target.value)}
                    maxLength={DTO_LIMITS.userAccountMax}
                    className={`h-11 rounded-2xl pl-10 ${isAccountDirty && accountType === 'invalid' ? 'border-destructive focus-visible:ring-destructive/20' : ''}`}
                    required
                  />
                </div>
                {isAccountDirty && accountType === 'invalid' && (
                  <p className="text-xs text-destructive ml-1">{t("auth.invalid_account")}</p>
                )}
              </div>

              {/* Password */}
              <div className="space-y-1.5">
                <label className="text-sm font-medium ml-1">{t("auth.password")}</label>
                <PasswordField
                  icon={<Lock size={16} />}
                  placeholder={t("auth.password_placeholder")}
                  value={password}
                  onChange={(e) => handlePasswordChange(e.target.value)}
                  maxLength={DTO_LIMITS.userPasswordMax}
                  className={`h-11 rounded-2xl pl-10 ${isPasswordDirty && !isPasswordValid ? 'border-destructive focus-visible:ring-destructive/20' : ''}`}
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

            <Button
              type="submit"
              disabled={isLoading || accountType === 'invalid' || !isPasswordValid}
              size="lg"
              className="w-full rounded-2xl font-semibold shadow-md shadow-primary/20 gap-2"
            >
              {isLoading ? <Loader2 className="animate-spin" size={18} /> : <>{t("auth.signup")} <ArrowRight size={18} /></>}
            </Button>
          </form>
        ) : (
          /* Verification step */
          <form onSubmit={handleVerify} className="space-y-5">
            <p className="text-sm text-center font-medium">
              {t("auth.verification_code_sent_to")}{" "}
              <span className="text-primary">{account}</span>
            </p>
            <div className="space-y-1.5">
              <label className="text-sm font-medium ml-1">{t("auth.verification_code")}</label>
              <Input
                type="text"
                placeholder="------"
                value={verificationCode}
                onChange={(e) => setVerificationCode(e.target.value)}
                className="h-12 rounded-2xl text-center text-2xl tracking-[0.5em] font-bold"
                maxLength={6}
                required
              />
            </div>
            <div className="text-center">
              <Button
                type="button"
                variant="link"
                onClick={handleResendCode}
                disabled={isLoading || countdown > 0}
              >
                {countdown > 0
                  ? t("auth.resend_code_in", { count: countdown })
                  : t("auth.resend_code")}
              </Button>
            </div>
            <Button
              type="submit"
              disabled={isLoading || verificationCode.length < 4}
              size="lg"
              className="w-full rounded-2xl font-semibold shadow-md shadow-primary/20 gap-2"
            >
              {isLoading ? <Loader2 className="animate-spin" size={18} /> : <>{t("auth.verify_and_complete")} <ArrowRight size={18} /></>}
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() => setStep(1)}
              className="w-full rounded-xl"
            >
              {t("auth.back_to_registration")}
            </Button>
          </form>
        )}

        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            {t("auth.already_have_account")}{" "}
            <Link to="/login" className="font-semibold text-primary hover:underline">{t("auth.signin")}</Link>
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
