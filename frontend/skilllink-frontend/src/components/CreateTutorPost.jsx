import React, { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { tutorPostsApi } from "../api";

/* small atoms to match the style */
const GlassCard = ({ className = "", children }) => (
  <div
    className={
      "rounded-2xl border border-black/10 dark:border-white/10 " +
      "bg-white/60 dark:bg-ink-900/60 backdrop-blur-xl shadow-xl " +
      "transition-all " +
      className
    }
  >
    {children}
  </div>
);

const MacButton = ({ className = "", children, ...props }) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl border text-sm transition " +
      "border-black/10 dark:border-white/10 " +
      "bg-white/70 hover:bg-black/5 dark:bg-ink-800/70 dark:hover:bg-ink-700/80 " +
      "focus:outline-none focus:ring-1 focus:ring-blue-400/30 " +
      "text-black/80 dark:text-white/70 active:text-black dark:active:text-white " +
      className
    }
  >
    {children}
  </button>
);

const MacPrimary = ({ className = "", children, ...props }) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl text-sm font-medium text-white transition " +
      "bg-blue-600 hover:bg-blue-700 active:bg-blue-800 " +
      "focus:outline-none focus:ring-2 focus:ring-blue-400/40 " +
      className
    }
  >
    {children}
  </button>
);

export default function CreateTutorPost() {
  const { user } = useAuth();
  const [title, setTitle] = useState("");
  const [desc, setDesc] = useState("");
  const [max, setMax] = useState(5);
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState("");

  const validate = () => {
    if (!title.trim()) return "Title is required";
    if (!desc.trim()) return "Description is required";
    const m = Number(max);
    if (!Number.isFinite(m) || m < 1 || m > 500) return "Max participants should be between 1 and 500";
    return "";
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const v = validate();
    if (v) {
      setMsg(v);
      return;
    }
    try {
      setLoading(true);
      setMsg("");
      await tutorPostsApi.create("/tutor-posts", {
        tutorId: user?.userId, // from auth
        title: title.trim(),
        description: desc.trim(),
        maxParticipants: Number(max),
      });
      setMsg("Post created successfully!");
      setTitle("");
      setDesc("");
      setMax(5);
    } catch (err) {
      console.error(err);
      setMsg(err.response?.data?.message || "Failed to create post");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center p-6">
      <div className="w-full max-w-xl">
        <GlassCard className="p-6">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-slate-100">Create Tutor Post</h2>

          {msg && (
            <div
              className={
                "mb-4 px-4 py-2 rounded-lg " +
                (msg.toLowerCase().includes("success")
                  ? "bg-emerald-50 text-emerald-700"
                  : "bg-red-50 text-red-700")
              }
              role="status"
            >
              {msg}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-3">
            <div>
              <label className="block text-sm text-slate-700 dark:text-slate-300">Title *</label>
              <input
                className="mt-1 w-full rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-slate-900 dark:text-slate-100 px-3 py-2 focus:ring-2 focus:ring-blue-500"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="e.g., Web Dev Bootcamp"
                required
              />
            </div>

            <div>
              <label className="block text-sm text-slate-700 dark:text-slate-300">Description *</label>
              <textarea
                className="mt-1 w-full rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-slate-900 dark:text-slate-100 px-3 py-2 focus:ring-2 focus:ring-blue-500"
                rows={4}
                value={desc}
                onChange={(e) => setDesc(e.target.value)}
                placeholder="Describe what you will teach, prerequisites, schedule preferences, etc."
                required
              />
            </div>

            <div>
              <label className="block text-sm text-slate-700 dark:text-slate-300">Max Participants *</label>
              <input
                type="number"
                min={1}
                max={500}
                className="mt-1 w-full rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-slate-900 dark:text-slate-100 px-3 py-2 focus:ring-2 focus:ring-blue-500"
                value={max}
                onChange={(e) => setMax(e.target.value)}
                required
              />
            </div>

            <div className="pt-2 flex items-center gap-2">
              <MacPrimary type="submit" disabled={loading}>
                {loading ? "Creating…" : "Create"}
              </MacPrimary>
              <MacButton type="button" onClick={() => { setTitle(""); setDesc(""); setMax(5); }}>
                Clear
              </MacButton>
            </div>
          </form>

          <p className="mt-3 text-xs text-slate-500 dark:text-slate-400">
            Note: Your post will be visible to learners. You can manage your posts in your dashboard later.
          </p>
        </GlassCard>
      </div>
    </div>
  );
}
