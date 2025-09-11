import React, { useEffect, useMemo, useRef, useState } from "react";
import { useAuth } from "../context/AuthContext";
import api from "../api/axios";
import { toImageUrl } from "../api/base";
import { useNavigate } from "react-router-dom";

// --- utils ---
const debounce = (fn, delay = 300) => {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), delay);
  };
};

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

const levels = ["Beginner", "Intermediate", "Advanced", "Expert"];

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

const SkeletonRow = () => (
  <div className="border border-slate-200 dark:border-slate-700 rounded-xl p-4 animate-pulse flex items-center justify-between">
    <div>
      <div className="h-4 w-40 bg-slate-200 dark:bg-slate-700 rounded" />
      <div className="h-3 w-24 bg-slate-200 dark:bg-slate-700 rounded mt-2" />
    </div>
    <div className="h-8 w-20 bg-slate-200 dark:bg-slate-700 rounded" />
  </div>
);

const EmptyState = ({ title, subtitle, action }) => (
  <div className="border border-dashed border-slate-300 dark:border-slate-600 rounded-2xl p-10 text-center">
    <svg viewBox="0 0 200 200" className="w-24 h-24 mx-auto text-blue-200" fill="none">
      <circle cx="100" cy="100" r="90" stroke="currentColor" strokeWidth="8" />
      <path d="M65 120c20 16 50 16 70 0" stroke="currentColor" strokeWidth="8" strokeLinecap="round" />
      <circle cx="75" cy="80" r="6" fill="currentColor" />
      <circle cx="125" cy="80" r="6" fill="currentColor" />
    </svg>
    <h3 className="mt-4 text-lg font-semibold text-slate-900 dark:text-slate-100">{title}</h3>
    <p className="mt-1 text-slate-600 dark:text-slate-400">{subtitle}</p>
    {action}
  </div>
);

