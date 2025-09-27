import React, { useEffect, useState, useMemo } from "react";
import api from "../api/axios";
import { useAuth } from "../context/AuthContext";
import { useNavigate } from "react-router-dom";
import { toImageUrl } from "../api/base";
import Dock from "../components/Dock";
import { MacButton, MacPrimary, GlassBar, GlassCard, Chip } from "../components/UI";
import FriendsDrawer from "../components/friends/FriendsDrawer";

const statusStyles = {
  PENDING:
    "bg-yellow-200/70 text-yellow-900 dark:bg-yellow-400/20 dark:text-yellow-200",
  SCHEDULED:
    "bg-blue-200/70 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  COMPLETED:
    "bg-emerald-200/70 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
  CANCELLED:
    "bg-red-200/70 text-red-900 dark:bg-red-400/20 dark:text-red-200",
  Open:
    "bg-blue-200/70 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  Closed:
    "bg-slate-200/70 text-slate-900 dark:bg-slate-400/20 dark:text-slate-200",
  Scheduled:
    "bg-amber-200/70 text-amber-900 dark:bg-amber-400/20 dark:text-amber-200",
};

const Avatar = ({ name, imageUrl, size = 9 }) => {
    const classes = `w-${size} h-${size} rounded-full flex items-center justify-center overflow-hidden`;
    if (imageUrl) {
      return (
        <img
          src={toImageUrl(imageUrl)}
          alt={name || "User"}
          className={`${classes} border border-slate-200 dark:border-slate-700 object-cover`}
        />
      );
    }
    return (
      <div
        className={`${classes} bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white font-semibold`}
      >
        {(name?.[0] || "U").toUpperCase()}
      </div>
    );
  };
  

