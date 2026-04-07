import { useState, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Link, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { userService } from "../../services/userService";
import { authService } from "../../services/authService";
import { logout, setCredentials } from "../../stores/authSlice";
import type { RootState } from "../../stores";
import { 
  Mail, 
  Phone,
  Calendar, 
  Shield, 
  LogOut, 
  Edit2, 
  Save, 
  X, 
  CheckCircle2, 
  AlertCircle,
  Camera,
  RefreshCw,
  Key,
  ArrowRight
} from "lucide-react";
import type { UserDto } from "../../types";

export default function ProfilePage() {
  const { t } = useTranslation();
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const currentUser = useSelector((state: RootState) => state.auth.user);
  
  const [user, setUser] = useState<UserDto | null>(currentUser);
  const [isEditing, setIsEditing] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

  // Verification states
  const [showVerifyModal, setShowVerifyModal] = useState<'email' | 'phone' | null>(null);
  const [newValue, setNewValue] = useState("");
  const [verificationCode, setVerificationCode] = useState("");
  const [isVerifying, setIsVerifying] = useState(false);
  const [countdown, setCountdown] = useState(0);

  // Form states
  const [name, setName] = useState(currentUser?.name || "");
  const [info, setInfo] = useState(currentUser?.info || "");
  const [avatar, setAvatar] = useState(currentUser?.avatar || "");

  useEffect(() => {
    let timer: any;
    if (countdown > 0) {
      timer = setInterval(() => setCountdown(c => c - 1), 1000);
    }
    return () => clearInterval(timer);
  }, [countdown]);

  useEffect(() => {
    fetchProfile();
  }, []);

  const fetchProfile = async () => {
    try {
      const response = await userService.getProfile();
      if (response.data) {
        setUser(response.data);
        setName(response.data.name);
        setInfo(response.data.info || "");
        setAvatar(response.data.avatar);
      }
    } catch (err) {
      console.error("Failed to fetch profile", err);
    }
  };

  const handleUpdate = async () => {
    setIsLoading(true);
    setMessage(null);
    try {
      const response = await userService.updateProfile({
        name,
        info,
        avatar
      });
      if (response.data) {
        setUser(response.data);
        // Update redux store if needed - usually setCredentials is used for login, 
        // but we can update just the user part.
        // For simplicity in this app, we'll just update local state and 
        // rely on re-fetch or manual store update.
        // Assuming we have a way to update just user:
        dispatch(setCredentials({ user: response.data, token: localStorage.getItem('token') || "" }));
        setIsEditing(false);
        setMessage({ type: 'success', text: t("profile.success_update") });
      }
    } catch (err: any) {
      setMessage({ type: 'error', text: err.message || t("common.error") });
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogout = () => {
    dispatch(logout());
    navigate("/login");
  };

  const randomizeAvatar = () => {
    const seed = Math.random().toString(36).substring(7);
    setAvatar(`https://api.dicebear.com/9.x/avataaars/svg?seed=${seed}`);
  };

  const handleSendCode = async () => {
    if (!newValue) return;
    setIsVerifying(true);
    try {
      if (showVerifyModal === 'email') {
        // Here we first update the profile to set the new email
        await userService.updateProfile({ email: newValue });
        await authService.sendEmailCode();
      } else {
        await userService.updateProfile({ phone: newValue });
        await authService.sendPhoneCode();
      }
      setCountdown(60);
      setMessage({ type: 'success', text: t("profile.verification_sent") });
    } catch (err: any) {
      setMessage({ type: 'error', text: err.message || t("common.error") });
    } finally {
      setIsVerifying(false);
    }
  };

  const handleVerifyAndSave = async () => {
    if (!verificationCode) return;
    setIsVerifying(true);
    try {
      if (showVerifyModal === 'email') {
        // verify-email-code is the correct endpoint for authorized email verification
        await authService.verifyEmailCode({ code: verificationCode });
      } else {
        // verify-phone is used for both anonymous and authorized phone verification
        await authService.verifyPhone(verificationCode);
      }
      setMessage({ type: 'success', text: t("profile.success_update") });
      setShowVerifyModal(null);
      fetchProfile();
    } catch (err: any) {
      setMessage({ type: 'error', text: err.message || t("common.error") });
    } finally {
      setIsVerifying(false);
    }
  };

  if (!user) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <RefreshCw className="animate-spin text-primary" size={40} />
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto py-12 px-4 space-y-8">
      {/* Profile Header Card */}
      <div className="glass-card rounded-[2.5rem] overflow-hidden animate-in fade-in slide-in-from-bottom-4 duration-700">
        <div className="h-32 bg-linear-to-r from-primary/20 to-primary/10 relative">
          <div className="absolute -bottom-16 left-8">
            <div className="relative group">
              <div className="h-32 w-32 rounded-3xl overflow-hidden border-4 border-background shadow-2xl bg-muted">
                <img 
                  src={avatar} 
                  alt={user.name} 
                  className="h-full w-full object-cover"
                />
              </div>
              {isEditing && (
                <button
                  onClick={randomizeAvatar}
                  className="absolute -right-2 -bottom-2 p-2 bg-primary text-white rounded-xl shadow-lg hover:rotate-180 transition-all duration-500"
                  title={t("auth.click_to_randomize_avatar")}
                >
                  <Camera size={18} />
                </button>
              )}
            </div>
          </div>
          
          <div className="absolute top-4 right-4 flex gap-2">
            <Link
              to={`/user/${user.id}`}
              className="flex items-center gap-2 px-4 py-2 bg-background/50 backdrop-blur-md hover:bg-background/80 rounded-xl text-sm font-bold transition-all"
            >
              {t("profile.view_public_profile")}
            </Link>
            {!isEditing ? (
              <button
                onClick={() => setIsEditing(true)}
                className="flex items-center gap-2 px-4 py-2 bg-background/50 backdrop-blur-md hover:bg-background/80 rounded-xl text-sm font-bold transition-all"
              >
                <Edit2 size={16} /> {t("profile.edit_profile")}
              </button>
            ) : (
              <div className="flex gap-2">
                <button
                  onClick={() => setIsEditing(false)}
                  className="flex items-center gap-2 px-4 py-2 bg-destructive/10 text-destructive hover:bg-destructive/20 rounded-xl text-sm font-bold transition-all"
                >
                  <X size={16} /> {t("profile.cancel")}
                </button>
                <button
                  onClick={handleUpdate}
                  disabled={isLoading}
                  className="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground hover:opacity-90 rounded-xl text-sm font-bold transition-all shadow-lg shadow-primary/20 disabled:opacity-50"
                >
                  {isLoading ? <RefreshCw className="animate-spin" size={16} /> : <Save size={16} />} 
                  {t("profile.save_changes")}
                </button>
              </div>
            )}
          </div>
        </div>

        <div className="pt-20 pb-8 px-8 space-y-6">
          {message && (
            <div className={`p-4 rounded-2xl flex items-center gap-3 animate-in fade-in zoom-in duration-300 ${
              message.type === 'success' ? 'bg-primary/10 text-primary' : 'bg-destructive/10 text-destructive'
            }`}>
              {message.type === 'success' ? <CheckCircle2 size={20} /> : <AlertCircle size={20} />}
              <span className="text-sm font-medium">{message.text}</span>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div className="space-y-6">
              <div className="space-y-2">
                <label className="text-xs font-bold uppercase tracking-wider text-muted-foreground ml-1">
                  {t("auth.display_name")}
                </label>
                {isEditing ? (
                  <input
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    className="w-full px-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                  />
                ) : (
                  <div className="text-2xl font-bold flex items-center gap-2">
                    {user.name}
                    {user.isActive && <CheckCircle2 className="text-primary" size={20} />}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <label className="text-xs font-bold uppercase tracking-wider text-muted-foreground ml-1">
                  {t("profile.info")}
                </label>
                {isEditing ? (
                  <textarea
                    value={info}
                    onChange={(e) => setInfo(e.target.value)}
                    placeholder={t("profile.info_placeholder")}
                    rows={4}
                    className="w-full px-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all resize-none"
                  />
                ) : (
                  <p className="text-muted-foreground leading-relaxed">
                    {user.info || t("common.null")}
                  </p>
                )}
              </div>
            </div>

            <div className="space-y-4">
              <div className="p-6 rounded-3xl bg-muted/30 border border-border/50 space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3 text-muted-foreground">
                    <Mail size={18} />
                    <span className="text-sm font-medium">{t("auth.email")}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-bold truncate max-w-30">{user.email || "---"}</span>
                    <button 
                      onClick={() => { setShowVerifyModal('email'); setNewValue(user.email); setVerificationCode(""); }}
                      className="p-1 hover:bg-muted rounded-lg text-primary transition-colors"
                    >
                      <Edit2 size={14} />
                    </button>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3 text-muted-foreground">
                    <Phone size={18} />
                    <span className="text-sm font-medium">{t("auth.phone")}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-bold">{user.phone || "---"}</span>
                    <button 
                      onClick={() => { setShowVerifyModal('phone'); setNewValue(user.phone); setVerificationCode(""); }}
                      className="p-1 hover:bg-muted rounded-lg text-primary transition-colors"
                    >
                      <Edit2 size={14} />
                    </button>
                  </div>
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3 text-muted-foreground">
                    <Shield size={18} />
                    <span className="text-sm font-medium">{t("profile.role")}</span>
                  </div>
                  <span className="px-3 py-1 bg-primary/10 text-primary text-xs font-bold rounded-full uppercase">
                    {user.role}
                  </span>
                </div>

                <div className="flex items-center justify-between border-t border-border/50 pt-4">
                  <div className="flex items-center gap-3 text-muted-foreground">
                    <Key size={18} />
                    <span className="text-sm font-medium">{t("profile.change_password")}</span>
                  </div>
                  <Link 
                    to="/profile/reset-password"
                    className="p-1 hover:bg-muted rounded-lg text-primary transition-colors"
                  >
                    <ArrowRight size={18} />
                  </Link>
                </div>
              </div>

                {/* Third Party Bindings */}
                <div className="p-6 rounded-3xl bg-muted/30 border border-border/50 space-y-4">
                  <h4 className="text-sm font-bold uppercase tracking-widest text-muted-foreground ml-1">
                    {t("profile.third_party_bindings")}
                  </h4>
                  
                  {[
                    { name: 'Ours', id: user.oursId },
                    { name: 'Github', id: user.githubId },
                    { name: 'Google', id: user.googleId },
                    { name: 'Weixin', id: user.weixinId },
                  ].map((platform) => (
                    <div key={platform.name} className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold ${platform.id ? 'bg-primary/20 text-primary' : 'bg-muted text-muted-foreground'}`}>
                          {platform.name[0]}
                        </div>
                        <span className="text-sm font-medium">{platform.name}</span>
                      </div>
                      {platform.id ? (
                        <span className="text-xs font-bold text-primary bg-primary/10 px-2 py-1 rounded-lg">
                          {t("profile.bound")}
                        </span>
                      ) : (
                        <button 
                          onClick={() => {
                            const returnUrl = window.location.origin + "/profile";
                            window.location.href = authService.getExternalLoginUrl(platform.name, returnUrl, 'bind');
                          }}
                          className="text-xs font-bold text-muted-foreground hover:text-primary transition-colors"
                        >
                          {t("profile.bind_now")}
                        </button>
                      )}
                    </div>
                  ))}
                </div>

                {/* Account Metadata */}
                <div className="p-6 rounded-3xl bg-muted/30 border border-border/50 space-y-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3 text-muted-foreground">
                      <Calendar size={18} />
                      <span className="text-sm font-medium">{t("profile.joined_at")}</span>
                    </div>
                    <span className="text-sm font-bold">
                      {new Date(user.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>

                {/* Verification Status */}
                <div className="grid grid-cols-2 gap-4">
                  <div className={`p-4 rounded-3xl border border-border/50 flex flex-col items-center gap-2 ${
                    user.isEmailVerified ? 'bg-primary/5 border-primary/20' : 'bg-muted/30'
                  }`}>
                    <div className={user.isEmailVerified ? 'text-primary' : 'text-muted-foreground'}>
                      <CheckCircle2 size={20} />
                    </div>
                    <span className="text-[10px] font-bold uppercase tracking-tighter">
                      {t("profile.email_verified")}
                    </span>
                  </div>

                  <div className={`p-4 rounded-3xl border border-border/50 flex flex-col items-center gap-2 ${
                    user.isPhoneVerified ? 'bg-primary/5 border-primary/20' : 'bg-muted/30'
                  }`}>
                    <div className={user.isPhoneVerified ? 'text-primary' : 'text-muted-foreground'}>
                      <CheckCircle2 size={20} />
                    </div>
                    <span className="text-[10px] font-bold uppercase tracking-tighter">
                      {t("profile.phone_verified")}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <div className="pt-6 border-t border-border/50">
              <button
                onClick={handleLogout}
                className="flex items-center gap-2 text-destructive hover:text-destructive/80 font-bold transition-all"
              >
                <LogOut size={20} /> {t("profile.logout")}
              </button>
            </div>
          </div>
        </div>

        {/* Verification Modal */}
        {showVerifyModal && (
          <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
            <div className="absolute inset-0 bg-background/80 backdrop-blur-sm" onClick={() => setShowVerifyModal(null)} />
            <div className="relative w-full max-w-md glass-card rounded-[2.5rem] p-8 space-y-6 animate-in zoom-in duration-300">
              <div className="flex items-center justify-between">
                <h3 className="text-xl font-bold">{t("profile.verify_new", { type: showVerifyModal === 'email' ? 'Email' : 'Phone' })}</h3>
                <button onClick={() => setShowVerifyModal(null)} className="p-2 hover:bg-muted rounded-full transition-colors">
                  <X size={20} />
                </button>
              </div>

              <div className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-semibold ml-1">
                    {showVerifyModal === 'email' ? t("auth.account") : t("auth.account")}
                  </label>
                  <input
                    type="text"
                    value={newValue}
                    onChange={(e) => setNewValue(e.target.value)}
                    className="w-full px-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                    placeholder={showVerifyModal === 'email' ? "new@email.com" : "13800138000"}
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-semibold ml-1">{t("auth.verification_code")}</label>
                  <div className="relative">
                    <input
                      type="text"
                      value={verificationCode}
                      onChange={(e) => setVerificationCode(e.target.value)}
                      className="w-full px-4 py-3 rounded-2xl bg-muted/50 border border-border/50 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
                      placeholder="6-digit code"
                    />
                    <button
                      onClick={handleSendCode}
                      disabled={countdown > 0 || isVerifying || !newValue}
                      className="absolute right-2 top-1/2 -translate-y-1/2 px-3 py-1.5 rounded-xl bg-primary/10 text-primary text-xs font-bold hover:bg-primary/20 transition-all disabled:opacity-50"
                    >
                      {countdown > 0 ? t("auth.resend_code", { count: countdown }) : t("auth.send_code")}
                    </button>
                  </div>
                </div>
              </div>

              <button
                onClick={handleVerifyAndSave}
                disabled={isVerifying || !verificationCode}
                className="w-full py-4 rounded-2xl bg-primary text-primary-foreground font-bold text-lg hover:opacity-90 transition-all shadow-xl shadow-primary/20 flex items-center justify-center gap-2 disabled:opacity-50"
              >
                {isVerifying ? <RefreshCw className="animate-spin" size={20} /> : t("profile.confirm_change")}
              </button>
            </div>
          </div>
        )}
      </div>
    );
  }
