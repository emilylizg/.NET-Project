import { useState, useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import TransactionForm from "../../Components/TransactionForm/TransactionForm";
import TransactionList from "../../Components/TransactionList/TransactionList";
import transactionService from "../../services/transactionService";
import "./Transactions.css";

function Transactions() {
  const [searchParams] = useSearchParams();
  const type = searchParams.get("type");
  const [loading, setLoading] = useState(false);
  const [transactions, setTransactions] = useState([]);
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    if (type) {
      fetchTransactions();
    }
  }, [type]);

  useEffect(() => {
    if (successMessage) {
      const timer = setTimeout(() => setSuccessMessage(""), 3000);
      return () => clearTimeout(timer);
    }
  }, [successMessage]);

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const data = await transactionService.getTransactions();
      const filtered = data.filter((t) => t.Type.toLowerCase() === type.toLowerCase());
      setTransactions(filtered);
    } catch (err) {
      console.error("Error fetching transactions:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleAddTransaction = async (data) => {
    try {
      await transactionService.addTransaction(data);
      setSuccessMessage("✓ Transaction submitted successfully!");
      if (type) {
        fetchTransactions();
      }
    } catch (error) {
      console.error("Error adding transaction:", error);
    }
  };

  return (
    <div className="transactions-page">
      {successMessage && <div className="success-message">{successMessage}</div>}
      {type ? (
        <>
          <h1>{type.charAt(0).toUpperCase() + type.slice(1)} Transactions</h1>
          {loading ? <p>Loading...</p> : <TransactionList transactions={transactions} />}
        </>
      ) : (
        <>
          <h1>Add a Transaction</h1>
          <TransactionForm onSubmit={handleAddTransaction} />
        </>
      )}
    </div>
  );
}

export default Transactions;