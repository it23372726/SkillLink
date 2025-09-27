// src/pages/RequestsPage.jsx
import React, { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import api from "../api/axios";
import Dock from "../components/Dock";

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

  // Tutor Posts
  Open:
    "bg-blue-200/70 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
  Closed:
    "bg-slate-200/70 text-slate-900 dark:bg-slate-400/20 dark:text-slate-200",
  Scheduled:
    "bg-amber-200/70 text-amber-900 dark:bg-amber-400/20 dark:text-amber-200",
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
  const [requests, setRequests] = useState([]);
  const [acceptedRequests, setAcceptedRequests] = useState([]);

  const [tab, setTab] = useState("ALL"); // ALL | MINE | ACCEPTED
  const [sortBy, setSortBy] = useState("NEWEST"); // NEWEST | OLDEST
  const [searchQuery, setSearchQuery] = useState("");
  const [liveQuery, setLiveQuery] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState("");

  // modals/forms
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingRequest, setEditingRequest] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    skillName: "",
    topic: "",
    description: "",
  });

  useEffect(() => {
    loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadAll = async () => {
    try {
      setIsLoading(true);
      const [reqRes, accRes] = await Promise.all([
        api.get("/requests"),
        api.get("/requests/accepted"),
      ]);
      setRequests(reqRes.data || []);
      setAcceptedRequests(accRes.data || []);
    } catch {
      setMessage("Failed to load requests");
    } finally {
      setIsLoading(false);
    }
  };

  // fast lookup for accepted
  const acceptedMap = useMemo(() => {
    const m = {};
    for (const ar of acceptedRequests) m[ar.requestId] = true;
    return m;
  }, [acceptedRequests]);

  // debounced live search save
  useEffect(() => {
    const run = debounce((q) => setSearchQuery(q), 350);
    run(liveQuery);
  }, [liveQuery]);

  // derived list
  const filteredSorted = useMemo(() => {
    let list = [...requests];

    if (tab === "MINE") list = list.filter((r) => r.learnerId === user?.userId);
    if (tab === "ACCEPTED") list = list.filter((r) => acceptedMap[r.requestId]);

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
  }, [requests, tab, acceptedMap, searchQuery, sortBy, user?.userId]);

  // actions
  const acceptRequest = async (requestId) => {
    try {
      setMessage("");
      if (!acceptedMap[requestId]) {
        setAcceptedRequests((prev) => [...prev, { requestId, status: "PENDING" }]);
      }
      await api.post(`/requests/${requestId}/accept`);
      setMessage("Request accepted successfully!");
      const acc = await api.get("/requests/accepted");
      setAcceptedRequests(acc.data || []);
    } catch {
      setMessage("Failed to accept request");
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
      await api.post("/requests", {
        // Server infers learnerId from the authenticated user token
        ...formData,
      });
      setMessage("Request created successfully!");
      setShowCreateModal(false);
      setFormData({ skillName: "", topic: "", description: "" });
      loadAll();
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
      await api.put(`/requests/${editingRequest.requestId}`, {
        ...formData,
      });
      setMessage("Request updated successfully!");
      cancelEditing();
      loadAll();
    } catch {
      setMessage("Failed to update request");
    } finally {
      setIsSubmitting(false);
    }
  };

  const deleteRequest = async (requestId) => {
    if (!window.confirm("Are you sure you want to delete this request?")) return;
    try {
      await api.delete(`/requests/${requestId}`);
      setMessage("Request deleted successfully!");
      loadAll();
    } catch {
      setMessage("Failed to delete request");
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
              {["ALL", "MINE", "ACCEPTED"].map((t) => (
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
                  {t === "ALL" ? "All" : t === "MINE" ? "My Requests" : "Accepted by Me"}
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

              <MacPrimary id="open-create-request-id" data-testid="open-create-request" onClick={() => setShowCreateModal(true)}>+ Create Request</MacPrimary>
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
                  {searchQuery || tab !== "ALL"
                    ? "No requests match your filters."
                    : "No requests yet. Be the first to create one!"}
                </p>
                {tab !== "ALL" && (
                  <MacButton onClick={() => setTab("ALL")} className="mt-4">
                    Reset filters
                  </MacButton>
                )}
              </GlassCard>
            ) : (
              <div className="grid md:grid-cols-2 gap-5">
                {filteredSorted.map((request) => {
                  const accepted = !!acceptedMap[request.requestId];
                  const isOwner = user.userId === request.learnerId;

                  return (
                    <GlassCard key={request.requestId} className="p-6">
                      <div className="flex justify-between items-start">
                        <div>
                          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                            {request.skillName}
                          </h2>
                          {request.topic && (
                            <p className="text-slate-600 dark:text-slate-300 mt-1">{request.topic}</p>
                          )}
                        </div>
                        <Chip className={statusStyles[request.status] || ""}>{request.status}</Chip>
                      </div>

                      {request.description && (
                        <p className="text-slate-700 dark:text-slate-200 mt-3">{request.description}</p>
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
                          {!isOwner && (
                            <MacPrimary
                              onClick={() => acceptRequest(request.requestId)}
                              disabled={accepted}
                              className={accepted ? "opacity-60 cursor-not-allowed" : ""}
                            >
                              {accepted ? "Accepted" : "Accept"}
                            </MacPrimary>
                          )}

                          {isOwner && (
                            <>
                              <MacButton onClick={() => startEditing(request)}>Edit</MacButton>
                              <MacDanger onClick={() => deleteRequest(request.requestId)}>
                                Delete
                              </MacDanger>
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
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50" data-testid="create-request-modal">
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
                <MacPrimary type="submit" disabled={isSubmitting} id="create-request-submit-id" data-testid="create-request-submit">
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
              <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
                Edit Request
              </h2>
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
    </>
  );
};

/* ========================== Lessons Pane (My Lessons Only, with Image Upload) ========================== */
const LessonsPane = () => {
  const { user } = useAuth();
  const [lessons, setLessons] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");

  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({ title: "", description: "", maxParticipants: 5 });
  const [imageFile, setImageFile] = useState(null);           // NEW: create image
  const [editImageFile, setEditImageFile] = useState(null);   // NEW: edit image

  const [showSchedule, setShowSchedule] = useState(false);
  const [scheduleTarget, setScheduleTarget] = useState(null);
  const [scheduledAt, setScheduledAt] = useState(formatDateTimeLocal(new Date()));

  const MAX_IMG_SIZE = 10 * 1024 * 1024; // 10MB

  const isValidImage = (file) => {
    if (!file) return true; // optional
    const okType = file.type.startsWith("image/");
    const okSize = file.size <= MAX_IMG_SIZE;
    return okType && okSize;
  };

  const loadLessons = async () => {
    try {
      setLoading(true);
      const res = await api.get("/tutor-posts");
      setLessons(res.data || []);
    } catch {
      setMsg("Failed to load lessons");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadLessons();
  }, []);

  // Only my lessons
  const myLessons = useMemo(
    () => lessons.filter((l) => l.tutorId === user.userId),
    [lessons, user?.userId]
  );

  const onChange = (e) => setForm((p) => ({ ...p, [e.target.name]: e.target.value }));

  const uploadLessonImage = async (postId, file) => {
    const fd = new FormData();
    fd.append("file", file);
    await api.post(`/tutor-posts/${postId}/image`, fd, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  };

  const createLesson = async (e) => {
    e.preventDefault();
    try {
      // Validate image if present
      if (!isValidImage(imageFile)) {
        setMsg("Image must be an image file and ≤ 10MB");
        return;
      }

      // Create post (do NOT send tutorId — we read from JWT)
      const createRes = await api.post("/tutor-posts", {
        title: form.title.trim(),
        description: form.description?.trim() || "",
        maxParticipants: Number(form.maxParticipants) || 1,
      });

      const newId = createRes?.data?.postId;
      // Optional image upload
      if (imageFile && newId) {
        await uploadLessonImage(newId, imageFile);
      }

      setMsg("Lesson posted successfully!");
      setShowCreate(false);
      setForm({ title: "", description: "", maxParticipants: 5 });
      setImageFile(null);
      loadLessons();
    } catch (err) {
      setMsg(err?.response?.data?.message || "Failed to create lesson");
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
      // Validate image if present
      if (!isValidImage(editImageFile)) {
        setMsg("Image must be an image file and ≤ 10MB");
        return;
      }

      // Update metadata
      await api.put(`/tutor-posts/${editing.postId}`, {
        title: form.title.trim(),
        description: form.description?.trim() || "",
        maxParticipants: Number(form.maxParticipants) || 1,
      });

      // Optional image replace
      if (editImageFile) {
        await uploadLessonImage(editing.postId, editImageFile);
      }

      setMsg("Lesson updated successfully!");
      setShowEdit(false);
      setEditing(null);
      setEditImageFile(null);
      loadLessons();
    } catch (err) {
      setMsg(err?.response?.data?.message || "Failed to update");
    }
  };

  const deleteLesson = async (postId) => {
    if (!window.confirm("Delete this lesson post?")) return;
    try {
      await api.delete(`/tutor-posts/${postId}`);
      setMsg("Lesson deleted");
      loadLessons();
    } catch {
      setMsg("Failed to delete");
    }
  };

  const openSchedule = (post) => {
    setScheduleTarget(post);
    const base = post.scheduledAt ? new Date(post.scheduledAt) : new Date();
    setScheduledAt(formatDateTimeLocal(base));
    setShowSchedule(true);
  };

  const schedule = async (e) => {
    e.preventDefault();
    try {
      await api.put(`/tutor-posts/${scheduleTarget.postId}/schedule`, {
        scheduledAt: new Date(scheduledAt).toISOString(),
      });
      setMsg("Meeting scheduled");
      setShowSchedule(false);
      setScheduleTarget(null);
      loadLessons();
    } catch {
      setMsg("Failed to schedule");
    }
  };

  /* -------------------- UI -------------------- */
  return (
    <>
      <div className="max-w-6xl mx-auto px-6 py-6 space-y-6">
        <GlassCard className="p-6">
          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
              My Lessons
            </h2>
            <div className="flex gap-2">
              <MacPrimary onClick={() => setShowCreate(true)}>+ New Lesson</MacPrimary>
              <MacButton onClick={loadLessons}>Refresh</MacButton>
            </div>
          </div>

          {/* Messages */}
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

          {/* Lessons List */}
          {loading ? (
            <div className="grid gap-4">
              {[...Array(3)].map((_, i) => <SkeletonCard key={i} />)}
            </div>
          ) : myLessons.length === 0 ? (
            <GlassCard className="p-10 mt-4 text-center">
              <EmptyIllustration />
              <p className="mt-4 text-slate-600 dark:text-slate-300">
                You haven't posted any lessons yet. Create one!
              </p>
            </GlassCard>
          ) : (
            <div className="grid mt-4 md:grid-cols-2 gap-5">
              {myLessons.map((p) => {
                const scheduled = !!p.scheduledAt;

                return (
                  <GlassCard key={p.postId} className="p-6">
                    {/* Top: Title + Status */}
                    <div className="flex justify-between items-start">
                      <div>
                        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                          {p.title}
                        </h2>
                        <p className="text-slate-600 dark:text-slate-300 mt-1">
                          {new Date(p.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                      <Chip className={statusStyles[p.status] || ""}>{p.status}</Chip>
                    </div>

                    {/* Description */}
                    {p.description && (
                      <p className="text-slate-700 dark:text-slate-200 mt-3">{p.description}</p>
                    )}

                    {/* Scheduled At */}
                    {scheduled && (
                      <div className="mt-2 text-sm text-blue-600 dark:text-blue-400">
                        Scheduled: {new Date(p.scheduledAt).toLocaleString()}
                      </div>
                    )}

                    {/* Tutor Info (you) */}
                    <div className="flex justify-between items-center mt-5 text-sm text-slate-500 dark:text-slate-400">
                      <div className="flex items-center">
                        <div className="w-8 h-8 bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white rounded-full flex items-center justify-center mr-2 font-semibold">
                          {p.tutorName?.charAt(0).toUpperCase() || user.fullName?.charAt(0).toUpperCase() || "T"}
                        </div>
                        <div>
                          <span className="font-medium text-slate-800 dark:text-slate-200">
                            {p.tutorName || user.fullName || "Tutor"}
                          </span>
                          <span className="ml-1">
                            • {new Date(p.createdAt).toLocaleDateString()}
                          </span>
                        </div>
                      </div>

                      {/* Actions (owner-only) */}
                      <div className="flex items-center gap-3">
                        <MacButton onClick={() => openEdit(p)}>Edit</MacButton>
                        <MacDanger onClick={() => deleteLesson(p.postId)}>Delete</MacDanger>
                        <MacPrimary onClick={() => openSchedule(p)}>
                          {scheduled ? "Edit Schedule" : "Schedule"}
                        </MacPrimary>
                      </div>
                    </div>
                  </GlassCard>
                );
              })}
            </div>
          )}
        </GlassCard>
      </div>

      {/* --- Create Modal (with image upload) --- */}
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

              {/* Image (optional, ≤ 10MB) */}
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
                             dark:file:bg-slate-800 dark:file:text-slate-200 dark:hover:file:bg-slate-700"
                />
                {imageFile && (
                  <div className="mt-1 text-xs text-slate-600 dark:text-slate-400">
                    Selected: {imageFile.name} ({Math.round(imageFile.size / 1024)} KB)
                  </div>
                )}
              </div>

              <div className="flex justify-end gap-2">
                <MacButton type="button" onClick={() => { setShowCreate(false); setImageFile(null); }}>
                  Cancel
                </MacButton>
                <MacPrimary type="submit">Create</MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* --- Edit Modal (with optional replace image) --- */}
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

              {/* Optional replace image */}
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
                             dark:file:bg-slate-800 dark:file:text-slate-200 dark:hover:file:bg-slate-700"
                />
                {editImageFile && (
                  <div className="mt-1 text-xs text-slate-600 dark:text-slate-400">
                    Selected: {editImageFile.name} ({Math.round(editImageFile.size / 1024)} KB)
                  </div>
                )}
              </div>

              <div className="flex justify-end gap-2">
                <MacButton type="button" onClick={() => { setShowEdit(false); setEditImageFile(null); }}>
                  Cancel
                </MacButton>
                <MacPrimary type="submit">Update</MacPrimary>
              </div>
            </form>
          </GlassCard>
        </div>
      )}

      {/* Schedule Modal */}
      {showSchedule && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <GlassCard className="w-full max-w-md">
            <div className="p-6 border-b border-slate-200 dark:border-slate-700">
              <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Schedule Meeting</h3>
            </div>
            <form onSubmit={schedule} className="p-6 space-y-4">
              <input
                type="datetime-local"
                value={scheduledAt}
                onChange={(e) => setScheduledAt(e.target.value)}
                required
                className="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white/70 dark:bg-slate-800/60 text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-blue-500/40 outline-none"
              />
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
      {/* Background */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-slate-900 dark:via-slate-900 dark:to-slate-800" />

      {/* Top Bar */}
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
          </div>
        </div>
      </GlassBar>

      {activeTab === "REQUESTS" ? <RequestsPane /> : <LessonsPane />}

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

export default RequestsPage;
