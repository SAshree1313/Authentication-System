import api from "../axios.ts"; 
import {
  prepareCredentialCreateOptions,
  prepareAssertionOptions,
  attestationToJSON,
  assertionToJSON,
  bufferToBase64URL,
} from "../utils/WebAuthn.ts";
 
// ------------------------------
// Registration
// ------------------------------
export async function startRegister({ name, email }) {
  const res = await api.post("/passkey/register/begin", { name, email });
  return res.data; // Should return { options, challengeId }
}

export async function finishRegister(challengeId, options, deviceName = null) {
  const publicKey = prepareCredentialCreateOptions(options);

  const credential = await navigator.credentials.create({ publicKey });

  const payload = {
    challengeId,
    ...attestationToJSON(credential),
    DeviceName: deviceName // optional, backend saves device name
  };

  const res = await api.post("/passkey/register/complete", payload);
  return res.data; // Should return { success, token, recoveryCode, message }
}

// ------------------------------
// Login
// ------------------------------
export async function startLogin({ email }) {
  const res = await api.post("/passkey/login/begin", { email });
  return res.data;
}

export async function finishLogin(challengeId, options) {
  const publicKey = prepareAssertionOptions(options);

  const assertion = await navigator.credentials.get({ publicKey });

  const payload = {
    ChallengeId: challengeId,
    Id: assertion.id,
    RawId: bufferToBase64URL(assertion.rawId),
    Type: assertion.type,
    Response: {
      ClientDataJSON: bufferToBase64URL(assertion.response.clientDataJSON),
      AuthenticatorData: bufferToBase64URL(assertion.response.authenticatorData),
      Signature: bufferToBase64URL(assertion.response.signature),
      UserHandle: assertion.response.userHandle ? bufferToBase64URL(assertion.response.userHandle) : null,
    },
  };

  const res = await api.post("/passkey/login/complete", payload);
  return res.data;
}


/* ---------------------------
   PASSKEY RECOVERY
--------------------------- */

// Step 1: Start recovery with email
export async function startRecovery(email) {
  const res = await api.post("/passkey/recovery/begin", { Email: email });
  return res.data; // { success, message, challengeId }
}

// Step 2: Verify recovery code
export async function verifyRecoveryCode(challengeId, recoveryCode) {
  const res = await api.post("/passkey/recovery/verify-code", {
    ChallengeId: challengeId,
    RecoveryCode: recoveryCode,
  });
  return res.data; // { challengeId, options }
}

// Step 3: Complete recovery (generate passkey)
export async function finishRecovery(challengeId, fidoOptions, deviceName) {
  const publicKey = prepareCredentialCreateOptions(fidoOptions);

  const credential = await navigator.credentials.create({ publicKey });

  const payload = {
    ChallengeId: challengeId,
    RawId: bufferToBase64URL(credential.rawId),
    DeviceName: deviceName,
    Response: {
      AttestationObject: bufferToBase64URL(
        credential.response.attestationObject
      ),
      ClientDataJSON: bufferToBase64URL(
        credential.response.clientDataJSON
      ),
    },
  };

  const res = await api.post("/passkey/recovery/complete", payload);
  return res.data;
}