import { useState } from "react";
import Header from "./Components/header.jsx";
import Footer from "./Components/footer.jsx";
import Login from "./Components/login.jsx";
import Register from "./Components/register.jsx";
import Dashboard from "./Components/dashboard.jsx";
import UserProfile from "./Components/userprofile.jsx";
import AdminPage from "./Components/adminpage.jsx";
import BookingsPage from "./Components/bookingspage.jsx";
import {
  getUserFromToken,
  jsonOptions,
  removeToken,
  request,
  restoreSession,
  storeToken,
} from "./api.js";
import "./App.css";

function App() {
  const [session, setSession] = useState(restoreSession);
  const [currentPage, setCurrentPage] = useState(
    session?.user.roles.includes("Admin")
      ? "admin"
      : session
        ? "dashboard"
        : "login",
  );
  const user = session?.user ?? null;

  const getLandingPage = (authenticatedUser) =>
    authenticatedUser.roles.includes("Admin") ? "admin" : "dashboard";

  const handleAuthenticated = (token, fallbackUser = {}) => {
    const tokenUser = getUserFromToken(token);
    const authenticatedUser = {
      ...fallbackUser,
      ...tokenUser,
      name: tokenUser.name || fallbackUser.name || "",
      email: tokenUser.email || fallbackUser.email || "",
    };

    storeToken(token);
    setSession({ token, user: authenticatedUser });
    setCurrentPage(getLandingPage(authenticatedUser));
  };

  const handleLogin = async (email, password) => {
    const token = await request(
      "/User/login",
      jsonOptions("POST", { Name: "", Email: email.trim(), Password: password }),
    );
    if (typeof token !== "string" || !token) {
      throw new Error("A bejelentkezés nem adott vissza tokent.");
    }

    handleAuthenticated(token, { email: email.trim() });
  };

  const handleRegister = async (name, email, password) => {
    const token = await request(
      "/User/create",
      jsonOptions("POST", {
        Name: name.trim(),
        Email: email.trim(),
        Password: password,
      }),
    );
    if (typeof token !== "string" || !token) {
      throw new Error("A regisztráció nem adott vissza tokent.");
    }

    handleAuthenticated(token, { name: name.trim(), email: email.trim() });
  };

  const handleLogout = () => {
    removeToken();
    setSession(null);
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
            onRegister={handleRegister}
            onNavigateLogin={() => setCurrentPage("login")}
          />
        )}

        {currentPage === "dashboard" && (
          <Dashboard
            user={user}
            onBookingCreated={() => setCurrentPage("bookings")}
          />
        )}

        {currentPage === "admin" && <AdminPage />}

        {currentPage === "profile" && (
          <UserProfile
            user={user}
            onUserUpdated={(updatedUser) =>
              setSession((currentSession) =>
                currentSession
                  ? {
                      ...currentSession,
                      user: { ...currentSession.user, ...updatedUser },
                    }
                  : currentSession,
              )
            }
          />
        )}

        {currentPage === "bookings" && <BookingsPage user={user} />}
      </main>

      <Footer />
    </div>
  );
}

export default App;
