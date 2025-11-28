import React, { useContext } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../auth/AuthContext.jsx";

export default function ProtectedRoute({ children }) {
  const { user, token, loading } = useContext(AuthContext);

  // Still verifying token → show loading
  if (loading) {
    return (
      <div style={{ textAlign: "center", marginTop: "50px" }}>
        <h2>Loading...</h2>
      </div>
    );
  }

  // If no token or no user → redirect to login 
  if (!token || !user) {
    return <Navigate to="/login" replace />;
  }
  
  // Otherwise show the protected page
  return children;
}
