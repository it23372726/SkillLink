import React, { useEffect, useMemo, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";

/* ---------- Small UI atoms (match your style) ---------- */
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
    className={
      "px-4 py-2 rounded-xl border text-sm transition " +
      "border-black/10 dark:border-white/10 " +
      "bg-white/60 hover:bg-white/80 dark:bg-ink-800/60 dark:hover:bg-ink-800/80 " +
      "text-black/80 dark:text-white/70 focus:outline-none focus:ring-1 " +
      "focus:ring-blue-400/30 " +
      className
    }
    {...props}
  >
    {children}
  </button>
);

const MacPrimary = ({ className = "", ...props }) => (
  <button
    className={
      "px-5 py-2.5 rounded-xl text-sm font-medium text-white " +
      "bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow " +
      "focus:outline-none focus:ring-2 focus:ring-blue-400/40 " +
      className
    }
    {...props}
  />
);

const Dot = ({ active, onClick }) => (
  <button
    onClick={onClick}
    aria-label="Go to slide"
    className={
      "w-2.5 h-2.5 rounded-full transition " +
      (active
        ? "bg-blue-600 dark:bg-blue-400"
        : "bg-slate-300 dark:bg-slate-600 hover:bg-slate-400 dark:hover:bg-slate-500")
    }
  />
);

const Blobs = () => (
  <>
    <div className="absolute -top-24 -left-20 -z-10 w-[520px] h-[520px] rounded-full bg-blue-300/30 blur-3xl dark:bg-blue-400/20" />
    <div className="absolute -bottom-24 -right-16 -z-10 w-[460px] h-[460px] rounded-full bg-indigo-300/25 blur-2xl dark:bg-indigo-400/20" />
  </>
);

/* ---------- Slides content ---------- */
const slides = [
  {
    title: "Learn & Teach — Effortlessly",
    subtitle:
      "SkillLink connects learners and tutors in minutes. Create requests, discover people, and start sessions without friction.",
    art: (
      <svg width="300" height="220" viewBox="0 0 300 220" className="drop-shadow">
        <defs>
          <linearGradient id="g1" x1="0" x2="1" y1="0" y2="1">
            <stop offset="0%" stopColor="#60a5fa" />
            <stop offset="100%" stopColor="#6366f1" />
          </linearGradient>
        </defs>
        <rect x="0" y="0" width="300" height="220" rx="20" fill="url(#g1)" opacity="0.9" />
        <g transform="translate(24,24)">
          <rect x="0" y="0" width="252" height="28" rx="8" fill="white" opacity="0.3" />
          <rect x="0" y="48" width="160" height="12" rx="6" fill="white" opacity="0.6" />
          <rect x="0" y="68" width="120" height="12" rx="6" fill="white" opacity="0.35" />
          <rect x="0" y="104" width="252" height="72" rx="12" fill="white" opacity="0.15" />

          <circle cx="224" cy="12" r="6" fill="#22c55e" />
        </g>
      </svg>
    ),
    bullets: ["Create a request in 1 minute", "Get matched with tutors", "Start instantly"],
  },
  {
    title: "Smart Matching & Live Sessions",
    subtitle:
      "We recommend tutors based on skills, level and availability. Schedule online or in-person — all inside SkillLink.",
    art: (
      <svg width="300" height="220" viewBox="0 0 300 220" className="drop-shadow">
        <defs>
          <linearGradient id="g2" x1="0" x2="1" y1="0" y2="1">
            <stop offset="0%" stopColor="#34d399" />
            <stop offset="100%" stopColor="#22d3ee" />
          </linearGradient>
        </defs>
        <rect x="0" y="0" width="300" height="220" rx="20" fill="url(#g2)" opacity="0.9" />
        <g transform="translate(28,24)">
          <rect x="0" y="0" width="244" height="40" rx="10" fill="white" opacity="0.25" />
          <rect x="0" y="56" width="120" height="12" rx="6" fill="white" opacity="0.6" />
          <rect x="0" y="76" width="92" height="12" rx="6" fill="white" opacity="0.35" />
          <rect x="0" y="112" width="110" height="72" rx="12" fill="white" opacity="0.15" />
          <rect x="134" y="112" width="110" height="72" rx="12" fill="white" opacity="0.15" />
          <circle cx="220" cy="20" r="8" fill="#f59e0b" />
        </g>
      </svg>
    ),
    bullets: ["Smart tutor recommendations", "Schedule with one click", "Online or in-person"],
  },
  {
    title: "Build Your Profile & Reputation",
    subtitle:
      "Earn a Tutor badge, showcase skills, and grow your network. Own your learning and teaching journey.",
    art: (
      <svg width="300" height="220" viewBox="0 0 300 220" className="drop-shadow">
        <defs>
          <linearGradient id="g3" x1="0" x2="1" y1="0" y2="1">
            <stop offset="0%" stopColor="#fb7185" />
            <stop offset="100%" stopColor="#a78bfa" />
          </linearGradient>
        </defs>
        <rect x="0" y="0" width="300" height="220" rx="20" fill="url(#g3)" opacity="0.9" />
        <g transform="translate(24,24)">
          <circle cx="36" cy="36" r="22" fill="white" opacity="0.8" />
          <rect x="72" y="22" width="160" height="12" rx="6" fill="white" opacity="0.6" />
          <rect x="72" y="42" width="120" height="12" rx="6" fill="white" opacity="0.35" />
          <rect x="0" y="84" width="252" height="72" rx="12" fill="white" opacity="0.15" />
          <rect x="0" y="164" width="180" height="12" rx="6" fill="white" opacity="0.35" />
        </g>
      </svg>
    ),
    bullets: ["Tutor badge & skills", "Beautiful profile", "Grow your network"],
  },
];

