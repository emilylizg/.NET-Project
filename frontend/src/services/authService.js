import axios from "axios";

const API_URL = "http://localhost:5000/auth";

// Register new user
const register = async (userData) => {
  const res = await axios.post(`${API_URL}/register`, userData);
  return res.data;
};

// Login user
const login = async (userData) => {
  const res = await axios.post(`${API_URL}/login`, userData);
  return res.data.token;
};

export default { register, login };
