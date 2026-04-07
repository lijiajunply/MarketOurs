import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { useDispatch } from "react-redux";
import { setCredentials } from "../../stores/authSlice";
import { authService } from "../../services/authService";
import { Loader2 } from "lucide-react";
import { useTranslation } from "react-i18next";

export default function LoginCallbackPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const dispatch = useDispatch();

  useEffect(() => {
    const accessToken = searchParams.get("accessToken");
    const refreshToken = searchParams.get("refreshToken");
    const error = searchParams.get("error");
    const message = searchParams.get("message");

    if (error) {
      navigate(`/login?error=${encodeURIComponent(error)}`);
      return;
    }

    if (message === "绑定成功" || message === "Binding successful") {
      navigate("/profile?message=success");
      return;
    }

    if (accessToken && refreshToken) {
      handleLogin(accessToken, refreshToken);
    } else {
      navigate(`/login?error=${encodeURIComponent(t("auth.error_failed_to_login"))}`);
    }
  }, [searchParams, t]);

  const handleLogin = async (accessToken: string, refreshToken: string) => {
    try {
      // Store tokens
      localStorage.setItem("token", accessToken);
      localStorage.setItem("refreshToken", refreshToken);

      // Get user info
      const userInfo = await authService.getInfo();
      dispatch(setCredentials({ 
        user: userInfo.data, 
        token: accessToken 
      }));
      
      navigate("/");
    } catch (err) {
      console.error("Failed to process login callback", err);
      navigate(`/login?error=${encodeURIComponent(t("auth.error_failed_to_login"))}`);
    }
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] space-y-4">
      <Loader2 className="animate-spin text-primary" size={48} />
      <p className="text-muted-foreground font-medium animate-pulse">
        {t("common.loading")}
      </p>
    </div>
  );
}
