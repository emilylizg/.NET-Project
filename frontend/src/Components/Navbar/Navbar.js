import { Link, useNavigate } from "react-router-dom";
import "./Navbar.css";

function Navbar() {

  const handleLogout = (e) => {
    e.preventDefault();        // stop default navigation
    localStorage.clear();      // clear stored token/data
    window.location.href = "/login";       // redirect to login
  };

  return (
    <nav className="navbar">
      <h2>Hisaab-Kitaab</h2>
      <ul>
        <li><Link to="/">Home</Link></li>
        <li><Link to="/transactions">Transactions</Link></li>
        <li><Link to="/dashboard">Dashboard</Link></li>
        <li>
          <Link to="/login" onClick={handleLogout}>
            Logout
          </Link>
        </li>
      </ul>
    </nav>
  );
}

export default Navbar;