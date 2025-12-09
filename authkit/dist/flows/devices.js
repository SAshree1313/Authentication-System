import { http } from "../http/client";
import { performRegistration } from "../webauthn/perform";
export async function listDevices(token) {
    return await http.get("/passkey/device/list", token);
}
export async function renameDevice(credentialId, deviceName, token) {
    return await http.put(`/passkey/device/${encodeURIComponent(credentialId)}`, { deviceName }, token);
}
export async function deleteDevice(credentialId, token) {
    return await http.del(`/passkey/device/${encodeURIComponent(credentialId)}`, token);
}
export async function addDeviceBegin(token) {
    return await http.post("/passkey/device/add/begin", {}, token);
}
export async function addDeviceComplete(challengeId, optionsFromServer, deviceName, token) {
    const att = await performRegistration(optionsFromServer);
    return await http.post("/passkey/device/add/complete", {
        challengeId,
        id: att.id,
        rawId: att.rawId,
        response: {
            clientDataJSON: att.response.clientDataJSON,
            attestationObject: att.response.attestationObject,
        },
        type: att.type,
        deviceName,
    }, token);
}