const SkillsManagement = () => {
  const { user } = useAuth();
  const [skills, setSkills] = useState([]);
  const [isLoadingSkills, setIsLoadingSkills] = useState(false);

  const navigate = useNavigate();

  // add form
  const [query, setQuery] = useState("");
  const [level, setLevel] = useState("Beginner");

  // suggestions
  const [suggestions, setSuggestions] = useState([]);
  const [sLoading, setSLoading] = useState(false);
  const [sOpen, setSOpen] = useState(false);
  const [sIndex, setSIndex] = useState(-1);
  const boxRef = useRef(null);

  // toast
  const [toast, setToast] = useState({ kind: "info", text: "" });

  // explore
  const [filteredUsers, setFilteredUsers] = useState([]);
  const [isFiltering, setIsFiltering] = useState(false);
  const [activeExploreSkill, setActiveExploreSkill] = useState("");

  // --- effects ---
  useEffect(() => {
    if (user?.userId) loadSkills();
  }, [user?.userId]);

  useEffect(() => {
    const onClick = (e) => {
      if (boxRef.current && !boxRef.current.contains(e.target)) {
        setSOpen(false);
        setSIndex(-1);
      }
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  // --- loaders ---
  const loadSkills = async () => {
    if (!user?.userId) return;
    try {
      setIsLoadingSkills(true);
      const res = await api.get(`/skills/user/${user.userId}`);
      setSkills(res.data || []);
    } finally {
      setIsLoadingSkills(false);
    }
  };

  const debouncedSuggest = useMemo(
    () =>
      debounce(async (raw) => {
        const q = (raw || "").trim();
        if (q.length < 2) {
          setSuggestions([]);
          setSOpen(false);
          setSIndex(-1);
          setSLoading(false);
          return;
        }
        try {
          setSLoading(true);
          // use axios params to encode safely
          const res = await api.get("/skills/suggest", { params: { q } });
          setSuggestions(res.data || []);
          setSOpen(true);
          setSIndex(-1);
        } catch (err) {
          // ignore 400 from server (bad q)
          if (err?.response?.status !== 400) {
            console.error("Suggest error:", err);
          }
          setSuggestions([]);
          setSOpen(false);
          setSIndex(-1);
        } finally {
          setSLoading(false);
        }
      }, 250),
    []
  );
  

  // --- handlers ---
  const onQueryChange = (e) => {
    const v = e.target.value;
    // collapse multiple spaces to single and keep the raw for user view if you like
    const sanitized = v.replace(/\s{2,}/g, " ");
    setQuery(sanitized);
    // only trigger suggestion search on input change (debounced)
    debouncedSuggest(sanitized);
  };
  

  const pickSuggestion = (s) => {
    setQuery(s.name);
    setSOpen(false);
    setSIndex(-1);
  };

  const addSkill = async () => {
    const name = query.trim().replace(/\s{2,}/g, " ");
    if (name.length < 2) {
      showToast("info", "Please enter at least 2 characters");
      return;
    }
  
    const dup = skills.some((s) => s.skill?.name?.toLowerCase() === name.toLowerCase());
    if (dup) {
      showToast("info", "Already added this skill");
      return;
    }
  
    try {
      setSkills((prev) => [
        { skillId: `tmp_${Date.now()}`, skill: { name }, level, _optimistic: true },
        ...prev,
      ]);
  
      await api.post("/skills/add", {
        userId: user.userId,
        skillName: name,
        level,
      });
  
      showToast("success", "Skill added");
      setQuery("");
      setLevel("Beginner");
      loadSkills();
    } catch {
      showToast("error", "Failed to add skill");
      setSkills((prev) => prev.filter((s) => !s._optimistic));
    }
  };
  

  const deleteSkill = async (skillId) => {
    if (!window.confirm("Remove this skill?")) return;
    try {
      setSkills((prev) => prev.filter((s) => s.skillId !== skillId));
      await api.delete(`/skills/${user.userId}/${skillId}`);
      showToast("success", "Skill removed");
    } catch {
      showToast("error", "Failed to remove skill");
      loadSkills();
    }
  };

  const exploreSkill = async (name) => {
    try {
      setIsFiltering(true);
      setActiveExploreSkill(name);
      const res = await api.get(`/skills/filter?skill=${encodeURIComponent(name)}`);
      setFilteredUsers(res.data || []);
    } finally {
      setIsFiltering(false);
    }
  };

  const showToast = (kind, text) => {
    setToast({ kind, text });
    setTimeout(() => setToast({ kind: "info", text: "" }), 2000);
  };

  return (
    <div className="relative min-h-screen font-sans overflow-hidden">
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
              <p>Skills</p>
            </div>
            
          </div>
        </GlassBar>
      </div>
      {/* Hero */}
      <div className="relative">
        {/* Gradient Background */}
        <div className="absolute inset-0 -z-10 bg-gradient-to-r from-blue-600 to-indigo-600 dark:from-blue-800 dark:to-indigo-800" />
        
        <div className="max-w-6xl mx-auto px-6 py-10">
          <h1 className="text-3xl font-bold tracking-tight text-white dark:text-slate-100">
            Skills & Discovery
          </h1>
          <p className="text-blue-100 dark:text-slate-300 mt-2">
            Track your strengths and find people who share the same passions.
          </p>
        </div>
      </div>

      {/* Body */}
      <div className="max-w-6xl mx-auto px-6 -mt-6">
        <div className="rounded-2xl shadow-lg ring-1 ring-slate-200 dark:ring-slate-700 bg-white dark:bg-slate-900 p-6">
          {/* Toast */}
          {toast.text && (
            <div
              className={`mb-4 px-4 py-2 rounded-lg ${
                toast.kind === "success"
                  ? "bg-emerald-50 text-emerald-700"
                  : toast.kind === "error"
                  ? "bg-red-50 text-red-700"
                  : "bg-blue-50 text-blue-700"
              }`}
            >
              {toast.text}
            </div>
          )}

          {/* Add Skill */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4" ref={boxRef}>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Skill Name
              </label>
              <div className="relative">
                <input
                  value={query}
                  onChange={onQueryChange}
                  placeholder="Start typing (e.g., React, Guitar, UI Design)…"
                  className="w-full px-4 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-blue-500"
                />
                {sOpen && (sLoading || suggestions.length > 0) && (
                  <div className="absolute z-20 mt-2 w-full bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-xl overflow-hidden">
                    {sLoading && <div className="px-4 py-3 text-sm text-slate-500">Searching…</div>}
                    {!sLoading &&
                      suggestions.map((s, i) => (
                        <button
                          type="button"
                          key={s.skillId ?? `${s.name}-${i}`}
                          onClick={() => pickSuggestion(s)}
                          className={`w-full text-left px-4 py-2 text-sm text-black/80 dark:text-white/80 hover:bg-slate-100 dark:hover:bg-slate-700 ${
                            i === sIndex ? "bg-slate-100 dark:bg-slate-700" : ""
                          }`}
                        >
                          {s.name}
                        </button>
                      ))}
                  </div>
                )}
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Proficiency Level
              </label>
              <select
                value={level}
                onChange={(e) => setLevel(e.target.value)}
                className="w-full px-4 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-blue-500"
              >
                {levels.map((lv) => (
                  <option key={lv} value={lv}>
                    {lv}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="mt-3">
            <button
              onClick={addSkill}
              className="px-4 py-2 rounded-xl bg-blue-600 text-white hover:bg-blue-700"
            >
              + Add Skill
            </button>
          </div>

          {/* Your Skills */}
          <div className="mt-8">
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Your Skills</h2>
              {skills.length > 0 && (
                <div className="flex gap-2 overflow-x-auto no-scrollbar">
                  {skills.slice(0, 6).map((s) => (
                    <button
                      key={`quick-${s.skillId}`}
                      onClick={() => exploreSkill(s.skill?.name)}
                      className="px-3 py-1.5 rounded-full text-sm border text-black/80 dark:text-white/50 border-black/10 dark:border-white/10 bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700"
                    >
                      {s.skill?.name}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {isLoadingSkills ? (
              <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                {[...Array(6)].map((_, i) => (
                  <SkeletonRow key={i} />
                ))}
              </div>
            ) : skills.length === 0 ? (
              <EmptyState
                title="No skills yet"
                subtitle="Add your first skill to start discovering peers and tutors."
                action={
                  <button
                    onClick={() =>
                      document.querySelector("input[placeholder^='Start typing']")?.focus()
                    }
                    className="mt-4 px-4 py-2 rounded-xl border text-black/80 dark:text-white/80 border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800"
                  >
                    Add a skill
                  </button>
                }
              />
            ) : (
              <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                {skills.map((s) => (
                  <div
                    key={s.skillId}
                    className="border cursor-pointer border-slate-200 dark:border-slate-700 rounded-xl p-4 flex items-center justify-between"
                    onClick={() => exploreSkill(s.skill?.name)}
                  >
                    <div>
                      <div className="font-medium text-slate-900 dark:text-slate-100">{s.skill?.name}</div>
                      <span
                        className={`inline-block mt-1 px-2 py-0.5 text-xs font-semibold rounded-full ${levelPill(
                          s.level
                        )}`}
                      >
                        {s.level}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        className="text-red-600 hover:text-red-800"
                        onClick={(e) => {
                          e.stopPropagation();
                          deleteSkill(s.skillId);
                        }}
                      >
                        <i className="fas fa-trash-alt"></i>
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Explore users */}
          {activeExploreSkill && (
            <div className="mt-10">
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                  Users with <span className="text-blue-600">{activeExploreSkill}</span>
                </h2>
                <button
                  onClick={() => {
                    setActiveExploreSkill("");
                    setFilteredUsers([]);
                  }}
                  className="text-slate-500 hover:text-slate-700 dark:hover:text-slate-300"
                >
                  Clear
                </button>
              </div>

              {isFiltering ? (
                <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {[...Array(6)].map((_, i) => (
                    <SkeletonRow key={i} />
                  ))}
                </div>
              ) : filteredUsers.length === 0 ? (
                <EmptyState
                  title="No matches yet"
                  subtitle="Try another skill or broaden your search."
                />
              ) : (
                <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {filteredUsers.map((u) => (
                    <div
                      key={u.userId}
                      className="border border-slate-200 dark:border-slate-700 rounded-xl p-4 hover:shadow-sm transition"
                    >
                      <div className="flex items-center gap-3 mb-3">
                        <div className="w-12 h-12 bg-slate-100 dark:bg-slate-800 rounded-full flex items-center justify-center overflow-hidden">
                          {u.profilePicture ? (
                            <img
                              src={toImageUrl(u.profilePicture)}
                              alt={u.fullName}
                              className="w-12 h-12 object-cover"
                            />
                          ) : (
                            <span className="text-blue-700 font-semibold">
                              {u.fullName?.charAt(0).toUpperCase() || "U"}
                            </span>
                          )}
                        </div>
                        <div>
                          <div className="font-semibold text-slate-900 dark:text-slate-100 flex items-center gap-2">
                            {u.fullName}
                            {u.readyToTeach && (
                              <span className="text-xs px-2 py-0.5 rounded-full bg-emerald-200/60 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200">
                                Tutor
                              </span>
                            )}
                          </div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">{u.role}</div>
                        </div>
                      </div>
                      {u.location && (
                        <div className="text-sm text-slate-600 dark:text-slate-400 flex items-center">
                          📍 {u.location}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
      <div className="fixed bottom-8 left-1/2 -translate-x-1/2 flex gap-3 px-4 py-3 rounded-2xl border border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-900/70 backdrop-blur-xl shadow z-30">
        <MacButton onClick={() => navigate("/request")}>+ Request</MacButton>
        <MacButton onClick={() => navigate("/skill")}>Skills</MacButton>
        <MacButton onClick={() => navigate("/VideoSession")}>Session</MacButton>
        <MacButton onClick={() => navigate("/dashboard")}>Dashboard</MacButton>
      </div>
    </div>
  );
};

export default SkillsManagement;
