import { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { authService } from "../../services/authService";
import { User, Mail, Lock, Loader2, ArrowRight, RefreshCw } from "lucide-react";

export default function RegisterPage() {
  const { t } = useTranslation();
  const [name, setName] = useState("");
  const [account, setAccount] = useState("");
  const [password, setPassword] = useState("");
  const [avatarSeed, setAvatarSeed] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  
  const navigate = useNavigate();
  const [accountType, setAccountType] = useState<'email' | 'phone' | 'invalid'>('invalid');
  const [isAccountDirty, setIsAccountDirty] = useState(false);
  const [isPasswordDirty, setIsPasswordDirty] = useState(false);
  const [isPasswordValid, setIsPasswordValid] = useState(false);

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

  // Generate a random seed on mount
  useEffect(() => {
    generateRandomAvatar();
  }, []);

  const generateRandomAvatar = () => {
    const randomSeed = Math.random().toString(36).substring(7);
    setAvatarSeed(randomSeed);
  };

  const avatarUrl = `https://api.dicebear.com/9.x/avataaars/svg?seed=${avatarSeed}`;

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      await authService.register({ 
        name, 
        account, 
        password,
        avatar: avatarUrl 
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
          <h1 className="text-3xl font-bold tracking-tight">{t("auth.create_account")}</h1>
          <p className="text-muted-foreground">{t("auth.join_community")}</p>
        </div>

        {/* Avatar Selection Area */}
        <div className="flex flex-col items-center space-y-4">
          <div className="relative group">
            <div className="h-24 w-24 rounded-full overflow-hidden border-4 border-primary/20 shadow-xl transition-transform hover:scale-105">
              <img 
                src={avatarUrl} 
                alt="Avatar Preview" 
                className="h-full w-full object-cover"
              />
            </div>
            <button
              type="button"
              onClick={generateRandomAvatar}
              className="absolute -right-2 -bottom-2 p-2 bg-primary text-white rounded-full shadow-lg hover:rotate-180 transition-all duration-500"
              title={t("auth.click_to_randomize_avatar")}
            >
              <RefreshCw size={16} />
            </button>
          </div>
          <p className="text-xs text-muted-foreground font-medium">{t("auth.click_to_randomize_avatar")}</p>
        </div>

        {error && (
          <div className="p-4 rounded-2xl bg-destructive/10 text-destructive text-sm font-medium animate-in fade-in zoom-in duration-300">
            {error}
          </div>
        )}

        <form onSubmit={handleRegister} className="space-y-6">
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
              <div className="relative">
                <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
                <input
                  type="password"
                  placeholder={t("auth.password_placeholder")}
                  value={password}
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
          </div>

          <button
            type="submit"
            disabled={isLoading || accountType === 'invalid' || !isPasswordValid}
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
