// src/index.ts
import * as Register from "./flows/register";
import * as Login from "./flows/login";
import * as Recovery from "./flows/recovery";
import * as Devices from "./flows/devices";
import * as Profile from "./flows/profile";
import * as Account from "./flows/account";
import * as Google from "./flows/google";
import { http } from "./http/client";

type AuthKitConfig = {
  baseUrl?: string;
  googleClientId?: string;
};

export const AuthKit = {
  init(config: AuthKitConfig = {}) {
    if (config.baseUrl) {
      http.setBase(config.baseUrl);
    }

    if (config.googleClientId) {
      Google.configureGoogle(config.googleClientId);
    }
  },

  // Passkey
  register: Register.register,
  login: Login.login,
  recoveryBegin: Recovery.recoveryBegin,
  recoveryVerifyCode: Recovery.recoveryVerifyCode,
  recoveryComplete: Recovery.recoveryComplete,

  // Google
  googleRegister: Google.googleRegister,
  googleLogin: Google.googleLogin,

  // Devices
  listDevices: Devices.listDevices,
  renameDevice: Devices.renameDevice,
  deleteDevice: Devices.deleteDevice,
  addDeviceBegin: Devices.addDeviceBegin,
  addDeviceComplete: Devices.addDeviceComplete,

  // Profile
  me: Profile.me,

  // Account
  deleteAccount: Account.deleteAccount,
};
