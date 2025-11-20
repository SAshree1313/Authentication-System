import "./RegistrationPage.css";
import { Link, useNavigate } from "react-router-dom";
import { useState, useContext } from "react";
import { FaLock, FaLockOpen } from "react-icons/fa";
import { AuthContext } from "../auth/AuthContext";

export default function RegisterPage() {
  const navigate = useNavigate();
  const { register } = useContext(AuthContext);

  const [showPassword, setShowPassword] = useState(false);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const validateEmail = (email) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

  const validatePassword = (password) =>
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$/.test(password);

  const handleRegister = async () => {
    if (!name || !email || !password) {
      setMessage("All fields are required.");
      return;
    }
    if (!validateEmail(email)) {
      setMessage("Invalid email format.");
      return;
    }
    if (!validatePassword(password)) {
      setMessage(
        "Password must be at least 8 characters long and contain one uppercase letter, one lowercase letter, one number, and one special character."
      );
      return;
    }

    try {
      setLoading(true);
      setMessage("");

      const res = await register(name, email, password); // ✅ use AuthContext

      if (res?.token) {
        setMessage("Registration successful!");
        navigate("/welcome"); // navigate to Welcome after AuthContext updates
      } else {
        setMessage(res?.message || "Registration succeeded but no token received.");
      }
    } catch (err) {
      console.error(err);
      setMessage(err.response?.data?.message || "Error during registration");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-container">
      <div className="register-card">
        <h1 className="register-title">Create your account</h1>
        <form className="register-form" onSubmit={(e) => e.preventDefault()}>
          <label>Name</label>
          <input type="text" placeholder="John Doe" value={name} onChange={(e) => setName(e.target.value)} />

          <label>Email address</label>
          <input type="email" placeholder="you@example.com" value={email} onChange={(e) => setEmail(e.target.value)} />

          <label>Password</label>
          <div className="password-wrapper">
            <input
              type={showPassword ? "text" : "password"}
              placeholder="Password123#"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
            <span className="password-icon" onClick={() => setShowPassword(!showPassword)}>
              {showPassword ? <FaLockOpen /> : <FaLock />}
            </span>
          </div>

          <button type="button" className="primary-btn" onClick={handleRegister} disabled={loading}>
            {loading ? "Registering..." : "Register"}
          </button>

          {message && <p style={{ marginTop: "12px", color: "red" }}>{message}</p>}
        </form>
      </div>

      <div className="register-footer">
        <span>Already have an account? </span>
        <Link to="/login">Sign in</Link>
      </div>
    </div>
  );
}
