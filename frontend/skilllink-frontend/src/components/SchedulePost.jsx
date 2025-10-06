// src/components/SchedulePost.jsx
import { useState } from "react";
import { tutorPostsApi } from "../api";

export default function SchedulePost({ postId, onScheduled }) {
  const [date, setDate] = useState("");
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState("");

  const schedule = async () => {
    try {
      setMsg("");
      if (!date) {
        setMsg("Please choose a date/time.");
        return;
      }
      const iso = new Date(date).toISOString();
      if (Number.isNaN(new Date(date).getTime())) {
        setMsg("Invalid date.");
        return;
      }
      if (new Date(iso).getTime() < Date.now()) {
        setMsg("Scheduled time must be in the future.");
        return;
      }
      setBusy(true);
      await tutorPostsApi.schedule(`${postId}`, { scheduledAt: iso });
      setMsg("Session scheduled!");
      if (typeof onScheduled === "function") onScheduled();
    } catch (e) {
      setMsg(
        e?.response?.data?.message ||
          e?.message ||
          "Failed to schedule session."
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="p-4">
      {msg && (
        <div className="mb-3 text-sm border rounded px-3 py-2">{msg}</div>
      )}
      <input
        type="datetime-local"
        value={date}
        onChange={(e) => setDate(e.target.value)}
        className="border p-2 rounded"
      />
      <button
        disabled={busy}
        onClick={schedule}
        className={`ml-2 px-4 py-2 rounded text-white ${
          busy ? "bg-indigo-400" : "bg-indigo-600 hover:bg-indigo-700"
        }`}
      >
        {busy ? "Scheduling..." : "Schedule"}
      </button>
    </div>
  );
}
