// src/pages/DiscoverPosts.jsx
import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { tutorPostsApi } from "../api";

export default function DiscoverPosts() {
  const { user } = useAuth();
  const [posts, setPosts] = useState([]);
  const [busyId, setBusyId] = useState(null);
  const [msg, setMsg] = useState("");

  const load = async () => {
    try {
      const res = await tutorPostsApi.list("/tutor-posts");
      setPosts(res.data || []);
    } catch (e) {
      setMsg(
        e?.response?.data?.message ||
          e?.message ||
          "Failed to load tutor posts."
      );
    }
  };

  useEffect(() => {
    load();
  }, []);

  const accept = async (postId) => {
    if (!user) {
      setMsg("Please log in to accept.");
      return;
    }
    setMsg("");
    setBusyId(postId);
    try {
      // Backend should read learnerId from JWT — no learnerId in path/body
      await tutorPostsApi.accept(`${postId}`);
      setMsg("Accepted the lesson successfully!");
      await load();
    } catch (e) {
      setMsg(
        e?.response?.data?.message ||
          e?.message ||
          "Failed to accept the lesson."
      );
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="p-6">
      <h2 className="text-xl font-semibold mb-4">Available Tutor Posts</h2>

      {msg && (
        <div className="mb-4 rounded border px-3 py-2 text-sm">
          {msg}
        </div>
      )}

      {posts.length === 0 ? (
        <div className="text-slate-600">No posts found.</div>
      ) : (
        posts.map((p) => {
          const canAccept = p.status === "Open";
          return (
            <div key={p.postId} className="border p-4 mb-3 rounded">
              <h3 className="font-bold">{p.title}</h3>
              {p.imageUrl && (
                <img
                  src={p.imageUrl}
                  alt={p.title}
                  className="mt-2 h-40 w-full object-cover rounded"
                />
              )}
              <p className="mt-2">{p.description}</p>
              <p className="mt-1 text-sm">Status: {p.status}</p>
              <button
                disabled={!canAccept || busyId === p.postId}
                onClick={() => accept(p.postId)}
                className={`mt-2 px-3 py-1 rounded text-white ${
                  canAccept ? "bg-green-600 hover:bg-green-700" : "bg-gray-400"
                } ${busyId === p.postId ? "opacity-70 cursor-not-allowed" : ""}`}
              >
                {busyId === p.postId ? "Processing..." : "Accept"}
              </button>
            </div>
          );
        })
      )}
    </div>
  );
}
