import { useState } from "react";
import ErrorToast from "./errorToast.jsx";

export default function Login({ onLogin, onNavigateRegister }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      await onLogin(email, password);
    } catch (submitError) {
      setError(submitError.message || "Nem sikerült bejelentkezni.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-card">
      <ErrorToast message={error} onDismiss={() => setError("")} />
      <div className="auth-header">
        <h2>Jelentkezzen be az egyetemi e-mail címével</h2>
      </div>

      <form onSubmit={handleSubmit} className="auth-form">
        <div className="form-group">
          <label htmlFor="email">E-mail</label>
          <input
            type="email"
            id="email"
            placeholder="user@uni-pannon.hu"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="password">Jelszó</label>
          <input
            type="password"
            id="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </div>

        <button
          type="submit"
          className="primary-btn full-width"
          disabled={isSubmitting}
        >
          {isSubmitting ? "Bejelentkezés..." : "Bejelentkezés"}
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
