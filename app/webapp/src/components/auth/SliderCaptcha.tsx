import { useState, useRef, useEffect, useCallback } from "react";
import { apiClient } from "@/services/apiClient";
import { Loader2, ChevronRight, RefreshCw } from "lucide-react";

interface CaptchaChallenge {
  token: string;
  backgroundImage: string;
  puzzleImage: string;
  puzzleWidth: number;
  puzzleHeight: number;
}

interface SliderCaptchaProps {
  onVerify: (token: string) => void;
  onCancel: () => void;
}

export function SliderCaptcha({ onVerify, onCancel }: SliderCaptchaProps) {
  const [challenge, setChallenge] = useState<CaptchaChallenge | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [sliderValue, setSliderValue] = useState(0);
  const [verifying, setVerifying] = useState(false);
  const [success, setSuccess] = useState(false);
  const trackRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const dragRef = useRef(false);
  const trackWidth = 280;

  const fetchChallenge = useCallback(async () => {
    setLoading(true);
    setError("");
    setSliderValue(0);
    setSuccess(false);
    try {
      const res = await apiClient.get<CaptchaChallenge>("/Auth/captcha-challenge");
      if (res.data) {
        setChallenge(res.data);
        drawImages(res.data, 0);
      }
    } catch {
      setError("获取验证失败，请重试");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchChallenge();
  }, [fetchChallenge]);

  const drawImages = (ch: CaptchaChallenge, offset: number) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    canvas.width = ch.puzzleWidth * 2;
    canvas.height = Math.max(60, ch.puzzleHeight + 4);

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const bgImg = new Image();
    bgImg.onload = () => {
      ctx.drawImage(bgImg, 0, 0, bgImg.width, bgImg.height, 0, 0, canvas.width, canvas.height);
      
      const puzzleImg = new Image();
      puzzleImg.onload = () => {
        ctx.drawImage(
          puzzleImg,
          offset * (canvas.width / bgImg.width),
          0,
          puzzleImg.width,
          puzzleImg.height
        );
      };
      puzzleImg.src = `data:image/png;base64,${ch.puzzleImage}`;
    };
    bgImg.src = `data:image/png;base64,${ch.backgroundImage}`;
  };

  const handlePointerDown = (e: React.PointerEvent) => {
    if (verifying || success) return;
    e.preventDefault();
    dragRef.current = true;
    (e.target as HTMLElement).setPointerCapture(e.pointerId);
  };

  const handlePointerMove = (e: React.PointerEvent) => {
    if (!dragRef.current || verifying || success || !trackRef.current) return;
    const rect = trackRef.current.getBoundingClientRect();
    let x = e.clientX - rect.left;
    x = Math.max(0, Math.min(x, trackWidth));
    setSliderValue(x);
    if (challenge) drawImages(challenge, x);
  };

  const handlePointerUp = async () => {
    if (!dragRef.current || verifying || success) return;
    dragRef.current = false;
    if (!challenge || sliderValue < 5) {
      setSliderValue(0);
      if (challenge) drawImages(challenge, 0);
      return;
    }

    setVerifying(true);
    try {
      const res = await apiClient.post<string>("/Auth/verify-captcha", {
        token: challenge.token,
        x: Math.round(sliderValue),
      });
      if (res.data) {
        setSuccess(true);
        setTimeout(() => onVerify(res.data!), 500);
      }
    } catch {
      setError("验证失败，请重试");
      setSliderValue(0);
      if (challenge) drawImages(challenge, 0);
    } finally {
      setVerifying(false);
    }
  };

  const progress = ((sliderValue / trackWidth) * 100).toFixed(0);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm" onClick={onCancel}>
      <div
        className="glass-card rounded-3xl p-6 w-[340px] space-y-5 animate-in fade-in zoom-in-95 duration-300"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="text-center space-y-1">
          <h3 className="text-lg font-semibold">请完成验证</h3>
          <p className="text-xs text-muted-foreground">拖动滑块使拼图对齐</p>
        </div>

        {error && (
          <div className="rounded-2xl bg-destructive/10 px-3 py-2 text-xs font-medium text-destructive text-center">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="animate-spin text-muted-foreground" size={28} />
          </div>
        ) : challenge ? (
          <>
            <canvas
              ref={canvasRef}
              className="w-full rounded-xl border border-border/50"
              style={{ height: Math.max(60, challenge.puzzleHeight + 4) }}
            />

            <div className="space-y-3">
              <div
                ref={trackRef}
                className="relative h-12 rounded-2xl bg-muted/60 overflow-hidden select-none touch-none"
              >
                <div
                  className="absolute inset-y-0 left-0 bg-primary/20 rounded-2xl transition-all duration-75"
                  style={{ width: `${progress}%` }}
                />
                <div
                  className="absolute top-0 rounded-2xl h-12 w-12 bg-primary flex items-center justify-center cursor-grab active:cursor-grabbing shadow-md transition-transform duration-75"
                  style={{
                    left: `${Math.min(sliderValue, trackWidth - 44)}px`,
                    transform: success ? "scale(1.05)" : "none",
                  }}
                  onPointerDown={handlePointerDown}
                  onPointerMove={handlePointerMove}
                  onPointerUp={handlePointerUp}
                >
                  {verifying ? (
                    <Loader2 className="animate-spin text-primary-foreground" size={18} />
                  ) : success ? (
                    <span className="text-primary-foreground text-lg">✓</span>
                  ) : (
                    <ChevronRight className="text-primary-foreground" size={20} />
                  )}
                </div>
                {!success && !verifying && (
                  <span className="absolute inset-0 flex items-center justify-center text-xs text-muted-foreground font-medium pointer-events-none">
                    拖动滑块完成拼图
                  </span>
                )}
              </div>

              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={fetchChallenge}
                  disabled={verifying}
                  className="flex-1 flex items-center justify-center gap-1.5 rounded-xl py-2 text-xs font-medium text-muted-foreground hover:bg-muted transition-colors"
                >
                  <RefreshCw size={14} />
                  刷新
                </button>
                <button
                  type="button"
                  onClick={onCancel}
                  className="flex-1 rounded-xl py-2 text-xs font-medium text-muted-foreground hover:bg-muted transition-colors"
                >
                  取消
                </button>
              </div>
            </div>
          </>
        ) : null}
      </div>
    </div>
  );
}
