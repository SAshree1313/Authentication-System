// src/webauthn/converters.ts
export function base64urlToArrayBuffer(base64url: string): ArrayBuffer {
let base64 = base64url.replace(/-/g, "+").replace(/_/g, "/");
while (base64.length % 4) base64 += "=";
const binary = atob(base64);
const bytes = new Uint8Array(binary.length);
for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
return bytes.buffer;
}


export function arrayBufferToBase64url(buffer: ArrayBuffer): string {
const bytes = new Uint8Array(buffer);
let binary = "";
for (let b of bytes) binary += String.fromCharCode(b);
return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}