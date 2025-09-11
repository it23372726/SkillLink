import React, { useEffect, useMemo, useState, useRef } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api/axios";
import { toImageUrl } from "../api/base";
import { useAuth } from "../context/AuthContext";

// ---------- status chip colors ----------
const chipByStatus = {
  PENDING: "bg-yellow-200/60 text-yellow-900 dark:bg-yellow-400/20 dark:text-yellow-200",
  SCHEDULED: "bg-blue-200/60 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  COMPLETED: "bg-emerald-200/60 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
  CANCELLED: "bg-red-200/60 text-red-900 dark:bg-red-400/20 dark:text-red-200",
};

// ---------- small UI atoms (no external CSS needed) ----------
const GlassCard = ({ className = "", children }) => (
  <div
    className={
      "relative rounded-2xl border shadow " +
      "backdrop-blur-xl transition-all duration-300 " +
      "border-black/10 dark:border-white/10 " +
      " bg-ink-100/50 dark:bg-ink-900/50 " +
      className
    }
  >
    {children}
  </div>
);

const GlassBar = ({ className = "", children }) => (
  <div
    className={
      "relative border shadow " +
      "backdrop-blur-xl transition-all duration-300 " +
      "border-white/40 dark:border-white/10 " +
      "bg-white/60 dark:bg-ink-900/50 " +
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
      " text-black/80  dark:text-white/65 active:dark:text-white active:text-black"+
      className
    }
    {...props}
  >
    {children}
  </button>
);

const MacPrimary = (props) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl text-sm transition text-white " +
      "bg-blue-600 hover:bg-blue-700 active:bg-blue-800 " +
      "focus:outline-none focus:ring-2 focus:ring-blue-400/40 " +
      (props.className || "")
    }
  />
);
const MacDanger = (props) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl text-sm transition text-white " +
      "bg-red-600 hover:bg-red-700 active:bg-red-800 " +
      "focus:outline-none focus:ring-2 focus:ring-red-400/40 " +
      (props.className || "")
    }
  />
);

const MacToggle = ({ checked, onChange, disabled }) => (
  <button
    type="button"
    disabled={disabled}
    onClick={onChange}
    aria-pressed={checked}
    className={
      "relative inline-flex h-7 w-12 items-center rounded-full transition-colors " +
      (checked
        ? "bg-gradient-to-b from-emerald-400 to-emerald-500"
        : "bg-gradient-to-b from-slate-200 to-slate-300 dark:from-ink-700 dark:to-ink-800") +
      " disabled:opacity-60"
    }
  >
    <span
      className={
        "inline-block h-6 w-6 transform rounded-full bg-white shadow transition-transform " +
        (checked ? "translate-x-5" : "translate-x-1")
      }
    />
  </button>
);

const Chip = ({ children, className = "" }) => (
  <span className={"px-2.5 py-1 text-xs font-medium rounded-full border border-white/30 dark:border-white/10 " + className}>
    {children}
  </span>
);

const SectionCard = ({ title, action, children }) => (
  <GlassCard className="p-6">
    <div className="flex items-center justify-between border-b border-black/10 dark:border-white/10 pb-4 mb-4">
      <h3 className="text-lg font-semibold text-ink-800 dark:text-ink-100">{title}</h3>
      {action}
    </div>
    <div>{children}</div>
  </GlassCard>
);

