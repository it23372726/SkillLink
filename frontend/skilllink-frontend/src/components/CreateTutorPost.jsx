import { useState } from "react";
import api from "../api/axios";

export default function CreateTutorPost() {
  const [title, setTitle] = useState("");
  const [desc, setDesc] = useState("");
  const [max, setMax] = useState(5);

  const handleSubmit = async (e) => {
    e.preventDefault();
    await api.post("/tutor-posts", {
      tutorId: 1, // get from auth context
      title,
      description: desc,
      maxParticipants: max,
    });
    alert("Post created!");
  };

  return (
    <form onSubmit={handleSubmit} className="p-6 max-w-md mx-auto">
      <h2 className="text-xl font-semibold mb-4">Create Tutor Post</h2>
      <input className="w-full border p-2 mb-2" value={title} onChange={(e)=>setTitle(e.target.value)} placeholder="Title"/>
      <textarea className="w-full border p-2 mb-2" value={desc} onChange={(e)=>setDesc(e.target.value)} placeholder="Description"/>
      <input type="number" className="w-full border p-2 mb-2" value={max} onChange={(e)=>setMax(e.target.value)} />
      <button type="submit" className="bg-blue-600 text-white px-4 py-2 rounded">Create</button>
    </form>
  );
}
