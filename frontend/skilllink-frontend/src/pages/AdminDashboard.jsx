import React, { useEffect, useMemo, useRef, useState } from "react";
import api from "../api/axios";
import { useNavigate } from "react-router-dom";

/* ========================== Utilities ========================== */
const debounce = (fn, ms = 400) => {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), ms);
  };
};

const toCSV = (rows) => {
  if (!rows?.length) return "";
  const headers = Object.keys(rows[0]);
  const escape = (v) =>
    `"${String(v ?? "").replaceAll('"', '""').replaceAll("\n", " ")}"`;
  const lines = [headers.map(escape).join(",")];
  for (const r of rows) lines.push(headers.map((h) => escape(r[h])).join(","));
  return lines.join("\n");
};

const formatDate = (d) => {
  if (!d) return "";
  try {
    return new Date(d).toLocaleString();
  } catch {
    return d;
  }
};

const avatarBg = (seed) => {
  const colors = [
    "bg-blue-100 text-blue-700",
    "bg-amber-100 text-amber-700",
    "bg-emerald-100 text-emerald-700",
    "bg-purple-100 text-purple-700",
    "bg-pink-100 text-pink-700",
  ];
  const i = (seed?.charCodeAt?.(0) ?? 0) % colors.length;
  return colors[i];
};

/* ========================== Small UI Atoms ========================== */
const GlassCard = ({ className = "", children }) => (
  <div
    className={
      " rounded-2xl border shadow backdrop-blur-xl transition-all duration-300 " +
      "border-black/10 dark:border-white/10 bg-white/70 dark:bg-ink-900/50 " +
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
      "border-black/10 dark:border-white/10 bg-white/70 dark:bg-ink-900/50 " +
      className
    }
  >
    {children}
  </div>
);

const MacButton = ({ children, className = "", ...props }) => (
  <button
    {...props}
    className={
        " px-4 py-2 rounded-xl border text-sm transition " +
        " border-black/10 dark:border-white/10 " +
        " bg-white/50 hover:bg-black/5 dark:hover:bg-white/10 active:bg-white/80 " +
        " dark:bg-ink-800/60 dark:hover:bg-ink-800/80 " +
        " focus:outline-none focus:ring-1 focus:ring-blue-400/30 dark:focus:text-white/80 focus:text-black" +
        " text-black/80  dark:text-white/65 active:dark:text-white active:text-black"+
        className
    }
  >
    {children}
  </button>
);

const MacPrimary = (props) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl text-sm font-medium text-white " +
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
      "px-4 py-2 rounded-xl text-sm font-medium text-white " +
    //   "bg-red-600 hover:bg-red-700 active:bg-red-800 " +
      "focus:outline-none focus:ring-2 focus:ring-red-400/40 " +
      (props.className || "")
    }
  />
);

