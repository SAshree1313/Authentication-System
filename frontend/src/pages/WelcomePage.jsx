import "./WelcomePage.css";
import { FaKey } from "react-icons/fa";
import { FiLogOut } from "react-icons/fi";
import { useContext, useState } from "react";
import { AuthContext } from "../auth/AuthContext";
import { startRegister, finishRegister } from "../services/PasskeyService";

export default function WelcomePage() {
  const { user, logout } = useContext(AuthContext);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const handleGeneratePasskey = async () => {
    if (!user) return;

    try {
      setLoading(true);
      setMessage("");

      const { challengeId, options } = await startRegister(user.id);
      const result = await finishRegister(challengeId, options);

      if (result.success) {
        setMessage("Passkey registered successfully ✅");
      } else {
        setMessage(`Failed: ${result.message}`);
      }
    } catch (err) {
      console.error(err);
      setMessage("Error registering passkey");
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = () => {
    logout(); // calls AuthContext.logout and redirects
  };

  return (
    <div className="welcome-container">

      <div className="welcome-nav">
        <button type = "button" className="btn-generate" onClick={handleGeneratePasskey} disabled={loading}>
          <FaKey style={{ marginRight: "6px" }} />
          {loading ? "Registering..." : "Generate Passkey"}
        </button>

        <button className="btn-logout" onClick={handleLogout}>
          <FiLogOut style={{ marginRight: "6px" }} />
          Logout
        </button>
      </div>

      <div className="welcome-message">
        <h1>Welcome to CEI India Pvt Limited 👋</h1>
        {message && <p style={{ marginTop: "12px", color: "green" }}>{message}</p>}
      </div>

    </div>
  );
}
