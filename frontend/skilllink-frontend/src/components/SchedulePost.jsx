import { useState } from "react";
import api from "../api/axios";

export default function SchedulePost({ postId }) {
  const [date, setDate] = useState("");

  const schedule = async () => {
    await api.put(`/tutor-posts/${postId}/schedule`, JSON.stringify(date), {
      headers: { "Content-Type": "application/json" }
    });
    alert("Session scheduled!");
  };

  return (
    <div className="p-4">
      <input type="datetime-local" value={date} onChange={(e)=>setDate(e.target.value)} className="border p-2"/>
      <button onClick={schedule} className="ml-2 bg-indigo-600 text-white px-4 py-2 rounded">
        Schedule
      </button>
    </div>
  );
}
