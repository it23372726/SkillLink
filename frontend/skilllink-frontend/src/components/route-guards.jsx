// src/routes/ProtectedRoute.jsx
import React from "react";
import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

const Spinner = () => (
  <div className="min-h-screen flex items-center justify-center">
    <div className="animate-spin h-10 w-10 rounded-full border-t-2 border-b-2 border-blue-500" />
  </div>
);

export const ProtectedRoute = () => {
  const { user, loading } = useAuth();
  if (loading) return <Spinner />;
  return user ? <Outlet /> : <Navigate to="/login" replace />;
};

export const AdminRoute = () => {
  const { user, loading } = useAuth();
  if (loading) return <Spinner />;
  if (!user) return <Navigate to="/login" replace />;

  const role = (user.role || "").toString().toUpperCase();
  return role === "ADMIN" ? <Outlet /> : <Navigate to="/dashboard" replace />;
};
