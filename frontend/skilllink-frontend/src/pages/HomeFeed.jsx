import React, { useEffect, useState, useMemo, useCallback, useRef } from "react";
import { useAuth } from "../context/AuthContext";
import { generatePath, useNavigate } from "react-router-dom";
import Dock from "../components/Dock";
import { MacButton, MacPrimary, GlassBar, GlassCard, Chip } from "../components/UI";
import FriendsDrawer from "../components/friends/FriendsDrawer";
import { authApi, feedApi, requestsApi, tutorPostsApi } from "../api";
import { toImageUrl } from "../utils/image";

const statusStyles = {
  PENDING: "bg-yellow-200/70 text-yellow-900 dark:bg-yellow-400/20 dark:text-yellow-200",
  SCHEDULED: "bg-blue-200/70 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  COMPLETED: "bg-emerald-200/70 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
  CANCELLED: "bg-red-200/70 text-red-900 dark:bg-red-400/20 dark:text-red-200",
  Open: "bg-blue-200/70 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  Closed: "bg-slate-200/70 text-slate-900 dark:bg-slate-400/20 dark:text-slate-200",
  Scheduled: "bg-amber-200/70 text-amber-900 dark:bg-amber-400/20 dark:text-amber-200",
};

const Avatar = ({ name, onClick, imageUrl, size = 9 }) => {
  const classes = `w-${size} h-${size} rounded-full flex items-center justify-center overflow-hidden`;
  if (imageUrl) {
    return (
      <img
        src={toImageUrl(imageUrl)}
        alt={name || "User"}
        className={`${classes} border border-slate-200 dark:border-slate-700 object-cover cursor-pointer`}
        onClick={onClick}
      />
    );
  }
  return (
    <div className={`${classes} bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white font-semibold`}>
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
  const [preview, setPreview] = useState(null);
  const [loadingPreview, setLoadingPreview] = useState(false);

  const iLiked = item.myReaction === "LIKE";
  const iDisliked = item.myReaction === "DISLIKE";
  const isOwner = user?.userId === item.authorId;
  const navigate = useNavigate();

  const openProfile = useCallback((id) => {
    navigate(generatePath("/u/:id", { id }));
  }, [navigate]);

  const norm = (s) => (s || "").toUpperCase();
  const lessonAcceptable = item.postType === "LESSON" && norm(item.status) === "OPEN" && !isOwner;
  const requestAcceptable = item.postType === "REQUEST" && !["COMPLETED", "CANCELLED", "CLOSED"].includes(norm(item.status)) && !isOwner;
  const canAccept = lessonAcceptable || requestAcceptable;

  const loadComments = useCallback(async () => {
    setLoadingComments(true);
    try {
      const res = await feedApi.listComments(item.postType, item.postId, { limit: 20, sort: "asc" });
      setComments(res.data || []);
    } finally {
      setLoadingComments(false);
    }
  }, [item.postType, item.postId]);

  const loadPreview = useCallback(async () => {
    if (!item?.commentCount || item.commentCount <= 0) {
      setPreview(null);
      return;
    }
    setLoadingPreview(true);
    try {
      const res = await feedApi.listComments(item.postType, item.postId, { limit: 1, sort: "desc" });
      setPreview(res.data?.[0] || null);
    } catch {
      setPreview(null);
    } finally {
      setLoadingPreview(false);
    }
  }, [item.commentCount, item.postType, item.postId]);

  const toggleComments = useCallback(async () => {
    if (!openComments) {
      await loadComments();
    }
    setOpenComments((v) => !v);
  }, [openComments, loadComments]);

  const addComment = async (e) => {
    e.preventDefault();
    const content = cmt.trim();
    if (!content) return;
    try {
      await feedApi.addComment(item.postType, item.postId, content);
      setCmt("");
      await loadComments();
      await loadPreview();
    } catch (err) {
      alert(err?.response?.data?.message || "Failed to add comment");
    }
  };

  const removeComment = async (commentId) => {
    try {
      await feedApi.deleteComment(commentId);
      await loadComments();
      await loadPreview();
    } catch (err) {
      alert(err?.response?.data?.message || "Failed to delete comment");
    }
  };

  useEffect(() => {
    if (item?.commentCount > 0 && !openComments) {
      loadPreview();
    } else {
      setPreview(null);
    }
  }, [item?.commentCount, openComments, loadPreview]);

  return (
    <GlassCard className="p-4">
      {/* Header */}
      <div className="flex items-start gap-3">
        <Avatar name={item.authorName} imageUrl={item.authorPic} onClick={() => openProfile(item.authorId)} />
        <div className="flex-1">
          <div className="flex items-center justify-between">
            <div className="font-semibold text-slate-900 dark:text-slate-100 cursor-pointer" onClick={() => openProfile(item.authorId)}>
              <span className=" hover:text-blue-200">{item.authorName}</span>
              <span className="ml-2 text-xs text-slate-500 dark:text-slate-400">@{item.postType.toLowerCase()}</span>
            </div>
            {item.status ? (
              <Chip className={statusStyles[item.status] || statusStyles[(item.status || "").toUpperCase()] || ""}>{item.status}</Chip>
            ) : null}
          </div>
          <div className="text-xs text-slate-500 dark:text-slate-400">
            {new Date(item.createdAt).toLocaleString()}
            {item.scheduledAt && (
              <span className="ml-2 text-blue-600 dark:text-blue-400">· Scheduled: {new Date(item.scheduledAt).toLocaleString('en-LK')}</span>
            )}
          </div>
        </div>
      </div>

      {/* Body */}
      <div className="mt-3">
        <div className="text-lg font-semibold text-slate-900 dark:text-slate-100">{item.title}</div>
        {item.subtitle && <div className="text-sm text-slate-600 dark:text-slate-300 mt-0.5">{item.subtitle}</div>}
        {item.body && <div className="text-slate-800 dark:text-slate-200 mt-2 whitespace-pre-wrap">{item.body}</div>}
        {item.imageUrl && (
          <div className="mt-3">
            <img src={toImageUrl(item.imageUrl)} alt="Post" className="w-full rounded-xl border border-slate-200 dark:border-slate-700" />
          </div>
        )}
      </div>

      {/* Compact preview */}
      {item.commentCount > 0 && !openComments && (
        <button type="button" onClick={toggleComments} className="mt-3 w-full text-left" title="Show all comments">
          <div className="flex items-start gap-2 px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/60 dark:bg-slate-800/40 hover:bg-slate-100/60 dark:hover:bg-slate-800/60 transition">
            <Avatar name={preview?.fullName || "U"} imageUrl={preview?.profilePicture || preview?.userPic} />
            <div className="min-w-0">
              <div className="text-sm font-medium text-slate-800 dark:text-slate-200 truncate max-w-[220px] sm:max-w-[280px] md:max-w-[360px]">
                {preview?.fullName || "View comments"}
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-300 truncate max-w-[220px] sm:max-w-[280px] md:max-w-[360px]">
                {loadingPreview ? "Loading comment…" : preview?.content || "See what others said…"}
              </div>
            </div>
            <span className="ml-auto text-xs text-slate-500 dark:text-slate-400">{item.commentCount}</span>
          </div>
        </button>
      )}

      {/* Footer actions */}
      <div className="mt-4 flex items-center justify-between text-sm">
        <div className="flex items-center gap-4">
          <button onClick={() => (iLiked ? onRemoveReaction(item) : onLike(item))} className={`inline-flex items-center gap-1 px-2 py-1 rounded hover:bg-black/5 dark:hover:bg-white/10 ${iLiked ? "text-blue-600 dark:text-blue-400" : "text-slate-600 dark:text-slate-300"}`}>
            👍 <span>{item.likes}</span>
          </button>
          <button onClick={() => (iDisliked ? onRemoveReaction(item) : onDislike(item))} className={`inline-flex items-center gap-1 px-2 py-1 rounded hover:bg-black/5 dark:hover:bg-white/10 ${iDisliked ? "text-red-600 dark:text-red-400" : "text-slate-600 dark:text-slate-300"}`}>
            👎 <span>{item.dislikes}</span>
          </button>
          <button onClick={toggleComments} className="inline-flex items-center gap-1 px-2 py-1 rounded hover:bg-black/5 dark:hover:bg-white/10 text-slate-600 dark:text-slate-300">
            💬 <span>{openComments ? "Hide" : "Comments"} ({item.commentCount})</span>
          </button>
        </div>

        {canAccept && (
          <MacPrimary onClick={() => !item.accepted && onAccept(item)} disabled={item.accepted}>
            {item.accepted ? "Accepted" : "Accept"}
          </MacPrimary>
        )}
      </div>

      {/* Comments */}
      {openComments && (
        <div className="mt-3 border-t border-slate-200 dark:border-slate-700 pt-3 space-y-3">
          {loadingComments ? (
            <div className="text-slate-500">Loading comments...</div>
          ) : comments.length === 0 ? (
            <div className="text-slate-500">Be the first to comment</div>
          ) : (
            comments.map((c) => {
              const canDelete = user?.userId === c.userId || user?.role === "ADMIN" || item.authorId === user?.userId;
              return (
                <div key={c.commentId} className="flex items-start gap-2">
                  <Avatar name={c.fullName} imageUrl={c.profilePicture || c.userPic} onClick={() => openProfile(c.userId)} />
                  <div className="flex-1">
                    <div className="text-sm font-medium text-slate-800 dark:text-slate-200 cursor-pointer" onClick={() => openProfile(c.userId)}>
                      <span className="hover:text-blue-200"> {c.fullName} </span>
                    </div>
                    <div className="text-sm text-slate-700 dark:text-slate-200">{c.content}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">
                      {new Date(c.createdAt).toLocaleString()}
                      {canDelete && (
                        <button onClick={() => removeComment(c.commentId)} className="ml-3 text-xs text-red-500 hover:underline">delete</button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          )}
          <form onSubmit={addComment} className="flex items-center gap-2">
            <input value={cmt} onChange={(e) => setCmt(e.target.value)} placeholder="Write a comment…" className="flex-1 px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-800 dark:text-slate-200" />
            <MacPrimary type="submit">Post</MacPrimary>
          </form>
        </div>
      )}
    </GlassCard>
  );
};

// --- FIX: Moved constants and helpers outside the component ---
const debounce = (fn, ms = 450) => {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), ms);
  };
};

const acceptedStatusSet = new Set(["SCHEDULED", "CLOSED", "COMPLETED", "ACCEPTED"]);
const PAGE_SIZE = 8;
const norm = (s) => (s || "").toUpperCase();
const keyOf = (it) => `${it.postType}-${it.postId}`;

/* ---- Page ---- */
const HomeFeed = () => {
  const { user: authUser } = useAuth();
  const navigate = useNavigate();

  const [meUser, setMeUser] = useState(null);
  const [loadingMe, setLoadingMe] = useState(true);
  const [feedTab, setFeedTab] = useState("POSTS");
  const isLoading = useRef(false);

  // State for posts
  const [posts, setPosts] = useState([]);
  const [postsPage, setPostsPage] = useState(1);
  const [postsDone, setPostsDone] = useState(false);
  const [postsInitialLoaded, setPostsInitialLoaded] = useState(false);

  // State for requests
  const [requests, setRequests] = useState([]);
  const [requestsPage, setRequestsPage] = useState(1);
  const [requestsDone, setRequestsDone] = useState(false);
  const [requestsInitialLoaded, setRequestsInitialLoaded] = useState(false);

  const [busy, setBusy] = useState(false);
  const [query, setQuery] = useState("");
  const [liveQ, setLiveQ] = useState("");

  const debouncedSetLiveQ = useMemo(() => debounce((q) => setLiveQ(q.trim()), 450), []);
  useEffect(() => { debouncedSetLiveQ(query); }, [query, debouncedSetLiveQ]);

  useEffect(() => {
    let ignore = false;
    const ensureUser = async () => {
      try {
        const res = await authApi.getProfile();
        if (!ignore) setMeUser(res.data || null);
      } catch {
        if (!ignore) setMeUser(null);
      } finally {
        if (!ignore) setLoadingMe(false);
      }
    };
    ensureUser();
    return () => { ignore = true; };
  }, [authUser?.userId]);

  // Function to load posts (LESSON)
  const loadPosts = useCallback(async (p = 1, qStr = "") => {
    // 1. Check the REF, not the state, to prevent the dependency cycle
    if (isLoading.current || (p > 1 && postsDone)) return;

    // 2. Set both the ref and the state
    isLoading.current = true;
    setBusy(true);

    try {
      const res = await feedApi.list({ page: p, pageSize: PAGE_SIZE, q: qStr, postType: "LESSON", statusNot: "COMPLETED" });
      const raw = res.data || [];
      const filtered = raw.filter((it) => it.postType === "LESSON" && norm(it.status) !== "COMPLETED");

      const serverAccepted = new Map(filtered.map((it) => [keyOf(it), acceptedStatusSet.has(norm(it.status))]));
      const lessonIds = filtered.map((it) => it.postId);
      let acceptedMap = new Map();
      if (lessonIds.length) {
        const { data } = await tutorPostsApi.acceptedStatusMany(lessonIds);
        acceptedMap = new Map(Object.entries(data || {}).map(([k, v]) => [Number(k), !!v]));
      }

      const merged = filtered.map((it) => ({
        ...it,
        accepted: Boolean(serverAccepted.get(keyOf(it)) || acceptedMap.get(it.postId)),
      }));

      if (raw.length < PAGE_SIZE) setPostsDone(true);
      setPosts((prev) => (p === 1 ? merged : [...prev, ...merged]));
      setPostsPage(p);
    } finally {
      // 3. Set both back to false
      isLoading.current = false;
      setBusy(false);
      setPostsInitialLoaded(true);
    }
  }, [postsDone]); // <-- The dependency array is now correct and stable!

  const loadRequests = useCallback(async (p = 1, qStr = "") => {
    // 1. Check the REF, not the state
    if (isLoading.current || (p > 1 && requestsDone)) return;

    // 2. Set both the ref and the state
    isLoading.current = true;
    setBusy(true);
    
    try {
      const res = await feedApi.list({ page: p, pageSize: PAGE_SIZE, q: qStr, postType: "REQUEST", statusNot: "COMPLETED", isPrivate: false });
      const raw = res.data || [];
      const filtered = raw.filter((it) => {
        const isCorrectType = it.postType === "REQUEST";
        const isNotCompleted = norm(it.status) !== "COMPLETED";
        const isPublic = it?.isPrivate !== true && it?.preferredTutorId == null;
        return isCorrectType && isNotCompleted && isPublic;
      });

      let finalData = filtered;
      if (filtered.length > 0) {
        try {
          const sortResponse = await feedApi.sort(filtered);
          console.log("sort : ",sortResponse); 
          finalData = sortResponse.data || filtered; 
        } catch (error) {
          console.error("Could not sort requests, displaying unsorted:", error);
          finalData = filtered;
        }
      }

      const merged = finalData.map((it) => ({
        ...it,
        accepted: acceptedStatusSet.has(norm(it.status)),
      }));
      
      if (raw.length < PAGE_SIZE) setRequestsDone(true);
      setRequests((prev) => (p === 1 ? merged : [...prev, ...merged]));
      setRequestsPage(p);

    } finally {
      // 3. Set both back to false
      isLoading.current = false;
      setBusy(false);
      setRequestsInitialLoaded(true);
    }
  }, [requestsDone]); // <-- The dependency array is now correct and stable!
  
  // Initial load and tab switching
  useEffect(() => {
    if (feedTab === "POSTS" && !postsInitialLoaded) {
      loadPosts(1, liveQ);
    } else if (feedTab === "REQUESTS" && !requestsInitialLoaded) {
      loadRequests(1, liveQ);
    }
  }, [feedTab, postsInitialLoaded, requestsInitialLoaded, liveQ, loadPosts, loadRequests]);
  
  // Search refresh
  useEffect(() => {
    setPostsDone(false);
    setRequestsDone(false);
    setPostsInitialLoaded(false);
    setRequestsInitialLoaded(false);

    if (feedTab === "POSTS") {
      loadPosts(1, liveQ);
    } else {
      loadRequests(1, liveQ);
    }
  }, [liveQ, feedTab, loadPosts, loadRequests]);

  const refreshCurrentTab = () => {
    if (feedTab === "POSTS") {
      setPostsDone(false);
      loadPosts(1, liveQ);
    } else {
      setRequestsDone(false);
      loadRequests(1, liveQ);
    }
  };

  const like = async (it) => { try { await feedApi.like(it.postType, it.postId); refreshCurrentTab(); } catch (e) { alert(e?.response?.data?.message || "Failed to like"); } };
  const dislike = async (it) => { try { await feedApi.dislike(it.postType, it.postId); refreshCurrentTab(); } catch (e) { alert(e?.response?.data?.message || "Failed to dislike"); } };
  const removeReaction = async (it) => { try { await feedApi.removeReaction(it.postType, it.postId); refreshCurrentTab(); } catch (e) { alert(e?.response?.data?.message || "Failed to remove reaction"); } };

  const onAccept = async (it) => {
    try {
      if (it.postType === "LESSON") await tutorPostsApi.accept(it.postId);
      else if (it.postType === "REQUEST") await requestsApi.accept(it.postId);
      refreshCurrentTab();
    } catch (e) {
      alert(e?.response?.data?.message || "Failed to accept");
    }
  };
  
  const isPostsTab = feedTab === "POSTS";
  const currentItems = isPostsTab ? posts : requests;
  const currentDone = isPostsTab ? postsDone : requestsDone;
  const currentInitialLoaded = isPostsTab ? postsInitialLoaded : requestsInitialLoaded;
  const searching = liveQ.length > 0;
  
  const onLoadMore = () => {
    if (busy || currentDone) return;
    if (isPostsTab) {
      loadPosts(postsPage + 1, liveQ);
    } else {
      loadRequests(requestsPage + 1, liveQ);
    }
  };

  return (
    <div className="relative min-h-screen font-sans">
      <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-slate-900 dark:via-slate-900 dark:to-slate-800" />
      <GlassBar className="sticky border-x-0 border-t-0 px-6 p-4 top-0 z-40">
        <div className=" mx-auto flex items-center gap-4">
          <div className="flex items-center gap-3 shrink-0">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow" />
            <div className="font-semibold text-slate-700 dark:text-slate-200">SkillLink</div>
          </div>
          <div className="relative flex flex-1 items-center justify-start">
            <input type="text" value={query} onChange={(e) => setQuery(e.target.value)} placeholder={`Search ${isPostsTab ? "Posts" : "Requests"}`} className="w-full max-w-xs pl-10 pr-8 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none" />
            <span className="absolute left-3 top-2.5 text-slate-400"><i className="fas fa-search" /></span>
            {query && (<button onClick={() => setQuery("")} className="absolute right-2 top-2 inline-flex items-center justify-center w-6 h-6 rounded hover:bg-black/5 dark:hover:bg-white/10 text-slate-500" title="Clear" aria-label="Clear search">✕</button>)}
          </div>
          <div>
            <button onClick={() => navigate("/notifications")} className="focus:outline-none" title="Notifications" aria-label="Notifications">
              <div className="w-10 h-10 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white flex items-center justify-center font-semibold"><i className="fas fa-bell"></i></div>
            </button>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <button onClick={() => navigate("/profile")} className="focus:outline-none" title="My Profile" aria-label="My Profile">
              {loadingMe ? (
                <div className="w-10 h-10 rounded-full bg-slate-200 dark:bg-slate-700 animate-pulse" />
              ) : meUser?.profilePicture ? (
                <img src={toImageUrl(meUser.profilePicture)} alt={meUser.fullName || "Profile"} className="w-10 h-10 rounded-full border border-slate-300 dark:border-slate-700 object-cover" />
              ) : (
                <div className="w-10 h-10 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white flex items-center justify-center font-semibold">{(meUser?.fullName?.[0] || "U").toUpperCase()}</div>
              )}
            </button>
          </div>
        </div>
      </GlassBar>

      <div className="relative max-w-7xl mx-auto w-full">
        <div className="mx-auto px-4 max-w-2xl sm:px-0 py-6 space-y-4 lg:mr-[22rem]">
          <div className="inline-flex bg-white/60 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-xl p-1 sticky top-[4.25rem] z-30">
            {["POSTS", "REQUESTS"].map((t) => (
              <button key={t} onClick={() => setFeedTab(t)} className={`px-4 py-1.5 text-sm font-medium rounded-lg transition ${feedTab === t ? "bg-blue-600 text-white shadow" : "text-slate-700 dark:text-slate-300 hover:bg-white/70 dark:hover:bg-slate-700/60"}`}>
                {t === "POSTS" ? "Posts" : "Requests"}
              </button>
            ))}
          </div>

          {currentInitialLoaded && (
            <div className="text-xs text-slate-500 dark:text-slate-400">
              {searching ? `Showing ${isPostsTab ? "posts" : "requests"} for “${liveQ}”` : `Showing latest ${isPostsTab ? "posts" : "requests"}`}
            </div>
          )}

          {currentItems.length === 0 && currentInitialLoaded ? (
            <div className="text-center text-slate-500 py-10">{busy ? (searching ? "Searching…" : "Loading…") : `No ${isPostsTab ? "posts" : "requests"} found`}</div>
          ) : (
            currentItems.map((it) => (
              <PostCard key={`${it.postType}-${it.postId}`} item={it} onLike={like} onDislike={dislike} onRemoveReaction={removeReaction} onAccept={onAccept} />
            ))
          )}

          {!currentDone && currentItems.length > 0 && (
            <div className="flex justify-center py-4 pb-20">
              <MacPrimary disabled={busy} onClick={onLoadMore}>{busy ? "Loading…" : "Load more"}</MacPrimary>
            </div>
          )}
        </div>
        <FriendsDrawer className="hidden lg:block fixed right-4 top-24 w-80 h-[calc(100vh-6rem)] z-30 bg-transparent" />
      </div>

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