const Badge = ({ children, color = "gray" }) => {
  const map = {
    gray: "bg-slate-200/60 text-slate-900 dark:bg-slate-400/20 dark:text-slate-200",
    green:
      "bg-emerald-200/60 text-emerald-900 dark:bg-emerald-400/20 dark:text-emerald-200",
    red: "bg-red-200/60 text-red-900 dark:bg-red-400/20 dark:text-red-200",
    blue: "bg-blue-200/60 text-blue-900 dark:bg-blue-400/20 dark:text-blue-200",
    purple:
      "bg-purple-200/60 text-purple-900 dark:bg-purple-400/20 dark:text-purple-200",
    amber:
      "bg-amber-200/60 text-amber-900 dark:bg-amber-400/20 dark:text-amber-200",
  };
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium ${map[color]}`}>
      {children}
    </span>
  );
};

const Toast = ({ kind = "info", message }) => {
  if (!message) return null;
  return (
    <GlassCard
      className={
        "px-4 py-2 " +
        (kind === "success"
          ? "ring-1 ring-emerald-400/40 text-emerald-600 dark:text-emerald-300"
          : kind === "error"
          ? "ring-1 ring-red-400/40 text-red-600 dark:text-red-300"
          : "ring-1 ring-blue-400/40 text-blue-600 dark:text-blue-300")
      }
    >
      {message}
    </GlassCard>
  );
};

const SkeletonRow = () => (
  <tr className="animate-pulse">
    <td className="px-4 py-4">
      <div className="h-8 w-48 bg-slate-200 dark:bg-slate-700 rounded" />
    </td>
    <td className="px-4 py-4">
      <div className="h-4 w-56 bg-slate-200 dark:bg-slate-700 rounded" />
    </td>
    <td className="px-4 py-4">
      <div className="h-6 w-24 bg-slate-200 dark:bg-slate-700 rounded" />
    </td>
    <td className="px-4 py-4">
      <div className="h-6 w-24 bg-slate-200 dark:bg-slate-700 rounded" />
    </td>
    <td className="px-4 py-4">
      <div className="h-6 w-20 bg-slate-200 dark:bg-slate-700 rounded" />
    </td>
    <td className="px-4 py-4">
      <div className="h-8 w-24 bg-slate-200 dark:bg-slate-700 rounded" />
    </td>
  </tr>
);

/* ========================== Donut Chart ========================== */
const Donut = ({ segments = [], size = 140, stroke = 18 }) => {
  const total = segments.reduce((a, b) => a + b.value, 0) || 1;
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  let offset = 0;
  const palette = ["#2563eb", "#10b981", "#f59e0b", "#8b5cf6", "#ef4444", "#14b8a6"];
  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <g transform={`rotate(-90 ${size / 2} ${size / 2})`}>
        {segments.map((s, i) => {
          const len = (s.value / total) * c;
          const dash = `${len} ${c - len}`;
          const el = (
            <circle
              key={s.label + i}
              cx={size / 2}
              cy={size / 2}
              r={r}
              fill="transparent"
              stroke={palette[i % palette.length]}
              strokeWidth={stroke}
              strokeDasharray={dash}
              strokeDashoffset={-offset}
              strokeLinecap="butt"
            />
          );
          offset += len;
          return el;
        })}
        <circle
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="transparent"
          stroke="#e5e7eb"
          strokeWidth={stroke}
          strokeDasharray={`${c} 0`}
          opacity="0.35"
        />
      </g>
      <text
        x="50%"
        y="50%"
        textAnchor="middle"
        dominantBaseline="central"
        className="fill-slate-800 dark:fill-slate-200"
        style={{ fontSize: 16, fontWeight: 700 }}
      >
        Roles
      </text>
    </svg>
  );
};

/* ========================== Page ========================== */
const AdminDashboard = () => {
  const [users, setUsers] = useState([]);
  const [q, setQ] = useState("");
  const [filter, setFilter] = useState("ALL"); // ALL | ACTIVE | INACTIVE | TUTORS | ADMINS
  const [loading, setLoading] = useState(true);

  const navigate = useNavigate();

  const [toast, setToast] = useState({ kind: "info", message: "" });

  // selection
  const [checked, setChecked] = useState({});
  const [checkAll, setCheckAll] = useState(false);

  // sort + pagination
  const [sortBy, setSortBy] = useState({ key: "fullName", dir: "asc" });
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 10;

  // drawer
  const [activeUser, setActiveUser] = useState(null);

  // bulk actions dropdown
  const [bulkOpen, setBulkOpen] = useState(false);
  const bulkRef = useRef(null);

  // persist dark mode on refresh
  useEffect(() => {
    const t = localStorage.theme;
    if (t === "dark") document.documentElement.classList.add("dark");
  }, []);

  const showToast = (kind, message) => {
    setToast({ kind, message });
    setTimeout(() => setToast({ kind: "info", message: "" }), 2200);
  };

  const load = async (search = q) => {
    try {
      setLoading(true);
      const url = `/admin/users${search ? `?q=${encodeURIComponent(search)}` : ""}`;
      const res = await api.get(url);
      setUsers(res.data || []);
      setPage(1);
      setChecked({});
      setCheckAll(false);
    } catch (e) {
      console.error(e);
      showToast("error", "Failed to load users");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line
  }, []);

  const debouncedLoad = useMemo(() => debounce(load, 500), []);

  // close bulk dropdown on outside click / ESC
  useEffect(() => {
    const onDocClick = (e) => {
      if (bulkRef.current && !bulkRef.current.contains(e.target)) {
        setBulkOpen(false);
      }
    };
    const onEsc = (e) => {
      if (e.key === "Escape") setBulkOpen(false);
    };
    document.addEventListener("mousedown", onDocClick);
    document.addEventListener("keydown", onEsc);
    return () => {
      document.removeEventListener("mousedown", onDocClick);
      document.removeEventListener("keydown", onEsc);
    };
  }, []);

  // derived
  const filtered = useMemo(() => {
    let list = [...users];
    if (filter === "ACTIVE") list = list.filter((u) => u.isActive);
    if (filter === "INACTIVE") list = list.filter((u) => !u.isActive);
    if (filter === "TUTORS")
      list = list.filter(
        (u) => u.readyToTeach || u.role?.toLowerCase?.() === "tutor"
      );
    if (filter === "ADMINS")
      list = list.filter((u) => u.role?.toLowerCase?.() === "admin");

    const { key, dir } = sortBy;
    list.sort((a, b) => {
      const av = (a[key] ?? "").toString().toLowerCase();
      const bv = (b[key] ?? "").toString().toLowerCase();
      if (av < bv) return dir === "asc" ? -1 : 1;
      if (av > bv) return dir === "asc" ? 1 : -1;
      return 0;
    });
    return list;
  }, [users, filter, sortBy]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const pageSlice = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  // stats
  const stats = useMemo(() => {
    const t = users.length;
    const active = users.filter((u) => u.isActive).length;
    const inactive = t - active;
    const tutors = users.filter(
      (u) => u.readyToTeach || u.role?.toLowerCase?.() === "tutor"
    ).length;
    const admins = users.filter((u) => u.role?.toLowerCase?.() === "admin").length;
    return { t, active, inactive, tutors, admins };
  }, [users]);

  const donutData = useMemo(() => {
    const roleGroups = users.reduce((acc, u) => {
      const r = (u.role || "Unknown").toString();
      acc[r] = (acc[r] || 0) + 1;
      return acc;
    }, {});
    return Object.keys(roleGroups).map((k) => ({ label: k, value: roleGroups[k] }));
  }, [users]);

  // actions
  const setActive = async (ids, isActive) => {
    try {
      await Promise.all(
        ids.map((id) => api.put(`/admin/users/${id}/active`, { isActive }))
      );
      showToast("success", "Updated user status");
      setBulkOpen(false);
      load();
    } catch (e) {
      console.error(e);
      showToast("error", "Failed to update status");
    }
  };

  const setRole = async (ids, role) => {
    try {
      await Promise.all(ids.map((id) => api.put(`/admin/users/${id}/role`, { role })));
      showToast("success", "Updated role");
      setBulkOpen(false);
      load();
    } catch (e) {
      console.error(e);
      showToast("error", "Failed to update role");
    }
  };

  const onBulkToggle = () => {
    const visibleIds = pageSlice.map((u) => u.userId);
    const allChecked = visibleIds.every((id) => checked[id]);
    const next = {};
    if (!allChecked) visibleIds.forEach((id) => (next[id] = true));
    setChecked(next);
    setCheckAll(!allChecked);
  };

  const selectedIds = useMemo(
    () => Object.keys(checked).filter((k) => checked[k]).map(Number),
    [checked]
  );

  const exportCSV = () => {
    const data = filtered.map((u) => ({
      userId: u.userId,
      fullName: u.fullName,
      email: u.email,
      role: u.role,
      readyToTeach: u.readyToTeach,
      isActive: u.isActive,
      createdAt: formatDate(u.createdAt),
      location: u.location ?? "",
    }));
    const blob = new Blob([toCSV(data)], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `users_${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const headerCell = (label, key) => (
    <th
      className="px-4 py-3 cursor-pointer select-none text-slate-700 dark:text-slate-200"
      onClick={() =>
        setSortBy((s) =>
          s.key === key ? { key, dir: s.dir === "asc" ? "desc" : "asc" } : { key, dir: "asc" }
        )
      }
      title="Sort"
    >
      <div className="inline-flex items-center gap-1">
        {label}
        {sortBy.key === key && (
          <span className="text-xs text-slate-500">
            {sortBy.dir === "asc" ? "▲" : "▼"}
          </span>
        )}
      </div>
    </th>
  );

  return (
    <div className="relative min-h-screen font-sans overflow-hidden">
      {/* Gradient background layers */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-ink-900 dark:via-ink-900 dark:to-ink-800" />
      <div className="absolute -top-20 -right-24 -z-10 w-[520px] h-[520px] rounded-full bg-blue-300/20 blur-3xl dark:bg-blue-400/10" />
      <div className="absolute -bottom-16 -left-16 -z-10 w-[420px] h-[420px] rounded-full bg-indigo-300/20 blur-2xl dark:bg-indigo-400/10" />

      {/* Top glass bar */}
      <GlassBar className="px-6 py-4 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto flex justify-between items-center">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow" />
            <div className="text-slate-800 dark:text-slate-200 font-semibold">
                SkillLink
            </div>
          </div>
          <div className="flex gap-12 items-center text-xs text-slate-500 dark:text-slate-400">
            <p>Admin Dashboard</p>
            <MacButton
              onClick={() => {
                const el = document.documentElement;
                el.classList.toggle("dark");
                localStorage.theme = el.classList.contains("dark") ? "dark" : "light";
              }}
            >
              <i className="fas fa-moon"></i> {/*  darkmode icon */}
            </MacButton>
          </div>
        </div>
      </GlassBar>

      {/* Hero */}
      <div className="relative">
        <div className="absolute inset-0 -z-10 bg-gradient-to-r from-blue-600 to-indigo-600 dark:from-blue-800 dark:to-indigo-800" />
        <div className="max-w-6xl mx-auto px-6 py-10 text-white">
          <h1 className="text-3xl font-bold tracking-tight">User Management & Insights</h1>
          <p className="text-blue-100 dark:text-slate-300">
            Manage users, roles, access & view analytics
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-6 -mt-6 space-y-6">
        <Toast kind={toast.kind} message={toast.message} />

        {/* KPIs */}
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          <GlassCard className="p-4">
            <div className="text-sm text-slate-600 dark:text-slate-400">Total Users</div>
            <div className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
              {stats.t}
            </div>
          </GlassCard>
          <GlassCard className="p-4">
            <div className="text-sm text-slate-600 dark:text-slate-400">Active</div>
            <div className="text-2xl font-semibold text-emerald-600">{stats.active}</div>
          </GlassCard>
          <GlassCard className="p-4">
            <div className="text-sm text-slate-600 dark:text-slate-400">Inactive</div>
            <div className="text-2xl font-semibold text-red-600">{stats.inactive}</div>
          </GlassCard>
          <GlassCard className="p-4">
            <div className="text-sm text-slate-600 dark:text-slate-400">Tutors</div>
            <div className="text-2xl font-semibold text-purple-600">{stats.tutors}</div>
          </GlassCard>
          <GlassCard className="p-4">
            <div className="text-sm text-slate-600 dark:text-slate-400">Admins</div>
            <div className="text-2xl font-semibold text-blue-600">{stats.admins}</div>
          </GlassCard>
        </div>

        {/* Search / Filters / Actions */}
        <GlassCard className="p-4 relative z-[60] overflow-visible">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
            <div className="flex items-center gap-2">
              <div className="relative">
                <input
                  className="border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-slate-800 dark:text-slate-200 rounded-xl px-3 py-2 w-72 focus:ring-2 focus:ring-blue-400/40 outline-none"
                  placeholder="Search name or email…"
                  value={q}
                  onChange={(e) => {
                    setQ(e.target.value);
                    debouncedLoad(e.target.value);
                  }}
                />
                <div className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                  ⌘K
                </div>
              </div>
              <div className="hidden md:block text-sm text-slate-500 dark:text-slate-400">
                {filtered.length} result{filtered.length !== 1 ? "s" : ""}
              </div>
            </div>

            <div className="flex items-center gap-2">
              {["ALL", "ACTIVE", "INACTIVE", "TUTORS", "ADMINS"].map((f) => (
                <MacButton
                  key={f}
                  onClick={() => setFilter(f)}
                  className={
                    filter === f
                      ? "hover:bg-black/10 text-black/80 border-blue-600"
                      : "text-slate-800 dark:text-slate-200"
                  }
                >
                  {f[0] + f.slice(1).toLowerCase()}
                </MacButton>
              ))}
            </div>

            <div className="flex items-center gap-2 relative" ref={bulkRef}>
              <MacButton onClick={exportCSV}>Export CSV</MacButton>
              <MacButton
                onClick={() => setBulkOpen((v) => !v)}
                disabled={selectedIds.length === 0}
                className={
                  selectedIds.length === 0
                    ? " opacity-50 cursor-not-allowed"
                    : " bg-slate-100 text-black/80 hover:bg-slate-800"
                }
                aria-haspopup="menu"
                aria-expanded={bulkOpen}
                title={
                  selectedIds.length === 0
                    ? "Select users to enable bulk actions"
                    : "Bulk Actions"
                }
              >
                Bulk Actions
              </MacButton>

              {bulkOpen && (
                <GlassCard
                  role="menu"
                  className="absolute right-0 top-12 z-[70] min-w-[220px] bg-[rgba(255,255,255,0.9)] dark:bg-[rgb(15_23_42/var(--tw-bg-opacity,1))] overflow-hidden"
                  >
                  <button
                    onClick={() => selectedIds.length && setActive(selectedIds, true)}
                    className="block w-full text-left px-4 py-2 text-black/80 dark:text-white/65 hover:bg-black/10 dark:hover:bg-ink-800/60"
                    role="menuitem"
                  >
                    Activate selected
                  </button>
                  <button
                    onClick={() => selectedIds.length && setActive(selectedIds, false)}
                    className="block w-full text-left px-4 py-2 text-black/80 dark:text-white/65 hover:bg-black/10 dark:hover:bg-ink-800/60"
                    role="menuitem"
                  >
                    Deactivate selected
                  </button>
                  <div className="border-t border-white/40 dark:border-white/10" />
                  {["Learner", "Tutor", "Admin"].map((r) => (
                    <button
                      key={r}
                      onClick={() => selectedIds.length && setRole(selectedIds, r)}
                      className="block w-full text-left px-4 py-2 text-black/80 dark:text-white/65 hover:bg-black/10 dark:hover:bg-ink-800/60"
                      role="menuitem"
                    >
                      Set role: {r}
                    </button>
                  ))}
                </GlassCard>
              )}
            </div>
          </div>
        </GlassCard>

        {/* Charts & Status */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <GlassCard className="md:col-span-2 p-6">
            <div className="text-sm font-semibold mb-4 text-slate-800 dark:text-slate-200">
              Role distribution
            </div>
            <div className="flex items-center gap-6">
              <Donut segments={donutData} />
              <div className="grid grid-cols-2 gap-2">
                {donutData.map((s, i) => (
                  <div key={s.label + i} className="flex items-center gap-2">
                    <span
                      className="inline-block w-3 h-3 rounded-sm"
                      style={{
                        backgroundColor: [
                          "#2563eb",
                          "#10b981",
                          "#f59e0b",
                          "#8b5cf6",
                          "#ef4444",
                          "#14b8a6",
                        ][i % 6],
                      }}
                    />
                    <span className="text-sm text-slate-700 dark:text-slate-300">
                      {s.label}
                    </span>
                    <span className="ml-auto text-xs text-slate-500 dark:text-slate-400">
                      {s.value}
                    </span>
                  </div>
                ))}
                {donutData.length === 0 && (
                  <div className="text-sm text-slate-500 dark:text-slate-400">
                    No role data
                  </div>
                )}
              </div>
            </div>
          </GlassCard>

          <GlassCard className="p-6">
            <div className="text-sm font-semibold mb-3 text-slate-800 dark:text-slate-200">
              Status quick view
            </div>
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-slate-700 dark:text-slate-300">Active</span>
                <Badge color="green">{stats.active}</Badge>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-700 dark:text-slate-300">Inactive</span>
                <Badge color="red">{stats.inactive}</Badge>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-700 dark:text-slate-300">Tutors</span>
                <Badge color="purple">{stats.tutors}</Badge>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-700 dark:text-slate-300">Admins</span>
                <Badge color="blue">{stats.admins}</Badge>
              </div>
            </div>
          </GlassCard>
        </div>

        {/* Users table */}
        <GlassCard className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="bg-white/60 dark:bg-ink-800/60 text-left">
                <th className="px-4 py-3">
                  <input type="checkbox" checked={checkAll} onChange={onBulkToggle} />
                </th>
                {headerCell("Name", "fullName")}
                {headerCell("Email", "email")}
                {headerCell("Role", "role")}
                <th className="px-4 py-3 text-slate-700 dark:text-slate-200">Teach Badge</th>
                {headerCell("Active", "isActive")}
                <th className="px-4 py-3 text-slate-700 dark:text-slate-200">Actions</th>
              </tr>
            </thead>
            <tbody className="text-slate-800 dark:text-slate-200">
              {loading ? (
                <>
                  <SkeletonRow />
                  <SkeletonRow />
                  <SkeletonRow />
                </>
              ) : pageSlice.length ? (
                pageSlice.map((u) => (
                  <tr key={u.userId} className="border-t border-white/40 dark:border-white/10">
                    <td className="px-4 py-3">
                      <input
                        type="checkbox"
                        checked={!!checked[u.userId]}
                        onChange={(e) =>
                          setChecked((prev) => ({
                            ...prev,
                            [u.userId]: e.target.checked,
                          }))
                        }
                      />
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <div className={`w-9 h-9 rounded-full flex items-center justify-center ${avatarBg(u.fullName)}`}>
                          {(u.fullName?.[0] || "U").toUpperCase()}
                        </div>
                        <div className="font-medium">{u.fullName}</div>
                      </div>
                    </td>
                    <td className="px-4 py-3">{u.email}</td>
                    <td className="px-4 py-3">
                      <select
                        value={u.role}
                        onChange={(e) => setRole([u.userId], e.target.value)}
                        className="border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-slate-800 dark:text-slate-200 rounded px-2 py-1"
                      >
                        <option value="Learner">Learner</option>
                        <option value="Tutor">Tutor</option>
                        <option value="Admin">Admin</option>
                      </select>
                    </td>
                    <td className="px-4 py-3">
                      {u.readyToTeach ? (
                        <Badge color="purple">Tutor Badge</Badge>
                      ) : (
                        <span className="text-slate-400">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      {u.isActive ? <Badge color="green">Active</Badge> : <Badge color="red">Inactive</Badge>}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <MacDanger onClick={() => setActive([u.userId], !u.isActive)}
                        className={u.isActive ? "bg-red-600 hover:bg-red-700" : "bg-green-600 hover:bg-green-700"}>
                          {u.isActive ? "Deactivate" : "Activate"}
                        </MacDanger>
                        <MacButton onClick={() => setActiveUser(u)}>View</MacButton>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td
                    colSpan="7"
                    className="px-4 py-6 text-center text-slate-500 dark:text-slate-400"
                  >
                    No users found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </GlassCard>

        {/* Pagination */}
        <div className="flex items-center justify-between">
          <div className="text-sm text-slate-600 dark:text-slate-400">
            Page {page} of {totalPages}
          </div>
          <div className="flex items-center gap-2">
            <MacButton disabled={page === 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
              Prev
            </MacButton>
            <MacButton
              disabled={page === totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              Next
            </MacButton>
          </div>
        </div>
      </div>

      {/* Drawer */}
      {activeUser && (
        <div className="fixed inset-0 z-[80]">
          <div className="absolute inset-0 bg-black/40" onClick={() => setActiveUser(null)} />
          <div className="absolute right-0 top-0 h-full w-full max-w-md">
            <GlassCard className="h-full p-6 overflow-y-auto rounded-none">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">User Details</h2>
                <button
                  onClick={() => setActiveUser(null)}
                  className="text-slate-500 hover:text-slate-700 dark:hover:text-slate-300"
                >
                  ✕
                </button>
              </div>

              <div className="flex items-center gap-3 mb-4">
                <div
                  className={`w-12 h-12 rounded-full flex items-center justify-center ${avatarBg(
                    activeUser.fullName
                  )}`}
                >
                  {(activeUser.fullName?.[0] || "U").toUpperCase()}
                </div>
                <div>
                  <div className="font-semibold text-slate-900 dark:text-slate-100">
                    {activeUser.fullName}
                  </div>
                  <div className="text-sm text-slate-600 dark:text-slate-400">
                    {activeUser.email}
                  </div>
                </div>
              </div>

              <div className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-slate-500 dark:text-slate-400">Role</span>
                  <span className="font-medium text-slate-900 dark:text-slate-100">
                    {activeUser.role}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500 dark:text-slate-400">Tutor Badge</span>
                  <span className="font-medium text-slate-900 dark:text-slate-100">
                    {activeUser.readyToTeach ? "Yes" : "No"}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500 dark:text-slate-400">Status</span>
                  <span className="font-medium text-slate-900 dark:text-slate-100">
                    {activeUser.isActive ? "Active" : "Inactive"}
                  </span>
                </div>
                {activeUser.location && (
                  <div className="flex justify-between">
                    <span className="text-slate-500 dark:text-slate-400">Location</span>
                    <span className="font-medium text-slate-900 dark:text-slate-100">
                      {activeUser.location}
                    </span>
                  </div>
                )}
                {activeUser.createdAt && (
                  <div className="flex justify-between">
                    <span className="text-slate-500 dark:text-slate-400">Joined</span>
                    <span className="font-medium text-slate-900 dark:text-slate-100">
                      {formatDate(activeUser.createdAt)}
                    </span>
                  </div>
                )}
              </div>

              <div className="mt-6 grid grid-cols-2 gap-2">
                <MacDanger onClick={() => setActive([activeUser.userId], !activeUser.isActive)} className={activeUser.isActive ? "bg-red-600 hover:bg-red-700" : "bg-green-600 hover:bg-green-700"}>
                  {activeUser.isActive ? "Deactivate" : "Activate"}
                </MacDanger>
                <select
                  value={activeUser.role}
                  onChange={(e) => setRole([activeUser.userId], e.target.value)}
                  className="px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-slate-800 dark:text-slate-200"
                >
                  <option value="Learner">Learner</option>
                  <option value="Tutor">Tutor</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
            </GlassCard>
          </div>
        </div>
      )}

      {/* Dock Quick Actions */}
      <div className="fixed bottom-8 left-1/2 -translate-x-1/2 flex gap-3 px-4 py-3 rounded-2xl border border-white/40 dark:border-white/10 bg-white/70 dark:bg-ink-900/70 backdrop-blur-xl shadow z-30">
        <MacButton onClick={() => navigate("/request")}>+ Request</MacButton>
        <MacButton onClick={() => navigate("/skill")}>Skills</MacButton>
        <MacButton onClick={() => navigate("/VideoSession")}>Session</MacButton>
        <MacButton onClick={() => navigate("/profile")}>Profile</MacButton>
      </div>
    </div>
  );
};

export default AdminDashboard;
