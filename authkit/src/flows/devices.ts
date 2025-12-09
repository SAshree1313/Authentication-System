import { http } from "../http/client";
import { performRegistration } from "../webauthn/perform";

export async function listDevices(token: string) {
  return await http.get("/passkey/device/list", token);
}

export async function renameDevice(credentialId: string, deviceName: string, token: string) {
  return await http.put(`/passkey/device/${encodeURIComponent(credentialId)}`, { deviceName }, token);
}

export async function deleteDevice(credentialId: string, token: string) {
  return await http.del(`/passkey/device/${encodeURIComponent(credentialId)}`, token);
}

export async function addDeviceBegin(token: string) {
  return await http.post("/passkey/device/add/begin", {}, token);
}

export async function addDeviceComplete(
  challengeId: string,
  optionsFromServer: any,
  deviceName: string | undefined,
  token: string
) {
  const att = await performRegistration(optionsFromServer);

  return await http.post(
    "/passkey/device/add/complete",
    {
      challengeId,
      id: att.id,
      rawId: att.rawId,
      response: {
        clientDataJSON: att.response.clientDataJSON,
        attestationObject: att.response.attestationObject,
      },
      type: att.type,
      deviceName,
    },
    token
  );
}
