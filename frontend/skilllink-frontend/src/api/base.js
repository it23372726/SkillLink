// src/utils/image.js
export const API_BASE = process.env.REACT_APP_API_BASE || 'http://localhost:5159';

export const toImageUrl = (path) => {
  if (!path) return '';
  return path.startsWith('http') ? path : `${API_BASE}${path}`;
};
