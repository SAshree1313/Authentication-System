import "./LoginPage.css";
import { Link, useNavigate } from "react-router-dom";
import { useState, useContext } from "react";
import { FaLock, FaLockOpen } from "react-icons/fa";
import { AuthContext } from "../auth/AuthContext";
import { startLogin, finishLogin } from "../services/PasskeyService";

export default function LoginPage() {
  const navigate = useNavigate();
  const { login } = useContext(AuthContext);

  const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  // -----------------------------
  // Email + Password Login
  // -----------------------------
  const handleEmailLogin = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage("");

    try {
      const res = await login(email, password); // ✅ use AuthContext
      if (res?.token) {
        navigate("/welcome");
      } else {
        setMessage("Login succeeded but no token received.");
      }
    } catch (err) {
      console.error(err);
      setMessage(err.response?.data?.message || "Invalid credentials");
    } finally {
      setLoading(false);
    }
  };

  // -----------------------------
  // Passkey Login
  // -----------------------------
  const handlePasskeyLogin = async () => {
    setLoading(true);
    setMessage("");

    try {
      const begin = await startLogin();
      const assertionJSON = await finishLogin(begin.challengeId, begin.options);

      if (assertionJSON.success && assertionJSON.token) {
        localStorage.setItem("token", assertionJSON.token);
        setTimeout(() => navigate("/welcome"), 1500);
      } else {
        setMessage(assertionJSON.message || "Passkey login failed");
      }
    } catch (err) {
      console.error(err);
      setMessage("Passkey login failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <h1 className="login-title">Sign in to your account</h1>

        <form className="login-form" onSubmit={handleEmailLogin}>
          <label>Email address</label>
          <input
            type="email"
            placeholder="you@example.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />

          <label>Password</label>
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
          </div>

          <button type="submit" className="primary-btn" disabled={loading}>
            {loading ? "Loading..." : "Sign in"}
          </button>

          <button
            type="button"
            className="secondary-btn"
            onClick={handlePasskeyLogin}
            disabled={loading}
          >
            Login using passkey
          </button>

          {message && <p style={{ marginTop: "12px", color: "red" }}>{message}</p>}
        </form>
      </div>

      <div className="login-footer">
        <span>Don't have an account? </span>
        <Link to="/register">Register</Link>
      </div>
    </div>
  );
}
