// src/flows/login.ts
import { http } from "../http/client";
import { performLogin } from "../webauthn/perform";


export async function login(opts: { email: string }) {
const begin = await http.post("/passkey/login/begin", { email: opts.email });
const assertion = await performLogin(begin.options);


const complete = await http.post("/passkey/login/complete", {
challengeId: begin.challengeId,
id: assertion.id,
rawId: assertion.rawId,
response: {
authenticatorData: assertion.response.authenticatorData,
clientDataJSON: assertion.response.clientDataJSON,
signature: assertion.response.signature,
userHandle: assertion.response.userHandle,
},
type: assertion.type,
});

return complete;
}