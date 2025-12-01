import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  startRecovery,
  verifyRecoveryCode,
  finishRecovery,
} from "../services/PasskeyService";
import "./RecoveryPage.css";
import "./RegistrationPage.css"; // Reuse modal styling

export default function RecoveryPage() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [recoveryCode, setRecoveryCode] = useState("");
  const [deviceName, setDeviceName] = useState("");

  const [challengeId, setChallengeId] = useState(null);
  const [fidoOptions, setFidoOptions] = useState(null);
  const [step, setStep] = useState("email");

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  // MODALS
  const [showRecoveryModal, setShowRecoveryModal] = useState(false);
  const [newRecoveryCode, setNewRecoveryCode] = useState("");
  const [copied, setCopied] = useState(false);

  const [showRecoveryCode, setShowRecoveryCode] = useState(false);


  const copyRecoveryCode = () => {
    navigator.clipboard.writeText(newRecoveryCode);
    setCopied(true);
    setTimeout(() => setCopied(false), 1200);
  };

  const handleEmailNext = async () => {
    if (!email.trim()) {
      setMessage("Please enter your email.");
      return;
    }

    setLoading(true);
    setMessage("");

    try {
      const res = await startRecovery(email.trim());

      if (res.success) {
        setChallengeId(res.challengeId);
        setStep("code");
      } else {
        setMessage(res.message || "Failed to start recovery.");
      }
    } catch (err) {
      console.error(err);
      setMessage(err?.response?.data?.message || "Error starting recovery.");
    } finally {
      setLoading(false);
    }
  };

  const handleRecoveryCodeNext = async () => {
    if (!recoveryCode.trim() || !challengeId) return;

    setLoading(true);
    setMessage("");

    try {
      const res = await verifyRecoveryCode(challengeId, recoveryCode.trim());

      setChallengeId(res.challengeId);
      setFidoOptions(res.options);
      setStep("device");
    } catch (err) {
      console.error(err);
      setMessage(err?.response?.data?.message || "Invalid recovery code.");
    } finally {
      setLoading(false);
    }
  };

  const handleGeneratePasskey = async () => {
    if (!deviceName.trim() || !challengeId || !fidoOptions) return;

    setLoading(true);
    setMessage("");

    try {
      const res = await finishRecovery(
        challengeId,
        fidoOptions,
        deviceName.trim()
      );

      if (res.success) {
        // NEW: show recovery modal
        setNewRecoveryCode(res.newRecoveryCode);
        setShowRecoveryModal(true);
      } else {
        setMessage(res.message || "Failed to complete recovery.");
      }
    } catch (err) {
      console.error(err);
      setMessage("Error generating passkey.");
    } finally {
      setLoading(false);
    }
  };

  const closeRecoveryModal = () => {
    setShowRecoveryModal(false);
    navigate("/login");
  };

  return (
    <div className="recovery-container">
      <h1>Passkey Recovery</h1>

      {message && <p className="error-message">{message}</p>}

      <div className="recovery-field">
        <label>Email</label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={step !== "email"}
          placeholder="Enter your email"
        />
      </div>

      {step !== "email" && (
        <div className="recovery-input-wrapper">
          <label>Recovery Code</label>
           <div className="recovery-input-container">
            <input
              type={showRecoveryCode ? "text" : "password"}
              value={recoveryCode}
              onChange={(e) => setRecoveryCode(e.target.value)}
              disabled={step !== "code"}
              placeholder="Enter your recovery code"
              className="recovery-input"
            />

            <span
              className="recovery-toggle"
              onClick={() => setShowRecoveryCode((prev) => !prev)}
            >
              {showRecoveryCode ? "🔓" : "🔒"}
            </span>
          </div>
        </div>
      )}

      {step === "device" && (
        <div className="recovery-field">
          <label>Device Name</label>
          <input
            type="text"
            value={deviceName}
            onChange={(e) => setDeviceName(e.target.value)}
            placeholder="Enter a name for this device"
          />
        </div>
      )}

      <div className="recovery-actions">
        {step === "email" && (
          <button
            onClick={handleEmailNext}
            disabled={loading}
            className="btn-primary"
          >
            {loading ? "Loading..." : "Next"}
          </button>
        )}

        {step === "code" && (
          <button
            onClick={handleRecoveryCodeNext}
            disabled={loading}
            className="btn-primary"
          >
            {loading ? "Verifying..." : "Next"}
          </button>
        )}

        {step === "device" && (
          <button
            onClick={handleGeneratePasskey}
            disabled={loading}
            className="btn-primary"
          >
            {loading ? "Generating..." : "Generate Passkey"}
          </button>
        )}
      </div>

      <div className="back-to-signin">
        <span onClick={() => navigate("/login")}>Back to Sign In</span>
      </div>

      {/* NEW: RECOVERY CODE MODAL */}
      {showRecoveryModal && (
        <div className="recovery-modal-overlay">
          <div className="recovery-modal-container">
            <h2>Your new recovery code</h2>

            <p className="description">
              Store this code safely. You can’t view it again.
            </p>

            <div className="gh-copy-container">
              <span className="gh-code">{newRecoveryCode}</span>

              <button
                className="gh-copy-btn"
                onClick={copyRecoveryCode}
                title="Copy"
              >
                {copied ? "✔" : "📄"}
              </button>
            </div>

            {copied && <p className="gh-copied-text">Copied!</p>}

            <button className="modal-button" onClick={closeRecoveryModal}>
              I have stored this code
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
