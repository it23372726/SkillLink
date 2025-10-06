// src/utils/image.js
import { API_BASE } from "../api/http";

export const toImageUrl = (path) => {
  if (!path) return "";
  return path.startsWith("http") ? path : `${API_BASE}${path}`;
};
