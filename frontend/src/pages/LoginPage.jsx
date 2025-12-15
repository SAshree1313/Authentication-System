import "./LoginPage.css";
import { Link, useNavigate } from "react-router-dom";
import { useState, useContext, useEffect } from "react";
import { AuthContext } from "../auth/AuthContext";
import { startLogin } from "../services/PasskeyService";

export default function LoginPage() {
  const navigate = useNavigate();

  const { passkeyLogin, googleLogin } = useContext(AuthContext);

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

  // --------------------------------------------------
  // PASSKEY LOGIN
  // --------------------------------------------------
  const handleNext = async (e) => {
    e.preventDefault();
    setMessage("");

    if (!email) {
      setMessage("Please enter your email.");
      return;
    }

    setLoading(true);

    try {
      const res = await startLogin({ email });

      if (res?.success && res?.token) {
        await passkeyLogin(res.token);
        navigate("/welcome");
        return;
      }

      setMessage(res?.message || "Passkey login failed.");
    } catch (err) {
      const resp = err?.response;

      if (resp?.status === 429 || resp?.data?.cooldownSeconds) {
        const secs =
          resp.data?.cooldownSeconds ??
          parseInt((resp.data?.message || "").replace(/\D/g, "")) ??
          300;

        const until = Date.now() + secs * 1000;
        localStorage.setItem(COOLDOWN_KEY, until.toString());
        setCooldownSeconds(secs);

        setMessage("Too many attempts. Please wait before trying again.");
      } else {
        setMessage(resp?.data?.message || "Login failed.");
      }
    } finally {
      setLoading(false);
    }
  };

  // --------------------------------------------------
  // GOOGLE LOGIN
  // --------------------------------------------------
  const [showRecoveryModal, setShowRecoveryModal] = useState(false);
  const [recoveryCode, setRecoveryCode] = useState("");

  const handleGoogleLogin = async () => {
    setLoading(true);
    setMessage("");

    try {
      const res = await googleLogin();
      
      // If first-time user with recovery code, show modal
      if (res?.recoveryCode) {
        setRecoveryCode(res.recoveryCode);
        setShowRecoveryModal(true);
      } else {
        navigate("/welcome");
      }
    } catch (err) {
      setMessage(err?.message || "Google login failed.");
    } finally {
      setLoading(false);
    }
  };

  const handleRecoveryCodeAcknowledged = () => {
    setShowRecoveryModal(false);
    navigate("/welcome");
  };

  return (
    <>
      {/* Recovery Code Modal */}
      {showRecoveryModal && (
        <div className="modal-overlay" style={{ zIndex: 9999 }}>
          <div className="modal-content" style={{ maxWidth: "500px" }}>
            <h2>Save Your Recovery Code</h2>
            <p style={{ marginBottom: "16px", color: "#6a737d" }}>
              This is your account recovery code. Save it securely - you'll need it if you lose access to your devices.
            </p>
            
            <div style={{
              padding: "16px",
              background: "#f6f8fa",
              border: "1px solid #d0d7de",
              borderRadius: "6px",
              fontFamily: "monospace",
              fontSize: "18px",
              fontWeight: "bold",
              textAlign: "center",
              marginBottom: "16px",
              userSelect: "all"
            }}>
              {recoveryCode}
            </div>

            <button
              className="primary-btn"
              onClick={handleRecoveryCodeAcknowledged}
              style={{ width: "100%" }}
            >
              I've Saved My Recovery Code
            </button>
          </div>
        </div>
      )}

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

          <button
            type="submit"
            className="primary-btn"
            disabled={loading || cooldownActive}
          >
            {loading ? "Loading..." : "Next"}
          </button>

          <div style={{ textAlign: "center", margin: "-7px", color: "#6a737d" }}>
            or
          </div>

          <button
            type="button"
            className="secondary-btn"
            onClick={handleGoogleLogin}
            disabled={loading || cooldownActive}
          >
            Continue with Google
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
          <Link to="/register">Register Using Passkey</Link>
        </div>
      </div>
    </>
  );
}
