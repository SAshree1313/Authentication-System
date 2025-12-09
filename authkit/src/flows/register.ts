// src/flows/register.ts
import { http } from "../http/client";
import { performRegistration } from "../webauthn/perform";


export async function register(opts: { name: string; email: string; deviceName?: string }) {
const begin = await http.post("/passkey/register/begin", { name: opts.name, email: opts.email });
const att = await performRegistration(begin.options);
const complete = await http.post("/passkey/register/complete", {
challengeId: begin.challengeId,
id: att.id,
rawId: att.rawId,
response: { clientDataJSON: att.response.clientDataJSON, attestationObject: att.response.attestationObject },
type: att.type,
deviceName: opts.deviceName,
});

return complete;
}