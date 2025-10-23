// src/api/client.js
import http from "./http";

/* ----------------- Helpers ----------------- */
export const parseError = (err, fallback = "Unexpected error") => {
  return err?.response?.data?.message || err?.message || fallback;
};

/* ----------------- Public Profile ----------------- */
export const usersApi = {
  // Backend: GET /api/auth/users/{id}/public
  publicProfile: (userId) => http.get(`/auth/users/${userId}/public`),
};

export const socialApi = {
  followersOf: (userId) => http.get(`/friends/${userId}/followers`),
  followingOf: (userId) => http.get(`/friends/${userId}/following`),
};

/* ----------------- Auth ----------------- */
export const authApi = {
  login: (email, password) => http.post("/auth/login", { email, password }),
  register: (body) => http.post("/auth/register", body),
  me: () => http.get("/auth/me"),
  getProfile: () => http.get("/auth/profile"),
  updateProfile: (payload) => http.put("/auth/profile", payload),
  setTeachMode: (readyToTeach) => http.put("/auth/teach-mode", { readyToTeach }),
  uploadPhoto: (file) => {
    const fd = new FormData();
    fd.append("profilePicture", file);
    return http.put("/auth/profile/photo", fd);
  },
  removePhoto: () => http.delete("/auth/profile/photo"),
  setActive: (isActive) => http.put("/auth/active", { isActive }),
};

/* ----------------- Admin ----------------- */
export const adminApi = {
  listUsers: (q) => http.get(`/admin/users`, { params: q ? { q } : {} }),
  setActive: (userId, isActive) =>
    http.put(`/admin/users/${userId}/active`, { isActive }),
  setRole: (userId, role) => http.put(`/admin/users/${userId}/role`, { role }),
  deleteUser: (userId) => http.delete(`/auth/users/${userId}`),
  topSkills:(params) =>
    http.get("/admin/reports/skill-demand", { params }),
  feedbackList: (params = {}) =>
    http.get("/feedback", { params }), // { isRead: true|false|null, limit, offset }
  feedbackMarkRead: (id, isRead = true) =>
    http.put(`/feedback/${id}/read`, null, { params: { isRead } }),
  
};

/* ----------------- Feed ----------------- */
export const feedApi = {
  list: ({ page = 1, pageSize = 8, q = "" } = {}) =>
    http.get("/feed", {
      params: { page, pageSize, ...(q ? { q } : {}) },
    }),
  like: (postType, postId) => http.post(`/feed/${postType}/${postId}/like`),
  dislike: (postType, postId) => http.post(`/feed/${postType}/${postId}/dislike`),
  removeReaction: (postType, postId) =>
    http.delete(`/feed/${postType}/${postId}/reaction`),

  listComments: (postType, postId, { limit, sort } = {}) =>
    http.get(`/feed/${postType}/${postId}/comments`, {
      params: { ...(limit ? { limit } : {}), ...(sort ? { sort } : {}) },
    }),
  addComment: (postType, postId, content) =>
    http.post(`/feed/${postType}/${postId}/comments`, { content }),
  deleteComment: (commentId) => http.delete(`/feed/comments/${commentId}`),
  sort: (data) => http.post('/feed/sort', data),
};

/* ----------------- Requests ----------------- */
export const requestsApi = {
  list: () => http.get("/requests"),
  create: (payload) => http.post("/requests", payload),
  update: (id, payload) => http.put(`/requests/${id}`, payload),
  remove: (id) => http.delete(`/requests/${id}`),
  accept: (id) => http.post(`/requests/${id}/accept`),
  acceptedStatus: (id) => http.get(`/requests/${id}/accepted-status`),
  listAcceptedByMe: () => http.get("/requests/accepted"),
  listAcceptedAsRequester: () => http.get("/requests/accepted/requester"),
  scheduleAccepted: (acceptedRequestId, payload) =>
    http.post(`/requests/accepted/${acceptedRequestId}/schedule`, payload),
  listAcceptedForMyRequests: () => http.get("/requests/accepted/requester"),
  declineDirected: (id) => http.post(`/requests/${id}/decline-directed`),
  decline: (id) => http.post(`/requests/${id}/decline`),
  finishAccepted: (acceptedRequestId) =>
    http.post(`/requests/accepted/${acceptedRequestId}/complete`),
};

/* ----------------- Tutor Posts ----------------- */
export const tutorPostsApi = {
  list: () => http.get("/tutor-posts"),
  create: (payload) => http.post("/tutor-posts", payload),
  update: (postId, payload) => http.put(`/tutor-posts/${postId}`, payload),
  remove: (postId) => http.delete(`/tutor-posts/${postId}`),
  uploadImage: (postId, file) => {
    const fd = new FormData();
    fd.append("file", file);
    return http.post(`/tutor-posts/${postId}/image`, fd, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },
  schedule: (postId, body) =>
    http.put(`/tutor-posts/${postId}/schedule`,  body),
  accept: (postId) => http.post(`/tutor-posts/${postId}/accept`),
  acceptedStatus: (postId) => http.get(`/tutor-posts/${postId}/accepted-status`),
  acceptedStatusMany: (ids) =>
    http.get(`/tutor-posts/accepted-status`, { params: { ids: ids.join(",") } }),
};

/* ----------------- Skills ----------------- */
export const skillsApi = {
  byUser: (userId) => http.get(`/skills/user/${userId}`),
  add: (userId, skillName, level) =>
    http.post(`/skills/add`, { userId, skillName, level }),
  remove: (userId, skillId) => http.delete(`/skills/${userId}/${skillId}`),
  suggest: (q) => http.get(`/skills/suggest`, { params: { q } }),
  filterBySkill: (name) => http.get(`/skills/filter`, { params: { skill: name } }),
};

/* ----------------- Friends ----------------- */
export const friendsApi = {
  my: () => http.get(`/friends/my`),
  followers: () => http.get(`/friends/followers`),
  search: (q) => http.get(`/friends/search`, { params: { q } }),
  follow: (userId) => http.post(`/friends/${userId}/follow`),
  unfollow: (userId) => http.delete(`/friends/${userId}/unfollow`),
};

/* ----------------- Notification ----------------- */
export const notificationsApi = {
  list: () => http.get("/notifications"),
  markRead: (id) => http.post(`/notifications/${id}/read`),
  markAllRead: () => http.post(`/notifications/read-all`),
};


// Assuming you already have an axios instance named `http`
/* ----------------- Feedback ----------------- */
export const feedbackApi = {
  submit(payload) {
    // { subject, message, page, userAgent }
    return http.post("/feedback", payload);
  },
};

