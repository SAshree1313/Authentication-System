import api from "../axios.ts"; 
import {
  prepareCredentialCreateOptions,
  prepareAssertionOptions,
  attestationToJSON,
  assertionToJSON,
} from "../utils/WebAuthn.ts";
 
// ------------------------------
// Registration
// ------------------------------
export async function startRegister(userId) {
  const res = await api.post("/passkey/register/begin", { userId });
  return res.data;
}

export async function finishRegister(challengeId, options) {
  const publicKey = prepareCredentialCreateOptions(options);

  const credential = await navigator.credentials.create({ publicKey });

  const payload = {
    challengeId,
    ...attestationToJSON(credential),
  };

  const res = await api.post("/passkey/register/complete", payload);
  return res.data;
}

// ------------------------------
// Login
// ------------------------------
export async function startLogin() {
  const res = await api.post("/passkey/login/begin");
  return res.data;
}

export async function finishLogin(challengeId, options) {
  const publicKey = prepareAssertionOptions(options);

  const assertion = await navigator.credentials.get({ publicKey });

  const payload = {
    challengeId,
    ...assertionToJSON(assertion),
  };

  const res = await api.post("/passkey/login/complete", payload);
  return res.data;
}
