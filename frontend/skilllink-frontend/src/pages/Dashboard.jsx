import React, { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { toImageUrl } from "../utils/image";
import { ensureThemeFromStorage } from "../utils/theme";
import Dock from "../components/Dock";
import { authApi, requestsApi, ratingsApi } from "../api";
import SettingsMenu from "../components/SettingsMenu";

/* -------------------- small UI atoms -------------------- */
const GlassCard = ({ className = "", children }) => (
  <div
    className={
      "relative rounded-2xl border shadow backdrop-blur-xl transition-all duration-300 " +
      "border-black/10 dark:border-white/10 bg-white/60 dark:bg-ink-900/50 " +
      className
    }
  >
    {children}
  </div>
);

const GlassBar = ({ className = "", children }) => (
  <div
    className={
      "relative border shadow backdrop-blur-xl transition-all duration-300 " +
      "border-black/10 dark:border-white/10 bg-white/60 dark:bg-ink-900/50 " +
      className
    }
  >
    {children}
  </div>
);

const MacButton = ({ className = "", children, ...props }) => (
  <button
    className={
      " px-4 py-2 rounded-xl border text-sm transition " +
      " border-black/10 dark:border-white/10 " +
      " bg-white/50 hover:bg-black/5 dark:hover:bg-white/10 active:bg-white/80 " +
      " dark:bg-ink-800/60 dark:hover:bg-ink-800/80 " +
      " focus:outline-none focus:ring-1 focus:ring-blue-400/30 dark:focus:text-white/80 focus:text-black" +
      " text-black/80 dark:text-white/65 active:dark:text-white active:text-black" +
      (className ? " " + className : "")
    }
    {...props}
  >
    {children}
  </button>
);

const MacPrimary = ({ className = "", ...props }) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl text-sm transition text-white " +
      "bg-blue-600 hover:bg-blue-700 active:bg-blue-800 " +
      "focus:outline-none focus:ring-2 focus:ring-blue-400/40 " +
      className
    }
  />
);

const Chip = ({ children, className = "" }) => (
  <span
    className={
      "px-2 py-0.5 text-xs font-medium rounded-full border text-black dark:text-white/80 border-black/10 dark:border-white/10 " +
      className
    }
  >
    {children}
  </span>
);

const statusStyles = {
  PENDING:
    "bg-yellow-200/60 text-yellow-900 dark:bg-yellow-400/20 dark:text-yellow-200",
  SCHEDULED:
    "bg-blue-200/60 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  COMPLETED:
    "bg-emerald-200/60 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
  CANCELLED:
    "bg-red-200/60 text-red-900 dark:bg-red-400/20 dark:text-red-200",
};

