// //
// // ---------------------------------------------------------
// //  Base64URL <-> ArrayBuffer Utilities
// // ---------------------------------------------------------
// //

// export function bufferToBase64URL(buffer: ArrayBuffer): string {
//   const bytes = new Uint8Array(buffer);
//   let binary = "";
//   for (let b of bytes) binary += String.fromCharCode(b);
//   return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");
// }

// export function base64URLToBuffer(base64url: string): ArrayBuffer {
//   let base64 = base64url.replace(/-/g, "+").replace(/_/g, "/");
//   while (base64.length % 4 !== 0) base64 += "=";
//   const binary = atob(base64);
//   const bytes = new Uint8Array(binary.length);
//   for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
//   return bytes.buffer;
// }

// //
// // ---------------------------------------------------------
// //  Prepare Backend → WebAuthn Options
// // ---------------------------------------------------------
// //

// export function prepareCredentialCreateOptions(options: any) {
//   return {
//     ...options,
//     challenge: base64URLToBuffer(options.challenge),
//     user: {
//       ...options.user,
//       id: base64URLToBuffer(options.user.id),
//     },
//     excludeCredentials: (options.excludeCredentials || []).map((cred: any) => ({
//       ...cred,
//       id: base64URLToBuffer(cred.id),
//     })),
//   };
// }

// export function prepareAssertionOptions(options: any) {
//   return {
//     ...options,
//     challenge: base64URLToBuffer(options.challenge),
//     allowCredentials: (options.allowCredentials || []).map((cred: any) => ({
//       ...cred,
//       id: base64URLToBuffer(cred.id),
//     })),
//   };
// }

// //
// // ---------------------------------------------------------
// //  Convert WebAuthn → Backend
// // ---------------------------------------------------------
// //

// export function attestationToJSON(cred: PublicKeyCredential) {
//   const att = cred.response as AuthenticatorAttestationResponse;

//   return {
//     id: cred.id,
//     rawId: bufferToBase64URL(cred.rawId),
//     type: cred.type,
//     response: {
//       clientDataJSON: bufferToBase64URL(att.clientDataJSON),
//       attestationObject: bufferToBase64URL(att.attestationObject),
//     },
//   };
// }

// export function assertionToJSON(cred: PublicKeyCredential) {
//   const assn = cred.response as AuthenticatorAssertionResponse;

//   return {
//     id: cred.id,
//     rawId: bufferToBase64URL(cred.rawId),
//     type: cred.type,
//     response: {
//       clientDataJSON: bufferToBase64URL(assn.clientDataJSON),
//       authenticatorData: bufferToBase64URL(assn.authenticatorData),
//       signature: bufferToBase64URL(assn.signature),
//       userHandle: assn.userHandle ? bufferToBase64URL(assn.userHandle) : null,
//     },
//   };
// }

// //
// // ---------------------------------------------------------
// //  High-Level WebAuthn Actions
// // ---------------------------------------------------------
// //

// export async function createPasskey(optionsFromServer: any) {
//   const publicKey = prepareCredentialCreateOptions(optionsFromServer);
//   const credential = (await navigator.credentials.create({
//     publicKey,
//   })) as PublicKeyCredential;

//   return attestationToJSON(credential);
// }

// export async function getPasskeyAssertion(optionsFromServer: any) {
//   const publicKey = prepareAssertionOptions(optionsFromServer);
//   const assertion = (await navigator.credentials.get({
//     publicKey,
//   })) as PublicKeyCredential;

//   return assertionToJSON(assertion);
// }
