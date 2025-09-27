import { useEffect, useState } from "react";
import api from "../api/axios";

export default function DiscoverPosts() {
  const [posts, setPosts] = useState([]);

  useEffect(() => {
    api.get("/tutor-posts").then(res => setPosts(res.data));
  }, []);

  const accept = async (postId) => {
    await api.post(`/tutor-posts/${postId}/accept/2`); // learnerId from auth
    alert("Accepted!");
  };

  return (
    <div className="p-6">
      <h2 className="text-xl font-semibold mb-4">Available Tutor Posts</h2>
      {posts.map(p => (
        <div key={p.postId} className="border p-4 mb-3 rounded">
          <h3 className="font-bold">{p.title}</h3>
          <p>{p.description}</p>
          <p>Status: {p.status}</p>
          <button disabled={p.status !== "Open"} onClick={()=>accept(p.postId)} className="mt-2 bg-green-600 text-white px-3 py-1 rounded">
            Accept
          </button>
        </div>
      ))}
    </div>
  );
}