function Dashboard() {
  const navigate = useNavigate();
  const [me, setMe] = useState(null);
  const [profile, setProfile] = useState(null);

  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadingReq, setLoadingReq] = useState(true);
  const [err, setErr] = useState("");
  const [reqErr, setReqErr] = useState("");

  const [feedbacks, setFeedbacks] = useState([]);
  const [fbLoading, setFbLoading] = useState(true);
  const [fbErr, setFbErr] = useState("");

  useEffect(() => {
    ensureThemeFromStorage?.();
  }, []);

  useEffect(() => {
    const init = async () => {
      try {
        setLoading(true);
        const [meRes, profileRes] = await Promise.all([authApi.me(), authApi.getProfile()]);
        setMe(meRes.data);
        setProfile(profileRes.data);
      } catch {
        setErr("Failed to load your profile. Please log in again.");
      } finally {
        setLoading(false);
      }
    };
    init();
  }, []);

  // ---- role flags (declare BEFORE effects that depend on them)
  const role = useMemo(() => (me?.role || "").toString().toUpperCase(), [me]);
  const isAdmin = role === "ADMIN";
  const isTutor = role === "TUTOR" || (profile?.readyToTeach ?? false);

  useEffect(() => {
    const loadReqs = async () => {
      try {
        setLoadingReq(true);
        setReqErr("");
        const res = await requestsApi.list();
        setRequests(res.data || []);
      } catch (e) {
        console.error(e);
        setReqErr("Failed to load requests");
      } finally {
        setLoadingReq(false);
      }
    };
    loadReqs();
  }, []);

  useEffect(() => {
    const loadFeedbacks = async () => {
      try {
        setFbLoading(true);
        setFbErr("");
        const limit = 5;
        const res = isTutor
          ? await ratingsApi.listReceived({ limit })
          : await ratingsApi.listGiven({ limit });
        setFeedbacks(res.data || []);
      } catch (e) {
        console.error(e);
        setFbErr("Failed to load session feedback");
      } finally {
        setFbLoading(false);
      }
    };
    if (me && profile) loadFeedbacks();
  }, [me, profile, isTutor]);

  const Stars = ({ value = 0 }) => (
    <span aria-label={`${value} star rating`}>
      {"★★★★★".slice(0, value)}
      <span className="text-slate-300 dark:text-slate-600">
        {"★★★★★".slice(value)}
      </span>
    </span>
  );

  const stats = useMemo(() => {
    const total = requests.length;
    const pending = requests.filter((r) => r.status === "PENDING").length;
    const scheduled = requests.filter((r) => r.status === "SCHEDULED").length;
    const completed = requests.filter((r) => r.status === "COMPLETED").length;
    return { total, pending, scheduled, completed };
  }, [requests]);

  if (loading) {
    return (
      <div className="flex justify-center items-center h-[60vh]">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500" />
      </div>
    );
  }

  if (err) {
    return (
      <div className="max-w-3xl mx-auto p-6">
        <GlassCard className="p-6 text-red-600">{err}</GlassCard>
        <MacPrimary onClick={() => navigate("/login")} className="mt-4">
          Go to Login
        </MacPrimary>
      </div>
    );
  }

  const avatarUrl = toImageUrl(profile?.profilePicture);
  const firstLetter = profile?.fullName?.[0]?.toUpperCase() || "U";

  return (
    <div className="relative min-h-screen font-sans">
      {/* Background */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-ink-900 dark:via-ink-900 dark:to-ink-800" />

      {/* Top bar */}
      <GlassBar className=" border-x-0 border-t-0 px-6 py-4 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow" />
            <div className="text-slate-700 dark:text-slate-200 font-semibold">SkillLink</div>
          </div>
          <div className=" flex gap-12 items-center text-xs text-slate-500 dark:text-slate-400">
            <div className="flex gap-3">
              {isAdmin ? (
                <MacPrimary onClick={() => navigate("/admin-dashboard")}>Admin Panel</MacPrimary>
              ) : (
                <p>Dashboard</p>
              )}
            </div>
            <SettingsMenu />
          </div>
        </div>
      </GlassBar>

      {/* Content */}
      <div className="max-w-6xl mx-auto p-6 space-y-6">
        {/* Header */}
        <GlassCard className="p-6 flex items-center justify-between flex-wrap gap-4">
          <div className="flex items-center gap-4">
            <div className="w-16 h-16 rounded-full bg-slate-100 dark:bg-ink-800/60 flex items-center justify-center overflow-hidden">
              {avatarUrl ? (
                <img src={avatarUrl} alt={profile?.fullName} className="w-full h-full object-cover" />
              ) : (
                <span className="text-xl font-semibold text-slate-500 dark:text-slate-300">
                  {firstLetter}
                </span>
              )}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
                  Hi, {profile?.fullName || "there"} 👋
                </h1>
                <Chip
                  className={
                    isAdmin
                      ? "bg-purple-200/60 text-purple-900 dark:bg-purple-400/20 dark:text-purple-200"
                      : isTutor
                      ? "bg-emerald-200/60 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200"
                      : "bg-slate-200/60 text-slate-900 dark:bg-slate-400/20 dark:text-slate-200"
                  }
                >
                  {isAdmin ? "Admin" : isTutor ? "Tutor" : "Learner"}
                </Chip>
              </div>
              <p className="text-slate-600 dark:text-slate-400">{me?.email}</p>
            </div>
          </div>
          <div className="flex gap-2">
            <MacPrimary onClick={() => navigate("/request")}>+ New Request</MacPrimary>
            <MacButton onClick={() => navigate("/profile")}>View Profile</MacButton>
          </div>
        </GlassCard>

        {/* Stats */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <GlassCard className="p-4">
            <div className="text-sm text-slate-500 dark:text-slate-400">Total Requests</div>
            <div className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
              {stats.total}
            </div>
          </GlassCard>
          <GlassCard className="p-4">
            <div className="text-sm text-slate-500 dark:text-slate-400">Pending</div>
            <div className="text-2xl font-semibold text-yellow-600">{stats.pending}</div>
          </GlassCard>
          <GlassCard className="p-4">
            <div className="text-sm text-slate-500 dark:text-slate-400">Scheduled</div>
            <div className="text-2xl font-semibold text-blue-600">{stats.scheduled}</div>
          </GlassCard>
          <GlassCard className="p-4">
            <div className="text-sm text-slate-500 dark:text-slate-400">Completed</div>
            <div className="text-2xl font-semibold text-emerald-600">{stats.completed}</div>
          </GlassCard>
        </div>

        {/* Recent requests */}
        <GlassCard>
          <div className=" px-6 py-4 border-b border-black/10 dark:border-white/10 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
              Recent Requests
            </h2>
            <MacButton onClick={() => navigate("/request")}>View all</MacButton>
          </div>
          {loadingReq ? (
            <div className="p-6 text-slate-500 dark:text-slate-400">Loading…</div>
          ) : reqErr ? (
            <div className="p-6 text-red-600">{reqErr}</div>
          ) : requests.length === 0 ? (
            <div className="p-6 text-slate-500 dark:text-slate-400">
              No requests yet. Create your first one!
            </div>
          ) : (
            <ul className="divide-y divide-black/10 dark:divide-white/10">
              {requests.slice(0, 5).map((r) => (
                <li
                  key={r.requestId}
                  className="px-6 py-4 flex items-center justify-between hover:bg-black/5 dark:hover:bg-white/5"
                >
                  <div>
                    <p className="font-medium text-slate-900 dark:text-slate-100">
                      {r.skillName}
                    </p>
                    {r.topic && (
                      <p className="text-sm text-slate-500 dark:text-slate-400">{r.topic}</p>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <Chip className={statusStyles[r.status] || ""}>{r.status}</Chip>
                    {r.status === "SCHEDULED" && (
                      <MacPrimary onClick={() => navigate("/VideoSession")}>
                        Join
                      </MacPrimary>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </GlassCard>

        {/* Session Feedback */}
        <GlassCard className="mt-6">
          <div className="px-6 py-4 border-b border-black/10 dark:border-white/10 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
              {isTutor ? "Latest Feedback from Learners" : "Your Recent Session Feedback"}
            </h2>
            <span className="text-xs text-slate-500 dark:text-slate-400">
              {isTutor ? "What learners said about you" : "What you shared after sessions"}
            </span>
          </div>

          {fbLoading ? (
            <div className="p-6 text-slate-500 dark:text-slate-400">Loading…</div>
          ) : fbErr ? (
            <div className="p-6 text-red-600">{fbErr}</div>
          ) : feedbacks.length === 0 ? (
            <div className="p-6 text-slate-500 dark:text-slate-400">
              {isTutor ? "No feedback yet." : "You haven't left feedback yet."}
            </div>
          ) : (
            <ul className="divide-y divide-black/10 dark:divide-white/10">
              {feedbacks.map((f) => (
                <li key={f.ratingId || f.acceptedRequestId} className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <Stars value={Math.max(0, Math.min(5, Number(f.rating) || 0))} />
                      <div className="text-sm text-slate-500 dark:text-slate-400">
                        {new Date(f.createdAt).toLocaleString()}
                      </div>
                    </div>
                    {f.skillName && (
                      <Chip className="bg-slate-200/60 dark:bg-slate-700/40">{f.skillName}</Chip>
                    )}
                  </div>
                  {f.comment && (
                    <p className="mt-2 text-slate-700 dark:text-slate-200">{f.comment}</p>
                  )}
                  <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                    {isTutor
                      ? `From: ${f.fromUserName || "Learner"}`
                      : `To: ${f.toUserName || "Tutor"}`}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </GlassCard>

        {/* Dock */}
        <Dock peek={18}>
          <MacButton onClick={() => navigate("/home")}>Home</MacButton>
          <MacButton onClick={() => navigate("/request")}>+ Request</MacButton>
          <MacButton onClick={() => navigate("/skill")}>Skills</MacButton>
          <MacButton onClick={() => navigate("/VideoSession")}>Session</MacButton>
          <MacButton onClick={() => navigate("/profile")}>Profile</MacButton>
        </Dock>
      </div>
    </div>
  );
}

export default Dashboard;
