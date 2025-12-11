import fetch from "cross-fetch";
const DEFAULT_BASE = "http://localhost:8080/api";
class HttpClient {
    constructor() {
        this.baseUrl = DEFAULT_BASE;
    }
    setBase(url) {
        this.baseUrl = url.replace(/\/+$/, "");
    }
    // Called by frontend to hook 401 behavior
    setUnauthorizedHandler(handler) {
        this.unauthorizedHandler = handler;
    }
    headers(extra = {}) {
        return {
            "Content-Type": "application/json",
            ...extra
        };
    }
    // Centralized JSON parse + 401 handler
    async handleResponse(res) {
        const text = await res.text();
        let json = {};
        try {
            json = text ? JSON.parse(text) : {};
        }
        catch {
            json = {}; // Protect against parse errors
        }
        if (res.status === 401 && this.unauthorizedHandler) {
            this.unauthorizedHandler();
        }
        return json;
    }
    async post(path, body, token) {
        const res = await fetch(this.baseUrl + path, {
            method: "POST",
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers(),
            body: JSON.stringify(body)
        });
        return this.handleResponse(res);
    }
    async put(path, body, token) {
        const res = await fetch(this.baseUrl + path, {
            method: "PUT",
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers(),
            body: JSON.stringify(body)
        });
        return this.handleResponse(res);
    }
    async get(path, token) {
        const res = await fetch(this.baseUrl + path, {
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers()
        });
        return this.handleResponse(res);
    }
    async del(path, token) {
        const res = await fetch(this.baseUrl + path, {
            method: "DELETE",
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers()
        });
        return this.handleResponse(res);
    }
}
export const http = new HttpClient();
