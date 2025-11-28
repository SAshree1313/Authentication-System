import React, { createContext, useState, useEffect } from "react";
import api from "../axios.ts";

export const AuthContext = createContext();

export default function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(localStorage.getItem("token"));
  const [loading, setLoading] = useState(true);

  // ------------------------------------------
  //  Automatically fetch user if token exists
  // ------------------------------------------
  useEffect(() => {
    const fetchMe = async () => {
      if (!token) {
        setLoading(false);
        return;
      }

      try {
        const res = await api.get("/auth/me", {
          headers: { Authorization: `Bearer ${token}` }
        });

        setUser(res.data);
      } catch (err) {
        console.error("Token invalid, logging out...");
        logout();
      } finally {
        setLoading(false);
      }
    };

    fetchMe();
  }, [token]);

  // ------------------------------------------
  //  LOGIN
  // ------------------------------------------
  const login = async (email, password) => {
    const res = await api.post("/auth/login", {
      email
      //password
    });

    const jwt = res.data.token;

    localStorage.setItem("token", jwt);
    setToken(jwt);

    setUser({
      id: res.data.id,
      name: res.data.name,
      email: res.data.email
    });

    return res.data;
  };

  // ------------------------------------------
  //  REGISTER
  // ------------------------------------------
  const register = async (name, email, password) => {
    const res = await api.post("/auth/register", {
      name,
      email
      //password
    });

    const jwt = res.data.token;

    localStorage.setItem("token", jwt);
    setToken(jwt);

    setUser({
      id: res.data.id,
      name: res.data.name,
      email: res.data.email
    });

    return res.data;
  };

  // ------------------------------------------
  //  LOGOUT
  // ------------------------------------------
  const logout = () => {
    localStorage.removeItem("token");
    setToken(null);
    setUser(null);
  };

  // ------------------------------------------
  //  PASSKEY LOGIN
  // ------------------------------------------
  const passkeyLogin = async (token) => {
    localStorage.setItem("token", token);
    setToken(token);
    
    return { token };
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
        logout
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
