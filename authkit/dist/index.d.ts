import * as Register from "./flows/register";
import * as Login from "./flows/login";
import * as Recovery from "./flows/recovery";
import * as Devices from "./flows/devices";
import * as Profile from "./flows/profile";
import * as Account from "./flows/account";
import * as Google from "./flows/google";
type AuthKitConfig = {
    baseUrl?: string;
    googleClientId?: string;
};
export declare const AuthKit: {
    init(config?: AuthKitConfig): void;
    register: typeof Register.register;
    login: typeof Login.login;
    recoveryBegin: typeof Recovery.recoveryBegin;
    recoveryVerifyCode: typeof Recovery.recoveryVerifyCode;
    recoveryComplete: typeof Recovery.recoveryComplete;
    googleRegister: typeof Google.googleRegister;
    googleLogin: typeof Google.googleLogin;
    listDevices: typeof Devices.listDevices;
    renameDevice: typeof Devices.renameDevice;
    deleteDevice: typeof Devices.deleteDevice;
    addDeviceBegin: typeof Devices.addDeviceBegin;
    addDeviceComplete: typeof Devices.addDeviceComplete;
    me: typeof Profile.me;
    deleteAccount: typeof Account.deleteAccount;
};
export {};
