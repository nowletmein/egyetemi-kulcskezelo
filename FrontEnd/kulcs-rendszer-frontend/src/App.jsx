import { useState } from "react";
import Header from "./Components/header.jsx";
import Footer from "./Components/footer.jsx";
import Login from "./Components/login.jsx";
import Register from "./Components/register.jsx";
import Dashboard from "./Components/dashboard.jsx";
import UserProfile from "./Components/userprofile.jsx";
import "./App.css";

function App() {
  const [currentPage, setCurrentPage] = useState("login");
  const [user, setUser] = useState(null);

  const handleLogin = () => {
    setUser({ name: "Kovács Balázs", email: "student@unikey.hu" });
    setCurrentPage("dashboard");
  };

  const handleLogout = () => {
    setUser(null);
    setCurrentPage("login");
  };

  return (
    <div className="app-shell">
      <Header
        user={user}
        onLogout={handleLogout}
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />

      <main id="center">
        {currentPage === "login" && (
          <Login
            onLogin={handleLogin}
            onNavigateRegister={() => setCurrentPage("register")}
          />
        )}

        {currentPage === "register" && (
          <Register
            onRegisterSuccess={handleLogin}
            onNavigateLogin={() => setCurrentPage("login")}
          />
        )}

        {currentPage === "dashboard" && <Dashboard />}

        {currentPage === "profile" && <UserProfile user={user} />}

        {currentPage === "my-bookings" && (
          <div className="auth-card">
            <h2>Saját Aktív Kulcsok</h2>
            <p>Jelenleg kikért kulcsaid és lejárati időd.</p>
            <div
              style={{
                marginTop: "20px",
                textAlign: "left",
                padding: "15px",
                background: "var(--code-bg)",
                borderRadius: "8px",
              }}
            >
              <strong>I.101 Lab Kulcs</strong>
              <p style={{ fontSize: "14px", marginTop: "5px" }}>
                Lejárat: 16:30 (Hátralévő idő: 1ó 20p)
              </p>
            </div>
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
}

export default App;
