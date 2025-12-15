// frontend/src/setupAuthKit.ts
import { AuthKit } from "authkit";

AuthKit.init({
  baseUrl: "http://localhost:8080/api",
  googleClientId: process.env.REACT_APP_GOOGLE_CLIENT_ID
});

export default AuthKit;
    