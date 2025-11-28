import "./LoginPage.css";
import { Link, useNavigate } from "react-router-dom";
import { useState, useContext, useEffect } from "react";
//import { FaLock, FaLockOpen } from "react-icons/fa";
import { AuthContext } from "../auth/AuthContext";
import { startLogin, finishLogin } from "../services/PasskeyService";

export default function LoginPage() {
  const navigate = useNavigate();
  //const { login, passkeyLogin } = useContext(AuthContext);
  const { passkeyLogin } = useContext(AuthContext);

  //const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState("");
  //const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [cooldownSeconds, setCooldownSeconds] = useState(0);
  

  const cooldownActive = cooldownSeconds > 0;
  const COOLDOWN_KEY = "loginCooldownUntil";


  // Restore cooldown from localStorage
  useEffect(() => {
    const stored = localStorage.getItem(COOLDOWN_KEY);
    if (!stored) return;

    const cooldownUntil = parseInt(stored, 10);
    const now = Date.now();

    if (cooldownUntil > now) {
      const remaining = Math.floor((cooldownUntil - now) / 1000);
      setCooldownSeconds(remaining);
    } else {
      localStorage.removeItem(COOLDOWN_KEY);
    }
  }, []);

  // Start cooldown timer if active
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

  // Format cooldown nicely (mm:ss if >= 60s)
  const formatCooldown = (secs) => {
    if (!secs || secs <= 0) return "";
    if (secs >= 60) {
      const m = Math.floor(secs / 60).toString().padStart(2, "0");
      const s = (secs % 60).toString().padStart(2, "0");
      return `${m}:${s}`;
    }
    return `${secs}s`;
  };

  // -----------------------------
  // Email + Password Login
  // -----------------------------
  // const handleEmailLogin = async (e) => {
  //   e.preventDefault();
  //   setLoading(true);
  //   setMessage("");

  //   try {
  //     const res = await login(email, password); // ✅ use AuthContext
  //     if (res?.token) {
  //       navigate("/welcome");
  //     } else {
  //       setMessage("Login succeeded but no token received.");
  //     }
  //   } catch (err) {
  //     console.error(err);
  //     setMessage(err.response?.data?.message || "Invalid credentials");
  //   } finally {
  //     setLoading(false);
  //   }
  // };
  
  // -----------------------------
  // Submit: Email → Begin login
  // -----------------------------
    const handleNext = async (e) => {
      e?.preventDefault();
      setMessage("");
      if (!email) {
        setMessage("Please enter your email.");
        return;
      }

      setLoading(true);

      try {
        // 1) Begin login - send email to backend (per your backend DTO)
        const begin = await startLogin({ email }); // changed to accept email

        // backend returns { options, challengeId } per your DTO
        if (!begin?.options || !begin?.challengeId) {
          setMessage("Invalid server response. Try again.");
          setLoading(false);
          return;
        }

        // 2) Perform WebAuthn get() on the client
        let assertionResult;
        try {
          assertionResult = await finishLogin(begin.challengeId, begin.options);
        } catch (webauthnErr) {
          // navigator.credentials.get failures often throw with DOMException
          console.error("WebAuthn error:", webauthnErr);
          setMessage("Could not complete passkey authentication. Make sure your authenticator is available and you're using a supported browser.");
          setLoading(false);
          return;
        }

        // 3) finishLogin posts to backend and returns the server response
        // Successful response should include token
        if (assertionResult?.success && assertionResult?.token) {
          await passkeyLogin(assertionResult.token);
          // small delay so provider state updates propagate
          setTimeout(() => navigate("/welcome"), 400);
        } else {
          // backend returned an unsuccessful login response
          const msg = assertionResult?.message || "Passkey login failed.";
          setMessage(msg);
        }
      } catch (err) {
        console.error(err);

        const resp = err?.response;
      if (resp) {
        if (resp.status === 429 || resp.data?.cooldownSeconds) {
          const secs =
            resp.data?.cooldownSeconds ??
            parseInt((resp.data?.message || "").replace(/\D/g, "")) ??
            300;

          // save cooldown persistently
          const cooldownUntil = Date.now() + secs * 1000;
          localStorage.setItem(COOLDOWN_KEY, cooldownUntil.toString());

          setCooldownSeconds(secs);
          setMessage("Too many failed attempts. Please wait before trying again.");
        } else if (resp.data?.message) {
          setMessage(resp.data.message);
        } else {
          setMessage(`Login error: ${resp.status}`);
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
            required
            disabled={loading || cooldownActive}
          />

          {/* <label>Password</label>
          <div className="password-wrapper">
            <input
              type={showPassword ? "text" : "password"}
              placeholder="Password123#"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
            <span
              className="password-icon"
              onClick={() => setShowPassword(!showPassword)}
            >
              {showPassword ? <FaLockOpen /> : <FaLock />}
            </span>
          </div> */}

          <button type="submit" className="primary-btn" disabled={loading || cooldownActive}>
            {loading ? "Loading..." : "Next"}
          </button>

          {/* Can't login → recovery */}
          <div className="login-recovery-link">
            <Link to="/recovery">
              Can't login? Regenerate passkey
            </Link>
          </div>

          {message && <p style={{ marginTop: "12px", color: "red" }}>{message}</p>}

          {/* Cooldown UI */}
          {cooldownActive && (
            <div style={{ marginTop: 12, color: "#6a737d", fontSize: 13 }}>
              Please wait{" "}
              <strong className="cooldown-timer">
                {formatCooldown(cooldownSeconds)}
              </strong>{" "}
              before trying again.
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
