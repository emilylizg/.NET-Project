import React, { useState, useEffect } from "react";
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from "recharts";
import "./Dashboard.css";

function Dashboard() {
  const [frequency, setFrequency] = useState("monthly");
  const [data, setData] = useState({
    Food: 0,
    Medical: 0,
    Utilities: 0,
    Other: 0,
    Travel: 0,
    Income: 0,
  });
  const [weeklyData, setWeeklyData] = useState({
    Food: 0,
    Medical: 0,
    Utilities: 0,
    Other: 0,
    Travel: 0,
  });
  const [monthlyData, setMonthlyData] = useState({
    Food: 0,
    Medical: 0,
    Utilities: 0,
    Other: 0,
    Travel: 0,
  });
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const categories = ["Food", "Medical", "Utilities", "Other", "Travel"];
  const COLORS = ["#636B2F", "#8B9556", "#B4BE82", "#D4DE95", "#9BA765"];

  // 🔹 Fetch Dashboard Summary
  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        setLoading(true);
        setError(null);

        const token = localStorage.getItem("token");

        const response = await fetch(
          `http://localhost:5000/dashboard?period=${frequency}`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (!response.ok) {
          throw new Error("Failed to fetch dashboard data");
        }

        const res = await response.json();

        setData({
          Food: res.Food || 0,
          Medical: res.Medical || 0,
          Utilities: res.Utilities || 0,
          Other: res.Other || 0,
          Travel: res.Travel || 0,
          Income: res.Income || 0,
        });

      } catch (err) {
        console.error("Dashboard error:", err);
        setError("Could not load dashboard data.");
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, [frequency]);

  // 🔹 Fetch Weekly and Monthly Data for Pie Charts
  useEffect(() => {
    const fetchChartData = async () => {
      try {
        const token = localStorage.getItem("token");

        // Fetch weekly data
        const weeklyResponse = await fetch(
          `http://localhost:5000/dashboard?period=weekly`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );
        if (weeklyResponse.ok) {
          const weeklyRes = await weeklyResponse.json();
          setWeeklyData({
            Food: weeklyRes.Food || 0,
            Medical: weeklyRes.Medical || 0,
            Utilities: weeklyRes.Utilities || 0,
            Other: weeklyRes.Other || 0,
            Travel: weeklyRes.Travel || 0,
          });
        }

        // Fetch monthly data
        const monthlyResponse = await fetch(
          `http://localhost:5000/dashboard?period=monthly`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );
        if (monthlyResponse.ok) {
          const monthlyRes = await monthlyResponse.json();
          setMonthlyData({
            Food: monthlyRes.Food || 0,
            Medical: monthlyRes.Medical || 0,
            Utilities: monthlyRes.Utilities || 0,
            Other: monthlyRes.Other || 0,
            Travel: monthlyRes.Travel || 0,
          });
        }
      } catch (err) {
        console.error("Chart data fetch error:", err);
      }
    };

    fetchChartData();
  }, []);

  // 🔹 Fetch All Transactions
  useEffect(() => {
    const fetchTransactions = async () => {
      try {
        const token = localStorage.getItem("token");

        const response = await fetch(
          "http://localhost:5000/transactions",
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (!response.ok) {
          throw new Error("Failed to fetch transactions");
        }

        const result = await response.json();
        setTransactions(result);

      } catch (err) {
        console.error("Transaction fetch error:", err);
      }
    };

    fetchTransactions();
  }, []);

  const totalExpense = categories.reduce(
    (sum, category) => sum + (data[category] || 0),
    0
  );

  // Prepare data for pie charts
  const weeklyChartData = categories
    .map((category) => ({
      name: category,
      value: weeklyData[category] || 0,
    }))
    .filter((item) => item.value > 0);

  const monthlyChartData = categories
    .map((category) => ({
      name: category,
      value: monthlyData[category] || 0,
    }))
    .filter((item) => item.value > 0);

  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <h1>Expense Analytics</h1>

        <select
          className="frequency-select"
          value={frequency}
          onChange={(e) => setFrequency(e.target.value)}
        >
          <option value="daily">Daily</option>
          <option value="weekly">Weekly</option>
          <option value="monthly">Monthly</option>
        </select>
      </div>

      {loading && <p>Loading dashboard...</p>}
      {error && <p className="error-text">{error}</p>}

      {!loading && !error && (
        <>
          <div className="category-grid">
            {categories.map((category) => (
              <div key={category} className="category-card">
                <h3>{category}</h3>
                <div className="category-amount">
                  ₹{(data[category] || 0).toLocaleString()}
                </div>
              </div>
            ))}
          </div>

          <div className="summary-section">
            <div className="summary-box">
              <h4>Grand Total Expense</h4>
              <p>₹{totalExpense.toLocaleString()}</p>
            </div>

            <div className="summary-box">
              <h4>Grand Total Income</h4>
              <p>₹{(data.Income || 0).toLocaleString()}</p>
            </div>
          </div>

          {/* 🔹 Pie Charts Section */}
          <div className="charts-section">
            <div className="chart-container">
              <h3>This Week's Expenditure</h3>
              {weeklyChartData.length > 0 ? (
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={weeklyChartData}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ name, percent }) =>
                        `${name}: ${(percent * 100).toFixed(0)}%`
                      }
                      outerRadius={100}
                      fill="#8884d8"
                      dataKey="value"
                    >
                      {weeklyChartData.map((entry, index) => (
                        <Cell
                          key={`cell-${index}`}
                          fill={COLORS[index % COLORS.length]}
                        />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value) => `₹${value.toLocaleString()}`} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <p className="no-data-message">No weekly expenses recorded</p>
              )}
            </div>

            <div className="chart-container">
              <h3>This Month's Expenditure</h3>
              {monthlyChartData.length > 0 ? (
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={monthlyChartData}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ name, percent }) =>
                        `${name}: ${(percent * 100).toFixed(0)}%`
                      }
                      outerRadius={100}
                      fill="#8884d8"
                      dataKey="value"
                    >
                      {monthlyChartData.map((entry, index) => (
                        <Cell
                          key={`cell-${index}`}
                          fill={COLORS[index % COLORS.length]}
                        />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value) => `₹${value.toLocaleString()}`} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <p className="no-data-message">No monthly expenses recorded</p>
              )}
            </div>
          </div>
        </>
      )}

      {/* 🔹 Transactions Table */}
      <h2 style={{ marginTop: "40px" }}>All Transactions</h2>

      <table className="transaction-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Title</th>
            <th>Category</th>
            <th>Type</th>
            <th>Amount</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((tx) => (
            <tr key={tx.Id}>
              <td>
                {new Date(tx.TransactionDate).toLocaleDateString()}
              </td>
              <td>{tx.Title}</td>
              <td>{tx.Category}</td>
              <td>{tx.Type}</td>
              <td>₹{tx.Amount.toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default Dashboard;