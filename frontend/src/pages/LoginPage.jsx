// src/pages/LoginPage.jsx
import "./LoginPage.css";
import { Link, useNavigate } from "react-router-dom";
import { useState, useContext, useEffect } from "react";
import { AuthContext } from "../auth/AuthContext";
import { startLogin } from "../services/PasskeyService";

export default function LoginPage() {
  const navigate = useNavigate();
  const { passkeyLogin } = useContext(AuthContext);

  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const [cooldownSeconds, setCooldownSeconds] = useState(0);
  const COOLDOWN_KEY = "loginCooldownUntil";

  const cooldownActive = cooldownSeconds > 0;

  // Restore cooldown from storage
  useEffect(() => {
    const stored = localStorage.getItem(COOLDOWN_KEY);
    if (!stored) return;

    const until = parseInt(stored, 10);
    const now = Date.now();
    if (until > now) {
      setCooldownSeconds(Math.floor((until - now) / 1000));
    } else {
      localStorage.removeItem(COOLDOWN_KEY);
    }
  }, []);

  // Cooldown countdown
  useEffect(() => {
    if (!cooldownActive) return;

    const timer = setInterval(() => {
      setCooldownSeconds((s) => {
        if (s <= 1) {
          localStorage.removeItem(COOLDOWN_KEY);
          clearInterval(timer);
          return 0;
        }
        return s - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [cooldownActive]);

  const formatCooldown = (secs) => {
    if (!secs) return "";
    if (secs >= 60) {
      const m = String(Math.floor(secs / 60)).padStart(2, "0");
      const s = String(secs % 60).padStart(2, "0");
      return `${m}:${s}`;
    }
    return `${secs}s`;
  };

  // ------------------------------------------------------
  // PASSKEY LOGIN 
  // ------------------------------------------------------
  const handleNext = async (e) => {
    e.preventDefault();
    setMessage("");

    if (!email) {
      setMessage("Please enter your email.");
      return;
    }

    setLoading(true);

    try {
      // 1) SDK automatically does begin → webauthn → complete
      const res = await startLogin({ email });

      // backend returns { success, token, message }
      if (res?.success && res?.token) {
        await passkeyLogin(res.token);
        navigate("/welcome");
        return;
      }

      // If backend returned success: false
      setMessage(res?.message || "Passkey login failed.");

    } catch (err) {
      console.error("Login error:", err);

      const resp = err?.response;

      if (resp) {
        // 429 rate limit
        if (resp.status === 429 || resp.data?.cooldownSeconds) {
          const secs =
            resp.data?.cooldownSeconds ??
            parseInt((resp.data?.message || "").replace(/\D/g, "")) ??
            300;

          const until = Date.now() + secs * 1000;
          localStorage.setItem(COOLDOWN_KEY, until.toString());
          setCooldownSeconds(secs);

          setMessage("Too many attempts. Please wait before trying again.");
        } else {
          setMessage(resp.data?.message || "Login failed.");
        }
      } else {
        setMessage(err?.message || "Unknown error occurred.");
      }

    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <h1 className="login-title">Sign in to your account</h1>

        <form className="login-form" onSubmit={handleNext}>
          <label>Email address</label>
          <input
            type="email"
            placeholder="you@example.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={loading || cooldownActive}
            required
          />

          <button type="submit" className="primary-btn" disabled={loading || cooldownActive}>
            {loading ? "Loading..." : "Next"}
          </button>

          <div className="login-recovery-link">
            <Link to="/recovery">Can't login? Regenerate passkey</Link>
          </div>

          {message && (
            <p style={{ marginTop: 12, color: "red" }}>{message}</p>
          )}

          {cooldownActive && (
            <div style={{ marginTop: 12, color: "#6a737d", fontSize: 13 }}>
              Please wait{" "}
              <strong>{formatCooldown(cooldownSeconds)}</strong> before trying again.
            </div>
          )}
        </form>
      </div>

      <div className="login-footer">
        <span>Don't have an account? </span>
        <Link to="/register">Register</Link>
      </div>
    </div>
  );
}
