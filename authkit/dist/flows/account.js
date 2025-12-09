import { http } from "../http/client";
export async function deleteAccount(token) {
    return await http.del("/passkey/delete-account", token);
}
