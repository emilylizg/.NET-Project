import "./TransactionList.css";

function TransactionList({ transactions }) {
  if (!transactions || transactions.length === 0) {
    return <p className="no-transactions">No transactions found.</p>;
  }

  return (
    <div className="transaction-list-container">
      <table className="transaction-table">
        <thead>
          <tr>
            <th>Title</th>
            <th>Amount</th>
            <th>Category</th>
            <th>Date</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((transaction) => (
            <tr key={transaction.Id}>
              <td>{transaction.Title}</td>
              <td>₹{parseFloat(transaction.Amount).toFixed(2)}</td>
              <td>{transaction.Category}</td>
              <td>{new Date(transaction.TransactionDate).toLocaleDateString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default TransactionList;
