// src/components/routes/RequireAdmin.jsx
import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

const RequireAdmin = () => {
  const { user, loading } = useAuth(); // assume you track loading during bootstrap

  if (loading) return null; // or a spinner

  if (!user || user.role !== 'Admin') {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
};

export default RequireAdmin;
