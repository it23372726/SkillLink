// src/context/AuthContext.js
import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  useCallback,
} from "react";
import { http } from "../api";

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null); // { userId, fullName, email, role }
  const [loading, setLoading] = useState(true);

  // Keep Authorization header in sync (stable function)
  const setAuthHeader = useCallback((token) => {
    if (token) {
      http.defaults.headers.common["Authorization"] = `Bearer ${token}`;
    } else {
      delete http.defaults.headers.common["Authorization"];
    }
  }, []);

  // Init session on first load
  useEffect(() => {
    const init = async () => {
      const token = localStorage.getItem("token");
      setAuthHeader(token);
      if (!token) {
        setLoading(false);
        return;
      }
      try {
        const res = await http.get("/auth/me");
        setUser(res.data || null);
      } catch (err) {
        localStorage.removeItem("token");
        setAuthHeader(null);
        setUser(null);
      } finally {
        setLoading(false);
      }
    };
    init();
  }, [setAuthHeader]);

  // Stable login function
  const login = useCallback(
    async (email, password) => {
      // Adjust based on backend structure
      const res = await http.post("/auth/login", { email, password });
      const token = res?.data?.token || res?.data?.accessToken;
      if (!token) throw new Error("Login response did not include a token.");

      localStorage.setItem("token", token);
      setAuthHeader(token);

      const me = await http.get("/auth/me");
      setUser(me.data || null);
      return me.data;
    },
    [setAuthHeader]
  );

  // Stable logout function
  const logout = useCallback(async () => {
    try {
      // optional: await api.post("/auth/logout");
    } catch {
      // ignore network errors for logout
    } finally {
      localStorage.removeItem("token");
      setAuthHeader(null);
      setUser(null);
    }
  }, [setAuthHeader]);

  // Provide a stable context value and satisfy eslint deps
  const value = useMemo(
    () => ({ user, setUser, login, logout, loading }),
    [user, loading, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => useContext(AuthContext);
