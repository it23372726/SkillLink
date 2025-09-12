import { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

// --- Apple style UI helpers ---
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

function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pwVisible, setPwVisible] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errMsg, setErrMsg] = useState("");
  const [remember, setRemember] = useState(true);

  const { login } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    const savedEmail = localStorage.getItem("sl_saved_email");
    if (savedEmail) setEmail(savedEmail);
  }, []);

  const handleLogin = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    setErrMsg("");

    try {
      const me = await login(email.trim(), password);
      if (remember) {
        localStorage.setItem("sl_saved_email", email.trim());
      } else {
        localStorage.removeItem("sl_saved_email");
      }
      if (me?.role === "Admin") navigate("/admin-dashboard");
      else navigate("/dashboard");
    } catch (err) {
      console.error(err);
      setErrMsg("Invalid email or password. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative min-h-screen flex items-center justify-center">
      {/* Gradient background */}
      <div className="absolute inset-0 bg-gradient-to-b from-slate-50 via-white to-slate-100 dark:from-ink-900 dark:via-ink-900 dark:to-ink-800" />

      {/* Decorative circles */}
      <div className="absolute -top-24 -left-24 w-96 h-96 rounded-full bg-blue-300/20 blur-3xl dark:bg-blue-400/10" />
      <div className="absolute bottom-0 right-0 w-[28rem] h-[28rem] rounded-full bg-indigo-300/20 blur-3xl dark:bg-indigo-400/10" />

      <div className="relative z-10 w-full max-w-6xl grid grid-cols-1 lg:grid-cols-2 gap-8 p-6">
        {/* Left panel */}
        <GlassCard className="hidden lg:flex flex-col justify-center p-10">
          <div className="flex items-center gap-3 mb-6">
            <span className="text-4xl">🍎</span>
            <h1 className="text-3xl font-extrabold tracking-tight text-ink-900 dark:text-ink-100">
              SkillLink
            </h1>
          </div>
          <h2 className="text-2xl font-semibold text-ink-800 dark:text-ink-200">
            Learn. Teach. Grow.
          </h2>
          <p className="mt-3 text-ink-600 dark:text-ink-400 leading-relaxed">
            Find mentors, become a tutor, and collaborate worldwide.  
            Switch to <span className="font-semibold">Tutor Mode</span> any time.
          </p>

          <ul className="mt-6 space-y-2 text-ink-700 dark:text-ink-300">
            <li>✓ Seamless scheduling & requests</li>
            <li>✓ Profile with photo & bio</li>
            <li>✓ Ready-to-teach badge</li>
          </ul>

          <div className="mt-8">
            <MacPrimary onClick={()=>{navigate("/register")}}>
              Create a free account →
            </MacPrimary>
          </div>
        </GlassCard>

        {/* Right panel */}
        <GlassCard className="flex flex-col justify-center p-8 mx-auto w-full sm:max-w-md">
          <div className="flex items-center justify-center gap-2 mb-6 lg:hidden">
            <span className="text-3xl">🍎</span>
            <h1 className="text-2xl font-bold text-ink-900 dark:text-ink-100">
              SkillLink
            </h1>
          </div>

          <h2 className="text-xl font-semibold text-ink-900 dark:text-ink-100">
            Welcome back
          </h2>
          <p className="text-sm text-ink-600 dark:text-ink-400 mt-1">
            Sign in to your account
          </p>

          {errMsg && (
            <div className="mt-4 p-3 rounded-lg bg-red-50 text-red-700 text-sm">
              {errMsg}
            </div>
          )}

          <form onSubmit={handleLogin} className="mt-6 space-y-4">
            <div>
              <label className="block text-sm font-medium text-ink-700 dark:text-ink-300">
                Email
              </label>
              <input
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="mt-1 w-full rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-ink-900 dark:text-ink-100 px-3 py-2 focus:ring-2 focus:ring-blue-500"
                placeholder="you@example.com"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-ink-700 dark:text-ink-300">
                Password
              </label>
              <div className="mt-1 relative">
                <input
                  type={pwVisible ? "text" : "password"}
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-ink-800 text-ink-900 dark:text-ink-100 px-3 py-2 pr-10 focus:ring-2 focus:ring-blue-500"
                  placeholder="••••••••"
                  required
                />
                <button
                  type="button"
                  onClick={() => setPwVisible((s) => !s)}
                  className="absolute inset-y-0 right-0 px-3 text-gray-500 dark:text-gray-400"
                >
                  {pwVisible ? <i className="fas fa-eye-slash"></i> : <i className="fas fa-eye"></i>}
                </button>
              </div>
            </div>

            <div className="flex items-center justify-between">
              <label className="flex items-center text-sm text-ink-700 dark:text-ink-300">
                <input
                  type="checkbox"
                  className="mr-2 rounded border-slate-300 dark:border-slate-600 text-blue-600 focus:ring-blue-500"
                  checked={remember}
                  onChange={(e) => setRemember(e.target.checked)}
                />
                Remember me
              </label>
              <button
                type="button"
                className="text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
                onClick={() => alert("Forgot password flow not implemented yet.")}
              >
                Forgot password?
              </button>
            </div>

            <MacPrimary type="submit" disabled={isLoading}>
              {isLoading ? (
                <>
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  Signing in…
                </>
              ) : (
                "Sign in"
              )}
            </MacPrimary>
          </form>

          <div className="mt-6">
            <div className="relative flex items-center">
              <div className="flex-grow border-t border-slate-200 dark:border-slate-700" />
              <span className="px-2 text-xs text-slate-500 dark:text-slate-400">
                New here?
              </span>
              <div className="flex-grow border-t border-slate-200 dark:border-slate-700" />
            </div>
            <div className="mt-6 grid grid-cols-2 gap-3">
              <MacButton onClick={()=>{navigate("/register")}}>
                Create account
              </MacButton>
              <MacButton onClick={()=>{navigate("/")}}>
                Back home
              </MacButton>
            </div>
          </div>

          <p className="mt-6 text-[11px] text-center text-slate-500 dark:text-slate-400">
            Tip: Admins can sign in with admin credentials to access the Admin Dashboard.
          </p>
        </GlassCard>
      </div>
    </div>
  );
}

export default Login;
