// src/pages/RecoveryPage.jsx
import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  startRecovery,
  verifyRecoveryCode,
  finishRecovery,
} from "../services/PasskeyService";
import "./RecoveryPage.css";
import "./RegistrationPage.css";

export default function RecoveryPage() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [recoveryCode, setRecoveryCode] = useState("");
  const [deviceName, setDeviceName] = useState("");

  const [challengeId, setChallengeId] = useState(null);
  const [optionsFromServer, setOptionsFromServer] = useState(null);
  const [step, setStep] = useState("email");

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  // New recovery modal
  const [showRecoveryModal, setShowRecoveryModal] = useState(false);
  const [newRecoveryCode, setNewRecoveryCode] = useState("");
  const [copied, setCopied] = useState(false);
  const [showRecoveryCodeField, setShowRecoveryCodeField] = useState(false);

  const copyRecoveryCode = () => {
    navigator.clipboard.writeText(newRecoveryCode);
    setCopied(true);
    setTimeout(() => setCopied(false), 1200);
  };

  // ---------------------------------------------------
  // Step 1: Begin recovery
  // ---------------------------------------------------
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
        setMessage(res.message || "Unable to start recovery.");
      }
    } catch (err) {
      setMessage(err?.response?.data?.message || "Error starting recovery.");
    } finally {
      setLoading(false);
    }
  };

  // ---------------------------------------------------
  // Step 2: Verify recovery code
  // ---------------------------------------------------
  const handleRecoveryCodeNext = async () => {
    if (!recoveryCode.trim()) return;

    setLoading(true);
    setMessage("");

    try {
      const res = await verifyRecoveryCode(challengeId, recoveryCode.trim());

      // Backend returns: { challengeId, options }
      setChallengeId(res.challengeId);
      setOptionsFromServer(res.options);
      setStep("device");
    } catch (err) {
      setMessage(err?.response?.data?.message || "Invalid recovery code.");
    } finally {
      setLoading(false);
    }
  };

  // ---------------------------------------------------
  // Step 3: Generate new passkey via WebAuthn (SDK handles WebAuthn)
  // ---------------------------------------------------
  const handleGeneratePasskey = async () => {
    if (!deviceName.trim()) {
      setMessage("Please provide a device name.");
      return;
    }

    setLoading(true);
    setMessage("");

    try {
      // SDK performs WebAuthn internally
      const result = await finishRecovery(
        challengeId,
        optionsFromServer,
        deviceName.trim()
      );

      if (!result.success) {
        setMessage(result.message || "Failed to complete recovery.");
        return;
      }

      // Show new recovery code
      setNewRecoveryCode(result.newRecoveryCode);
      setShowRecoveryModal(true);

    } catch (err) {
      console.error(err);
      setMessage("Error completing recovery.");
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

      {/* Step 1: Enter Email */}
      <div className="recovery-field">
        <label>Email</label>
        <input
          type="email"
          value={email}
          disabled={step !== "email"}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="you@example.com"
        />
      </div>

      {/* Step 2: Enter Recovery Code */}
      {step !== "email" && (
        <div className="recovery-input-wrapper">
          <label>Recovery Code</label>
          <div className="recovery-input-container">
            <input
              type={showRecoveryCodeField ? "text" : "password"}
              value={recoveryCode}
              disabled={step !== "code"}
              onChange={(e) => setRecoveryCode(e.target.value)}
            />
            <span
              className="recovery-toggle"
              onClick={() => setShowRecoveryCodeField((v) => !v)}
            >
              {showRecoveryCodeField ? "🔓" : "🔒"}
            </span>
          </div>
        </div>
      )}

      {/* Step 3: New Device */}
      {step === "device" && (
        <div className="recovery-field">
          <label>Device Name</label>
          <input
            type="text"
            value={deviceName}
            onChange={(e) => setDeviceName(e.target.value)}
            placeholder="e.g., My iPhone"
          />
        </div>
      )}

      <div className="recovery-actions">
        {step === "email" && (
          <button disabled={loading} className="btn-primary" onClick={handleEmailNext}>
            {loading ? "Loading..." : "Next"}
          </button>
        )}

        {step === "code" && (
          <button disabled={loading} className="btn-primary" onClick={handleRecoveryCodeNext}>
            {loading ? "Verifying..." : "Next"}
          </button>
        )}

        {step === "device" && (
          <button disabled={loading} className="btn-primary" onClick={handleGeneratePasskey}>
            {loading ? "Generating..." : "Generate Passkey"}
          </button>
        )}
      </div>

      <div className="back-to-signin">
        <span onClick={() => navigate("/login")}>Back to Sign In</span>
      </div>

      {/* Recovery Code Modal */}
      {showRecoveryModal && (
        <div className="recovery-modal-overlay">
          <div className="recovery-modal-container">
            <h2>Your new recovery code</h2>

            <p className="description">Store this safely.</p>

            <div className="gh-copy-container">
              <span className="gh-code">{newRecoveryCode}</span>
              <button className="gh-copy-btn" onClick={copyRecoveryCode}>
                {copied ? "✔" : "📄"}
              </button>
            </div>

            <button className="modal-button" onClick={closeRecoveryModal}>
              I have stored this code
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
