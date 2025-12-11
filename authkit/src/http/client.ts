import fetch from "cross-fetch";

const DEFAULT_BASE = "http://localhost:8080/api";

class HttpClient {
  baseUrl: string = DEFAULT_BASE;

  // Optional unauthorized callback
  private unauthorizedHandler?: () => void;

  setBase(url: string) {
    this.baseUrl = url.replace(/\/+$/, "");
  }

  // Called by frontend to hook 401 behavior
  setUnauthorizedHandler(handler: () => void) {
    this.unauthorizedHandler = handler;
  }

  private headers(extra: Record<string, string> = {}) {
    return {
      "Content-Type": "application/json",
      ...extra
    };
  }

  // Centralized JSON parse + 401 handler
  private async handleResponse(res: Response) {
  const text = await res.text();

  let json: any = {};
  try {
    json = text ? JSON.parse(text) : {};
  } catch {
    json = {}; // Protect against parse errors
  }

  if (res.status === 401 && this.unauthorizedHandler) {
    this.unauthorizedHandler();
  }

  return json;
}


  async post(path: string, body: any, token?: string) {
    const res = await fetch(this.baseUrl + path, {
      method: "POST",
      headers: token
        ? { ...this.headers(), Authorization: `Bearer ${token}` }
        : this.headers(),
      body: JSON.stringify(body)
    });

    return this.handleResponse(res);
  }

  async put(path: string, body: any, token?: string) {
    const res = await fetch(this.baseUrl + path, {
      method: "PUT",
      headers: token
        ? { ...this.headers(), Authorization: `Bearer ${token}` }
        : this.headers(),
      body: JSON.stringify(body)
    });

    return this.handleResponse(res);
  }

  async get(path: string, token?: string) {
    const res = await fetch(this.baseUrl + path, {
      headers: token
        ? { ...this.headers(), Authorization: `Bearer ${token}` }
        : this.headers()
    });

    return this.handleResponse(res);
  }

  async del(path: string, token?: string) {
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
