import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import Dock from "../components/Dock";
import { requestsApi, tutorPostsApi, ratingsApi } from "../api";
import { toImageUrl } from "../utils/image";
import StarRating from "../components/StarRating";
import SettingsMenu from "../components/SettingsMenu";

const SKIP_RATING_KEY = "ratings:skipped:v1";
const loadSkipSet = () => {
  try {
    const raw = localStorage.getItem(SKIP_RATING_KEY);
    const arr = raw ? JSON.parse(raw) : [];
    return new Set(arr);
  } catch {
    return new Set();
  }
};
const addSkip = (acceptedRequestId) => {
  const s = loadSkipSet();
  s.add(String(acceptedRequestId));
  localStorage.setItem(SKIP_RATING_KEY, JSON.stringify([...s]));
  return s;
};
const hasSkipped = (acceptedRequestId) => loadSkipSet().has(String(acceptedRequestId));

/* ========================== UI Atoms ========================== */
const GlassCard = ({ className = "", children }) => (
  <div
    className={
      "relative rounded-2xl border shadow backdrop-blur-xl transition-all duration-300 " +
      "border-slate-200/60 dark:border-slate-700/60 " +
      "bg-white/70 dark:bg-slate-900/60 " +
      className
    }
  >
    {children}
  </div>
);

const chipClassFor = (s) =>
  ({
    Open: "bg-emerald-200/70 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
    Closed: "bg-slate-200/70 text-slate-900 dark:bg-slate-700/30 dark:text-slate-200",
    Scheduled: "bg-blue-200/70 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
    Completed: "bg-emerald-200/70 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
  }[s] || "");

const GlassBar = ({ className = "", children }) => (
  <div
    className={
      "relative border shadow backdrop-blur-xl transition-all duration-300 " +
      "border-slate-200/60 dark:border-slate-700/60 " +
      "bg-white/70 dark:bg-slate-900/60 " +
      className
    }
  >
    {children}
  </div>
);

const MacButton = ({ className = "", children, ...props }) => (
  <button
    className={
      "px-4 py-2 rounded-xl border text-sm transition " +
      "border-black/10 dark:border-white/10 " +
      "bg-white/50 hover:bg-black/5 dark:hover:bg-white/10 active:bg-white/80 " +
      "dark:bg-ink-800/60 dark:hover:bg-ink-800/80 " +
      "text-black/80 dark:text-white/65 focus:outline-none focus:ring-1 focus:ring-blue-400/30 " +
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

const Chip = ({ children, className = "" }) => (
  <span
    className={
      "px-2.5 py-1 text-xs font-medium rounded-full border " +
      "border-slate-200/60 dark:border-slate-700/60 " +
      " text-black/80 dark:text-white/80" +
      className
    }
  >
    {children}
  </span>
);

// Prefer COMPLETED, then SCHEDULED, else newest by acceptedAt
const pickBestAccepted = (curr, cand) => {
  if (!curr) return cand;
  const rank = (s) => {
    switch ((s || "").toUpperCase()) {
      case "COMPLETED":
        return 3;
      case "SCHEDULED":
        return 2;
      case "ACCEPTED":
        return 1;
      default:
        return 0;
    }
  };
  const r1 = rank(curr.status);
  const r2 = rank(cand.status);
  if (r1 !== r2) return (r2 > r1 ? cand : curr);

  const a1 = curr.acceptedAt ? new Date(curr.acceptedAt).getTime() : 0;
  const a2 = cand.acceptedAt ? new Date(cand.acceptedAt).getTime() : 0;
  return a2 >= a1 ? cand : curr;
};

/* ========================== Utils ========================== */
const statusStyles = {
  PENDING:
    "bg-yellow-200/70 text-yellow-900 dark:bg-yellow-400/20 dark:text-yellow-200",
  SCHEDULED:
    "bg-blue-200/70 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  COMPLETED:
    "bg-emerald-200/70 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
  CANCELLED:
    "bg-red-200/70 text-red-900 dark:bg-red-400/20 dark:text-red-200",
};

const debounce = (fn, delay = 300) => {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), delay);
  };
};

const formatDateTimeLocal = (d) => {
  const dt = new Date(d);
  const pad = (n) => (n < 10 ? `0${n}` : n);
  return `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(
    dt.getDate()
  )}T${pad(dt.getHours())}:${pad(dt.getMinutes())}`;
};

/* ========================== Placeholders ========================== */
const EmptyIllustration = () => (
  <svg className="mx-auto h-28 w-28 text-blue-200" viewBox="0 0 200 200" fill="none">
    <circle cx="100" cy="100" r="96" stroke="currentColor" strokeWidth="8" />
    <path d="M65 120c20 16 50 16 70 0" stroke="currentColor" strokeWidth="8" strokeLinecap="round" />
    <circle cx="75" cy="80" r="6" fill="currentColor" />
    <circle cx="125" cy="80" r="6" fill="currentColor" />
  </svg>
);

const SkeletonCard = () => (
  <GlassCard className="p-6 animate-pulse">
    <div className="h-5 w-48 bg-slate-200 dark:bg-slate-700 rounded mb-2" />
    <div className="h-4 w-64 bg-slate-200 dark:bg-slate-700 rounded" />
    <div className="h-4 w-full bg-slate-200 dark:bg-slate-700 rounded mt-3" />
    <div className="flex justify-between items-center mt-6">
      <div className="flex items-center gap-2">
        <div className="w-8 h-8 bg-slate-200 dark:bg-slate-700 rounded-full" />
        <div className="h-4 w-44 bg-slate-200 dark:bg-slate-700 rounded" />
      </div>
      <div className="h-9 w-28 bg-slate-200 dark:bg-slate-700 rounded-xl" />
    </div>
  </GlassCard>
);

