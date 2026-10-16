import React from 'react';

export default function Register({ onRegisterSuccess, onNavigateLogin }) {
  return (
    <div className="auth-card">
      <div className="auth-header">
        <h2>Fiók létrehozása</h2>
      </div>

      <form onSubmit={(e) => { e.preventDefault(); onRegisterSuccess(); }} className="auth-form">
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="firstName">Teljes név</label>
            <input type="text" id="firstName"/>
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="reg-email">Egyetemi e-mail cím</label>
          <input type="email" id="reg-email" placeholder="user@uni-pannon.hu"/>
        </div>

        <div className="form-group">
          <label htmlFor="reg-password">Jelszó</label>
          <input type="password" id="reg-password"/>
        </div>

        <button type="submit" className="primary-btn full-width">Regisztráció véglegesítése</button>
      </form>

      <div className="auth-footer">
        <p>Már van fiókja? <button type="button" onClick={onNavigateLogin} className="text-link">Jelentkezzen be</button></p>
      </div>
    </div>
  );
}