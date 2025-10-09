// src/components/UI.jsx
import React from "react";

/* ========= tiny utility ========= */
const cn = (...classes) => classes.filter(Boolean).join(" ");

/* ========= Glass surfaces ========= */
export const GlassCard = ({ className = "", onClick, children, as: Tag = "div", ...props }) => (
  <Tag
    {...props}
    className={cn(
      "relative rounded-2xl border shadow backdrop-blur-xl transition-all duration-300",
      "border-slate-200/60 dark:border-slate-700/60",
      "bg-white/70 dark:bg-slate-900/60",
      className
    )}
    onClick={onClick}
  >
    {children}
  </Tag>
);

export const GlassBar = ({ className = "", children, as: Tag = "div", ...props }) => (
  <Tag
    {...props}
    className={cn(
      "relative border shadow backdrop-blur-xl transition-all duration-300",
      "border-slate-200/60 dark:border-slate-700/60",
      "bg-white/70 dark:bg-slate-900/60",
      className
    )}
  >
    {children}
  </Tag>
);

/* ========= Buttons (variant + size) ========= */
export const MacButton = React.forwardRef(
  (
    {
      className = "",
      variant = "default", // default | primary | danger | ghost
      size = "md", // sm | md | lg
      disabled = false,
      children,
      ...props
    },
    ref
  ) => {
    const base =
      "inline-flex items-center justify-center rounded-xl border text-sm font-medium transition " +
      "focus:outline-none focus:ring-1 focus:ring-blue-400/30";

    const sizes = {
      sm: "px-3 py-1.5",
      md: "px-4 py-2",
      lg: "px-5 py-2.5",
    };

    const variants = {
      default:
        "border-black/10 dark:border-white/10 " +
        "bg-white/60 text-slate-800 " +
        "dark:bg-slate-800/60 dark:text-slate-200 " +
        "hover:bg-black/5 dark:hover:bg-white/10 active:bg-white/80",
      primary:
        "border-transparent text-white " +
        "bg-blue-600 hover:bg-blue-700 active:bg-blue-800 " +
        "focus:ring-2 focus:ring-blue-400/40",
      danger:
        "border-transparent text-white " +
        "bg-red-600 hover:bg-red-700 active:bg-red-800 " +
        "focus:ring-2 focus:ring-red-400/40",
      ghost:
        "border-transparent bg-transparent " +
        "text-slate-800 dark:text-slate-200 " +
        "hover:bg-black/5 dark:hover:bg-white/10",
    };

    const disabledCls = disabled ? "opacity-60 cursor-not-allowed" : "";

    return (
      <button
        ref={ref}
        disabled={disabled}
        className={cn(base, sizes[size], variants[variant], disabledCls, className)}
        {...props}
      >
        {children}
      </button>
    );
  }
);
MacButton.displayName = "MacButton";

// Backward-compatible shorthands if you like them
export const MacPrimary = (props) => <MacButton variant="primary" {...props} />;
export const MacDanger = (props) => <MacButton variant="danger" {...props} />;

/* ========= Chip / Badge ========= */
export const Chip = ({ children, color = "gray", className = "", ...props }) => {
  const map = {
    gray:
      "border-slate-200/60 dark:border-slate-700/60 " +
      "text-slate-700 dark:text-slate-200 " +
      "bg-white/40 dark:bg-slate-800/40",
    green:
      "border-emerald-300/40 bg-emerald-50/60 dark:bg-emerald-400/10 " +
      "text-emerald-700 dark:text-emerald-300",
    red:
      "border-red-300/40 bg-red-50/60 dark:bg-red-400/10 " +
      "text-red-700 dark:text-red-300",
    blue:
      "border-blue-300/40 bg-blue-50/60 dark:bg-blue-400/10 " +
      "text-blue-700 dark:text-blue-300",
    purple:
      "border-purple-300/40 bg-purple-50/60 dark:bg-purple-400/10 " +
      "text-purple-700 dark:text-purple-300",
    amber:
      "border-amber-300/40 bg-amber-50/60 dark:bg-amber-400/10 " +
      "text-amber-800 dark:text-amber-300",
  };
  return (
    <span
      {...props}
      className={cn(
        "inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full border",
        map[color],
        className
      )}
    >
      {children}
    </span>
  );
};

/* ========= Form inputs (same glass look) ========= */
export const Input = React.forwardRef(({ className = "", ...props }, ref) => (
  <input
    ref={ref}
    className={cn(
      "w-full px-3 py-2 rounded-xl border",
      "border-slate-200 dark:border-slate-700",
      "bg-white/70 dark:bg-slate-800/60",
      "text-slate-700 dark:text-slate-200 placeholder-slate-400",
      "focus:outline-none focus:ring-2 focus:ring-blue-500/40",
      className
    )}
    {...props}
  />
));
Input.displayName = "Input";

export const Textarea = React.forwardRef(({ className = "", rows = 3, ...props }, ref) => (
  <textarea
    ref={ref}
    rows={rows}
    className={cn(
      "w-full px-3 py-2 rounded-xl border",
      "border-slate-200 dark:border-slate-700",
      "bg-white/70 dark:bg-slate-800/60",
      "text-slate-700 dark:text-slate-200 placeholder-slate-400",
      "focus:outline-none focus:ring-2 focus:ring-blue-500/40",
      className
    )}
    {...props}
  />
));
Textarea.displayName = "Textarea";
