import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./pages/LoginPage";
import Register from "./pages/RegistrationPage";
import Welcome from "./pages/WelcomePage";
import Recovery from "./pages/RecoveryPage";
import ProtectedRoute from "./components/ProtectedRoute";
import AuthProvider from "./auth/AuthContext";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>

          {/* Public routes */}
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/recovery" element={<Recovery />} />

          {/* Protected route */}
          <Route
            path="/welcome"
            element={
              <ProtectedRoute>
                <Welcome />
              </ProtectedRoute>
            }
          />

          {/* Redirect root to login */}
          <Route path="*" element={<Welcome />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

