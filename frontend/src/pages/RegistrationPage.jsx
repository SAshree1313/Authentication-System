import "./RegistrationPage.css";
import { Link, useNavigate } from "react-router-dom";
import { useState, useContext } from "react";
import { AuthContext } from "../auth/AuthContext";
import { startRegister, finishRegister } from "../services/PasskeyService";

export default function RegisterPage() {
  const navigate = useNavigate();
  const { passkeyLogin } = useContext(AuthContext);

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const [pendingRegistration, setPendingRegistration] = useState(null);

  // Modal A
  const [showDeviceModal, setShowDeviceModal] = useState(false);
  const [deviceName, setDeviceName] = useState("");

  // Modal B
  const [showRecoveryModal, setShowRecoveryModal] = useState(false);
  const [recoveryCode, setRecoveryCode] = useState("");

  // Copy state for GitHub-like copy UI
  const [copied, setCopied] = useState(false);

  const validateEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

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

  const beginPasskeyRegistration = async () => {
    setShowDeviceModal(false);

    setLoading(true);
    setMessage("");

    try {
      const begin = await startRegister({ name, email });

      if (!begin?.options || !begin?.challengeId) {
        setMessage("Invalid server response.");
        setLoading(false);
        setDeviceName("");
        return;
      }

      setPendingRegistration(begin);
      await runWebauthnFlow(begin);

    } catch (err) {
      console.error(err);
      setMessage(err.response?.data?.message || "Registration error");
      setDeviceName("");
    } finally {
      setLoading(false);
    }
  };

  const runWebauthnFlow = async (begin) => {
    try {
      const { challengeId, options } = begin;

      const result = await finishRegister(
        challengeId,
        options,
        deviceName.trim() || null
      );

      if (!result?.success) {
        setMessage(result?.message || "Passkey registration failed.");
        setDeviceName("");
        return;
      }

      setRecoveryCode(result.recoveryCode || "");
      setShowRecoveryModal(true);

      await passkeyLogin(result.token);

    } catch (err) {
      console.error(err);
      setMessage("Error finishing registration");
      setDeviceName("");
    }
  };

  const finishRecoveryModal = () => {
    setShowRecoveryModal(false);
    navigate("/welcome");
  };

  // Copy handler
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

            <button className="modal-button" onClick={beginPasskeyRegistration}>
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

            <p className="description">
              Store this code safely. You can’t view it again.
            </p>

            {/* GitHub-style Copy UI */}
            <div className="gh-copy-container">
              <span className="gh-code">{recoveryCode}</span>

              <button
                className="gh-copy-btn"
                onClick={copyRecoveryCode}
                title="Copy"
              >
                {copied ? "✔" : "📄"}
              </button>
            </div>

            {copied && (
              <p className="gh-copied-text">Copied!</p>
            )}

            <button className="modal-button" onClick={finishRecoveryModal}>
              I have stored this code
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
