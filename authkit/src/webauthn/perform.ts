// src/webauthn/perform.ts
import { arrayBufferToBase64url } from "./converters";
import { prepareCreateOptions, prepareGetOptions } from "./prepare";


export async function performRegistration(optionsFromServer: any) {
const publicKey = prepareCreateOptions(optionsFromServer);
const cred = (await navigator.credentials.create({ publicKey })) as PublicKeyCredential;


const att = cred.response as AuthenticatorAttestationResponse;


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


export async function performLogin(optionsFromServer: any) {
const publicKey = prepareGetOptions(optionsFromServer);
const cred = (await navigator.credentials.get({ publicKey })) as PublicKeyCredential;
const res = cred.response as AuthenticatorAssertionResponse;


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