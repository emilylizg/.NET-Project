import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import Navbar from "./Components/Navbar/Navbar.js";
import Footer from "./Components/Footer/Footer.js";
import Home from "./Pages/Home/Home.js";
import Register from "./Pages/Register/Register.js";
import Login from "./Pages/Login/Login.js";
import Transactions from "./Pages/Transactions/Transactions.js";
import PrivateRoute from "./Components/PrivateRoute/PrivateRoute.js";
import Dashboard from "./Pages/Dashboard/Dashboard.js"; 

function App() {
  const token = localStorage.getItem("token");

  return (
    <Router>
      <Navbar />
      <Routes>

        {/* Default Route Logic */}
        <Route
          path="/"
          element={
            token ? <Navigate to="/home" replace /> : <Navigate to="/login" replace />
          }
        />

        {/* Public Routes */}
        <Route path="/register" element={<Register />} />
        <Route
          path="/login"
          element={
            token ? <Navigate to="/home" replace /> : <Login />
          }
        />

        {/* Protected Routes */}
        <Route
          path="/home"
          element={
            <PrivateRoute>
              <Home />
            </PrivateRoute>
          }
        />

        <Route
          path="/transactions"
          element={
            <PrivateRoute>
              <Transactions />
            </PrivateRoute>
          }
        />

        { <Route
          path="/dashboard"
          element={
            <PrivateRoute>
              <Dashboard />
            </PrivateRoute>
          }
        /> }

      </Routes>
      <Footer />
    </Router>
  );
}

export default App;