import React, { useEffect, useRef, useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { toggleTheme } from "../utils/theme";
import { feedbackApi } from "../api";

/** A small Mac-like button */
const MacButton = ({ className = "", children, ...props }) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl border text-sm transition " +
      "border-black/10 dark:border-white/10 " +
      "bg-white/50 hover:bg-black/5 active:bg-white/80 " +
      "dark:bg-ink-800/60 dark:hover:bg-ink-800/80 " +
      "focus:outline-none focus:ring-1 focus:ring-blue-400/30 " +
      "text-black/80 dark:text-white/65 " +
      className
    }
  >
    {children}
  </button>
);

/** Simple glass card shell for dropdown + modal */
const GlassCard = ({ className = "", children }) => (
  <div
    className={
      "rounded-2xl border shadow backdrop-blur-xl transition-all duration-300 " +
      "border-black/10 dark:border-white/10 dark:bg-ink-900/95 bg-white/95 " +
      className
    }
  >
    {children}
  </div>
);

export default function SettingsMenu() {
  const navigate = useNavigate();
  const location = useLocation();

  const [open, setOpen] = useState(false);
  const [feedbackOpen, setFeedbackOpen] = useState(false);
  const [sending, setSending] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState("");
  const [form, setForm] = useState({ subject: "", message: "" });

  const btnRef = useRef(null);
  const dropRef = useRef(null);

  // close on outside click / ESC
  useEffect(() => {
    const onDocClick = (e) => {
      if (!open) return;
      if (
        dropRef.current &&
        !dropRef.current.contains(e.target) &&
        btnRef.current &&
        !btnRef.current.contains(e.target)
      ) {
        setOpen(false);
      }
    };
    const onEsc = (e) => e.key === "Escape" && (setOpen(false), setFeedbackOpen(false));
    document.addEventListener("mousedown", onDocClick);
    document.addEventListener("keydown", onEsc);
    return () => {
      document.removeEventListener("mousedown", onDocClick);
      document.removeEventListener("keydown", onEsc);
    };
  }, [open]);

  const sendFeedback = async (e) => {
    e?.preventDefault?.();
    if (!form.message.trim()) {
      setError("Please enter your feedback.");
      return;
    }
    try {
      setSending(true);
      setError("");
      setSent(false);
      await feedbackApi.submit({
        subject: form.subject || "General feedback",
        message: form.message,
        page: location.pathname,
        userAgent: navigator?.userAgent,
      });
      setSent(true);
      setForm({ subject: "", message: "" });
      setTimeout(() => setFeedbackOpen(false), 900);
    } catch (err) {
      console.error(err);
      setError(
        err?.response?.data?.message ||
          "Failed to send feedback. Please try again."
      );
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="relative">
      <MacButton
        ref={btnRef}
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        title="Settings"
        className=" border-0 bg-transparent dark:bg-transparent"
      >
        <i className="fas fa-gear" />
      </MacButton>

      {/* Dropdown */}
      {open && (
        <div
          ref={dropRef}
          role="menu"
          className="absolute right-0 mt-2 min-w-[240px] z-50"
        >
          <GlassCard className="overflow-hidden dark:text-white/50">
            <div className="p-2">
              <button
                onClick={() => {
                  toggleTheme();
                  setOpen(false);
                }}
                className="w-full text-left px-3 py-2 rounded-xl hover:bg-black/5 dark:hover:bg-white/10 text-sm"
                role="menuitem"
              >
                <i className="fas fa-moon mr-2" />
                Toggle Dark Mode
              </button>

              <button
                onClick={() => {
                  setFeedbackOpen(true);
                  setOpen(false);
                }}
                className="w-full text-left px-3 py-2 rounded-xl hover:bg-black/5 dark:hover:bg-white/10 text-sm"
                role="menuitem"
              >
                <i className="fas fa-comment-dots mr-2" />
                Give Feedback
              </button>

              <button
                onClick={() => {
                  setOpen(false);
                  navigate("/help");
                }}
                className="w-full text-left px-3 py-2 rounded-xl hover:bg-black/5 dark:hover:bg-white/10 text-sm"
                role="menuitem"
              >
                <i className="fas fa-circle-question mr-2" />
                Help & Support
              </button>

              <button
                onClick={() => {
                  setOpen(false);
                  navigate("/profile");
                }}
                className="w-full text-left px-3 py-2 rounded-xl hover:bg-black/5 dark:hover:bg-white/10 text-sm"
                role="menuitem"
              >
                <i className="fas fa-user-gear mr-2" />
                Profile Settings
              </button>

              <button
                onClick={() => {
                  setOpen(false);
                  navigate("/notifications");
                }}
                className="w-full text-left px-3 py-2 rounded-xl hover:bg-black/5 dark:hover:bg-white/10 text-sm"
                role="menuitem"
              >
                <i className="fas fa-bell mr-2" />
                Notifications
              </button>
            </div>
          </GlassCard>
        </div>
      )}

      {/* Feedback modal */}
      {feedbackOpen && (
        <div className="fixed inset-0 z-[60] bg-black/40 flex items-center justify-center px-4 mt-[30%]">
          <GlassCard className="w-full max-w-md">
            <div className="px-5 py-4 border-b border-black/10 dark:border-white/10 flex items-center justify-between">
              <div className="font-semibold text-slate-900 dark:text-slate-100">
                Send Feedback
              </div>
              <button
                onClick={() => setFeedbackOpen(false)}
                className="text-slate-500 hover:text-slate-700 dark:hover:text-slate-300"
                aria-label="Close"
              >
                ✕
              </button>
            </div>

            <form onSubmit={sendFeedback} className="p-5 space-y-3">
              <div>
                <label className="text-xs text-slate-600 dark:text-slate-400">
                  Subject (optional)
                </label>
                <input
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-ink-300/30 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                  placeholder="Feature request, bug, general feedback…"
                  value={form.subject}
                  onChange={(e) => setForm((p) => ({ ...p, subject: e.target.value }))}
                />
              </div>
              <div>
                <label className="text-xs text-slate-600 dark:text-slate-400">
                  Message *
                </label>
                <textarea
                  required
                  rows={5}
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-ink-300/30 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                  placeholder="Tell us what's on your mind…"
                  value={form.message}
                  onChange={(e) => setForm((p) => ({ ...p, message: e.target.value }))}
                />
              </div>

              {error && (
                <div className="text-sm text-red-600">{error}</div>
              )}
              {sent && (
                <div className="text-sm text-emerald-600">
                  Thanks! Your feedback has been sent to the admins.
                </div>
              )}

              <div className="flex justify-end gap-2 pt-1">
                <MacButton onClick={() => setFeedbackOpen(false)} type="button">
                  Cancel
                </MacButton>
                <button
                  type="submit"
                  disabled={sending}
                  className={
                    "px-4 py-2 rounded-xl text-sm text-white " +
                    "bg-blue-600 hover:bg-blue-700 active:bg-blue-800 " +
                    "focus:outline-none focus:ring-2 focus:ring-blue-400/40 " +
                    (sending ? "opacity-70 cursor-not-allowed" : "")
                  }
                >
                  {sending ? "Sending…" : "Send"}
                </button>
              </div>
            </form>
          </GlassCard>
        </div>
      )}
    </div>
  );
}
