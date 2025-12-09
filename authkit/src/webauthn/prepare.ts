// src/webauthn/prepare.ts
import { base64urlToArrayBuffer } from "../webauthn/converters";


export function prepareCreateOptions(options: any) {
return {
...options,
challenge: base64urlToArrayBuffer(options.challenge),
user: { ...options.user, id: base64urlToArrayBuffer(options.user.id) },
excludeCredentials: (options.excludeCredentials || []).map((c: any) => ({ ...c, id: base64urlToArrayBuffer(c.id) })),
};
}


export function prepareGetOptions(options: any) {
return {
...options,
challenge: base64urlToArrayBuffer(options.challenge),
allowCredentials: (options.allowCredentials || []).map((c: any) => ({ ...c, id: base64urlToArrayBuffer(c.id) })),
};
}