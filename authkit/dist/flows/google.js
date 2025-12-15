// authkit/flows/google.ts
import { http } from "../http/client";
/**
 * Google configuration injected by AuthKit.init
 */
let googleClientId = null;
export function configureGoogle(clientId) {
    googleClientId = clientId;
}
/**
 * Google SDK loader (idempotent)
 */
let googleSdkLoaded = false;
function loadGoogleSdk() {
    return new Promise((resolve, reject) => {
        if (googleSdkLoaded) {
            resolve();
            return;
        }
        const existing = document.querySelector('script[src="https://accounts.google.com/gsi/client"]');
        if (existing) {
            googleSdkLoaded = true;
            resolve();
            return;
        }
        const script = document.createElement("script");
        script.src = "https://accounts.google.com/gsi/client";
        script.async = true;
        script.defer = true;
        script.onload = () => {
            googleSdkLoaded = true;
            resolve();
        };
        script.onerror = () => {
            reject(new Error("Failed to load Google Identity Services SDK"));
        };
        document.head.appendChild(script);
    });
}
/**
 * Retrieve Google ID token
 */
async function getGoogleIdToken() {
    if (!googleClientId) {
        throw new Error("Google Client ID not configured. Pass googleClientId to AuthKit.init().");
    }
    await loadGoogleSdk();
    return new Promise((resolve, reject) => {
        if (!window.google?.accounts?.id) {
            reject(new Error("Google SDK not available"));
            return;
        }
        // Timeout after 30 seconds if no response
        const timeout = setTimeout(() => {
            reject(new Error("Google sign-in timed out. Please check your Google Cloud Console authorized origins."));
        }, 30000);
        window.google.accounts.id.initialize({
            client_id: googleClientId,
            callback: (response) => {
                clearTimeout(timeout);
                if (!response?.credential) {
                    reject(new Error("No Google credential received"));
                    return;
                }
                resolve(response.credential);
            },
        });
        window.google.accounts.id.prompt((notification) => {
            // FedCM-compatible: check for dismissal without deprecated methods
            if (notification.getDismissedReason && notification.getDismissedReason()) {
                clearTimeout(timeout);
                reject(new Error("Google sign-in was cancelled"));
            }
        });
    });
}
/**
 * Public SDK APIs
 */
export async function googleLogin() {
    const idToken = await getGoogleIdToken();
    return http.post("/auth/google/login", { idToken });
}
export async function googleRegister() {
    const idToken = await getGoogleIdToken();
    return http.post("/auth/google/register", { idToken });
}
