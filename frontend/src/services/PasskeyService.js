// src/services/PasskeyService.js
import AuthKit from "../setupAuthKit.ts";

// REGISTRATION
export const startRegister = ({ name, email, deviceName }) =>
  AuthKit.register({ name, email, deviceName });

// LOGIN
export const startLogin = ({ email }) =>
  AuthKit.login({ email });

// RECOVERY (Step 1)
export const startRecovery = (email) =>
  AuthKit.recoveryBegin(email);

// RECOVERY (Step 2)
export const verifyRecoveryCode = (challengeId, code) =>
  AuthKit.recoveryVerifyCode(challengeId, code);

// RECOVERY (Step 3)
export const finishRecovery = (challengeId, attestation, deviceName) =>
  AuthKit.recoveryComplete(challengeId, attestation, deviceName);
