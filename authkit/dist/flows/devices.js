import { http } from "../http/client";
import { performRegistration } from "../webauthn/perform";
// List devices
export function listDevices(token) {
    return http.get("/passkey/device/list", token);
}
// Rename device
export function renameDevice(credentialId, deviceName, token) {
    return http.put(`/passkey/device/${encodeURIComponent(credentialId)}`, { deviceName }, token);
}
// Delete device — ⚠ backend may return a NEW TOKEN
export function deleteDevice(credentialId, token) {
    return http.del(`/passkey/device/${encodeURIComponent(credentialId)}`, token);
}
// Begin add device
export function addDeviceBegin(token) {
    return http.post("/passkey/device/add/begin", {}, token);
}
// Complete add device — ⚠ backend returns NEW TOKEN
export async function addDeviceComplete(challengeId, optionsFromServer, deviceName, token) {
    const att = await performRegistration(optionsFromServer);
    return http.post("/passkey/device/add/complete", {
        challengeId,
        id: att.id,
        rawId: att.rawId,
        response: {
            clientDataJSON: att.response.clientDataJSON,
            attestationObject: att.response.attestationObject,
        },
        type: att.type,
        deviceName
    }, token);
}
