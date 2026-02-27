import { useState } from "react";
import { Link } from "react-router-dom";   // <-- add this
import "./Login.css";
import authService from "../../services/authService";

function Login() {
  const [form, setForm] = useState({ email: "", password: "" });

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const token = await authService.login(form);
      localStorage.setItem("token", token);
      alert("Login successful!");
      window.location.href = "/home"; // redirect to Home
    } catch (err) {
      alert("Login failed: " + err.message);
    }
  };

  return (
    <div className="login">
      <h2>Login</h2>
      <form onSubmit={handleSubmit}>
        <input name="email" placeholder="Email" onChange={handleChange} />
        <input name="password" type="password" placeholder="Password" onChange={handleChange} />
        <button type="submit">Login</button>
      </form>
      <p className="register-link">
        New user? <Link to="/register">Register here</Link>
      </p>
    </div>
  );
}

export default Login;
