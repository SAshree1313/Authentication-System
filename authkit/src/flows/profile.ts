// src/flows/profile.ts
import { http } from "../http/client";

export async function me(token: string) {
  return await http.get("/auth/me", token);
}