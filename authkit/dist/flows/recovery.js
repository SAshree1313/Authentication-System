// src/flows/recovery.ts
import { http } from "../http/client";
import { performRegistration } from "../webauthn/perform";
export async function recoveryBegin(email) {
    return await http.post("/passkey/recovery/begin", { email });
}
export async function recoveryVerifyCode(challengeId, recoveryCode) {
    return await http.post("/passkey/recovery/verify-code", { challengeId, recoveryCode });
}
export async function recoveryComplete(challengeId, optionsFromServer, deviceName) {
    const att = await performRegistration(optionsFromServer);
    return await http.post("/passkey/recovery/complete", {
        challengeId,
        id: att.id,
        rawId: att.rawId,
        type: att.type,
        response: {
            attestationObject: att.response.attestationObject,
            clientDataJSON: att.response.clientDataJSON
        },
        deviceName
    });
}
