import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5068/api", // your backend base
  withCredentials: false,
});

export default api;
