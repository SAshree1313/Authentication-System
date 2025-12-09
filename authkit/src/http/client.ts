import fetch from "cross-fetch";

const DEFAULT_BASE = "http://localhost:8080/api";

class HttpClient {
  baseUrl: string = DEFAULT_BASE;

  setBase(url: string) {
    this.baseUrl = url.replace(/\/+$/, "");
  }

  // Remove token from SDK
  private headers(extra: Record<string, string> = {}) {
    return {
      "Content-Type": "application/json",
      ...extra
    };
  }

  async post(path: string, body: any, token?: string) {
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

  async put(path: string, body: any, token?: string) {
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

  async get(path: string, token?: string) {
    const res = await fetch(this.baseUrl + path, {
      headers: token
        ? { ...this.headers(), Authorization: `Bearer ${token}` }
        : this.headers()
    });

    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async del(path: string, token?: string) {
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
