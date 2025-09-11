import React, { useMemo, useRef, useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../api/axios";

// --- Apple/mac style UI helpers ---
const GlassCard = ({ children, className = "" }) => (
  <div
    className={
      "rounded-2xl border border-black/10 dark:border-white/10 " +
      "bg-white/60 dark:bg-ink-900/60 backdrop-blur-xl shadow-xl " +
      "transition-all " +
      className
    }
  >
    {children}
  </div>
);

const MacButton = ({ children, className = "", ...props }) => (
  <button
    {...props}
    className={
      "px-4 py-2 rounded-xl border text-sm transition " +
      "border-black/10 dark:border-white/10 " +
      "bg-white/70 hover:bg-black/5 dark:bg-ink-800/70 dark:hover:bg-ink-700/80 " +
      "focus:outline-none focus:ring-1 focus:ring-blue-400/30 " +
      "text-black/80 dark:text-white/70 active:text-black dark:active:text-white " +
      className
    }
  >
    {children}
  </button>
);

const MacPrimary = ({ children, className = "", ...props }) => (
  <button
    {...props}
    className={
      "w-full px-4 py-2.5 rounded-xl text-sm font-medium text-white transition " +
      "bg-blue-600 hover:bg-blue-700 active:bg-blue-800 " +
      "focus:outline-none focus:ring-2 focus:ring-blue-400/40 " +
      className
    }
  >
    {children}
  </button>
);

const RegisterPage = () => {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: "",
    confirmPassword: "",
    role: "Learner",
    profilePicture: null,
  });

  const [errors, setErrors] = useState({});
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [previewUrl, setPreviewUrl] = useState("");
  const [pwVisible, setPwVisible] = useState(false);
  const [cpwVisible, setCpwVisible] = useState(false);

  const dropRef = useRef(null);

  // handle inputs
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) setErrors((prev) => ({ ...prev, [name]: "" }));
  };

  // password strength
  const pwStrength = useMemo(() => {
    const pw = formData.password || "";
    let score = 0;
    if (pw.length >= 6) score++;
    if (pw.length >= 10) score++;
    if (/[A-Z]/.test(pw)) score++;
    if (/[a-z]/.test(pw)) score++;
    if (/\d/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    return Math.min(score, 5);
  }, [formData.password]);

  const strengthLabel = ["Very weak", "Weak", "Okay", "Good", "Strong", "Strong"][pwStrength];
  const strengthBarClass = [
    "bg-red-400",
    "bg-orange-400",
    "bg-yellow-400",
    "bg-lime-500",
    "bg-green-600",
    "bg-green-600",
  ][pwStrength];

  // file handling
  const setProfileFile = (file) => {
    if (!file) return;
    if (!file.type.startsWith("image/")) {
      setMessage("Please upload a valid image.");
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      setMessage("Image is too large. Max 2MB.");
      return;
    }
    setFormData((prev) => ({ ...prev, profilePicture: file }));
  };
  const onFileChange = (e) => {
    const f = e.target.files?.[0];
    if (f) setProfileFile(f);
  };

  // drag & drop
  useEffect(() => {
    const el = dropRef.current;
    if (!el) return;
    const prevent = (e) => {
      e.preventDefault();
      e.stopPropagation();
    };
    const onDrop = (e) => {
      prevent(e);
      const f = e.dataTransfer.files?.[0];
      if (f) setProfileFile(f);
    };
    ["dragenter", "dragover", "dragleave", "drop"].forEach((ev) =>
      el.addEventListener(ev, prevent)
    );
    el.addEventListener("drop", onDrop);
    return () => {
      ["dragenter", "dragover", "dragleave", "drop"].forEach((ev) =>
        el.removeEventListener(ev, prevent)
      );
      el.removeEventListener("drop", onDrop);
    };
  }, []);

  // preview
  useEffect(() => {
    if (formData.profilePicture) {
      const url = URL.createObjectURL(formData.profilePicture);
      setPreviewUrl(url);
      return () => URL.revokeObjectURL(url);
    } else setPreviewUrl("");
  }, [formData.profilePicture]);

  // validation
  const validateForm = () => {
    const newErrors = {};
    if (!formData.fullName.trim()) newErrors.fullName = "Full name is required";
    if (!formData.email.trim()) newErrors.email = "Email is required";
    else if (!/\S+@\S+\.\S+/.test(formData.email)) newErrors.email = "Email is invalid";
    if (!formData.password) newErrors.password = "Password is required";
    else if (formData.password.length < 6)
      newErrors.password = "Password must be at least 6 characters";
    if (formData.password !== formData.confirmPassword)
      newErrors.confirmPassword = "Passwords do not match";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // submit
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;
    setIsLoading(true);
    setMessage("");
    try {
      const data = new FormData();
      data.append("fullName", formData.fullName);
      data.append("email", formData.email);
      data.append("password", formData.password);
      data.append("role", "Learner");
      if (formData.profilePicture) {
        data.append("profilePicture", formData.profilePicture);
      }
      const response = await api.post("/auth/register", data, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      if (response.status === 200) {
        setMessage("Registration successful! Redirecting…");
        setTimeout(() => navigate("/login"), 1500);
      }
    } catch (err) {
      console.error("Registration error:", err);
      const apiMessage = err.response?.data?.message;
      if (err.response?.status === 409) setMessage(apiMessage || "Email already exists.");
      else if (err.response?.status === 400)
        setMessage(apiMessage || "Full name, email and password are required.");
      else setMessage(apiMessage || "Registration failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative min-h-screen flex items-center justify-center">
      {/* Background gradient */}
      <div className="absolute inset-0 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-ink-900 dark:via-ink-900 dark:to-ink-800" />
      {/* Decorative circles */}
      <div className="absolute -top-24 -left-24 w-96 h-96 rounded-full bg-blue-300/20 blur-3xl dark:bg-blue-400/10" />
      <div className="absolute bottom-0 right-0 w-[28rem] h-[28rem] rounded-full bg-indigo-300/20 blur-3xl dark:bg-indigo-400/10" />

      <div className="relative z-10 w-full max-w-5xl px-6">
        <GlassCard className="p-8 sm:p-10">
          <div className="flex items-center justify-center gap-2 mb-6">
            <span className="text-3xl">🍎</span>
            <h1 className="text-3xl font-extrabold text-ink-900 dark:text-ink-100">
              Join SkillLink
            </h1>
          </div>
          <p className="text-center text-sm text-ink-600 dark:text-ink-400 mb-8">
            Create your account — you can enable <span className="font-semibold">Tutor Mode</span>{" "}
            later from your profile.
          </p>

          {message && (
            <div
              className={`mb-6 p-3 rounded-md ${
                message.toLowerCase().includes("success")
                  ? "bg-emerald-50 text-emerald-700"
                  : "bg-red-50 text-red-700"
              }`}
            >
              {message}
            </div>
          )}

          <form className="grid grid-cols-1 lg:grid-cols-3 gap-6" onSubmit={handleSubmit}>
            {/* Avatar uploader */}
            <div className="lg:col-span-1">
              <label className="block text-sm font-medium text-ink-700 dark:text-ink-300 mb-2">
                Profile Picture (optional)
              </label>
              <div
                ref={dropRef}
                className="rounded-xl border border-dashed border-slate-300 dark:border-slate-700 p-4 flex flex-col items-center justify-center text-center bg-white/50 dark:bg-ink-800/50"
              >
                <div className="w-28 h-28 rounded-full bg-white shadow-sm flex items-center justify-center overflow-hidden mb-3">
                  {previewUrl ? (
                    <img src={previewUrl} alt="preview" className="w-full h-full object-cover" />
                  ) : (
                    <span className="text-gray-400 text-3xl">👤</span>
                  )}
                </div>
                <p className="text-sm text-ink-600 dark:text-ink-400">
                  Drag & drop or{" "}
                  <label
                    htmlFor="profilePicture"
                    className="text-blue-600 hover:underline cursor-pointer"
                  >
                    browse
                  </label>
                </p>
                <p className="text-xs text-gray-500 mt-1">Max 2MB • JPG/PNG</p>
                <input
                  id="profilePicture"
                  name="profilePicture"
                  type="file"
                  accept="image/*"
                  onChange={onFileChange}
                  className="hidden"
                />
                {previewUrl && (
                  <button
                    type="button"
                    onClick={() => setFormData((p) => ({ ...p, profilePicture: null }))}
                    className="mt-3 text-xs text-red-600 hover:text-red-700"
                  >
                    Remove photo
                  </button>
                )}
              </div>
            </div>

            {/* Form fields */}
            <div className="lg:col-span-2 grid grid-cols-1 sm:grid-cols-2 gap-6">
              {/* Full name */}
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-ink-700 dark:text-ink-300">
                  Full Name *
                </label>
                <input
                  name="fullName"
                  value={formData.fullName}
                  onChange={handleInputChange}
                  className={`mt-1 block w-full px-3 py-2 border ${
                    errors.fullName ? "border-red-300" : "border-slate-300 dark:border-slate-700"
                  } rounded-lg bg-white dark:bg-ink-800 text-ink-900 dark:text-ink-100`}
                  placeholder="Jane Doe"
                />
                {errors.fullName && (
                  <p className="mt-1 text-sm text-red-600">{errors.fullName}</p>
                )}
              </div>

              {/* Email */}
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-ink-700 dark:text-ink-300">
                  Email *
                </label>
                <input
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleInputChange}
                  className={`mt-1 block w-full px-3 py-2 border ${
                    errors.email ? "border-red-300" : "border-slate-300 dark:border-slate-700"
                  } rounded-lg bg-white dark:bg-ink-800 text-ink-900 dark:text-ink-100`}
                  placeholder="you@example.com"
                />
                {errors.email && <p className="mt-1 text-sm text-red-600">{errors.email}</p>}
              </div>

              {/* Password */}
              <div>
                <label className="block text-sm font-medium text-ink-700 dark:text-ink-300">
                  Password *
                </label>
                <div className="mt-1 relative">
                  <input
                    name="password"
                    type={pwVisible ? "text" : "password"}
                    value={formData.password}
                    onChange={handleInputChange}
                    className={`block w-full px-3 py-2 border ${
                      errors.password ? "border-red-300" : "border-slate-300 dark:border-slate-700"
                    } rounded-lg bg-white dark:bg-ink-800 text-ink-900 dark:text-ink-100 pr-10`}
                    placeholder="At least 6 characters"
                  />
                  <button
                    type="button"
                    onClick={() => setPwVisible((s) => !s)}
                    className="absolute inset-y-0 right-0 px-3 text-gray-500 dark:text-gray-400"
                  >
                    {pwVisible ? <i className="fas fa-eye-slash"></i> : <i className="fas fa-eye"></i>}
                  </button>
                </div>
                <div className="mt-2">
                  <div className="w-full h-1.5 bg-gray-200 rounded">
                    <div
                      className={`h-1.5 rounded ${strengthBarClass}`}
                      style={{
                        width: `${(Math.min(pwStrength, 4) / 4) * 100}%`,
                        transition: "width 200ms",
                      }}
                    />
                  </div>
                  <div className="text-xs text-gray-500 mt-1">{strengthLabel}</div>
                </div>
                {errors.password && (
                  <p className="mt-1 text-sm text-red-600">{errors.password}</p>
                )}
              </div>

              {/* Confirm password */}
              <div>
                <label className="block text-sm font-medium text-ink-700 dark:text-ink-300">
                  Confirm Password *
                </label>
                <div className="mt-1 relative">
                  <input
                    name="confirmPassword"
                    type={cpwVisible ? "text" : "password"}
                    value={formData.confirmPassword}
                    onChange={handleInputChange}
                    className={`block w-full px-3 py-2 border ${
                      errors.confirmPassword
                        ? "border-red-300"
                        : "border-slate-300 dark:border-slate-700"
                    } rounded-lg bg-white dark:bg-ink-800 text-ink-900 dark:text-ink-100 pr-10`}
                    placeholder="Retype your password"
                  />
                  <button
                    type="button"
                    onClick={() => setCpwVisible((s) => !s)}
                    className="absolute inset-y-0 right-0 px-3 text-gray-500 dark:text-gray-400"
                  >
                    {cpwVisible ? <i className="fas fa-eye-slash"></i> : <i className="fas fa-eye"></i>}
                  </button>
                </div>
                {errors.confirmPassword && (
                  <p className="mt-1 text-sm text-red-600">{errors.confirmPassword}</p>
                )}
              </div>

              {/* Info box */}
              <div className="sm:col-span-2">
                <div className="flex items-start gap-3 rounded-lg bg-blue-50 dark:bg-blue-900/30 p-3">
                  <div className="text-xl">ℹ️</div>
                  <p className="text-sm text-blue-900 dark:text-blue-300">
                    You’ll start as a <span className="font-semibold">Learner</span>. When you’re
                    ready to teach, enable Tutor Mode from your profile to get a Tutor badge.
                  </p>
                </div>
              </div>

              {/* Submit */}
              <div className="sm:col-span-2">
                <MacPrimary type="submit" disabled={isLoading}>
                  {isLoading ? "Creating account…" : "Create account"}
                </MacPrimary>
              </div>
            </div>
          </form>

          {/* Footer */}
          <div className="mt-8">
            <div className="relative flex items-center">
              <div className="flex-grow border-t border-slate-200 dark:border-slate-700" />
              <span className="px-2 text-xs text-slate-500 dark:text-slate-400">
                Already have an account?
              </span>
              <div className="flex-grow border-t border-slate-200 dark:border-slate-700" />
            </div>
            <div className="mt-6">
              <MacButton onClick={()=>{navigate("/login")}}>
                Sign in instead
              </MacButton>
            </div>
          </div>
        </GlassCard>

        <p className="text-center text-xs text-slate-500 dark:text-slate-400 mt-4">
          By creating an account, you agree to our Terms of Service.
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
