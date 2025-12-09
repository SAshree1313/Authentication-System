import fetch from "cross-fetch";
const DEFAULT_BASE = "http://localhost:8080/api";
class HttpClient {
    constructor() {
        this.baseUrl = DEFAULT_BASE;
    }
    setBase(url) {
        this.baseUrl = url.replace(/\/+$/, "");
    }
    // Remove token from SDK
    headers(extra = {}) {
        return {
            "Content-Type": "application/json",
            ...extra
        };
    }
    async post(path, body, token) {
        const res = await fetch(this.baseUrl + path, {
            method: "POST",
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers(),
            body: JSON.stringify(body)
        });
        const text = await res.text();
        return text ? JSON.parse(text) : {};
    }
    async put(path, body, token) {
        const res = await fetch(this.baseUrl + path, {
            method: "PUT",
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers(),
            body: JSON.stringify(body)
        });
        const text = await res.text();
        return text ? JSON.parse(text) : {};
    }
    async get(path, token) {
        const res = await fetch(this.baseUrl + path, {
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers()
        });
        const text = await res.text();
        return text ? JSON.parse(text) : {};
    }
    async del(path, token) {
        const res = await fetch(this.baseUrl + path, {
            method: "DELETE",
            headers: token
                ? { ...this.headers(), Authorization: `Bearer ${token}` }
                : this.headers()
        });
        const text = await res.text();
        return text ? JSON.parse(text) : {};
    }
}
export const http = new HttpClient();