/* ---- Post Card ---- */
const PostCard = ({ item, onLike, onDislike, onRemoveReaction, onAccept }) => {
    const { user } = useAuth();
    const [openComments, setOpenComments] = useState(false);
    const [comments, setComments] = useState([]);
    const [cmt, setCmt] = useState("");
    const [loadingComments, setLoadingComments] = useState(false);
  
    // NEW — preview state
    const [preview, setPreview] = useState(null);
    const [loadingPreview, setLoadingPreview] = useState(false);
  
    const iLiked = item.myReaction === "LIKE";
    const iDisliked = item.myReaction === "DISLIKE";
    const isOwner = user?.userId === item.authorId;
  
    // Acceptable rules
    const lessonAcceptable =
      item.postType === "LESSON" && item.status === "Open" && !isOwner;
    const requestAcceptable =
      item.postType === "REQUEST" &&
      !["COMPLETED", "CANCELLED"].includes(item.status) &&
      !isOwner;
    const canAccept = lessonAcceptable || requestAcceptable;
  
    // ========= Comments handling =========
    const loadComments = async () => {
      setLoadingComments(true);
      try {
        const res = await api.get(`/feed/${item.postType}/${item.postId}/comments`);
        setComments(res.data || []);
      } finally {
        setLoadingComments(false);
      }
    };
  
    const toggleComments = async () => {
      if (!openComments) await loadComments();
      setOpenComments((v) => !v);
    };
  
    const addComment = async (e) => {
      e.preventDefault();
      const content = cmt.trim();
      if (!content) return;
      await api.post(`/feed/${item.postType}/${item.postId}/comments`, { content });
      setCmt("");
      // refresh both full list and preview
      await loadComments();
      setPreview({
        fullName: user?.fullName || "You",
        content,
        createdAt: new Date().toISOString(),
      });
    };
  
    const removeComment = async (commentId) => {
      await api.delete(`/feed/comments/${commentId}`);
      await loadComments();
      // update preview if we just deleted the preview
      if (preview && comments?.length) {
        const stillExists = comments.some((c) => c.commentId === preview.commentId);
        if (!stillExists) await loadPreview();
      } else {
        await loadPreview();
      }
    };
  
    // ========= NEW: Preview loader =========
    const loadPreview = async () => {
      if (!item?.commentCount || item.commentCount <= 0) {
        setPreview(null);
        return;
      }
      setLoadingPreview(true);
      try {
        // Try to get latest one fast
        const res = await api.get(
          `/feed/${item.postType}/${item.postId}/comments`,
          { params: { limit: 1, sort: "desc" } } // if supported by backend
        );
        const arr = res.data || [];
        if (arr.length > 0) setPreview(arr[0]);
        else setPreview(null);
      } catch (err) {
        // Fallback: fetch all and take last/first
        try {
          const res2 = await api.get(`/feed/${item.postType}/${item.postId}/comments`);
          const all = res2.data || [];
          // choose the most recent — backend may already return newest first
          setPreview(all[0] || null);
        } catch {
          setPreview(null);
        }
      } finally {
        setLoadingPreview(false);
      }
    };
  
    useEffect(() => {
      // Whenever post changes or comment count changes, re-try preview
      if (item?.commentCount > 0 && !openComments) {
        loadPreview();
      } else {
        setPreview(null);
      }
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [item.postId, item.commentCount, openComments]);
  
    return (
      <GlassCard className="p-4">
        {/* Header */}
        <div className="flex items-start gap-3">
          <Avatar name={item.authorName} imageUrl={item.authorPic} />
          <div className="flex-1">
            <div className="flex items-center justify-between">
              <div className="font-semibold text-slate-900 dark:text-slate-100">
                {item.authorName}
                <span className="ml-2 text-xs text-slate-500 dark:text-slate-400">
                  @{item.postType.toLowerCase()}
                </span>
              </div>
              {/* Status chip */}
              {item.status ? (
                <Chip className={statusStyles[item.status] || ""}>
                  {item.status}
                </Chip>
              ) : null}
            </div>
            <div className="text-xs text-slate-500 dark:text-slate-400">
              {new Date(item.createdAt).toLocaleString()}
              {item.scheduledAt && (
                <span className="ml-2 text-blue-600 dark:text-blue-400">
                  · Scheduled: {new Date(item.scheduledAt).toLocaleString()}
                </span>
              )}
            </div>
          </div>
        </div>
  
        {/* Body */}
        <div className="mt-3">
          <div className="text-lg font-semibold text-slate-900 dark:text-slate-100">
            {item.title}
          </div>
          {item.subtitle && (
            <div className="text-sm text-slate-600 dark:text-slate-300 mt-0.5">
              {item.subtitle}
            </div>
          )}
          {item.body && (
            <div className="text-slate-800 dark:text-slate-200 mt-2 whitespace-pre-wrap">
              {item.body}
            </div>
          )}
          {item.imageUrl && (
            <div className="mt-3">
              <img
                src={item.imageUrl ? toImageUrl(item.imageUrl) : ""}
                alt="Post"
                className="w-full rounded-xl border border-slate-200 dark:border-slate-700"
              />
            </div>
          )}
        </div>
  
        {/* NEW — Compact preview (shown only when not expanded) */}
        {item.commentCount > 0 && !openComments && (
          <button
            type="button"
            onClick={toggleComments}
            className="mt-3 w-full text-left"
            title="Show all comments"
          >
            <div className="flex items-start gap-2 px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/60 dark:bg-slate-800/40 hover:bg-slate-100/60 dark:hover:bg-slate-800/60 transition">
              <Avatar name={preview?.fullName || "U"} imageUrl={item.authorPic}/>
              <div className="min-w-0">
                <div className="text-sm font-medium text-slate-800 dark:text-slate-200 truncate max-w-[220px] sm:max-w-[280px] md:max-w-[360px]">
                  {preview?.fullName || "View comments"}
                </div>
                <div className="text-sm text-slate-600 dark:text-slate-300 truncate max-w-[220px] sm:max-w-[280px] md:max-w-[360px]">
                  {loadingPreview
                    ? "Loading comment…"
                    : preview?.content || "See what others said…"}
                </div>
              </div>
              <span className="ml-auto text-xs text-slate-500 dark:text-slate-400">
                {item.commentCount}
              </span>
            </div>
          </button>
        )}
  
        {/* Footer actions */}
        <div className="mt-4 flex items-center justify-between text-sm">
          <div className="flex items-center gap-4">
            {/* Like */}
            <button
              onClick={() => (iLiked ? onRemoveReaction(item) : onLike(item))}
              className={
                "inline-flex items-center gap-1 px-2 py-1 rounded hover:bg-black/5 dark:hover:bg-white/10 " +
                (iLiked ? "text-blue-600 dark:text-blue-400" : "text-slate-600 dark:text-slate-300")
              }
            >
              👍 <span>{item.likes}</span>
            </button>
  
            {/* Dislike */}
            <button
              onClick={() => (iDisliked ? onRemoveReaction(item) : onDislike(item))}
              className={
                "inline-flex items-center gap-1 px-2 py-1 rounded hover:bg-black/5 dark:hover:bg-white/10 " +
                (iDisliked ? "text-red-600 dark:text-red-400" : "text-slate-600 dark:text-slate-300")
              }
            >
              👎 <span>{item.dislikes}</span>
            </button>
  
            {/* Comment (toggle) */}
            <button
              onClick={toggleComments}
              className="inline-flex items-center gap-1 px-2 py-1 rounded hover:bg-black/5 dark:hover:bg-white/10 text-slate-600 dark:text-slate-300"
            >
              💬 <span>{openComments ? "Hide" : "Comments"} ({item.commentCount})</span>
            </button>
          </div>
  
          {/* Accept */}
          {canAccept && (
            <MacPrimary
              onClick={() => !item.accepted && onAccept(item)} // prevent double click
              disabled={item.accepted} // disable if already accepted
            >
              {item.accepted ? "Accepted" : "Accept"}
            </MacPrimary>
          )}
        </div>
  
        {/* Full Comments */}
        {openComments && (
          <div className="mt-3 border-t border-slate-200 dark:border-slate-700 pt-3 space-y-3">
            {loadingComments ? (
              <div className="text-slate-500">Loading comments...</div>
            ) : comments.length === 0 ? (
              <div className="text-slate-500">Be the first to comment</div>
            ) : (
              comments.map((c) => {
                const canDelete =
                  (user?.userId && user.userId === c.userId) ||
                  user?.role === "ADMIN" ||
                  user?.roles?.includes?.("ADMIN") ||
                  item.authorId === user?.userId;
  
                return (
                  <div key={c.commentId} className="flex items-start gap-2">
                    <Avatar name={c.fullName} imageUrl={item.authorPic}/>
                    <div className="flex-1">
                      <div className="text-sm font-medium text-slate-800 dark:text-slate-200">
                        {c.fullName}
                      </div>
                      <div className="text-sm text-slate-700 dark:text-slate-200">
                        {c.content}
                      </div>
                      <div className="text-xs text-slate-500 dark:text-slate-400">
                        {new Date(c.createdAt).toLocaleString()}
                        {canDelete && (
                          <button
                            onClick={() => removeComment(c.commentId)}
                            className="ml-3 text-xs text-red-500 hover:underline"
                          >
                            delete
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })
            )}
  
            <form onSubmit={addComment} className="flex items-center gap-2">
              <input
                value={cmt}
                onChange={(e) => setCmt(e.target.value)}
                placeholder="Write a comment…"
                className="flex-1 px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-800 dark:text-slate-200"
              />
              <MacPrimary type="submit">Post</MacPrimary>
            </form>
          </div>
        )}
      </GlassCard>
    );
  };
  const debounce = (fn, ms = 450) => {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  };
  

/* ---- Page ---- */
const HomeFeed = () => {
    const { user: authUser } = useAuth();  // ✅ take from AuthContext if present
    const navigate = useNavigate();
  
    // avatar user (from context OR fetched)
    const [meUser, setMeUser] = useState(null);
    const [loadingMe, setLoadingMe] = useState(true);
  
    const [items, setItems] = useState([]);
    const [page, setPage] = useState(1);
    const [busy, setBusy] = useState(false);
    const [done, setDone] = useState(false);
    const [initialLoaded, setInitialLoaded] = useState(false);
  
    // Accept-disabling state
    const [acceptedLocal, setAcceptedLocal] = useState(() => new Set());
  
    // ===== New: Search state (server-side) =====
    const [query, setQuery] = useState("");
    const [liveQ, setLiveQ] = useState("");
    const [searching, setSearching] = useState(false);
    console.log("user: ", meUser);
  
    // Debounce query so we don't call API for each keystroke
    const debouncedSetLiveQ = useMemo(
      () =>
        debounce((q) => {
          setLiveQ(q.trim());
        }, 450),
      []
    );
  
    useEffect(() => {
      debouncedSetLiveQ(query);
    }, [query, debouncedSetLiveQ]);
  
    // Load current user for avatar
    useEffect(() => {
      let ignore = false;
  
      const ensureUser = async () => {
        try {
          // Fallback: fetch profile for avatar/header
          const res = await api.get("/auth/profile");
          if (!ignore) {
            setMeUser(res.data || null);
            setLoadingMe(false);
          }
        } catch (err) {
          if (!ignore) {
            setMeUser(null);
            setLoadingMe(false);
          }
        }
      };
  
      ensureUser();
      return () => {
        ignore = true;
      };
    }, [authUser?.userId]); // re-run if user changes
  
    // Load function that supports normal & search mode
    const PAGE_SIZE = 8;
    const load = async (p = 1, qStr = "") => {
      if (busy || (p !== 1 && done)) return;
  
      setBusy(true);
      try {
        const params = { page: p, pageSize: PAGE_SIZE };
        if (qStr) params.q = qStr;
  
        const res = await api.get("/feed", { params });
        const arr = res.data || [];
  
        if (arr.length < PAGE_SIZE) setDone(true);
  
        setItems((prev) => (p === 1 ? arr : [...prev, ...arr]));
        setPage(p);
        setSearching(!!qStr);
        setInitialLoaded(true);
      } finally {
        setBusy(false);
      }
    };
  
    // Initial load
    useEffect(() => {
      load(1, "");
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
  
    // When debounced search query changes, reset & reload
    useEffect(() => {
      setDone(false);
      setAcceptedLocal(new Set());
      load(1, liveQ);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [liveQ]);
  
    const keyOf = (it) => `${it.postType}-${it.postId}`;
  
    const refreshOne = async () => {
      setDone(false);
      await load(1, liveQ);
    };
  
    const like = async (it) => {
      try {
        await api.post(`/feed/${it.postType}/${it.postId}/like`);
        refreshOne();
      } catch (e) {
        alert(e?.response?.data?.message || "Failed to like");
      }
    };
    const dislike = async (it) => {
      try {
        await api.post(`/feed/${it.postType}/${it.postId}/dislike`);
        refreshOne();
      } catch (e) {
        alert(e?.response?.data?.message || "Failed to dislike");
      }
    };
    const removeReaction = async (it) => {
      try {
        await api.delete(`/feed/${it.postType}/${it.postId}/reaction`);
        refreshOne();
      } catch (e) {
        alert(e?.response?.data?.message || "Failed to remove reaction");
      }
    };
  
    const onAccept = async (it) => {
      try {
        if (it.postType === "LESSON") {
          await api.post(`/tutor-posts/${it.postId}/accept`);
        } else if (it.postType === "REQUEST") {
          await api.post(`/requests/${it.postId}/accept`);
        }
        setAcceptedLocal((prev) => new Set(prev).add(keyOf(it)));
        await refreshOne();
      } catch (e) {
        alert(e?.response?.data?.message || "Failed to accept");
      }
    };
  
    const onLoadMore = () => {
      if (busy || done) return;
      const next = page + 1;
      load(next, liveQ);
    };
  
    return (
      <div className="relative min-h-screen font-sans">
        {/* BG */}
        <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-slate-900 dark:via-slate-900 dark:to-slate-800" />
  
        {/* Top Bar */}
        <GlassBar className="sticky border-x-0 border-t-0 px-6 p-4 top-0 z-40">
          <div className=" mx-auto flex items-center gap-4">
            {/* Logo */}
            <div className="flex items-center gap-3 shrink-0">
              <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow" />
              <div className="font-semibold text-slate-700 dark:text-slate-200">SkillLink</div>
            </div>
  
            {/* Search */}
            <div className="relative flex flex-1 items-center justify-start">
              <input
                type="text"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search Feed"
                className="w-full max-w-xs pl-10 pr-8 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />
              <span className="absolute left-3 top-2.5 text-slate-400">
                <i className="fas fa-search" />
              </span>
              {query && (
                <button
                  onClick={() => setQuery("")}
                  className="absolute right-2 top-2 inline-flex items-center justify-center w-6 h-6 rounded hover:bg-black/5 dark:hover:bg-white/10 text-slate-500"
                  title="Clear"
                >
                  ✕
                </button>
              )}
            </div>
              <div>
              <button
                onClick={() => navigate("/profile")}
                className="focus:outline-none"
                title="Notifiacations"
              >
                <div className="w-10 h-10 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white flex items-center justify-center font-semibold">
                    <i className="fas fa-bell"></i>
                </div>
              </button>
              </div>
            {/* Profile Avatar */}
            <div className="flex items-center gap-2 shrink-0">
              <button
                onClick={() => navigate("/profile")}
                className="focus:outline-none"
                title="My Profile"
              >
                {loadingMe ? (
                  // tiny skeleton while loading
                  <div className="w-10 h-10 rounded-full bg-slate-200 dark:bg-slate-700 animate-pulse" />
                ) : meUser?.profilePicture ? (
                  <img
                    src={toImageUrl(meUser.profilePicture)}
                    alt={meUser.fullName || "Profile"}
                    className="w-10 h-10 rounded-full border border-slate-300 dark:border-slate-700 object-cover"
                  />
                ) : (
                  <div className="w-10 h-10 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white flex items-center justify-center font-semibold">
                    {(meUser?.fullName?.[0] || "U").toUpperCase()}
                  </div>
                )}
              </button>
            </div>
          </div>
        </GlassBar>
  
        {/* Main area */}
        <div className="relative max-w-7xl mx-auto w-full">
          {/* Feed */}
          <div className="mx-auto px-4 max-w-2xl sm:px-0 py-6 space-y-4 lg:mr-[22rem]">
            {/* Status label */}
            {initialLoaded && (
              <div className="text-xs text-slate-500 dark:text-slate-400">
                {searching ? `Showing results for “${liveQ}”` : "Showing latest posts"}
              </div>
            )}
  
            {/* List */}
            {items.length === 0 && initialLoaded ? (
              <div className="text-center text-slate-500 py-10">
                {busy ? "Searching…" : "No posts found"}
              </div>
            ) : (
              items.map((it) => {
                const accepted =
                  acceptedLocal.has(keyOf(it)) ||
                  it.status === "Scheduled" ||
                  it.status === "Closed" ||
                  it.status === "COMPLETED";
  
                return (
                  <PostCard
                    key={`${it.postType}-${it.postId}`}
                    item={{ ...it, accepted }}
                    onLike={like}
                    onDislike={dislike}
                    onRemoveReaction={removeReaction}
                    onAccept={() => onAccept(it)}
                  />
                );
              })
            )}
  
            {/* Load more */}
            {!done && items.length > 0 && (
              <div className="flex justify-center py-4 pb-20">
                <MacPrimary disabled={busy} onClick={onLoadMore}>
                  {busy ? (searching ? "Searching…" : "Loading…") : "Load more"}
                </MacPrimary>
              </div>
            )}
          </div>
  
          {/* Friends Drawer (fixed right) */}
          <FriendsDrawer className="hidden lg:block fixed right-4 top-24 w-80 h-[calc(100vh-6rem)] z-30 bg-transparent" />
        </div>
  
        {/* Dock */}
        <Dock>
          <MacButton onClick={() => navigate("/home")}>Home</MacButton>
          <MacButton onClick={() => navigate("/request")}>+ Request</MacButton>
          <MacButton onClick={() => navigate("/skill")}>Skills</MacButton>
          <MacButton onClick={() => navigate("/VideoSession")}>Session</MacButton>
          <MacButton onClick={() => navigate("/profile")}>Profile</MacButton>
        </Dock>
      </div>
    );
  };  
  
  export default HomeFeed;

  