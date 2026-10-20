import React from "react";

export default function Login({ onLogin, onNavigateRegister }) {
  return (
    <div className="auth-card">
      <div className="auth-header">
        <h2>Jelentkezzen be az egyetemi e-mail címével</h2>
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          onLogin();
        }}
        className="auth-form"
      >
        <div className="form-group">
          <label htmlFor="email">E-mail</label>
          <input
            type="text"
            id="email"
            placeholder="user@uni-pannon.hu"
            defaultValue="tanar@uni-pannon.hu "
          />
        </div>

        <div className="form-group">
          <label htmlFor="password">Jelszó</label>
          <input type="password" id="password" defaultValue="Password123!!!" />
        </div>

        <div className="form-options">
          <a href="#forgot" className="forgot-link">
            Elfelejtett jelszó?
          </a>
        </div>

        <button type="submit" className="primary-btn full-width">
          Bejelentkezés
        </button>
      </form>

      <div className="auth-footer">
        <p>
          Még nincs fiókja?{" "}
          <button
            type="button"
            onClick={onNavigateRegister}
            className="text-link"
          >
            Regisztráljon itt
          </button>
        </p>
      </div>
    </div>
  );
}
