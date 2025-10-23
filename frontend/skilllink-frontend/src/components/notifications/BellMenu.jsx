import { useEffect, useRef } from "react";
import { MacButton, MacPrimary } from "../UI";

/* ========================== Notification Bell ========================== */
export const Bell = ({ count = 0, onClick, title = "Notifications" }) => (
    <button
      onClick={onClick}
      className="relative w-10 h-10 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white flex items-center justify-center focus:outline-none hover:opacity-90"
      aria-label={title}
      title={title}
    >
      <i className="fas fa-bell" />
      {count > 0 && (
        <span className="absolute -top-1 -right-1 inline-flex items-center justify-center rounded-full min-w-[18px] h-[18px] px-1 text-[11px] font-semibold bg-red-600 text-white">
          {count > 99 ? "99+" : count}
        </span>
      )}
    </button>
  );
  
  export const NotifDropdown = ({
    open,
    anchorRef,
    items,
    loading,
    onRefresh,
    onMarkAll,
    onClearAll,
    onMarkRead,
    onView,
  }) => {
    const dropRef = useRef(null);
  
    useEffect(() => {
      if (!open) return;
      const onEsc = (e) => e.key === "Escape" && anchorRef.current?.click();
      const onClick = (e) => {
        if (
          dropRef.current &&
          !dropRef.current.contains(e.target) &&
          anchorRef.current &&
          !anchorRef.current.contains(e.target)
        ) {
          anchorRef.current?.click();
        }
      };
      document.addEventListener("keydown", onEsc);
      document.addEventListener("mousedown", onClick);
      return () => {
        document.removeEventListener("keydown", onEsc);
        document.removeEventListener("mousedown", onClick);
      };
    }, [open, anchorRef]);
  
    if (!open) return null;
  
    return (
      <div
        ref={dropRef}
        className="absolute right-0 mt-2 w-[380px] max-w-[92vw] rounded-2xl border border-white/40 dark:border-white/10 bg-white/80 dark:bg-slate-900/80 backdrop-blur-xl shadow-xl z-50"
      >
        {/* header actions */}
        <div className="flex items-center justify-between gap-2 px-3 py-2 border-b border-white/40 dark:border-white/10">
          <div className="font-medium text-slate-800 dark:text-slate-100 text-sm">
            Notifications
          </div>
          <div className="flex items-center gap-1">
            <button
              className="text-xs px-2 py-1 rounded-lg hover:bg-black/5 dark:hover:bg-white/10 text-slate-600 dark:text-slate-300"
              onClick={onRefresh}
              title="Refresh"
            >
              <i className="fas fa-rotate" />
            </button>
            <button
              className="text-xs px-2 py-1 rounded-lg hover:bg-black/5 dark:hover:bg-white/10 text-slate-600 dark:text-slate-300"
              onClick={onMarkAll}
              title="Mark all read"
            >
              Mark all
            </button>
            <button
              className="text-xs px-2 py-1 rounded-lg hover:bg-red-50/70 dark:hover:bg-red-400/10 text-red-600"
              onClick={onClearAll}
              title="Clear all"
            >
              Clear
            </button>
          </div>
        </div>
  
        {/* list */}
        <div className="max-h-[60vh] overflow-auto">
          {loading ? (
            <div className="p-4 text-sm text-slate-600 dark:text-slate-300">
              Loading…
            </div>
          ) : items.length === 0 ? (
            <div className="p-4 text-sm text-slate-600 dark:text-slate-300">
              No notifications.
            </div>
          ) : (
            <ul className="divide-y divide-white/40 dark:divide-white/10">
              {items.map((n) => (
                <li
                  key={n.notificationId}
                  className={`px-3 py-3 ${
                    !n.isRead ? "bg-blue-50/40 dark:bg-blue-900/10" : ""
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <div className="mt-0.5 w-8 h-8 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-800 dark:text-white flex items-center justify-center shrink-0">
                      <i className="fas fa-bell" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="text-sm font-medium text-slate-900 dark:text-slate-100 line-clamp-2">
                        {n.title}
                        {!n.isRead && (
                          <span className="ml-2 text-[10px] px-1.5 py-0.5 rounded bg-blue-600 text-white align-middle">
                            new
                          </span>
                        )}
                      </div>
                      {n.body && (
                        <div className="text-sm text-slate-700 dark:text-slate-200 mt-0.5 line-clamp-3">
                          {n.body}
                        </div>
                      )}
                      <div className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                        {new Date(n.createdAt).toLocaleString()}
                      </div>
                      <div className="mt-2 flex items-center gap-2">
                        {n.link && (
                          <MacPrimary
                            onClick={() => onView(n)}
                            className="!px-3 !py-1 text-xs"
                          >
                            View
                          </MacPrimary>
                        )}
                        {!n.isRead && (
                          <MacButton
                            onClick={() => onMarkRead(n)}
                            className="!px-3 !py-1 text-xs"
                          >
                            Mark read
                          </MacButton>
                        )}
                      </div>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    );
  };
  