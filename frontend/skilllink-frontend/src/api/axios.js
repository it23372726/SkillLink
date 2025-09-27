import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5159/api", // your ASP.NET backend URL
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 403) {
      // optionally show toast: "Forbidden"
    }
    if (err.response?.status === 401) {
      // logout & go to login
      // auth.logout(); navigate('/login');
    }
    return Promise.reject(err);
  }
);

export default api;

