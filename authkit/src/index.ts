// src/index.ts
import * as Register from "./flows/register";
import * as Login from "./flows/login";
import * as Recovery from "./flows/recovery";
import * as Devices from "./flows/devices";
import * as Profile from "./flows/profile";
import * as Account from "./flows/account";
import { http } from "./http/client";


export const AuthKit = {
init(baseUrl: string = "http://localhost:8080/api") {
http.setBase(baseUrl);
},


// Flows
register: Register.register,
login: Login.login,
recoveryBegin: Recovery.recoveryBegin,
recoveryVerifyCode: Recovery.recoveryVerifyCode,
recoveryComplete: Recovery.recoveryComplete,


// Devices
listDevices: Devices.listDevices,
renameDevice: Devices.renameDevice,
deleteDevice: Devices.deleteDevice,
addDeviceBegin: Devices.addDeviceBegin,
addDeviceComplete: Devices.addDeviceComplete,


// Profile
me: Profile.me,

// Delete Account
deleteAccount: Account.deleteAccount,
};