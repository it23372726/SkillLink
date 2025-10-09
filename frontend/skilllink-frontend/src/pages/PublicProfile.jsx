// src/pages/PublicProfile.jsx
import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { toImageUrl } from "../utils/image";
import { useAuth } from "../context/AuthContext";
import Dock from "../components/Dock";
import {
  friendsApi,
  requestsApi,
  skillsApi,
  tutorPostsApi,
  usersApi,
  socialApi  
} from "../api/client";

/* ===== Atoms (matching your profile look) ===== */
const GlassCard = ({ className = "", children }) => (
  <div
    className={
      "relative rounded-2xl border shadow backdrop-blur-xl transition-all duration-300 " +
      "border-black/10 dark:border-white/10 bg-ink-100/50 dark:bg-ink-900/50 " +
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
      "border-white/40 dark:border-white/10 bg-white/60 dark:bg-ink-900/50 " +
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
      "border-black/10 dark:border-white/10 bg-white/50 hover:bg-black/5 " +
      "dark:hover:bg-white/10 active:bg-white/80 dark:bg-ink-800/60 " +
      "dark:hover:bg-ink-800/80 focus:outline-none focus:ring-1 " +
      "focus:ring-blue-400/30 dark:focus:text-white/80 focus:text-black " +
      "text-black/80 dark:text-white/65 active:dark:text-white active:text-black " +
      (className ? " " + className : "")
    }
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
const Chip = ({ children, className = "" }) => (
  <span
    className={
      "px-2.5 py-1 text-xs font-medium rounded-full border " +
      "border-white/30 dark:border-white/10 " +
      className
    }
  >
    {children}
  </span>
);
const SectionCard = ({ title, className = "", action, children }) => (
  <GlassCard className={"p-6"}>
    <div className="flex items-center justify-between border-b border-black/10 dark:border-white/10 pb-4 mb-4">
      <h3 className="text-lg font-semibold text-ink-800 dark:text-ink-100">
        {title}
      </h3>
      {action}
    </div>
    <div className={className}>{children}</div>
  </GlassCard>
);

const levelPill = (level) => {
  switch (level) {
    case "Beginner":
      return "bg-blue-200/60 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200";
    case "Intermediate":
      return "bg-emerald-200/60 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200";
    case "Advanced":
      return "bg-yellow-200/60 text-yellow-900 dark:bg-yellow-400/20 dark:text-yellow-200";
    case "Expert":
      return "bg-purple-200/60 text-purple-900 dark:bg-purple-400/20 dark:text-purple-200";
    default:
      return "bg-slate-200/60 text-slate-900 dark:bg-slate-400/20 dark:text-slate-200";
  }
};

/* ========================= Page ========================= */
const PublicProfile = () => {
  const { userId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth(); // { userId, ... }

  const [profile, setProfile] = useState(null);
  const [loadingProfile, setLoadingProfile] = useState(true);
  const [message, setMessage] = useState("");

  const [skills, setSkills] = useState([]);
  const [loadingSkills, setLoadingSkills] = useState(true);

  const [tutorPosts, setTutorPosts] = useState([]);
  const [loadingPosts, setLoadingPosts] = useState(true);

  const [following, setFollowing] = useState(false);
  const [checkingFollow, setCheckingFollow] = useState(true);

  // Optional counters if you add endpoints later
  const [followersCount, setFollowersCount] = useState(null);
  const [followingCount, setFollowingCount] = useState(null);

  // Request modal
  const [requestOpen, setRequestOpen] = useState(false);
  const [reqForm, setReqForm] = useState({
    skillName: "",
    topic: "",
    description: "",
  });
  const [creatingReq, setCreatingReq] = useState(false);

  // Redirect to /profile if user opens their own public page
  useEffect(() => {
    if (user?.userId && String(user.userId) === String(userId)) {
      navigate("/profile", { replace: true });
    }
  }, [user?.userId, userId, navigate]);

  useEffect(() => {
    if (!userId) return;
    loadPublicProfile(userId);
    loadSkills(userId);
    loadTutorPosts(userId);
    checkFollowing(userId);
    loadSocialCounts(userId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  const loadPublicProfile = async (id) => {
    try {
      setLoadingProfile(true);
      const res = await usersApi.publicProfile(id); // GET /api/auth/users/:id/public
      setProfile(res.data);
    } catch (err) {
      console.error("Public profile load error:", err);
      setMessage("Failed to load user.");
    } finally {
      setLoadingProfile(false);
    }
  };

  const loadSkills = async (id) => {
    try {
      setLoadingSkills(true);
      const res = await skillsApi.byUser(`${id}`);
      setSkills(res.data || []);
    } catch (err) {
      console.error("Public skills load error:", err);
    } finally {
      setLoadingSkills(false);
    }
  };

  const loadTutorPosts = async (id) => {
    try {
      setLoadingPosts(true);
      const res = await tutorPostsApi.list();
      const filtered = (res.data || []).filter(
        (p) => String(p.tutorId) === String(id)
      );
      setTutorPosts(filtered);
    } catch (err) {
      console.error("Tutor posts load error:", err);
    } finally {
      setLoadingPosts(false);
    }
  };

  const checkFollowing = async (id) => {
    try {
      setCheckingFollow(true);
      const myFollowing = await friendsApi.my(); // list I follow
      const isFollowing = (myFollowing.data || []).some(
        (u) => String(u.userId) === String(id)
      );
      setFollowing(isFollowing);
    } catch (err) {
      console.warn("Follow check failed:", err?.response?.data || err?.message);
      setFollowing(false);
    } finally {
      setCheckingFollow(false);
    }
  };

  // Optional counters (only if you exposed endpoints: /api/friends/:id/followers, /api/friends/:id/following)
  const loadSocialCounts = async (id) => {
    try {
      const [fols, fwing] = await Promise.all([
        socialApi.followersOf(id),
        socialApi.followingOf(id),
      ]);
      setFollowersCount((fols.data || []).length);
      setFollowingCount((fwing.data || []).length);
    } catch (e) {
      setFollowersCount(null);
      setFollowingCount(null);
    }
  };

  const onFollow = async () => {
    try {
      await friendsApi.follow(`${userId}`);
      setFollowing(true);
      setMessage(`You’re now following ${profile?.fullName || "this user"}.`);
    } catch (e) {
      setMessage(e?.response?.data?.message || "Failed to follow");
    }
  };

  const onUnfollow = async () => {
    try {
      await friendsApi.unfollow(`${userId}`);
      setFollowing(false);
      setMessage(`Unfollowed ${profile?.fullName || "the user"}.`);
    } catch (e) {
      setMessage(e?.response?.data?.message || "Failed to unfollow");
    }
  };

  const openRequest = () => {
    setReqForm({ skillName: "", topic: "", description: "" });
    setRequestOpen(true);
  };

  const createRequest = async (e) => {
    e.preventDefault();
    try {
      setCreatingReq(true);
      const payload = {
        skillName: reqForm.skillName,
        topic: reqForm.topic,
        description: reqForm.description,
        preferredTutorId: Number(userId), // backend may ignore if not implemented
        isPrivate: true,
      };
      await requestsApi.create(payload);
      setMessage("Request sent!");
      setRequestOpen(false);
    } catch (err) {
      console.error("Create request error:", err);
      setMessage("Failed to create request");
    } finally {
      setCreatingReq(false);
    }
  };

  const avatar = profile?.profilePicture ? toImageUrl(profile.profilePicture) : "";
  const initial = profile?.fullName?.[0]?.toUpperCase() || "U";

  return (
    <div className="relative min-h-screen font-sans">
      {/* Background */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-ink-900 dark:via-ink-900 dark:to-ink-800" />

      {/* Top bar */}
      <div className="sticky top-0 z-40">
        <GlassBar className="border-x-0 border-t-0 px-6 py-4">
          <div className="max-w-7xl mx-auto flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow" />
              <div className="text-slate-700 dark:text-slate-200 font-semibold">
                SkillLink
              </div>
            </div>
            <div className="flex mr-24 items-center text-xs text-slate-500 dark:text-slate-400">
              <p>Public Profile</p>
            </div>
          </div>
        </GlassBar>
      </div>

      {/* Content */}
      <div className="max-w-6xl mx-auto p-6 space-y-6 pt-3">
        {/* Flash */}
        {message && (
          <GlassCard
            className={
              "p-3 ring-1 " +
              (message.toLowerCase().includes("fail") ||
              message.toLowerCase().includes("error")
                ? "ring-red-300/50"
                : "ring-emerald-300/50")
            }
          >
            <div className="text-slate-700 dark:text-slate-200">{message}</div>
          </GlassCard>
        )}

        {/* Header */}
        <GlassCard className="p-6">
          {loadingProfile ? (
            <div className="animate-pulse h-20" />
          ) : !profile ? (
            <div className="text-red-600">User not found.</div>
          ) : (
            <div className="flex items-center justify-between flex-wrap gap-6">
              <div className="flex items-center gap-5">
                <div className="w-24 h-24 rounded-2xl overflow-hidden relative border border-white/40 dark:border-white/10">
                  {avatar ? (
                    <img
                      src={avatar}
                      alt={profile.fullName}
                      className="w-full h-full object-cover"
                    />
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
                  {profile.location && (
                    <p className="text-slate-500 dark:text-slate-400 mt-1 text-sm">
                      <i className="fas fa-map-marker-alt" /> {profile.location}
                    </p>
                  )}
                  <p className="text-slate-500 dark:text-slate-400 mt-1 text-xs">
                    Joined {new Date(profile.createdAt).toLocaleDateString()}
                  </p>
                </div>
              </div>

              <div className="flex items-center gap-3">
                {!checkingFollow && (
                  <>
                    {!following ? (
                      <MacPrimary onClick={onFollow}>Follow</MacPrimary>
                    ) : (
                      <MacButton
                        onClick={onUnfollow}
                        className="text-red-600 border-red-300/40"
                      >
                        Unfollow
                      </MacButton>
                    )}
                  </>
                )}
                <MacPrimary onClick={openRequest}>Request a Lesson</MacPrimary>
              </div>
            </div>
          )}
        </GlassCard>

        {/* About / Skills / Social */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* About */}
          <SectionCard title="About">
            {loadingProfile ? (
              <div className="animate-pulse h-24" />
            ) : !profile ? (
              <div className="text-slate-600">No info.</div>
            ) : (
              <div className="space-y-3">
                <div>
                  <div className="text-sm text-slate-500 dark:text-slate-400">
                    About
                  </div>
                  <div className="text-slate-900 dark:text-slate-100">
                    {profile.bio || "No bio provided."}
                  </div>
                </div>
                {profile.location && (
                  <div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">
                      Location
                    </div>
                    <div className="text-slate-900 dark:text-slate-100">
                      {profile.location}
                    </div>
                  </div>
                )}
                {profile.readyToTeach && (
                  <div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">
                      Availability
                    </div>
                    <div className="text-slate-900 dark:text-slate-100">
                      Open to teach
                    </div>
                  </div>
                )}
              </div>
            )}
          </SectionCard>

          {/* Skills */}
          <SectionCard title="Skills">
            {loadingSkills ? (
              <div className="grid sm:grid-cols-2 gap-3">
                {[...Array(4)].map((_, i) => (
                  <div
                    key={i}
                    className="border border-slate-200 dark:border-slate-700 rounded-xl p-4 animate-pulse h-16"
                  />
                ))}
              </div>
            ) : skills.length === 0 ? (
              <div className="text-slate-600 dark:text-slate-300">
                No skills added.
              </div>
            ) : (
              <div className="grid sm:grid-cols-2 gap-3">
                {skills.map((s) => (
                  <div
                    key={s.skillId}
                    className="border border-slate-200 dark:border-slate-700 rounded-xl p-4 flex items-center justify-between"
                  >
                    <div>
                      <div className="font-medium text-slate-900 dark:text-slate-100">
                        {s.skill?.name}
                      </div>
                      <span
                        className={`inline-block mt-1 px-2 py-0.5 text-xs font-semibold rounded-full ${levelPill(
                          s.level
                        )}`}
                      >
                        {s.level}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </SectionCard>

          {/* Social (optional counts) */}
          <SectionCard title="Social">
            <div className="grid grid-cols-2 gap-4">
              <div className="p-4 rounded-xl border border-black/10 dark:border-white/10 bg-white/100 dark:bg-ink-800/40">
                <div className="text-sm text-slate-600 dark:text-slate-400">
                  Followers
                </div>
                <div className="text-2xl font-semibold text-emerald-600">
                  {followersCount === null ? "—" : followersCount}
                </div>
              </div>
              <div className="p-4 rounded-xl border border-black/10 dark:border-white/10 bg-white/100 dark:bg-ink-800/40">
                <div className="text-sm text-slate-600 dark:text-slate-400">
                  Following
                </div>
                <div className="text-2xl font-semibold text-blue-600">
                  {followingCount === null ? "—" : followingCount}
                </div>
              </div>
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-3">
              Counts shown if API is available; otherwise placeholders.
            </p>
          </SectionCard>
        </div>

        {/* Tutor Posts */}
        <SectionCard title="Tutor Posts">
          {loadingPosts ? (
            <div className="text-slate-500 dark:text-slate-400">Loading…</div>
          ) : tutorPosts.length === 0 ? (
            <div className="text-slate-600 dark:text-slate-300">
              No tutor posts from this user yet.
            </div>
          ) : (
            <ul className="grid md:grid-cols-2 gap-4">
              {tutorPosts.map((p) => (
                <li
                  key={p.postId}
                  className="rounded-xl border border-black/10 dark:border-white/10 p-4 bg-white/70 dark:bg-ink-800/50"
                >
                  <div className="flex items-center justify-between">
                    <div className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                      {p.title}
                    </div>
                    <Chip className="bg-slate-200/60 dark:bg-slate-400/20">
                      {p.status}
                    </Chip>
                  </div>
                  {p.description && (
                    <p className="text-sm text-slate-600 dark:text-slate-300 mt-1">
                      {p.description}
                    </p>
                  )}
                  <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                    Max Participants: {p.maxParticipants} • Current:{" "}
                    {p.currentParticipants}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </SectionCard>
      </div>

      {/* Request Modal */}
      {requestOpen && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-white/30 dark:border-white/10">
              <h4 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                Request a Lesson
              </h4>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                This will create a request (optionally with a preferred tutor).
              </p>
            </div>
            <form onSubmit={createRequest} className="p-6 space-y-4">
              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">
                  Skill Name *
                </label>
                <input
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                  value={reqForm.skillName}
                  onChange={(e) =>
                    setReqForm((p) => ({ ...p, skillName: e.target.value }))
                  }
                  required
                />
              </div>
              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">
                  Topic
                </label>
                <input
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                  value={reqForm.topic}
                  onChange={(e) =>
                    setReqForm((p) => ({ ...p, topic: e.target.value }))
                  }
                />
              </div>
              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">
                  Description
                </label>
                <textarea
                  rows={3}
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-800/60 text-slate-800 dark:text-slate-200"
                  value={reqForm.description}
                  onChange={(e) =>
                    setReqForm((p) => ({ ...p, description: e.target.value }))
                  }
                />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <MacButton type="button" onClick={() => setRequestOpen(false)}>
                  Cancel
                </MacButton>
                <MacPrimary type="submit" disabled={creatingReq}>
                  {creatingReq ? "Sending..." : "Send Request"}
                </MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Dock */}
      <Dock peek={18}>
        <MacButton onClick={() => navigate("/home")}>Home</MacButton>
        <MacButton onClick={() => navigate("/request")}>+ Request</MacButton>
        <MacButton onClick={() => navigate("/skill")}>Skills</MacButton>
        <MacButton onClick={() => navigate("/VideoSession")}>Session</MacButton>
        <MacButton onClick={() => navigate("/dashboard")}>Dashboard</MacButton>
      </Dock>
    </div>
  );
};

export default PublicProfile;
