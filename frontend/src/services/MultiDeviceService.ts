import api from "../axios.ts";

// Fetch all devices
export async function getDevices() {
  const res = await api.get("/passkey/device/list");
  return res.data; // { devices: PasskeyDeviceDto[] }
}

// Update device name
export async function updateDeviceName(credentialId: string, deviceName: string) {
  const res = await api.put(`/passkey/device/${credentialId}`, { deviceName });
  return res.data; // updated device
}

// Delete device
export async function deleteDevice(credentialId: string) {
  const res = await api.delete(`/passkey/device/${credentialId}`);
  return res.data; // { success: true }
}

// Begin add device
export async function startRegisterExistingDevice() {
  const res = await api.post("/passkey/device/add/begin");
  return res.data; // { challengeId, options }
}

// Complete add device
export async function finishRegister(challengeId: string, attestationResponse: any, deviceName: string) {
  const res = await api.post("/passkey/device/add/complete", {
    ChallengeId: challengeId,
    ...attestationResponse,
    DeviceName: deviceName,
  });
  return res.data; // { success, credentialId, token, message }
}

// Delete account
export async function deleteAccount() {
  const res = await api.delete("/passkey/delete-account");
  return res.data;
}
