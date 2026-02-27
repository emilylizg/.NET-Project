import { useEffect, useState } from "react";
import SummaryBlocks from "../../Components/SummaryBlocks/SummaryBlocks.js"; // <-- corrected path
import { Link } from "react-router-dom";
import transactionService from "../../services/transactionService"; // <-- corrected path
import "./Home.css";

function Home() {
  const [summary, setSummary] = useState({ income: 0, expense: 0, savings: 0 });

  useEffect(() => {
    async function fetchSummary() {
      try {
        const data = await transactionService.getSummary();
        setSummary(data);
      } catch (err) {
        console.error("Error fetching summary:", err);
      }
    }
    fetchSummary();
  }, []);

  return (
    <div className="home">
      <SummaryBlocks summary={summary} />
      <div className="home-buttons">
        <Link to="/transactions"><button>Add Transaction</button></Link>
        <Link to="/dashboard"><button>View Summary / Charts</button></Link>
      </div>
    </div>
  );
}

export default Home;
