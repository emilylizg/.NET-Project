import { useState } from "react";
import "./TransactionForm.css";

function TransactionForm({ onSubmit }) {
  const [form, setForm] = useState({
    title: "",
    amount: "",
    type: "Income",
    category: "Food",   // default category
    transactionDate: ""
  });

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (onSubmit) {
      onSubmit(form); // parent will handle API call later
    }
    setForm({
      title: "",
      amount: "",
      type: "Income",
      category: "Food",
      transactionDate: ""
    });
  };

  return (
    <form className="transaction-form" onSubmit={handleSubmit}>
      <h2>Add Transaction</h2>
      <input
        name="title"
        placeholder="Title"
        value={form.title}
        onChange={handleChange}
        required
      />
      <input
        name="amount"
        type="number"
        placeholder="Amount"
        value={form.amount}
        onChange={handleChange}
        required
      />
      <select name="type" value={form.type} onChange={handleChange}>
        <option value="Income">Income</option>
        <option value="Expense">Expense</option>
      </select>

      {/* Category dropdown */}
      <select name="category" value={form.category} onChange={handleChange}>
        <option value="Food">Food</option>
        <option value="Travel">Travel</option>
        <option value="Medical">Medical</option>
        <option value="Utilities">Utilities</option>
        <option value="Others">Others</option>
      </select>

      <input
        name="transactionDate"
        type="date"
        value={form.transactionDate}
        onChange={handleChange}
        required
      />
      <button type="submit">Add Transaction</button>
    </form>
  );
}

export default TransactionForm;
