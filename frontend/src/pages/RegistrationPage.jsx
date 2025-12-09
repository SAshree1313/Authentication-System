// src/pages/RegistrationPage.jsx
import "./RegistrationPage.css";
import { Link, useNavigate } from "react-router-dom";
import { useState, useContext } from "react";
import { AuthContext } from "../auth/AuthContext";
import { startRegister } from "../services/PasskeyService";

export default function RegisterPage() {
  const navigate = useNavigate();
  const { passkeyLogin } = useContext(AuthContext);

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  // Device modal
  const [showDeviceModal, setShowDeviceModal] = useState(false);
  const [deviceName, setDeviceName] = useState("");

  // Recovery modal
  const [showRecoveryModal, setShowRecoveryModal] = useState(false);
  const [recoveryCode, setRecoveryCode] = useState("");

  const [copied, setCopied] = useState(false);

  const validateEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

  // Step 1 — Ask for name & email
  const handleRegister = () => {
    if (!name || !email) {
      setMessage("Name and email are required.");
      return;
    }
    if (!validateEmail(email)) {
      setMessage("Invalid email format.");
      return;
    }

    setMessage("");
    setShowDeviceModal(true);
  };

  // Step 2 — Begin SDK registration after device name
  const beginRegistration = async () => {
    setShowDeviceModal(false);
    setLoading(true);
    setMessage("");

    try {
      // SDK handles:
      // - registration begin
      // - WebAuthn create()
      // - registration complete
      const res = await startRegister({
        name,
        email,
        deviceName: deviceName.trim() || null,
      });

      if (!res.success) {
        setMessage(res.message || "Passkey registration failed.");
        return;
      }

      // Show recovery code modal
      setRecoveryCode(res.recoveryCode || "");
      setShowRecoveryModal(true);

      // Auto-login using token
      if (res.token) {
        await passkeyLogin(res.token);
      }
    } catch (err) {
      console.error("Registration error:", err);
      setMessage("Error during registration.");
    } finally {
      setLoading(false);
    }
  };

  const finishRecoveryModal = () => {
    setShowRecoveryModal(false);
    navigate("/welcome");
  };

  const copyRecoveryCode = () => {
    navigator.clipboard.writeText(recoveryCode);
    setCopied(true);
    setTimeout(() => setCopied(false), 1200);
  };

  return (
    <div className="register-container">
      <div className="register-card">
        <h1 className="register-title">Create your account</h1>

        <form className="register-form" onSubmit={(e) => e.preventDefault()}>
          <label>Name</label>
          <input
            type="text"
            placeholder="John Doe"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />

          <label>Email address</label>
          <input
            type="email"
            placeholder="you@example.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />

          <button
            type="button"
            className="primary-btn"
            onClick={handleRegister}
            disabled={loading}
          >
            {loading ? "Processing..." : "Generate Passkey & Login"}
          </button>

          {message && (
            <p style={{ marginTop: "12px", color: "red" }}>{message}</p>
          )}
        </form>
      </div>

      <div className="register-footer">
        <span>Already have an account? </span>
        <Link to="/login">Sign in</Link>
      </div>

      {/* DEVICE NAME MODAL */}
      {showDeviceModal && (
        <div className="recovery-modal-overlay">
          <div className="recovery-modal-container">
            <h2>Name this device</h2>
            <p className="description">
              Give a name for this device so you can recognize it later.
            </p>

            <input
              type="text"
              placeholder="e.g., John’s iPhone"
              value={deviceName}
              onChange={(e) => setDeviceName(e.target.value)}
            />

            <button className="modal-button" onClick={beginRegistration}>
              Continue
            </button>
          </div>
        </div>
      )}

      {/* RECOVERY CODE MODAL */}
      {showRecoveryModal && (
        <div className="recovery-modal-overlay">
          <div className="recovery-modal-container">
            <h2>Your recovery code</h2>

            <p className="description">Store this code safely. You can’t view it again.</p>

            <div className="gh-copy-container">
              <span className="gh-code">{recoveryCode}</span>

              <button className="gh-copy-btn" onClick={copyRecoveryCode} title="Copy">
                {copied ? "✔" : "📄"}
              </button>
            </div>

            {copied && <p className="gh-copied-text">Copied!</p>}

            <button className="modal-button" onClick={finishRecoveryModal}>
              I have stored this code
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