/* ========================== Requests Pane ========================== */
const RequestsPane = () => {
  const { user } = useAuth();
  const [searchParams] = useSearchParams();
  const focusId = searchParams.get("focus");

  const [allRequests, setAllRequests] = useState([]);
  const [acceptedRequests, setAcceptedRequests] = useState([]);

  const [tab, setTab] = useState("TO_ME"); // TO_ME | MINE | ACCEPTED
  const [sortBy, setSortBy] = useState("NEWEST"); // NEWEST | OLDEST
  const [searchQuery, setSearchQuery] = useState("");
  const [liveQuery, setLiveQuery] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState("");

  // Create / Edit modals
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingRequest, setEditingRequest] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    skillName: "",
    topic: "",
    description: "",
    learnerId: null,
  });

  // Schedule modal
  const [showSched, setShowSched] = useState(false);
  const [schedBusy, setSchedBusy] = useState(false);
  const [schedTarget, setSchedTarget] = useState(null); // accepted record
  const [schedForm, setSchedForm] = useState({
    scheduleDate: "",
    meetingType: "ONLINE",
    meetingLink: "",
  });

  // Rating modal
  const [showRate, setShowRate] = useState(false);
  const [rateTarget, setRateTarget] = useState(null); // accepted record (or tutor info)
  const [stars, setStars] = useState(0);
  const [note, setNote] = useState("");
  const [ratingBusy, setRatingBusy] = useState(false);
  const [alreadyRatedCache, setAlreadyRatedCache] = useState(new Set());
  const [skipSet, setSkipSet] = useState(loadSkipSet());

  /* ---------- tutorId derivation (fix for 400 on ratings) ---------- */
  const getTutorIdFromAccepted = useCallback(
    (ar) => {
      if (!ar) return null;

      // Prefer explicit tutor/user fields on the accepted record if present
      const direct =
        ar.tutorId ??
        ar.tutorUserId ??
        ar.acceptorId ?? // acceptor is tutor
        ar.acceptedByTutorId ??
        (ar.tutor && (ar.tutor.userId ?? ar.tutor.id)) ??
        null;
      if (direct) return direct;

      // Fallback to original request preferredTutorId
      const req = allRequests.find((r) => r.requestId === ar.requestId);
      if (req?.preferredTutorId) return req.preferredTutorId;

      return null;
    },
    [allRequests]
  );

  const openRate = async (acceptedRecord) => {
    // honor local skip
    if (hasSkipped(acceptedRecord.acceptedRequestId)) {
      setMessage("You chose to skip rating this session.");
      return;
    }

    if (!alreadyRatedCache.has(acceptedRecord.acceptedRequestId)) {
      try {
        const { data } = await ratingsApi.existsForAccepted(acceptedRecord.acceptedRequestId);
        if (data?.exists) {
          setAlreadyRatedCache((s) => new Set([...s, acceptedRecord.acceptedRequestId]));
          setMessage("You already rated this session. Thanks!");
          return;
        }
      } catch {}
    }

    const best =
      acceptedRequests
        .filter((ar) => ar.requestId === acceptedRecord.requestId)
        .reduce(pickBestAccepted, null) || acceptedRecord;

    if ((best.status || "").toUpperCase() !== "COMPLETED") {
      setMessage("You can only rate a completed session.");
      return;
    }

    const tid = getTutorIdFromAccepted(best);
    setRateTarget({ ...best, tutorId: tid });
    setStars(0);
    setNote("");
    setShowRate(true);
  };

  const skipRatingForThisSession = () => {
    if (!rateTarget) return;
    const next = addSkip(rateTarget.acceptedRequestId);
    setSkipSet(next);
    setShowRate(false);
    setRateTarget(null);
    setMessage("No problem — we won't ask you to rate this session again.");
  };

  const submitRating = async (e) => {
    e.preventDefault();
    if (!rateTarget || stars === 0) return;
    try {
      setRatingBusy(true);

      const tutorId = rateTarget.tutorId ?? getTutorIdFromAccepted(rateTarget);
      if (!tutorId) {
        setMessage("Couldn't determine the tutor for this session. Try refreshing the page.");
        return;
      }

      await ratingsApi.create({
        tutorId,
        acceptedRequestId: rateTarget.acceptedRequestId,
        rating: stars,
        comment: note?.trim() || "",
      });

      setAlreadyRatedCache((s) => new Set([...s, rateTarget.acceptedRequestId]));
      setShowRate(false);
      setRateTarget(null);
      setMessage("Thanks for rating your tutor!");
    } catch (err) {
      setMessage(
        err?.response?.data?.message || err?.message || "Couldn't submit rating"
      );
    } finally {
      setRatingBusy(false);
    }
  };

  // --- Zoom helpers (no API integration needed) ---
  const ZOOM_CREATE_URL = "https://zoom.us/start/videomeeting";
  const isZoomUrl = (u) =>
    typeof u === "string" &&
    /^(zoommtg:\/\/|https?:\/\/[\w.-]*zoom\.us\/(j|s)\/)/i.test(u.trim());

  const openZoomCreate = () => {
    window.open(ZOOM_CREATE_URL, "_blank", "noopener,noreferrer");
  };

  const pasteZoomFromClipboard = async () => {
    try {
      if (!navigator.clipboard || !window.isSecureContext) {
        setMessage("Clipboard read not available; paste the link manually.");
        return;
      }
      const text = (await navigator.clipboard.readText())?.trim();
      if (isZoomUrl(text)) {
        setSchedForm((p) => ({ ...p, meetingType: "ONLINE", meetingLink: text }));
        setMessage("Zoom link pasted from clipboard.");
      } else {
        setMessage("Clipboard doesn't look like a Zoom link. Copy it from Zoom and try again.");
      }
    } catch {
      setMessage("Couldn't read clipboard. Paste manually.");
    }
  };

  // Tick state (so Finish can appear automatically once time passes)
  const [, setNowTick] = useState(Date.now());
  useEffect(() => {
    const id = setInterval(() => setNowTick(Date.now()), 30000);
    return () => clearInterval(id);
  }, []);

  // Maps for quick lookups
  const acceptedByRequest = useMemo(() => {
    const m = {};
    for (const ar of acceptedRequests) {
      m[ar.requestId] = pickBestAccepted(m[ar.requestId], ar);
    }
    return m;
  }, [acceptedRequests]);

  const acceptedMap = useMemo(() => {
    const m = {};
    Object.keys(acceptedByRequest).forEach((rid) => (m[rid] = true));
    return m;
  }, [acceptedByRequest]);

  useEffect(() => {
    const run = debounce((q) => setSearchQuery(q), 350);
    run(liveQuery);
  }, [liveQuery]);

  useEffect(() => {
    loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const isDirectedToMe = useCallback(
    (r) => {
      const uid = `${user?.userId ?? ""}`;
      return r?.preferredTutorId != null && `${r.preferredTutorId}` === uid;
    },
    [user?.userId]
  );

  const loadAll = async () => {
    try {
      setIsLoading(true);
      setMessage("");

      const [allRes, accByMeRes, accForMeRes] = await Promise.all([
        requestsApi.list(),
        requestsApi.listAcceptedByMe(),
        requestsApi.listAcceptedForMyRequests(),
      ]);

      setAllRequests(allRes.data || []);
      setAcceptedRequests([...(accByMeRes.data || []), ...(accForMeRes.data || [])]);
    } catch {
      setMessage("Failed to load requests");
    } finally {
      setIsLoading(false);
    }
  };

  const filteredSorted = useMemo(() => {
    let list = [...allRequests];

    if (tab === "TO_ME") list = list.filter(isDirectedToMe);
    else if (tab === "MINE") list = list.filter((r) => r.learnerId === user?.userId);
    else if (tab === "ACCEPTED") list = list.filter((r) => acceptedMap[r.requestId]);

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      list = list.filter(
        (r) =>
          r.skillName?.toLowerCase().includes(q) ||
          r.topic?.toLowerCase().includes(q) ||
          r.description?.toLowerCase().includes(q) ||
          r.fullName?.toLowerCase().includes(q)
      );
    }

    list.sort((a, b) =>
      sortBy === "NEWEST"
        ? new Date(b.createdAt) - new Date(a.createdAt)
        : new Date(a.createdAt) - new Date(b.createdAt)
    );

    return list;
  }, [allRequests, tab, acceptedMap, searchQuery, sortBy, user?.userId, isDirectedToMe]);

  // actions
  const acceptRequest = async (requestId) => {
    try {
      setMessage("");
      await requestsApi.accept(requestId);
      setMessage("Request accepted successfully!");
      await loadAll();
    } catch {
      setMessage("Failed to accept request");
    }
  };

  const declineDirected = async (requestId) => {
    if (!window.confirm("Decline this directed request?")) return;
    try {
      setMessage("");
      await requestsApi.declineDirected(requestId);
      setMessage("Request declined (the requester now sees it as CANCELLED).");
      await loadAll();
    } catch {
      setMessage("Failed to decline request");
    }
  };

  const openSchedule = (acceptedRecord) => {
    setSchedTarget(acceptedRecord);
    const initialDate = acceptedRecord?.scheduleDate
      ? formatDateTimeLocal(acceptedRecord.scheduleDate)
      : formatDateTimeLocal(new Date());
    setSchedForm({
      scheduleDate: initialDate,
      meetingType: acceptedRecord?.meetingType || "ONLINE",
      meetingLink: acceptedRecord?.meetingLink || "",
    });
    setShowSched(true);
  };

  const doSchedule = async (e) => {
    e.preventDefault();
    if (!schedTarget) return;
    try {
      setSchedBusy(true);

      await requestsApi.scheduleAccepted(`${schedTarget.acceptedRequestId}`, {
        scheduleDate: new Date(schedForm.scheduleDate).toISOString(),
        meetingType: schedForm.meetingType,
        meetingLink: schedForm.meetingType === "ONLINE" ? schedForm.meetingLink : "",
      });

      // optimistic update
      setAcceptedRequests((prev) =>
        prev.map((ar) =>
          ar.acceptedRequestId === schedTarget.acceptedRequestId
            ? {
                ...ar,
                status: "SCHEDULED",
                scheduleDate: new Date(schedForm.scheduleDate).toISOString(),
                meetingType: schedForm.meetingType,
                meetingLink: schedForm.meetingType === "ONLINE" ? schedForm.meetingLink : "",
              }
            : ar
        )
      );
      setAllRequests((prev) =>
        prev.map((r) =>
          r.requestId === schedTarget.requestId ? { ...r, status: "SCHEDULED" } : r
        )
      );

      setMessage("Meeting scheduled successfully!");
      setShowSched(false);
      setSchedTarget(null);
      await loadAll();
    } catch {
      setMessage("Failed to schedule meeting");
    } finally {
      setSchedBusy(false);
    }
  };

  // Finish (Requests)
  const finishAccepted = async (acceptedRecord) => {
    try {
      setMessage("");
      await requestsApi.finishAccepted(acceptedRecord.acceptedRequestId);

      setAcceptedRequests((prev) =>
        prev.map((ar) =>
          ar.acceptedRequestId === acceptedRecord.acceptedRequestId
            ? { ...ar, status: "COMPLETED" }
            : ar
        )
      );
      setAllRequests((prev) =>
        prev.map((r) =>
          r.requestId === acceptedRecord.requestId ? { ...r, status: "COMPLETED" } : r
        )
      );

      setMessage("Marked as completed.");
      await loadAll();
    } catch {
      setMessage("Failed to mark as completed");
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((p) => ({ ...p, [name]: value }));
  };

  const createRequest = async (e) => {
    e.preventDefault();
    if (!formData.skillName.trim()) return;
    try {
      setIsSubmitting(true);
      await requestsApi.create({
        skillName: formData.skillName,
        topic: formData.topic,
        description: formData.description,
        learnerId: user.userId,
      });
      setMessage("Request created successfully!");
      setShowCreateModal(false);
      setFormData({ skillName: "", topic: "", description: "" });
      await loadAll();
      setTab("MINE");
    } catch {
      setMessage("Failed to create request");
    } finally {
      setIsSubmitting(false);
    }
  };

  const startEditing = (request) => {
    setEditingRequest(request);
    setFormData({
      skillName: request.skillName,
      topic: request.topic || "",
      description: request.description || "",
    });
  };

  const cancelEditing = () => {
    setEditingRequest(null);
    setFormData({ skillName: "", topic: "", description: "" });
  };

  const updateRequest = async (e) => {
    e.preventDefault();
    try {
      setIsSubmitting(true);
      await requestsApi.update(editingRequest.requestId, { ...formData });
      setMessage("Request updated successfully!");
      cancelEditing();
      await loadAll();
    } catch {
      setMessage("Failed to update request");
    } finally {
      setIsSubmitting(false);
    }
  };

  const deleteRequest = async (requestId) => {
    if (!window.confirm("Cancel this request for both sides?")) return;
    try {
      setMessage("");
      if (typeof requestsApi.cancel === "function") {
        await requestsApi.cancel(requestId);
      } else if (typeof requestsApi.updateStatus === "function") {
        await requestsApi.updateStatus(requestId, "CANCELLED");
      } else {
        await requestsApi.update(requestId, { status: "CANCELLED" });
      }
      setAllRequests((prev) =>
        prev.map((r) => (r.requestId === requestId ? { ...r, status: "CANCELLED" } : r))
      );
      setMessage("Request cancelled.");
      await loadAll();
    } catch {
      setMessage("Failed to cancel request");
    }
  };

  const removeRequestPermanently = async (requestId) => {
    if (!window.confirm("Permanently remove this cancelled request?")) return;
    try {
      setMessage("");
      await requestsApi.remove(requestId);
      setMessage("Request removed.");
      await loadAll();
    } catch {
      setMessage("Failed to remove request");
    }
  };

  return (
    <>
      <div className="max-w-6xl mx-auto px-6 py-6 space-y-6">
        <GlassCard className="p-6">
          {/* Controls */}
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
            {/* Tabs */}
            <div className="inline-flex bg-white/60 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-xl p-1">
              {["TO_ME", "MINE", "ACCEPTED"].map((t) => (
                <button
                  key={t}
                  onClick={() => setTab(t)}
                  className={
                    "px-4 py-1.5 text-sm font-medium rounded-lg transition " +
                    (tab === t
                      ? "bg-blue-600 text-white shadow"
                      : "text-slate-700 dark:text-slate-300 hover:bg-white/70 dark:hover:bg-slate-700/60")
                  }
                >
                  {t === "TO_ME" ? "To me" : t === "MINE" ? "My Requests" : "Accepted / Scheduled"}
                </button>
              ))}
            </div>

            <div className="flex flex-col sm:flex-row gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[220px]">
                <input
                  type="text"
                  value={liveQuery}
                  onChange={(e) => setLiveQuery(e.target.value)}
                  placeholder="Search by skill, topic, description, or user…"
                  className="w-full pl-10 pr-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
                />
                <span className="absolute left-3 top-2.5 text-slate-400">
                  <i className="fas fa-search"></i>
                </span>
              </div>

              {/* Sort */}
              <select
                className="px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
              >
                <option value="NEWEST">Newest first</option>
                <option value="OLDEST">Oldest first</option>
              </select>

              <MacPrimary
                id="open-create-request-id"
                data-testid="open-create-request"
                onClick={() => setShowCreateModal(true)}
              >
                + Create Request
              </MacPrimary>
            </div>
          </div>

          {/* Feedback */}
          {message && (
            <GlassCard
              className={
                "mt-4 p-3 " +
                (message.toLowerCase().includes("success")
                  ? "ring-1 ring-emerald-300/50"
                  : "ring-1 ring-blue-300/50")
              }
            >
              <div className="text-slate-700 dark:text-slate-200">{message}</div>
            </GlassCard>
          )}

          {/* List */}
          <div className="mt-6">
            {isLoading ? (
              <div className="grid gap-4">
                {[...Array(4)].map((_, idx) => (
                  <SkeletonCard key={idx} />
                ))}
              </div>
            ) : filteredSorted.length === 0 ? (
              <GlassCard className="p-10 text-center">
                <EmptyIllustration />
                <p className="mt-4 text-slate-600 dark:text-slate-300">
                  {searchQuery || tab !== "TO_ME"
                    ? "No requests match your filters."
                    : "No requests directed to you yet."}
                </p>
                {tab !== "TO_ME" && (
                  <MacButton onClick={() => setTab("TO_ME")} className="mt-4">
                    Show requests to me
                  </MacButton>
                )}
              </GlassCard>
            ) : (
              <div className="grid md:grid-cols-2 gap-5">
                {filteredSorted.map((request) => {
                  const acceptedRec = acceptedByRequest[request.requestId];
                  const accepted = !!acceptedRec;
                  const isOwner = user.userId === request.learnerId;
                  const directedToMe = isDirectedToMe(request);
                  const highlight = focusId && `${request.requestId}` === `${focusId}`;
                  const isSched = (acceptedRec?.status || "").toUpperCase() === "SCHEDULED";

                  return (
                    <GlassCard
                      key={request.requestId}
                      className={"p-6 " + (highlight ? "ring-2 ring-blue-400" : "")}
                    >
                      <div className="flex justify-between items-start">
                        <div>
                          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                            {request.skillName}
                          </h2>
                          {request.topic && (
                            <p className="text-slate-600 dark:text-slate-300 mt-1">
                              {request.topic}
                            </p>
                          )}
                        </div>
                        <Chip className={statusStyles[request.status] || ""}>
                          {request.status}
                        </Chip>
                      </div>

                      {request.description && (
                        <p className="text-slate-700 dark:text-slate-200 mt-3">
                          {request.description}
                        </p>
                      )}

                      {/* Scheduled summary — BOTH sides */}
                      {isSched && (
                        <div className="mt-3 p-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-blue-50/70 dark:bg-blue-900/20 text-sm">
                          <p className="text-blue-800 dark:text-blue-200 font-medium">Scheduled</p>
                          <p className="text-blue-700 dark:text-blue-300">
                            Date: {new Date(acceptedRec.scheduleDate).toLocaleString()}
                          </p>
                          <p className="text-blue-700 dark:text-blue-300">
                            Type: {acceptedRec.meetingType}
                          </p>
                          {acceptedRec.meetingType === "ONLINE" && acceptedRec.meetingLink && (
                            <p className="text-blue-700 dark:text-blue-300 truncate">
                              Link:{" "}
                              <a
                                href={acceptedRec.meetingLink}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="underline"
                              >
                                {acceptedRec.meetingLink}
                              </a>
                            </p>
                          )}
                        </div>
                      )}

                      <div className="flex justify-between items-center mt-5 text-sm text-slate-500 dark:text-slate-400">
                        <div className="flex items-center">
                          <div className="w-8 h-8 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center mr-2 font-semibold">
                            {request.fullName?.charAt(0).toUpperCase() || "U"}
                          </div>
                          <div>
                            <span className="font-medium text-slate-800 dark:text-slate-200">
                              {request.fullName}
                            </span>
                            <span className="ml-1">
                              • {new Date(request.createdAt).toLocaleDateString()}
                            </span>
                          </div>
                        </div>

                        <div className="flex items-center gap-3">
                          {/* Tutor side actions (not owner) */}
                          {!isOwner && (
                            <>
                              {request.status === "CANCELLED" ? (
                                <MacDanger onClick={() => removeRequestPermanently(request.requestId)}>
                                  Remove
                                </MacDanger>
                              ) : (
                                <>
                                  {directedToMe && !accepted && request.status === "PENDING" && (
                                    <>
                                      <MacPrimary onClick={() => acceptRequest(request.requestId)}>
                                        Accept
                                      </MacPrimary>
                                      <MacDanger onClick={() => declineDirected(request.requestId)}>
                                        Decline
                                      </MacDanger>
                                    </>
                                  )}

                                  {/* Schedule / Finish */}
                                  {accepted &&
                                    request.status !== "CANCELLED" &&
                                    request.status !== "COMPLETED" && (
                                      <>
                                        <MacPrimary onClick={() => openSchedule(acceptedRec)}>
                                          {isSched ? "Edit Schedule" : "Schedule"}
                                        </MacPrimary>

                                        {(() => {
                                          const canFinish =
                                            isSched &&
                                            acceptedRec?.scheduleDate &&
                                            new Date(acceptedRec.scheduleDate).getTime() <=
                                              Date.now();
                                          return canFinish ? (
                                            <MacPrimary
                                              className="bg-green-600 hover:bg-green-700"
                                              onClick={() => finishAccepted(acceptedRec)}
                                            >
                                              Finish
                                            </MacPrimary>
                                          ) : null;
                                        })()}
                                      </>
                                    )}
                                </>
                              )}
                            </>
                          )}

                          {/* Requester actions (owner) */}
                          {isOwner && (
                            <>
                              {request.status === "CANCELLED" ? (
                                <MacDanger onClick={() => removeRequestPermanently(request.requestId)}>
                                  Remove
                                </MacDanger>
                              ) : (
                                <>
                                  {(() => {
                                    const st = (request.status || "").toUpperCase();
                                    const isCompleted = st === "COMPLETED";
                                    const canRate =
                                      (request.status || "").toUpperCase() === "COMPLETED" &&
                                      acceptedRec &&
                                      (acceptedRec.status || "").toUpperCase() === "COMPLETED" &&
                                      !alreadyRatedCache.has(acceptedRec.acceptedRequestId);
                                    if (isCompleted) {
                                      return canRate ? (
                                        <MacPrimary onClick={() => openRate(acceptedRec)}>
                                          Rate Tutor
                                        </MacPrimary>
                                      ) : null;
                                    }

                                    return (
                                      <>
                                        {st === "PENDING" && (
                                          <MacButton onClick={() => startEditing(request)}>
                                            Edit
                                          </MacButton>
                                        )}
                                        {/* Keep Delete even when SCHEDULED; hide only when COMPLETED */}
                                        <MacDanger onClick={() => deleteRequest(request.requestId)}>
                                          Delete
                                        </MacDanger>
                                      </>
                                    );
                                  })()}
                                </>
                              )}
                            </>
                          )}
                        </div>
                      </div>
                    </GlassCard>
                  );
                })}
              </div>
            )}
          </div>
        </GlassCard>
      </div>

      {/* Create Modal */}
      {showCreateModal && (
        <div
          className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50"
          data-testid="create-request-modal"
        >
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
                Create New Request
              </h2>
            </div>
            <form onSubmit={createRequest} className="p-6 space-y-4">
              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Skill Name *</label>
                <input
                  type="text"
                  name="skillName"
                  value={formData.skillName}
                  onChange={handleInputChange}
                  className="mt-1 w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
                  placeholder="e.g., JavaScript, Guitar, UX Research"
                  required
                />
              </div>
              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Topic</label>
                <input
                  type="text"
                  name="topic"
                  value={formData.topic}
                  onChange={handleInputChange}
                  className="mt-1 w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
                  placeholder="Optional topic or context"
                />
              </div>
              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Description</label>
                <textarea
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  rows={3}
                  className="mt-1 w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
                  placeholder="What exactly do you need help with?"
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <MacButton type="button" onClick={() => setShowCreateModal(false)}>
                  Cancel
                </MacButton>
                <MacPrimary
                  type="submit"
                  disabled={isSubmitting}
                  id="create-request-submit-id"
                  data-testid="create-request-submit"
                >
                  {isSubmitting ? "Creating..." : "Create"}
                </MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Edit Modal */}
      {editingRequest && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">Edit Request</h2>
            </div>
            <form onSubmit={updateRequest} className="p-6 space-y-4">
              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Skill Name *</label>
                <input
                  type="text"
                  name="skillName"
                  value={formData.skillName}
                  onChange={handleInputChange}
                  className="mt-1 w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200"
                  required
                />
              </div>
              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Topic</label>
                <input
                  type="text"
                  name="topic"
                  value={formData.topic}
                  onChange={handleInputChange}
                  className="mt-1 w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200"
                />
              </div>
              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Description</label>
                <textarea
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  rows={3}
                  className="mt-1 w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200"
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <MacButton type="button" onClick={cancelEditing}>
                  Cancel
                </MacButton>
                <MacPrimary type="submit" disabled={isSubmitting}>
                  {isSubmitting ? "Updating..." : "Update"}
                </MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Schedule Modal (Requests) */}
      {showSched && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h4 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                {schedTarget?.status === "SCHEDULED" ? "Edit Schedule" : "Schedule Meeting"}
              </h4>
            </div>
            <form onSubmit={doSchedule} className="p-6 space-y-4">
              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">Date & Time *</label>
                <input
                  type="datetime-local"
                  name="scheduleDate"
                  value={schedForm.scheduleDate}
                  onChange={(e) => setSchedForm((p) => ({ ...p, scheduleDate: e.target.value }))}
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-800 dark:text-slate-200"
                  required
                />
              </div>

              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">Meeting Type *</label>
                <select
                  name="meetingType"
                  value={schedForm.meetingType}
                  onChange={(e) => setSchedForm((p) => ({ ...p, meetingType: e.target.value }))}
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-800 dark:text-slate-200"
                >
                  <option value="ONLINE">Online</option>
                  <option value="PHYSICAL">In-Person</option>
                </select>
              </div>

              {schedForm.meetingType === "ONLINE" && (
                <div>
                  <label className="text-sm text-slate-700 dark:text-slate-300">Meeting Link *</label>

                  <div className="flex gap-2 mt-1">
                    <button
                      type="button"
                      onClick={openZoomCreate}
                      className="px-3 py-2 rounded-xl text-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-400/40"
                      title="Open Zoom in a new tab and start an instant meeting"
                    >
                      Generate Zoom link
                    </button>
                    <button
                      type="button"
                      onClick={pasteZoomFromClipboard}
                      className="px-3 py-2 rounded-xl text-sm border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 hover:bg-black/5 dark:hover:bg-white/10 focus:outline-none focus:ring-2 focus:ring-blue-400/30"
                      title="Paste Zoom link from clipboard"
                    >
                      Paste from clipboard
                    </button>
                  </div>

                  <input
                    type="url"
                    name="meetingLink"
                    placeholder="https://zoom.us/j/123456789 (or Google Meet/Teams link)"
                    value={schedForm.meetingLink}
                    onChange={(e) => setSchedForm((p) => ({ ...p, meetingLink: e.target.value }))}
                    required
                    className="mt-2 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-800 dark:text-slate-200"
                  />
                  <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    Click <em>Generate Zoom link</em>, start the meeting in the new tab, copy the URL, then click <em>Paste from clipboard</em>.
                  </p>
                </div>
              )}

              <div className="flex justify-end gap-2 pt-2">
                <MacButton type="button" onClick={() => setShowSched(false)}>
                  Cancel
                </MacButton>
                <MacPrimary type="submit" disabled={schedBusy}>
                  {schedBusy ? "Saving..." : "Save"}
                </MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Rate Tutor Modal */}
      {showRate && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h4 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                Rate your tutor
              </h4>
            </div>
            <form onSubmit={submitRating} className="p-6 space-y-4">
              <div>
                <div className="text-sm text-slate-600 dark:text-slate-300 mb-1">
                  How was your session?
                </div>
                <StarRating value={stars} onChange={setStars} />
              </div>

              <div>
                <label className="text-sm text-slate-700 dark:text-slate-300">Optional note</label>
                <textarea
                  rows={3}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  placeholder="Share quick feedback that helps others"
                  className="mt-1 w-full rounded-xl border px-3 py-2 outline-none focus:ring-2 focus:ring-blue-400/30 border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-800 dark:text-slate-200"
                />
              </div>

              <div className="flex justify-between items-center gap-2 pt-2">
                <button
                  type="button"
                  onClick={skipRatingForThisSession}
                  className="px-3 py-2 rounded-xl text-sm border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 hover:bg-black/5 dark:hover:bg-white/10 focus:outline-none focus:ring-2 focus:ring-blue-400/30"
                  title="Hide this prompt for this session"
                >
                  Skip
                </button>

                <div className="flex gap-2">
                  <MacButton type="button" onClick={() => setShowRate(false)}>
                    Cancel
                  </MacButton>
                  <MacPrimary type="submit" disabled={ratingBusy || stars === 0}>
                    {ratingBusy ? "Submitting..." : "Submit"}
                  </MacPrimary>
                </div>
              </div>
            </form>
          </GlassCard>
        </div>
      )}
    </>
  );
};

/* ========================== Lessons Pane (My Lessons + Applied, with Image Upload) ========================== */
const LessonsPane = () => {
  const { user } = useAuth();
  const [lessons, setLessons] = useState([]);
  const [appliedRaw, setAppliedRaw] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadingApplied, setLoadingApplied] = useState(true);
  const [msg, setMsg] = useState("");

  const [lessonsTab, setLessonsTab] = useState("MINE"); // MINE | APPLIED

  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({
    title: "",
    description: "",
    maxParticipants: 5,
  });
  const [imageFile, setImageFile] = useState(null);
  const [editImageFile, setEditImageFile] = useState(null);

  const [showSchedule, setShowSchedule] = useState(false);
  const [scheduleTarget, setScheduleTarget] = useState(null);
  const [scheduledAt, setScheduledAt] = useState(formatDateTimeLocal(new Date()));
  const [meetingLink, setMeetingLink] = useState("");

  // Tick so "Finish" appears once time passes (no manual refresh needed)
  const [, setNowTick] = useState(Date.now());
  useEffect(() => {
    const id = setInterval(() => setNowTick(Date.now()), 30000);
    return () => clearInterval(id);
  }, []);

  // --- Zoom helpers for Lessons ---
  const ZOOM_CREATE_URL2 = "https://zoom.us/start/videomeeting";
  const isZoomUrl2 = (u) =>
    typeof u === "string" && /^(zoommtg:\/\/|https?:\/\/[\w.-]*zoom\.us\/(j|s)\/)/i.test(u?.trim());
  const openZoomCreate2 = () => window.open(ZOOM_CREATE_URL2, "_blank", "noopener,noreferrer");
  const pasteZoomFromClipboard2 = async () => {
    try {
      if (!navigator.clipboard || !window.isSecureContext) {
        setMsg("Clipboard read not available; paste manually.");
        return;
      }
      const text = (await navigator.clipboard.readText())?.trim();
      if (isZoomUrl2(text)) {
        setMeetingLink(text);
        setMsg("Zoom link pasted from clipboard.");
      } else {
        setMsg("Clipboard doesn't look like a Zoom link.");
      }
    } catch {
      setMsg("Couldn't read clipboard. Paste manually.");
    }
  };

  const MAX_IMG_SIZE = 10 * 1024 * 1024; // 10MB
  const isValidImage = (file) => {
    if (!file) return true;
    const okType = file.type.startsWith("image/");
    const okSize = file.size <= MAX_IMG_SIZE;
    return okType && okSize;
  };

  const loadLessons = async () => {
    try {
      setLoading(true);
      const res = await tutorPostsApi.list();
      setLessons(res.data || []);
    } catch {
      setMsg("Failed to load lessons");
    } finally {
      setLoading(false);
    }
  };

  // Load "Applied" using accepted-status-many map
  const loadApplied = async () => {
    setLoadingApplied(true);
    try {
      const listRes = await tutorPostsApi.list();
      const allPosts = listRes?.data || [];
      const ids = allPosts.map((p) => p.postId);

      let statusMap = {};
      if (ids.length > 0) {
        const mapRes = await tutorPostsApi.acceptedStatusMany(ids);
        statusMap = mapRes?.data || {};
      }

      const applied = allPosts.filter((p) => {
        const k = p.postId;
        return !!(statusMap[k] ?? statusMap[String(k)]);
      });

      setAppliedRaw(applied);
    } catch {
      setMsg("Failed to load applied lessons");
    } finally {
      setLoadingApplied(false);
    }
  };

  useEffect(() => {
    loadLessons();
    loadApplied();
  }, []);

  const myLessons = useMemo(() => lessons.filter((l) => l.tutorId === user.userId), [lessons, user?.userId]);

  const appliedLessons = useMemo(() => {
    const flat = (appliedRaw || []).map((a) => a.post || a);
    return flat;
  }, [appliedRaw]);

  const onChange = (e) => setForm((p) => ({ ...p, [e.target.name]: e.target.value }));

  const uploadLessonImage = async (postId, file) => {
    if (!file) return;
    let lastError = null;
    try {
      await tutorPostsApi.uploadImage(postId, file);
      return;
    } catch (err) {
      lastError = err;
    }
    const serverMsg =
      lastError?.response?.data?.message ||
      (lastError?.response?.data?.title &&
        `${lastError.response.data.title}${
          lastError?.response?.data?.errors
            ? " - " + Object.values(lastError.response.data.errors).flat().join(", ")
            : ""
        }`) ||
      lastError?.message ||
      "Upload failed";
    throw new Error(serverMsg);
  };

  const createLesson = async (e) => {
    e.preventDefault();
    try {
      if (!form.title.trim()) {
        setMsg("Title is required");
        return;
      }
      if (!isValidImage(imageFile)) {
        setMsg("Image must be an image file and ≤ 10MB");
        return;
      }

      const createRes = await tutorPostsApi.create({
        title: form.title.trim(),
        description: form.description?.trim() || "",
        maxParticipants: Number(form.maxParticipants) || 1,
      });

      const newId =
        createRes?.data?.postId ??
        createRes?.data?.id ??
        createRes?.data?.tutorPostId ??
        createRes?.data?.data?.postId ??
        null;

      if (!newId) {
        console.error("Create response doesn't include post id:", createRes?.data);
        setMsg("Server didn't return a post id. Check backend response.");
        return;
      }

      if (imageFile) {
        try {
          await uploadLessonImage(newId, imageFile);
        } catch (upErr) {
          setMsg(`Lesson created, but image upload failed: ${upErr.message || "Unknown error"}`);
          setShowCreate(false);
          setForm({ title: "", description: "", maxParticipants: 5 });
          setImageFile(null);
          await loadLessons();
          return;
        }
      }

      setMsg("Lesson posted successfully!");
      setShowCreate(false);
      setForm({ title: "", description: "", maxParticipants: 5 });
      setImageFile(null);
      loadLessons();
    } catch (err) {
      const emsg =
        err?.response?.data?.message ||
        (err?.response?.data?.title &&
          `${err.response.data.title}${
            err?.response?.data?.errors
              ? " - " + Object.values(err.response.data.errors).flat().join(", ")
              : ""
          }`) ||
        err?.message ||
        "Failed to create lesson";
      setMsg(emsg);
    }
  };

  const openEdit = (post) => {
    setEditing(post);
    setForm({
      title: post.title,
      description: post.description || "",
      maxParticipants: post.maxParticipants,
    });
    setEditImageFile(null);
    setShowEdit(true);
  };

  const updateLesson = async (e) => {
    e.preventDefault();
    try {
      if (!form.title.trim()) {
        setMsg("Title is required");
        return;
      }
      if (!isValidImage(editImageFile)) {
        setMsg("Image must be an image file and ≤ 10MB");
        return;
      }

      await tutorPostsApi.update(editing.postId, {
        title: form.title.trim(),
        description: form.description?.trim() || "",
        maxParticipants: Number(form.maxParticipants) || 1,
      });

      if (editImageFile) {
        try {
          await uploadLessonImage(editing.postId, editImageFile);
        } catch (upErr) {
          setMsg(`Lesson updated, but image upload failed: ${upErr.message || "Unknown error"}`);
          setShowEdit(false);
          setEditing(null);
          setEditImageFile(null);
          await loadLessons();
          return;
        }
      }

      setMsg("Lesson updated successfully!");
      setShowEdit(false);
      setEditing(null);
      setEditImageFile(null);
      loadLessons();
    } catch (err) {
      const emsg =
        err?.response?.data?.message ||
        (err?.response?.data?.title &&
          `${err.response.data.title}${
            err?.response?.data?.errors
              ? " - " + Object.values(err.response.data.errors).flat().join(", ")
              : ""
          }`) ||
        err?.message ||
        "Failed to update";
      setMsg(emsg);
    }
  };

  const deleteLesson = async (postId) => {
    if (!window.confirm("Delete this lesson post?")) return;
    try {
      await tutorPostsApi.remove(postId);
      setMsg("Lesson deleted");
      loadLessons();
      loadApplied();
    } catch {
      setMsg("Failed to delete");
    }
  };

  const openSchedule = (post) => {
    setScheduleTarget(post);
    const base = post.scheduledAt ? new Date(post.scheduledAt) : new Date();
    setScheduledAt(formatDateTimeLocal(base));
    setMeetingLink(post.meetingLink || "");
    setShowSchedule(true);
  };

  const schedule = async (e) => {
    e.preventDefault();
    try {
      await tutorPostsApi.schedule(scheduleTarget.postId, {
        scheduledAt: new Date(scheduledAt).toISOString(),
        meetingLink: meetingLink,
      });
      setMsg("Meeting scheduled");
      setShowSchedule(false);
      setScheduleTarget(null);
      loadLessons();
      loadApplied();
    } catch {
      setMsg("Failed to schedule");
    }
  };

  // Finish (Lessons) — tutor can complete after scheduled time has passed
  const finishLesson = async (post) => {
    try {
      setMsg("");
      await tutorPostsApi.update(post.postId, {
        title: post.title,
        description: post.description || "",
        maxParticipants: Number(post.maxParticipants) || 1,
        status: "Completed",
      });
      // optimistic UI
      setLessons((prev) =>
        prev.map((p) => (p.postId === post.postId ? { ...p, status: "Completed" } : p))
      );
      setMsg("Marked as completed.");
      await loadLessons();
      await loadApplied();
    } catch {
      setMsg("Failed to mark as completed");
    }
  };

  // helper to compute participant count safely
  const getCount = (p) =>
    p?.participantsCount ??
    p?.currentParticipants ??
    p?.enrolledCount ??
    (Array.isArray(p?.participants) ? p.participants.length : undefined) ??
    p?.currentCount ??
    0;

  const renderLessonCard = (p) => {
    const scheduled = !!p.scheduledAt;
    const count = getCount(p);
    const max = p.maxParticipants ?? p.capacity ?? 0;
    const st = (p.status || "").toUpperCase();
    const isCompleted = st === "COMPLETED";
    const isScheduledStatus = st === "SCHEDULED" || scheduled;

    const canFinish =
      scheduled &&
      new Date(p.scheduledAt).getTime() <= Date.now() &&
      p.tutorId === user.userId &&
      !isCompleted;

    return (
      <GlassCard key={p.postId} className="p-6">
        <div className="flex justify-between items-start">
          <div>
            <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">{p.title}</h2>
            <p className="text-slate-600 dark:text-slate-300 mt-1">
              {new Date(p.createdAt).toLocaleDateString()}
            </p>
          </div>
          <div className="flex items-center gap-2">
            {typeof max === "number" && max > 0 && (
              <Chip className="bg-slate-50/70 dark:bg-slate-800/50 text-black dark:text-slate-100/80">
                {count}/{max} joined
              </Chip>
            )}
            <Chip className={chipClassFor(p.status)}>{p.status}</Chip>
          </div>
        </div>

        {p.description && <p className="text-slate-700 dark:text-slate-200 mt-3">{p.description}</p>}

        {p.imageUrl && (
          <img
            src={toImageUrl(p.imageUrl)}
            alt={p.title}
            className="mt-3 h-40 w-full object-cover rounded-xl border border-slate-200 dark:border-slate-700"
            loading="lazy"
          />
        )}

        {scheduled && (
          <div className="mt-2 text-sm text-blue-700 dark:text-blue-300">
            <div>Scheduled: {new Date(p.scheduledAt).toLocaleString()}</div>
            {p.meetingLink && (
              <div className="truncate">
                Meeting:{" "}
                <a
                  href={p.meetingLink}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="underline"
                  title={p.meetingLink}
                >
                  {p.meetingLink}
                </a>
              </div>
            )}
          </div>
        )}

        <div className="flex justify-between items-center mt-5 text-sm text-slate-500 dark:text-slate-400">
          <div className="flex items-center">
            <div className="w-8 h-8 bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white rounded-full flex items-center justify-center mr-2 font-semibold">
              {p.tutorName?.charAt(0).toUpperCase() || "T"}
            </div>
            <div>
              <span className="font-medium text-slate-800 dark:text-slate-200">{p.tutorName || "Tutor"}</span>
              <span className="ml-1">• {new Date(p.createdAt).toLocaleDateString()}</span>
            </div>
          </div>

          {/* Tutor actions on own lessons */}
          {p.tutorId === user.userId ? (
            <div className="flex items-center gap-3">
              {!isCompleted && !isScheduledStatus && <MacButton onClick={() => openEdit(p)}>Edit</MacButton>}
              {!isCompleted && <MacDanger onClick={() => deleteLesson(p.postId)}>Delete</MacDanger>}
              {!isCompleted && (
                <MacPrimary onClick={() => openSchedule(p)}>
                  {scheduled ? "Edit Schedule" : "Schedule"}
                </MacPrimary>
              )}
              {!isCompleted && canFinish && (
                <MacPrimary className="bg-green-600 hover:bg-green-700" onClick={() => finishLesson(p)}>
                  Finish
                </MacPrimary>
              )}
            </div>
          ) : (
            <div />
          )}
        </div>
      </GlassCard>
    );
  };

  const isLoadingTab =
    (lessonsTab === "MINE" && loading) || (lessonsTab === "APPLIED" && (loadingApplied || loading));

  const listForTab = lessonsTab === "MINE" ? myLessons : appliedLessons;

  return (
    <>
      <div className="max-w-6xl mx-auto px-6 py-6 space-y-6">
        <GlassCard className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">Lessons</h2>
            <div className="flex gap-2">
              <MacPrimary onClick={() => setShowCreate(true)}>+ New Lesson</MacPrimary>
              <MacButton
                onClick={() => {
                  loadLessons();
                  loadApplied();
                }}
              >
                Refresh
              </MacButton>
            </div>
          </div>

          {/* Lessons sub-tabs */}
          <div className="inline-flex bg-white/60 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-xl p-1">
            {["MINE", "APPLIED"].map((t) => (
              <button
                key={t}
                onClick={() => setLessonsTab(t)}
                className={
                  "px-4 py-1.5 text-sm font-medium rounded-lg transition " +
                  (lessonsTab === t
                    ? "bg-blue-600 text-white shadow"
                    : "text-slate-700 dark:text-slate-300 hover:bg-white/70 dark:hover:bg-slate-700/60")
                }
              >
                {t === "MINE" ? "My Lessons" : "Applied"}
              </button>
            ))}
          </div>

          {msg && (
            <GlassCard
              className={
                "mt-4 p-3 " +
                (msg.toLowerCase().includes("success")
                  ? "ring-1 ring-emerald-300/50"
                  : "ring-1 ring-blue-300/50")
              }
            >
              <div className="text-slate-700 dark:text-slate-200">{msg}</div>
            </GlassCard>
          )}

          {isLoadingTab ? (
            <div className="grid gap-4 mt-4">
              {[...Array(3)].map((_, i) => (
                <SkeletonCard key={i} />
              ))}
            </div>
          ) : listForTab.length === 0 ? (
            <GlassCard className="p-10 mt-4 text-center">
              <EmptyIllustration />
              <p className="mt-4 text-slate-600 dark:text-slate-300">
                {lessonsTab === "MINE"
                  ? "You haven't posted any lessons yet. Create one!"
                  : "You haven't applied to any lessons yet."}
              </p>
            </GlassCard>
          ) : (
            <div className="grid mt-4 md:grid-cols-2 gap-5">
              {listForTab.map((p) => renderLessonCard(p))}
            </div>
          )}
        </GlassCard>
      </div>

      {/* New Lesson Modal */}
      {showCreate && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100">New Lesson</h3>
            </div>
            <form onSubmit={createLesson} className="p-6 space-y-4">
              <input
                name="title"
                value={form.title}
                onChange={onChange}
                placeholder="Title"
                required
                className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />
              <textarea
                name="description"
                value={form.description}
                onChange={onChange}
                placeholder="Description"
                rows={3}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />
              <input
                type="number"
                min={1}
                name="maxParticipants"
                value={form.maxParticipants}
                onChange={onChange}
                required
                className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />

              <div className="pt-1">
                <label className="block text-sm text-slate-700 dark:text-slate-300 mb-1">
                  Optional Image (≤ 10MB)
                </label>
                <input
                  type="file"
                  accept="image/*"
                  onChange={(e) => setImageFile(e.target.files?.[0] || null)}
                  className="w-full text-sm file:mr-4 file:py-2 file:px-4 file:rounded-lg 
                             file:border-0 file:text-sm file:font-semibold 
                             file:bg-blue-50 file:text-blue-700 
                             hover:file:bg-blue-100
                             dark:file:bg-slate-800 dark:file:text-slate-200 dark:hover$file:bg-slate-700"
                />
                {imageFile && (
                  <div className="mt-1 text-xs text-slate-600 dark:text-slate-400">
                    Selected: {imageFile.name} ({Math.round(imageFile.size / 1024)} KB)
                  </div>
                )}
              </div>

              <div className="flex justify-end gap-2">
                <MacButton
                  type="button"
                  onClick={() => {
                    setShowCreate(false);
                    setImageFile(null);
                  }}
                >
                  Cancel
                </MacButton>
                <MacPrimary type="submit">Create</MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Edit Lesson Modal */}
      {showEdit && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Edit Lesson</h3>
            </div>
            <form onSubmit={updateLesson} className="p-6 space-y-4">
              <input
                name="title"
                value={form.title}
                onChange={onChange}
                required
                className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />
              <textarea
                name="description"
                value={form.description}
                onChange={onChange}
                rows={3}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />
              <input
                type="number"
                min={1}
                name="maxParticipants"
                value={form.maxParticipants}
                onChange={onChange}
                required
                className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />

              <div className="pt-1">
                <label className="block text-sm text-slate-700 dark:text-slate-300 mb-1">
                  Replace Image (optional, ≤ 10MB)
                </label>
                <input
                  type="file"
                  accept="image/*"
                  onChange={(e) => setEditImageFile(e.target.files?.[0] || null)}
                  className="w-full text-sm file:mr-4 file:py-2 file:px-4 file:rounded-lg 
                             file:border-0 file:text-sm file:font-semibold 
                             file:bg-blue-50 file:text-blue-700 
                             hover:file:bg-blue-100
                             dark:file:bg-slate-800 dark:file:text-slate-200 dark:hover$file:bg-slate-700"
                />
                {editImageFile && (
                  <div className="mt-1 text-xs text-slate-600 dark:text-slate-400">
                    Selected: {editImageFile.name} ({Math.round(editImageFile.size / 1024)} KB)
                  </div>
                )}
              </div>

              <div className="flex justify-end gap-2">
                <MacButton
                  type="button"
                  onClick={() => {
                    setShowEdit(false);
                    setEditImageFile(null);
                  }}
                >
                  Cancel
                </MacButton>
                <MacPrimary type="submit">Update</MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Schedule Lesson Modal */}
      {showSchedule && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Schedule Meeting</h3>
            </div>
            <form onSubmit={schedule} className="p-6 space-y-4">
              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Date & Time *</label>
                <input
                  type="datetime-local"
                  value={scheduledAt}
                  onChange={(e) => setScheduledAt(e.target.value)}
                  required
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
                />
              </div>

              <div>
                <label className="block text-sm text-slate-700 dark:text-slate-300">Meeting Link *</label>

                <div className="flex gap-2 mt-1">
                  <button
                    type="button"
                    onClick={openZoomCreate2}
                    className="px-3 py-2 rounded-xl text-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-400/40"
                  >
                    Generate Zoom link
                  </button>
                  <button
                    type="button"
                    onClick={pasteZoomFromClipboard2}
                    className="px-3 py-2 rounded-xl text-sm border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 hover:bg-black/5 dark:hover:bg-white/10 focus:outline-none focus:ring-2 focus:ring-blue-400/30"
                  >
                    Paste from clipboard
                  </button>
                </div>

                <input
                  type="url"
                  value={meetingLink}
                  onChange={(e) => setMeetingLink(e.target.value)}
                  placeholder="https://zoom.us/j/123456789 (or Google Meet/Teams link)"
                  required
                  className="mt-2 w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
                />
                <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                  Start a Zoom meeting in the new tab, copy the URL, then click <em>Paste from clipboard</em>.
                </p>
              </div>

              <div className="flex justify-end gap-2">
                <MacButton type="button" onClick={() => setShowSchedule(false)}>
                  Cancel
                </MacButton>
                <MacPrimary type="submit">Schedule</MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}
    </>
  );
};

