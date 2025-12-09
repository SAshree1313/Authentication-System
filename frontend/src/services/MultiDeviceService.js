// src/services/MultiDeviceService.js
import AuthKit from "../setupAuthKit.ts";

// DEVICE LIST
export const getDevices = (token) =>
  AuthKit.listDevices(token);

// RENAME DEVICE
export const updateDeviceName = ({ id, name, token }) =>
  AuthKit.renameDevice(id, name, token);

// DELETE DEVICE
export const deleteDevice = ({ id, token }) =>
  AuthKit.deleteDevice(id, token);

// ADD DEVICE (BEGIN)
export const startRegisterExistingDevice = (token) =>
  AuthKit.addDeviceBegin(token);

// ADD DEVICE (COMPLETE)
export const finishRegister = ({ challengeId, attestation, deviceName, token }) =>
  AuthKit.addDeviceComplete(challengeId, attestation, deviceName, token);

// DELETE ACCOUNT
export const deleteAccount = (token) =>
  AuthKit.deleteAccount(token);
