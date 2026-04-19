import { useId, useState } from "react";
import type { InputHTMLAttributes, ReactNode } from "react";
import { Eye, EyeOff } from "lucide-react";

type PasswordFieldProps = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  "type"
> & {
  icon?: ReactNode;
  inputClassName?: string;
  wrapperClassName?: string;
};

export function PasswordField({
  icon,
  className,
  inputClassName,
  wrapperClassName,
  ...props
}: PasswordFieldProps) {
  const [isVisible, setIsVisible] = useState(false);
  const inputId = useId();

  return (
    <div className={wrapperClassName ?? "relative"}>
      {icon ? (
        <div className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground">
          {icon}
        </div>
      ) : null}
      <input
        {...props}
        id={props.id ?? inputId}
        type={isVisible ? "text" : "password"}
        className={className ?? inputClassName}
      />
      <button
        type="button"
        aria-label={isVisible ? "隐藏密码" : "显示密码"}
        aria-controls={props.id ?? inputId}
        aria-pressed={isVisible}
        onClick={() => setIsVisible((visible) => !visible)}
        className="absolute right-4 top-1/2 -translate-y-1/2 text-muted-foreground transition-colors hover:text-foreground focus:outline-none focus:ring-2 focus:ring-primary/20 rounded-full"
      >
        {isVisible ? <EyeOff size={18} /> : <Eye size={18} />}
      </button>
    </div>
  );
}
