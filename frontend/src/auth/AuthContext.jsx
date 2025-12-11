import React, { createContext, useState, useEffect, useCallback } from "react";
import api from "../axios.ts";
import AuthKit from "../setupAuthKit.ts";

export const AuthContext = createContext(null);

export default function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem("token"));
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // Centralized logout
  const logout = useCallback(() => {
    localStorage.removeItem("token");
    setToken(null);
    setUser(null);
  }, []);

  // Load user profile using the freshest token available
  const loadUser = useCallback(
    async (overrideToken) => {
      const effectiveToken = overrideToken ?? token ?? localStorage.getItem("token");

      if (!effectiveToken) {
        setUser(null);
        setLoading(false);
        return;
      }

      try {
        const res = await api.get("/auth/me");
        setUser(res.data);
      } catch {
        logout();
      } finally {
        setLoading(false);
      }
    },
    [token, logout]
  );

  useEffect(() => {
    loadUser();
  }, [loadUser]);


  // Attach unauthorized handler
  useEffect(() => {
    const handleUnauthorized = () => logout();

    const axiosInterceptorId = api.interceptors.response.use(
      (resp) => resp,
      (error) => {
        if (error?.response?.status === 401) {
          handleUnauthorized();
        }
        return Promise.reject(error);
      }
    );

    // Register handler into AuthKit as well
    if (AuthKit?.http?.setUnauthorizedHandler) {
      AuthKit.http.setUnauthorizedHandler(handleUnauthorized);
    }

    return () => {
      api.interceptors.response.eject(axiosInterceptorId);
      if (AuthKit?.http?.setUnauthorizedHandler) {
        AuthKit.http.setUnauthorizedHandler(() => {});
      }
    };
  }, [logout]);


  // LOGIN
  const login = async (email) => {
    const res = await AuthKit.login({ email });
    if (!res.token) return res;

    localStorage.setItem("token", res.token);
    setToken(res.token);
    await loadUser(res.token);
    return res;
  };

  // REGISTER
  const register = async (name, email) => {
    const res = await AuthKit.register({ name, email });
    if (!res.token) return res;

    localStorage.setItem("token", res.token);
    setToken(res.token);
    await loadUser(res.token);
    return res;
  };

  // Passkey login (already provides token)
  const passkeyLogin = async (jwt) => {
    localStorage.setItem("token", jwt);
    setToken(jwt);
    await loadUser(jwt);
    return { token: jwt };
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        setToken,   // ← IMPORTANT: so WelcomePage can save new tokens
        loading,
        login,
        register,
        passkeyLogin,
        logout
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