// ---------- page ----------
const UserProfile = () => {
  const navigate = useNavigate();
  const { logout } = useAuth(); // from your AuthContext

  const [profile, setProfile] = useState(null);
  const [loadingProfile, setLoadingProfile] = useState(true);
  const [message, setMessage] = useState("");

  // edit form state
  const [isEditing, setIsEditing] = useState(false);
  const [form, setForm] = useState({ fullName: "", bio: "", location: "" });
  const [saving, setSaving] = useState(false);

  // tutor mode
  const [toggling, setToggling] = useState(false);

  // lists
  const [acceptedRequests, setAcceptedRequests] = useState([]);
  const [learningRequests, setLearningRequests] = useState([]);
  const [loadingTeach, setLoadingTeach] = useState(true);
  const [loadingLearn, setLoadingLearn] = useState(true);

  // schedule modal
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedRequest, setSelectedRequest] = useState(null);
  const [scheduleForm, setScheduleForm] = useState({
    scheduleDate: "",
    meetingType: "ONLINE",
    meetingLink: "",
  });
  const [scheduling, setScheduling] = useState(false);

  // account actions
  const [moreOpen, setMoreOpen] = useState(false);
  const moreRef = useRef(null);
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);
  const [deactivating, setDeactivating] = useState(false);

  // tabs
  const [tab, setTab] = useState("overview"); // "overview" | "teaching" | "learning"

  // close "more" dropdown on outside clicks
  useEffect(() => {
    const onClick = (e) => {
      if (moreRef.current && !moreRef.current.contains(e.target)) {
        setMoreOpen(false);
      }
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  useEffect(() => {
    loadProfile();
    loadTeaching();
    loadLearning();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadProfile = async () => {
    try {
      setLoadingProfile(true);
      const res = await api.get("/auth/profile");
      setProfile(res.data);
      setForm({
        fullName: res.data.fullName || "",
        bio: res.data.bio || "",
        location: res.data.location || "",
      });
    } catch (err) {
      console.error("Profile load error:", err);
      setMessage("Failed to load profile. Please login again.");
      navigate("/");
    } finally {
      setLoadingProfile(false);
    }
  };

  const loadTeaching = async () => {
    try {
      setLoadingTeach(true);
      const res = await api.get("/requests/accepted");
      setAcceptedRequests(res.data || []);
    } catch (err) {
      console.error("Accepted requests error:", err);
    } finally {
      setLoadingTeach(false);
    }
  };

  const loadLearning = async () => {
    try {
      setLoadingLearn(true);
      const res = await api.get("/requests/accepted/requester");
      setLearningRequests(res.data || []);
    } catch (err) {
      console.error("Learning requests error:", err);
    } finally {
      setLoadingLearn(false);
    }
  };

  const onFormChange = (e) => {
    const { name, value } = e.target;
    setForm((p) => ({ ...p, [name]: value }));
  };

  const saveProfile = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      const res = await api.put("/auth/profile", form);
      if (res.status === 200) {
        setMessage("Profile updated successfully!");
        setIsEditing(false);
        loadProfile();
      } else {
        setMessage("Failed to update profile");
      }
    } catch (err) {
      console.error("Profile save error:", err);
      setMessage("Error updating profile");
    } finally {
      setSaving(false);
    }
  };

  const toggleTutorMode = async () => {
    if (!profile) return;
    try {
      setToggling(true);
      const next = !profile.readyToTeach;
      await api.put("/auth/teach-mode", { readyToTeach: next });
      setProfile((p) => ({ ...p, readyToTeach: next }));
      setMessage(next ? "Tutor mode enabled" : "Tutor mode disabled");
    } catch (err) {
      console.error("Tutor mode error:", err);
      setMessage("Failed to update Tutor mode");
    } finally {
      setToggling(false);
    }
  };

  const openSchedule = (req) => {
    setSelectedRequest(req);
    setScheduleForm({ scheduleDate: "", meetingType: "ONLINE", meetingLink: "" });
    setModalOpen(true);
  };

  const doSchedule = async (e) => {
    e.preventDefault();
    if (!selectedRequest) return;
    try {
      setScheduling(true);
      await api.post(
        `/requests/accepted/${selectedRequest.acceptedRequestId}/schedule`,
        scheduleForm
      );
      setMessage("Meeting scheduled successfully!");
      setModalOpen(false);
      loadTeaching();
      loadLearning();
    } catch (err) {
      console.error("Schedule error:", err);
      setMessage("Failed to schedule meeting");
    } finally {
      setScheduling(false);
    }
  };

  // Deactivate + Logout (self)
  const deactivateAccount = async () => {
    try {
      setDeactivating(true);
      await api.put("/auth/active", { isActive: false });
      setMessage("Your account has been deactivated.");
      setConfirmDeactivate(false);
      setTimeout(() => {
        logout();
        navigate("/login");
      }, 1200);
    } catch (err) {
      console.error("Deactivate error:", err);
      setMessage("Failed to deactivate account");
    } finally {
      setDeactivating(false);
    }
  };

  const overviewStats = useMemo(() => {
    const totalTeach = acceptedRequests.length;
    const scheduledTeach = acceptedRequests.filter((r) => r.status === "SCHEDULED").length;
    const completedTeach = acceptedRequests.filter((r) => r.status === "COMPLETED").length;

    const totalLearn = learningRequests.length;
    const scheduledLearn = learningRequests.filter((r) => r.status === "SCHEDULED").length;
    const completedLearn = learningRequests.filter((r) => r.status === "COMPLETED").length;

    return {
      totalTeach,
      scheduledTeach,
      completedTeach,
      totalLearn,
      scheduledLearn,
      completedLearn,
    };
  }, [acceptedRequests, learningRequests]);

  const formatDate = (d) => new Date(d).toLocaleString();

  if (loadingProfile) {
    return (
      <div className="relative min-h-screen font-sans overflow-hidden">
        {/* Background */}
        <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-ink-900 dark:via-ink-900 dark:to-ink-800" />
        <div className="absolute -top-20 -right-24 -z-10 w-[520px] h-[520px] rounded-full bg-blue-300/20 blur-3xl dark:bg-blue-400/10" />
        <div className="absolute -bottom-16 -left-16 -z-10 w-[420px] h-[420px] rounded-full bg-indigo-300/20 blur-2xl dark:bg-indigo-400/10" />
        <div className="flex justify-center items-center h-[60vh]">
          <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500" />
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="max-w-4xl mx-auto p-6">
        <SectionCard
          title="Profile"
          action={
            <MacPrimary onClick={loadProfile} className="px-3 py-1.5">
              Retry
            </MacPrimary>
          }
        >
          <p className="text-red-600">Couldn’t load profile.</p>
        </SectionCard>
      </div>
    );
  }

  const avatar = profile.profilePicture ? toImageUrl(profile.profilePicture) : "";
  const initial = profile.fullName?.[0]?.toUpperCase() || "U";

  return (
    <div className="relative min-h-screen font-sans">
      {/* Background layers */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-ink-900 dark:via-ink-900 dark:to-ink-800" />
      <div className="absolute -top-20 -right-24 -z-10 w-[520px] h-[520px] rounded-full bg-blue-300/20 blur-3xl dark:bg-blue-400/10" />
      <div className="absolute -bottom-16 -left-16 -z-10 w-[420px] h-[420px] rounded-full bg-indigo-300/20 blur-2xl dark:bg-indigo-400/10" />

      {/* Top glass bar */}
      <div className="sticky top-0 z-40">
        <GlassBar className="px-6 py-4">
          <div className="max-w-7xl mx-auto flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow" />
              <div className="text-slate-700 dark:text-slate-200 font-semibold">
                SkillLink
              </div>
            </div>
            
            <div className=" flex mr-24 items-center text-xs text-slate-500 dark:text-slate-400">
              <p>Profile</p>
            </div>
            
          </div>
        </GlassBar>
      </div>

      {/* Content */}
      <div className="max-w-6xl mx-auto p-6 space-y-6">
        {/* Header / Profile Card */}
        <GlassCard className="p-6">
          <div className="flex items-center justify-between flex-wrap gap-6">
            <div className="flex items-center gap-5">
              {/* Avatar */}
              <div className="w-24 h-24 rounded-2xl overflow-hidden relative border border-white/40 dark:border-white/10">
                {avatar ? (
                  <img src={avatar} alt={profile.fullName} className="w-full h-full object-cover" />
                ) : (
                  <div className="w-full h-full flex items-center justify-center bg-slate-100 dark:bg-ink-800/60">
                    <span className="text-3xl font-semibold text-slate-500 dark:text-slate-300">
                      {initial}
                    </span>
                  </div>
                )}
              </div>

              <div>
                <div className="flex items-center gap-2 flex-wrap">
                  <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
                    {profile.fullName}
                  </h1>
                  {profile.readyToTeach && (
                    <Chip className="bg-emerald-200/60 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200">
                      Tutor
                    </Chip>
                  )}
                </div>
                <p className="text-slate-600 dark:text-slate-300">{profile.email}</p>
                {profile.location && (
                  <p className="text-slate-500 dark:text-slate-400 mt-1 text-sm">
                    📍 {profile.location}
                  </p>
                )}
                {profile.isActive === false && (
                  <p className="text-red-600 text-sm mt-1">This account is inactive.</p>
                )}
              </div>
            </div>

            {/* Actions + Toggle */}
            <div className="flex items-center gap-3">
              {/* Tutor Mode — not for Admin */}
              {profile.role !== "Admin" && (
                <div className="flex items-center gap-2">
                  <div className="text-right">
                    <div className="text-xs font-medium text-slate-700 dark:text-slate-300">
                      Tutor Mode
                    </div>
                    <div className="text-[11px] text-slate-500 dark:text-slate-400">
                      Let others know you can teach
                    </div>
                  </div>
                  <MacToggle
                    checked={profile.readyToTeach}
                    onChange={toggleTutorMode}
                    disabled={toggling}
                  />
                </div>
              )}

              <MacPrimary onClick={() => setIsEditing(true)}>Edit Profile</MacPrimary>

              {/* More menu */}
              <div className="relative" ref={moreRef}>
                <MacButton onClick={() => setMoreOpen((s) => !s)} title="More">
                  •••
                </MacButton>
                {moreOpen && (
                  <div className="absolute right-0 mt-2 w-52 rounded-2xl border border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-900/70 backdrop-blur-xl shadow p-1 z-20">
                    <button
                      onClick={() => {
                        setMoreOpen(false);
                        logout();
                        navigate("/login");
                      }}
                      className="w-full text-left px-3 py-2 rounded-xl hover:bg-black/5 dark:hover:bg-white/10 dark:text-white/35"
                    >
                      Logout
                    </button>
                    <div className="border-t border-white/40 dark:border-white/10 my-1" />
                    <button
                      onClick={() => {
                        setMoreOpen(false);
                        setConfirmDeactivate(true);
                      }}
                      className="w-full text-left px-3 py-2 rounded-xl text-red-600 hover:bg-red-50/50 dark:hover:bg-red-400/10"
                    >
                      Deactivate Account
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        </GlassCard>

        {/* Tabs */}
        <div className="flex gap-2">
          <MacButton
            className={tab === "overview" ? "bg-blue-600 text-black" : ""}
            onClick={() => setTab("overview")}
          >
            Overview
          </MacButton>
          <MacButton
            className={tab === "teaching" ? "bg-blue-600 text-black" : ""}
            onClick={() => setTab("teaching")}
          >
            Teaching <Chip className="ml-2 bg-black/5 dark:bg-white/10">{acceptedRequests.length}</Chip>
          </MacButton>
          <MacButton
            className={tab === "learning" ? "bg-blue-600 text-black" : ""}
            onClick={() => setTab("learning")}
          >
            Learning <Chip className="ml-2 bg-black/5 dark:bg-white/10">{learningRequests.length}</Chip>
          </MacButton>
        </div>

        {/* Flash message */}
        {message && (
          <GlassCard
            className={
              "p-3 " +
              (message.toLowerCase().includes("fail") || message.toLowerCase().includes("error")
                ? "ring-1 ring-red-300/50"
                : "ring-1 ring-emerald-300/50")
            }
          >
            <div className="text-slate-700 dark:text-slate-200">
              {message}
            </div>
          </GlassCard>
        )}

        {/* Panels */}
        {tab === "overview" && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* About / Edit */}
            <SectionCard
              title="About"
              action={
                !isEditing && (
                  <button
                    onClick={() => setIsEditing(true)}
                    className="text-sm text-blue-600 hover:text-blue-700"
                  >
                    Edit
                  </button>
                )
              }
            >
              {isEditing ? (
                <form onSubmit={saveProfile} className="space-y-4">
                  <div>
                    <label className="text-sm text-slate-700 dark:text-slate-300">Full Name</label>
                    <input
                      name="fullName"
                      className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-ink-500/10 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                      value={form.fullName}
                      onChange={onFormChange}
                      required
                    />
                  </div>
                  <div>
                    <label className="text-sm text-slate-700 dark:text-slate-300">Bio</label>
                    <textarea
                      name="bio"
                      rows={3}
                      className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-ink-500/10 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                      value={form.bio}
                      onChange={onFormChange}
                    />
                  </div>
                  <div>
                    <label className="text-sm text-slate-700 dark:text-slate-300">Location</label>
                    <input
                      name="location"
                      className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-ink-500/10 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                      value={form.location}
                      onChange={onFormChange}
                    />
                  </div>
                  <div className="flex gap-2 pt-2">
                    <MacPrimary type="submit" disabled={saving}>
                      {saving ? "Saving..." : "Save"}
                    </MacPrimary>
                    <MacButton type="button" onClick={() => setIsEditing(false)}>
                      Cancel
                    </MacButton>
                  </div>
                </form>
              ) : (
                <div className="space-y-4">
                  <div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">Email</div>
                    <div className="text-slate-900 dark:text-slate-100">{profile.email}</div>
                  </div>
                  <div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">Member Since</div>
                    <div className="text-slate-900 dark:text-slate-100">
                      {new Date(profile.createdAt).toLocaleDateString()}
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">About</div>
                    <div className="text-slate-900 dark:text-slate-100">
                      {profile.bio || "No bio yet."}
                    </div>
                  </div>
                  {profile.location && (
                    <div>
                      <div className="text-sm text-slate-500 dark:text-slate-400">Location</div>
                      <div className="text-slate-900 dark:text-slate-100">{profile.location}</div>
                    </div>
                  )}
                </div>
              )}
            </SectionCard>

            {/* Overview Stats */}
            <SectionCard title="Your Stats">
              <div className="grid grid-cols-2 gap-4">
                <div className="p-4 rounded-xl border border-black/10 dark:border-white/10 bg-white/100 dark:bg-ink-800/40">
                  <div className="text-sm text-slate-600 dark:text-slate-400">Teaching (Total)</div>
                  <div className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
                    {overviewStats.totalTeach}
                  </div>
                  <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    {overviewStats.scheduledTeach} scheduled • {overviewStats.completedTeach} completed
                  </div>
                </div>
                <div className="p-4 rounded-xl border border-black/10 dark:border-white/10 bg-white/100 dark:bg-ink-800/40">
                  <div className="text-sm text-slate-600 dark:text-slate-400">Learning (Total)</div>
                  <div className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
                    {overviewStats.totalLearn}
                  </div>
                  <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    {overviewStats.scheduledLearn} scheduled • {overviewStats.completedLearn} completed
                  </div>
                </div>
              </div>
            </SectionCard>

            {/* Account & Security */}
            <SectionCard title="Account & Security">
              <div className="space-y-4">
                <div className="flex justify-between items-center">
                  <div>
                    <div className="text-sm text-slate-600 dark:text-slate-400">Status</div>
                    <div
                      className={`text-sm font-medium ${
                        profile.isActive ? "text-emerald-600" : "text-red-600"
                      }`}
                    >
                      {profile.isActive ? "Active" : "Inactive"}
                    </div>
                  </div>
                  <MacButton onClick={() => logout()}>Logout</MacButton>
                </div>

                <div className="border-t border-black/10 dark:border-white/10 pt-4">
                  <div className="text-sm text-slate-700 dark:text-slate-300 font-medium">
                    Danger Zone
                  </div>
                  <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
                    Deactivate your account to hide your profile and suspend usage. You can ask an admin to reactivate later.
                  </p>
                  <MacDanger
                    className="mt-3"
                    onClick={() => setConfirmDeactivate(true)}
                  >
                    Deactivate Account
                  </MacDanger>
                </div>
              </div>
            </SectionCard>
          </div>
        )}

        {tab === "teaching" && (
          <SectionCard
            title="Requests I Accepted (Teaching)"
            action={<button onClick={loadTeaching} className="text-sm text-blue-600 hover:text-blue-700">Refresh</button>}
          >
            {loadingTeach ? (
              <div className="text-slate-500 dark:text-slate-400">Loading…</div>
            ) : acceptedRequests.length === 0 ? (
              <div className="text-slate-600 dark:text-slate-300">
                You haven’t accepted any requests yet.
              </div>
            ) : (
              <ul className="divide-y divide-white/30 dark:divide-white/10">
                {acceptedRequests.map((r) => (
                  <li key={r.acceptedRequestId} className="py-4 flex items-start justify-between">
                    <div className="min-w-0 pr-4">
                      <div className="flex items-center gap-2 flex-wrap">
                        <p className="font-medium text-slate-900 dark:text-slate-100">{r.skillName}</p>
                        <Chip className={chipByStatus[r.status] || "bg-slate-200/60 text-slate-800 dark:bg-slate-400/20 dark:text-slate-200"}>
                          {r.status}
                        </Chip>
                      </div>
                      {r.topic && <p className="text-sm text-slate-600 dark:text-slate-300 mt-0.5">{r.topic}</p>}
                      {r.description && <p className="text-sm text-slate-700 dark:text-slate-200 mt-1">{r.description}</p>}
                      <p className="text-xs text-slate-500 dark:text-slate-400 mt-2">
                        Requested by: {r.requesterName} ({r.requesterEmail}) • Accepted on{" "}
                        {new Date(r.acceptedAt).toLocaleDateString()}
                      </p>

                      {r.status === "SCHEDULED" && (
                        <div className="mt-2 p-3 rounded-xl border border-white/40 dark:border-white/10 bg-blue-50/70 dark:bg-blue-900/20 text-sm">
                          <p className="text-blue-800 dark:text-blue-200 font-medium">Scheduled Meeting</p>
                          <p className="text-blue-700 dark:text-blue-300">Date: {formatDate(r.scheduleDate)}</p>
                          <p className="text-blue-700 dark:text-blue-300">Type: {r.meetingType}</p>
                          {r.meetingType === "ONLINE" && r.meetingLink && (
                            <p className="text-blue-700 dark:text-blue-300 truncate">
                              Link:{" "}
                              <a
                                href={r.meetingLink}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="underline"
                              >
                                {r.meetingLink}
                              </a>
                            </p>
                          )}
                        </div>
                      )}
                    </div>

                    <div className="flex flex-col items-end gap-2">
                      {r.status === "PENDING" && (
                        <MacPrimary onClick={() => openSchedule(r)} className="text-sm">
                          Schedule
                        </MacPrimary>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </SectionCard>
        )}

        {tab === "learning" && (
          <SectionCard
            title="Requests I Asked For (Learning)"
            action={<button onClick={loadLearning} className="text-sm text-blue-600 hover:text-blue-700">Refresh</button>}
          >
            {loadingLearn ? (
              <div className="text-slate-500 dark:text-slate-400">Loading…</div>
            ) : learningRequests.length === 0 ? (
              <div className="text-slate-600 dark:text-slate-300">
                You haven’t made any requests yet.
              </div>
            ) : (
              <ul className="divide-y divide-white/30 dark:divide-white/10">
                {learningRequests.map((r) => (
                  <li key={r.acceptedRequestId} className="py-4">
                    <div className="flex items-center justify-between">
                      <div className="min-w-0 pr-4">
                        <div className="flex items-center gap-2 flex-wrap">
                          <p className="font-medium text-slate-900 dark:text-slate-100">{r.skillName}</p>
                          <Chip className={chipByStatus[r.status] || "bg-slate-200/60 text-slate-800 dark:bg-slate-400/20 dark:text-slate-200"}>
                            {r.status}
                          </Chip>
                        </div>
                        {r.topic && <p className="text-sm text-slate-600 dark:text-slate-300 mt-0.5">{r.topic}</p>}
                        {r.description && <p className="text-sm text-slate-700 dark:text-slate-200 mt-1">{r.description}</p>}
                        {r.status === "SCHEDULED" && (
                          <div className="mt-2 p-3 rounded-xl border border-white/40 dark:border-white/10 bg-blue-50/70 dark:bg-blue-900/20 text-sm">
                            <p className="text-blue-800 dark:text-blue-200 font-medium">Scheduled Meeting</p>
                            <p className="text-blue-700 dark:text-blue-300">Date: {formatDate(r.scheduleDate)}</p>
                            <p className="text-blue-700 dark:text-blue-300">Type: {r.meetingType}</p>
                            {r.meetingType === "ONLINE" && r.meetingLink && (
                              <p className="text-blue-700 dark:text-blue-300 truncate">
                                Link:{" "}
                                <a
                                  href={r.meetingLink}
                                  target="_blank"
                                  rel="noopener noreferrer"
                                  className="underline"
                                >
                                  {r.meetingLink}
                                </a>
                              </p>
                            )}
                          </div>
                        )}
                      </div>

                      <div className="flex items-center gap-2">
                        {r.status === "SCHEDULED" && (
                          <MacPrimary onClick={() => navigate("/VideoSession")} className="text-sm">
                            Join
                          </MacPrimary>
                        )}
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </SectionCard>
        )}
      </div>

      {/* Schedule Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-white/30 dark:border-white/10">
              <h4 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Schedule Meeting</h4>
            </div>
            <form onSubmit={doSchedule} className="p-6 space-y-4">
              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">Date & Time *</label>
                <input
                  type="datetime-local"
                  name="scheduleDate"
                  value={scheduleForm.scheduleDate}
                  onChange={(e) => setScheduleForm((p) => ({ ...p, scheduleDate: e.target.value }))}
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                  required
                />
              </div>

              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">Meeting Type *</label>
                <select
                  name="meetingType"
                  value={scheduleForm.meetingType}
                  onChange={(e) => setScheduleForm((p) => ({ ...p, meetingType: e.target.value }))}
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                >
                  <option value="ONLINE">Online</option>
                  <option value="PHYSICAL">In-Person</option>
                </select>
              </div>

              {scheduleForm.meetingType === "ONLINE" && (
                <div>
                  <label className="text-sm text-slate-700 dark:text-slate-300">Meeting Link *</label>
                  <input
                    type="url"
                    name="meetingLink"
                    placeholder="https://meet.google.com/xxx-xxxx-xxx"
                    value={scheduleForm.meetingLink}
                    onChange={(e) => setScheduleForm((p) => ({ ...p, meetingLink: e.target.value }))}
                    required
                    className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                  />
                </div>
              )}

              <div className="flex justify-end gap-2 pt-2">
                <MacButton type="button" onClick={() => setModalOpen(false)}>
                  Cancel
                </MacButton>
                <MacPrimary type="submit" disabled={scheduling}>
                  {scheduling ? "Scheduling..." : "Schedule"}
                </MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Deactivate Confirmation Modal */}
      {confirmDeactivate && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-white/30 dark:border-white/10">
              <h4 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Deactivate Account</h4>
            </div>
            <div className="p-6 space-y-3">
              <p className="text-sm text-slate-700 dark:text-slate-300">
                Are you sure you want to deactivate your account? Your profile will be inactive and
                you will be logged out. You can ask an admin to reactivate it later.
              </p>
              <div className="flex justify-end gap-2 mt-4">
                <MacButton onClick={() => setConfirmDeactivate(false)}>Cancel</MacButton>
                <MacDanger onClick={deactivateAccount} disabled={deactivating}>
                  {deactivating ? "Deactivating…" : "Deactivate"}
                </MacDanger>
              </div>
            </div>
          </GlassCard>
        </div>
      )}

      {/* Dock Quick Actions */}
      <div className="fixed bottom-8 left-1/2 -translate-x-1/2 flex gap-3 px-4 py-3 rounded-2xl border border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-900/70 backdrop-blur-xl shadow z-30">
        <MacButton onClick={() => navigate("/request")}>+ Request</MacButton>
        <MacButton onClick={() => navigate("/skill")}>Skills</MacButton>
        <MacButton onClick={() => navigate("/VideoSession")}>Session</MacButton>
        <MacButton onClick={() => navigate("/dashboard")}>Dashboard</MacButton>
      </div>
    </div>
  );
};

export default UserProfile;