const Welcome = () => {
  const navigate = useNavigate();
  const [i, setI] = useState(0);

  // keyboard controls
  const onKey = useCallback((e) => {
    if (e.key === "ArrowRight") setI((x) => Math.min(slides.length - 1, x + 1));
    if (e.key === "ArrowLeft") setI((x) => Math.max(0, x - 1));
  }, []);
  useEffect(() => {
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onKey]);

  // smooth bg per slide
  const bg = useMemo(() => {
    if (i === 0) return "from-blue-600 to-indigo-600";
    if (i === 1) return "from-emerald-500 to-cyan-500";
    return "from-rose-500 to-violet-500";
  }, [i]);

  const next = () => setI((x) => Math.min(slides.length - 1, x + 1));
  const prev = () => setI((x) => Math.max(0, x - 1));

  return (
    <div className="relative min-h-screen font-sans overflow-hidden">
      {/* Thematic gradient */}
      <div className={`absolute inset-0 -z-10 bg-gradient-to-br ${bg}`} />
      <Blobs />

      {/* Top bar */}
      <GlassBar className="px-6 py-4 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto flex justify-between items-center">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-white/80 shadow" />
            <div className="text-slate-900 dark:text-slate-100 font-semibold">SkillLink</div>
          </div>

          <div className="flex gap-2">
            <MacButton onClick={() => navigate("/login")}>Login</MacButton>
            <MacPrimary onClick={() => navigate("/register")}>Register</MacPrimary>
          </div>
        </div>
      </GlassBar>

      {/* Center content */}
      <div className="max-w-6xl mx-auto px-6 py-10">
        {/* Skip controls */}
        <div className="flex justify-end mb-6">
          <button
            onClick={() => navigate("/login")}
            className="text-sm text-white/80 hover:text-white underline"
            title="Skip intro"
          >
            Skip
          </button>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 items-center">
          {/* Visual */}
          <div className="flex justify-center">
            <div className="scale-95 md:scale-100">{slides[i].art}</div>
          </div>

          {/* Text */}
          <div className="text-white">
            <h1 className="text-4xl md:text-5xl font-bold leading-tight drop-shadow">
              {slides[i].title}
            </h1>
            <p className="mt-3 text-white/90 md:text-lg">{slides[i].subtitle}</p>

            <ul className="mt-6 space-y-2">
              {slides[i].bullets.map((b, idx) => (
                <li key={idx} className="flex items-center gap-2">
                  <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-white/20">
                    ✓
                  </span>
                  <span className="text-white/95">{b}</span>
                </li>
              ))}
            </ul>

            {/* Controls */}
            <div className="mt-8 flex items-center gap-3 flex-wrap">
              {i > 0 ? (
                <MacButton onClick={prev} className="bg-white/40 text-white hover:bg-white/50">
                  ← Previous
                </MacButton>
              ) : (
                <div className="h-10" />
              )}

              {i < slides.length - 1 ? (
                <MacPrimary onClick={next}>Next →</MacPrimary>
              ) : (
                <>
                  <MacPrimary onClick={() => navigate("/register")}>Get Started</MacPrimary>
                  <MacButton onClick={() => navigate("/login")} className="bg-white/40 text-white">
                    I have an account
                  </MacButton>
                </>
              )}
            </div>

            {/* Progress Dots */}
            <div className="mt-6 flex items-center gap-2">
              {slides.map((_, idx) => (
                <Dot key={idx} active={idx === i} onClick={() => setI(idx)} />
              ))}
            </div>
          </div>
        </div>

        {/* Bottom helper */}
        <div className="mt-12 flex items-center justify-center text-white/80 text-sm">
          <span className=" animate-pulse">»</span>
          <span className="ml-2">Use arrow keys or Next</span>
        </div>
      </div>
    </div>
  );
};

export default Welcome;
