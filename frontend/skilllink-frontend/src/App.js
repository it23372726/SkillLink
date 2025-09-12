import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ProtectedRoute, AdminRoute } from "./components/route-guards";

import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import VideoSession from "./pages/VideoSession";
import UserProfile from "./pages/UserProfile";
import SkillsManagement from "./pages/SkillsManagement";
import RequestsPage from "./pages/RequestPage";
import RegisterPage from "./pages/RegisterPage";
import AdminDashboard from "./pages/AdminDashboard"; // create a placeholder
import { useEffect, useState } from "react";
import "@fortawesome/fontawesome-free/css/all.min.css";

function App() {

  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public */}
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Admin-only */}
          <Route element={<AdminRoute />}>
            <Route path="/admin-dashboard" element={<AdminDashboard />} />
          </Route>

          {/* Authenticated-only */}
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/VideoSession" element={<VideoSession />} />
            <Route path="/skill" element={<SkillsManagement />} />
            <Route path="/profile" element={<UserProfile />} />
            <Route path="/request" element={<RequestsPage />} />
          </Route>

          {/* Default redirect */}
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
