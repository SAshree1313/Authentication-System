import AuthKit from "../setupAuthKit.ts";

// GOOGLE LOGIN
export const startGoogleLogin = () =>
  AuthKit.googleLogin();

// GOOGLE REGISTER
export const startGoogleRegister = () =>
  AuthKit.googleRegister();
