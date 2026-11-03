import { useState } from "react";
import ErrorToast from "./errorToast.jsx";

export default function Register({ onRegister, onNavigateLogin }) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError("");

    if (password !== confirmPassword) {
      setError("A két jelszó nem egyezik.");
      return;
    }

    setIsSubmitting(true);
    try {
      await onRegister(name, email, password);
    } catch (submitError) {
      setError(submitError.message || "Nem sikerült regisztrálni.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-card">
      <ErrorToast message={error} onDismiss={() => setError("")} />
      <div className="auth-header">
        <h2>Fiók létrehozása</h2>
      </div>

      <form onSubmit={handleSubmit} className="auth-form">
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="register-name">Teljes név</label>
            <input
              type="text"
              id="register-name"
              autoComplete="name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              required
            />
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="reg-email">Egyetemi e-mail cím</label>
          <input
            type="email"
            id="reg-email"
            placeholder="user@uni-pannon.hu"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="reg-password">Jelszó</label>
          <input
            type="password"
            id="reg-password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="confirm-password">Jelszó megerősítése</label>
          <input
            type="password"
            id="confirm-password"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(event) => setConfirmPassword(event.target.value)}
            required
          />
        </div>

        <button
          type="submit"
          className="primary-btn full-width"
          disabled={isSubmitting}
        >
          {isSubmitting ? "Regisztráció..." : "Regisztráció véglegesítése"}
        </button>
      </form>

      <div className="auth-footer">
        <p>
          Már van fiókja?{" "}
          <button type="button" onClick={onNavigateLogin} className="text-link">
            Jelentkezzen be
          </button>
        </p>
      </div>
    </div>
  );
}