/* ========================== Page ========================== */
const RequestsPage = () => {
  const navigate = useNavigate();
  const { user, loading: authLoading } = useAuth();
  const [activeTab, setActiveTab] = useState("REQUESTS"); // REQUESTS | LESSONS

  if (authLoading) {
    return (
      <div className="max-w-6xl mx-auto p-6">
        <div className="grid gap-4">{[...Array(4)].map((_, i) => <SkeletonCard key={i} />)}</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="relative min-h-screen">
        <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-slate-900 dark:via-slate-900 dark:to-slate-800" />
        <div className="max-w-3xl mx-auto p-6">
          <GlassCard className="p-10 text-center">
            <EmptyIllustration />
            <p className="mt-4 text-red-500 font-medium">Please log in to continue.</p>
            <div className="mt-4">
              <MacPrimary onClick={() => navigate("/login")}>Go to Login</MacPrimary>
            </div>
          </GlassCard>
        </div>
      </div>
    );
  }

  return (
    <div className="relative min-h-screen font-sans">
      <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-slate-900 dark:via-slate-900 dark:to-slate-800" />

      <GlassBar className=" border-x-0 border-t-0 px-6 py-4 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow" />
            <div className="font-semibold text-slate-700 dark:text-slate-200">SkillLink</div>
          </div>
          <div className="flex gap-2 mr-24 items-center text-xs text-slate-500 dark:text-slate-400">
            <MacButton
              className={activeTab === "REQUESTS" ? "bg-black/10 dark:bg-white/10" : ""}
              onClick={() => setActiveTab("REQUESTS")}
            >
              Requests
            </MacButton>
            <MacButton
              className={activeTab === "LESSONS" ? "bg-black/10 dark:bg-white/10" : ""}
              onClick={() => setActiveTab("LESSONS")}
            >
              My Lessons
            </MacButton>
            <SettingsMenu />
          </div>
        </div>
      </GlassBar>

      {activeTab === "REQUESTS" ? <RequestsPane /> : <LessonsPane />}

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

export default RequestsPage;
