// src/webauthn/perform.ts
import { arrayBufferToBase64url } from "./converters";
import { prepareCreateOptions, prepareGetOptions } from "./prepare";
export async function performRegistration(optionsFromServer) {
    const publicKey = prepareCreateOptions(optionsFromServer);
    const cred = (await navigator.credentials.create({ publicKey }));
    const att = cred.response;
    return {
        id: cred.id,
        rawId: arrayBufferToBase64url(cred.rawId),
        type: cred.type,
        response: {
            clientDataJSON: arrayBufferToBase64url(att.clientDataJSON),
            attestationObject: arrayBufferToBase64url(att.attestationObject),
        },
    };
}
export async function performLogin(optionsFromServer) {
    const publicKey = prepareGetOptions(optionsFromServer);
    const cred = (await navigator.credentials.get({ publicKey }));
    const res = cred.response;
    return {
        id: cred.id,
        rawId: arrayBufferToBase64url(cred.rawId),
        type: cred.type,
        response: {
            clientDataJSON: arrayBufferToBase64url(res.clientDataJSON),
            authenticatorData: arrayBufferToBase64url(res.authenticatorData),
            signature: arrayBufferToBase64url(res.signature),
            userHandle: res.userHandle ? arrayBufferToBase64url(res.userHandle) : null,
        },
    };
}
