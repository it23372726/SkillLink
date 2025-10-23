import http from "./http";

export const ratingsApi = {
    create: (payload) => http.post("/ratings", payload),
    existsForAccepted: (acceptedRequestId) =>
      http.get(`/ratings/exists/${acceptedRequestId}`),
    summaryForTutor: (tutorId) => http.get(`/ratings/summary/${tutorId}`),
    listReceived: (params = {}) => http.get("/ratings/received", { params }),
    listGiven:    (params = {}) => http.get("/ratings/given",    { params }),
  };
