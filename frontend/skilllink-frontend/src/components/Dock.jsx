// src/components/Dock.jsx
import React, { useState } from "react";

const Dock = ({ children, peek = 16, className = "" }) => {
  const [open, setOpen] = useState(false);   // mobile tap toggle
  const [hover, setHover] = useState(false); // desktop hover
  const [focus, setFocus] = useState(false); // keyboard focus

  // Reveal when any of these is true
  const revealed = open || hover || focus;

  
  return (
    <div
      className="fixed bottom-0 left-1/2 -translate-x-1/2 z-[90]"
      // Hover for desktop
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
    >
      {/* Hover zone (easier to reveal without pixel-perfect aim) */}
      <div
        className="absolute -top-8 left-1/2 -translate-x-1/2 w-[460px] h-8 md:block hidden"
        aria-hidden="true"
      />

      {/* The sliding wrapper — use inline transform so 'peek' works at runtime */}
      <div
        className={`
          pb-[env(safe-area-inset-bottom)]
          transition-transform duration-300 ease-out
          ${className}
        `}
        style={{
          transform: revealed
            ? "translateY(0)"
            : `translateY(calc(100% - ${peek}px))`,
        }}
        // Keyboard accessibility: reveal when any child gets focus
        onFocusCapture={() => setFocus(true)}
        onBlurCapture={(e) => {
          if (!e.currentTarget.contains(e.relatedTarget)) setFocus(false);
        }}
      >
        <div
          className="
            mx-auto flex gap-3 px-4 py-3 rounded-2xl
            border border-black/20 dark:border-white/10
            bg-white/70 dark:bg-ink-900/70 backdrop-blur-xl shadow
            w-fit max-w-[90vw]
          "
          role="navigation"
          aria-label="Quick navigation dock"
        >
          {children}
        </div>

        {/* Grab handle — visible even when dock is hidden */}
        <div className="flex justify-center mt-2">
          <button
            type="button"
            onClick={() => setOpen((s) => !s)}
            className="
              h-1.5 w-14 rounded-full
              bg-slate-300/80 dark:bg-white/20
            "
            aria-label={open ? "Hide dock" : "Show dock"}
          />
        </div>
      </div>
    </div>
  );
};

export default Dock;
