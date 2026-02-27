import axios from "axios";

const API_URL = "http://localhost:5000/transactions";

const getAuthConfig = () => {
  const token = localStorage.getItem("token");

  if (!token) {
    console.error("No token found in localStorage");
    return {};
  }

  return {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  };
};

// Get summary
const getSummary = async () => {
  const res = await axios.get(`${API_URL}/summary`, getAuthConfig());
  return res.data;
};

// Add new transaction
const addTransaction = async (transaction) => {
  const res = await axios.post(`${API_URL}`, transaction, getAuthConfig());
  return res.data;
};

// Get all transactions
const getTransactions = async () => {
  const res = await axios.get(API_URL, getAuthConfig());
  return res.data;
};

export default { getSummary, addTransaction, getTransactions };