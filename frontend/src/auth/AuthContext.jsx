import React, { createContext, useState, useEffect, useCallback } from "react";
import api from "../axios.ts";
import AuthKit from "../setupAuthKit.ts";

export const AuthContext = createContext(null);

export default function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem("token"));
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // --------------------------------------------------
  // Centralized logout
  // --------------------------------------------------
  const logout = useCallback(() => {
    localStorage.removeItem("token");
    setToken(null);
    setUser(null);
  }, []);

  // --------------------------------------------------
  // Load profile using freshest token
  // --------------------------------------------------
  const loadUser = useCallback(
    async (overrideToken) => {
      const effectiveToken =
        overrideToken ?? token ?? localStorage.getItem("token");

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

  // --------------------------------------------------
  // Attach unauthorized handler
  // --------------------------------------------------
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

  // --------------------------------------------------
  // Shared token persistence helper
  // --------------------------------------------------
  const completeLoginWithToken = async (jwt) => {
    localStorage.setItem("token", jwt);
    setToken(jwt);
    await loadUser(jwt);
  };

  // --------------------------------------------------
  // PASSKEY LOGIN
  // --------------------------------------------------
  const login = async (email) => {
    const res = await AuthKit.login({ email });
    if (!res?.token) return res;

    await completeLoginWithToken(res.token);
    return res;
  };

  // --------------------------------------------------
  // PASSKEY REGISTER
  // --------------------------------------------------
  const register = async (name, email) => {
    const res = await AuthKit.register({ name, email });
    if (!res?.token) return res;

    await completeLoginWithToken(res.token);
    return res;
  };

  // --------------------------------------------------
  // GOOGLE LOGIN
  // --------------------------------------------------
  const googleLogin = async () => {
    const res = await AuthKit.googleLogin();
    if (!res?.accessToken) return res;

    await completeLoginWithToken(res.accessToken);
    
    // Return response with recovery code if present
    return res;
  };

  // --------------------------------------------------
  // GOOGLE REGISTER
  // --------------------------------------------------
  const googleRegister = async () => {
    const res = await AuthKit.googleRegister();
    if (!res?.accessToken) return res;

    await completeLoginWithToken(res.accessToken);
    return res;
  };

  // --------------------------------------------------
  // PASSKEY LOGIN (alias for compatibility)
  // --------------------------------------------------
  const passkeyLogin = async (jwt) => {
    await completeLoginWithToken(jwt);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        setToken,
        loading,

        // passkey
        login,
        register,
        passkeyLogin,

        // google
        googleLogin,
        googleRegister,

        // misc
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
