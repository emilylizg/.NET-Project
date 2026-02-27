import { useNavigate } from "react-router-dom";
import "./SummaryBlocks.css";

function SummaryBlocks({ summary }) {
  const navigate = useNavigate();

  return (
    <div className="summary-container">
      <div className="block income" onClick={() => navigate("/transactions?type=income")}>
        <h3>Income</h3><p>{summary.income}</p>
      </div>
      <div className="block expense" onClick={() => navigate("/transactions?type=expense")}>
        <h3>Expense</h3><p>{summary.expense}</p>
      </div>
      <div className="block savings">
        <h3>Savings</h3><p>{summary.savings}</p>
      </div>
    </div>
  );
}

export default SummaryBlocks;
