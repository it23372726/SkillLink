// src/api/http.js
import axios from "axios";

// Resolve API base (prod-ready):
// 1) REACT_APP_API_BASE (build-time)
// 2) window.__SL_API_BASE (runtime injection)
// 3) fallback to http://localhost:5159


// export const API_BASE =
//   process.env.REACT_APP_API_BASE ||
//   (typeof window !== "undefined" && window.__SL_API_BASE) ||
//   "http://localhost:5159";

  export const API_BASE = process.env.REACT_APP_API_BASE || (typeof window !== "undefined" && window.__SL_API_BASE) ||"http://localhost:5159";
  console.log("env : ", process.env.REACT_APP_API_BASE);
  console.log("base url : ", API_BASE);

const http = axios.create({
  baseURL: `${API_BASE}/api`,
  withCredentials: false,
});

// Allow AuthContext to register a logout handler
let onUnauthorized = null;
export const registerOnUnauthorized = (fn) => {
  onUnauthorized = fn;
};

// Attach token automatically
http.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Handle 401/403 globally
http.interceptors.response.use(
  (res) => res,
  (err) => {
    const status = err?.response?.status;
    if (status === 401 && typeof onUnauthorized === "function") {
      try { onUnauthorized(); } catch { /* ignore */ }
    }
    // You can also toast on 403 if you like
    return Promise.reject(err);
  }
);

export default http;
