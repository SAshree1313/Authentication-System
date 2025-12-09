// src/auth/AuthContext.jsx
import React, { createContext, useState, useEffect, useCallback } from "react";
import api from "../axios.ts";
import AuthKit from "../setupAuthKit.ts";

export const AuthContext = createContext(null);

export default function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem("token"));
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // ---------------------------------------------------
  // Load authenticated profile
  // ---------------------------------------------------
  const loadUser = useCallback(async (overrideToken) => {
    const effectiveToken = overrideToken ?? token ?? localStorage.getItem("token");
    if (!effectiveToken) {
      setUser(null);
      setLoading(false);
      return;
    }

    try {
      const me = await api.get("/auth/me", {
        headers: { Authorization: `Bearer ${effectiveToken}` },
      });
      setUser(me.data);
    } catch {
      logout();
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    loadUser();
  }, [loadUser]);

  // ---------------------------------------------------
  // LOGIN via SDK (Passkey)
  // ---------------------------------------------------
  const login = async (email) => {
    const res = await AuthKit.login({ email });

    if (!res.token) return res;

    localStorage.setItem("token", res.token);
    setToken(res.token);

    await loadUser(res.token);
    return res;
    };
    
    // ---------------------------------------------------
    // REGISTER via SDK (Passkey)
    // ---------------------------------------------------
    const register = async (name, email) => {
    const res = await AuthKit.register({ name, email });

    if (!res.token) return res;

    localStorage.setItem("token", res.token);
    setToken(res.token);

    await loadUser(res.token);
    return res;
    };
    
    // ---------------------------------------------------
    // PASSKEY LOGIN (token already given)
    // ---------------------------------------------------
    const passkeyLogin = async (jwt) => {
    localStorage.setItem("token", jwt);
    setToken(jwt);

    await loadUser(jwt);
    return { token: jwt };
    };
    
    // ---------------------------------------------------
    // LOGOUT
  // ---------------------------------------------------
  const logout = () => {
    localStorage.removeItem("token");
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        loading,
        login,
        register,
        passkeyLogin,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
