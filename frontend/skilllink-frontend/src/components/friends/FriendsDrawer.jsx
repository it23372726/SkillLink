// src/components/friends/FriendsDrawer.jsx
import React, { useEffect, useMemo, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import api from "../../api/axios";
import { GlassCard, MacButton, MacPrimary, Input } from "../UI";
import { toImageUrl } from "../../api/base";

/* ====== small utils ====== */
const debounce = (fn, ms = 350) => {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), ms);
  };
};

// highlight match in suggestions
const Highlight = ({ text, q }) => {
  if (!q) return <>{text}</>;
  const idx = text.toLowerCase().indexOf(q.toLowerCase());
  if (idx === -1) return <>{text}</>;
  const before = text.slice(0, idx);
  const match = text.slice(idx, idx + q.length);
  const after = text.slice(idx + q.length);
  return (
    <>
      {before}
      <mark className="bg-yellow-200/70 dark:bg-yellow-400/20 rounded px-0.5">
        {match}
      </mark>
      {after}
    </>
  );
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

export default function FriendsDrawer({ className = "" }) {
  const { user } = useAuth();

  const [friends, setFriends] = useState([]);
  const [query, setQuery] = useState("");
  const [sugg, setSugg] = useState([]);
  const [loadingFriends, setLoadingFriends] = useState(false);
  const [loadingSuggest, setLoadingSuggest] = useState(false);
  const [err, setErr] = useState("");

  const loadFriends = async () => {
    try {
      setLoadingFriends(true);
      const res = await api.get("/friends/my");
      setFriends(res.data || []);
    } catch (e) {
      setErr("Failed to load friends");
    } finally {
      setLoadingFriends(false);
    }
  };

  useEffect(() => {
    if (user?.userId) loadFriends();
  }, [user?.userId]);

  const doSuggest = useMemo(
    () =>
      debounce(async (q) => {
        if (!q?.trim()) {
          setSugg([]);
          return;
        }
        try {
          setLoadingSuggest(true);
          const res = await api.get(`/friends/search?q=${encodeURIComponent(q)}`);
          setSugg(res.data || []);
        } catch {
          // ignore
        } finally {
          setLoadingSuggest(false);
        }
      }, 350),
    []
  );

  useEffect(() => {
    doSuggest(query);
  }, [query, doSuggest]);

  const follow = async (id) => {
    try {
      await api.post(`/friends/${id}/follow`);
      await loadFriends(); // refresh list immediately
      setSugg((prev) => prev.filter((u) => u.userId !== id));
    } catch {
      // optionally show toast
    }
  };

  const unfollow = async (id) => {
    try {
      await api.delete(`/friends/${id}/unfollow`);
      await loadFriends();
    } catch {}
  };

  const friendSet = useMemo(
    () => new Set(friends.map((f) => f.userId)),
    [friends]
  );

  return (
    <div className={`pointer-events-auto ${className}`}>
      {/* outer container is sized/positioned by parent (fixed + top/right + height) */}
      <GlassCard className="h-full bg-transparent dark:bg-transparent border-0 shadow-none backdrop-blur-none rounded-2xl p-0 flex flex-col">
        {/* Header */}
        <div className="px-4 py-3 border-b border-slate-200/60 dark:border-slate-700/60 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="font-semibold text-slate-800 dark:text-slate-100">
              Friends
            </div>
          </div>
        </div>

        {/* Search */}
        <div className="p-4 border-b border-slate-200/60 dark:border-slate-700/60">
          <Input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search name or email…"
          />

          {/* Suggestions */}
          {query.trim() && (
            <div className="mt-2 max-h-56 overflow-auto space-y-2">
              {loadingSuggest ? (
                <div className="text-sm text-slate-500 dark:text-slate-400">
                  Searching…
                </div>
              ) : sugg.length === 0 ? (
                <div className="text-sm text-slate-500 dark:text-slate-400">
                  No matches
                </div>
              ) : (
                sugg.map((u) => (
                  <div
                    key={u.userId}
                    className="flex items-center justify-between px-2 py-2 rounded-lg hover:bg-black/5 dark:hover:bg-white/10 transition"
                  >
                    <div className="flex items-center gap-2">
                      <Avatar name={u.fullName} imageUrl={u.profilePicture} />
                      <div className="leading-tight">
                        <div className="font-medium text-slate-800 dark:text-slate-100">
                          <Highlight text={u.fullName} q={query} />
                        </div>
                        <div className="text-xs text-slate-500 dark:text-slate-400">
                          <Highlight text={u.email} q={query} />
                        </div>
                      </div>
                    </div>
                    {friendSet.has(u.userId) ? (
                      <MacButton size="sm" onClick={() => unfollow(u.userId)}>
                        Unfollow
                      </MacButton>
                    ) : (
                      <MacPrimary size="sm" onClick={() => follow(u.userId)}>
                        Follow
                      </MacPrimary>
                    )}
                  </div>
                ))
              )}
            </div>
          )}
        </div>

        {/* Friends List */}
        <div className="flex-1 overflow-auto p-4 space-y-2">
          <div className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400 mb-2">
            My Friends
          </div>

          {loadingFriends ? (
            <div className="space-y-2">
              {[...Array(4)].map((_, i) => (
                <div
                  key={i}
                  className="h-12 rounded-lg bg-slate-200/60 dark:bg-slate-800/60 animate-pulse"
                />
              ))}
            </div>
          ) : friends.length === 0 ? (
            <GlassCard className="p-4 text-sm text-slate-600 dark:text-slate-300">
              You’re not following anyone yet. Use the search above to find people.
            </GlassCard>
          ) : (
            friends.map((f) => (
              <GlassCard key={f.userId} className="p-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Avatar name={f.fullName} imageUrl={f.profilePicture} />
                  <div className="leading-tight">
                    <div className="font-medium text-slate-800 dark:text-slate-100">
                      {f.fullName}
                    </div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">
                      {f.email}
                    </div>
                  </div>
                </div>
                <MacButton size="sm" onClick={() => unfollow(f.userId)}>
                  Unfollow
                </MacButton>
              </GlassCard>
            ))
          )}
        </div>

        {/* Footer */}
        {err && (
          <div className="p-3 text-xs text-red-600 dark:text-red-400 border-t border-slate-200/60 dark:border-slate-700/60">
            {err}
          </div>
        )}
      </GlassCard>
    </div>
  );
}
