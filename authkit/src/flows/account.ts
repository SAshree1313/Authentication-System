import { http } from "../http/client";

export async function deleteAccount(token: string) {
  return await http.del("/passkey/delete-account", token);
}
