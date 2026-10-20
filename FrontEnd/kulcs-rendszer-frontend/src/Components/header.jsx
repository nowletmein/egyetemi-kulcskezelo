import React from "react";

export default function Header({
  user,
  onLogout,
  currentPage,
  setCurrentPage,
}) {
  return (
    <header className="header">
      <div className="header-brand">
        <div>
          <span className="header-title">UniKey</span>
          <span className="header-subtitle">Egyetemi Kulcskezelő</span>
        </div>
      </div>

      {user ? (
        <div className="header-user-section">
          <button
            className={`nav-btn ${currentPage === "dashboard" ? "active" : ""}`}
            onClick={() => setCurrentPage("dashboard")}
          >
            Foglalás
          </button>
          <button
            className={`nav-btn ${currentPage === "my-bookings" ? "active" : ""}`}
            onClick={() => setCurrentPage("my-bookings")}
          >
            Saját kulcsok
          </button>

          <div
            className={`user-profile-clickable ${currentPage === "profile" ? "active-profile" : ""}`}
            onClick={() => setCurrentPage("profile")}
            style={{ cursor: "pointer" }}
            title="Saját profil"
          >
            <div className="avatar"></div>
          </div>

          <button className="logout-btn" onClick={onLogout}>
            Kijelentkezés
          </button>
        </div>
      ) : (
        <div className="header-auth-buttons">
          <button
            className={`auth-tab ${currentPage === "login" ? "active" : ""}`}
            onClick={() => setCurrentPage("login")}
          >
            Bejelentkezés
          </button>
          <button
            className={`auth-tab ${currentPage === "register" ? "active" : ""}`}
            onClick={() => setCurrentPage("register")}
          >
            Regisztráció
          </button>
        </div>
      )}
    </header>
  );
}
