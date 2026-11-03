import React, { useEffect, useState } from "react";
import { jsonOptions, request } from "../api.js";

export default function UserProfile({ user, onUserUpdated }) {
  const [isEditingData, setIsEditingData] = useState(false);
  const [name, setName] = useState(user?.name || "");
  const [email, setEmail] = useState(user?.email || "");

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const canEditProfile =
    user &&
    (!user.roles?.length ||
      user.roles.some((role) =>
        ["Oktato", "Portas", "Admin", "Igazgato"].includes(role),
      ));

  useEffect(() => {
    setName(user?.name || "");
    setEmail(user?.email || "");
  }, [user?.name, user?.email]);

  const handleSaveData = async (e) => {
    e.preventDefault();
    setSaving(true);
    setError("");
    setNotice("");
    try {
      const updatedUser = await request(
        "/User/update",
        jsonOptions("PUT", {
          name: name.trim(),
          email: email.trim(),
          password: "",
        }),
      );
      const updatedDetails = {
        name: updatedUser?.name || name.trim(),
        email: updatedUser?.email || email.trim(),
      };
      setName(updatedDetails.name);
      setEmail(updatedDetails.email);
      onUserUpdated?.(updatedDetails);
      setIsEditingData(false);
      setNotice("Személyes adatok sikeresen frissítve.");
    } catch (saveError) {
      setError(`Az adatok mentése nem sikerült: ${saveError.message}`);
    } finally {
      setSaving(false);
    }
  };

  const handlePasswordChange = async (e) => {
    e.preventDefault();
    setError("");
    setNotice("");
    if (newPassword !== confirmPassword) {
      setError("A megadott új jelszavak nem egyeznek.");
      return;
    }
    setSaving(true);
    setError("");
    setNotice("");
    try {
      await request(
        "/User/change-password",
        jsonOptions("POST", {
          oldPassword: currentPassword,
          newPassword,
          confirmPassword,
        }),
      );
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setNotice("A jelszó sikeresen megváltozott.");
    } catch (passwordError) {
      setError(`A jelszó módosítása nem sikerült: ${passwordError.message}`);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="profile-container">
      <div className="profile-card">
        <div className="profile-header-bg"></div>
        <div className="profile-body">
          <div className="profile-avatar-large">
            {name
              .split(/\s+/)
              .filter(Boolean)
              .slice(0, 2)
              .map((part) => part[0]?.toLocaleUpperCase("hu"))
              .join("") || "?"}
          </div>
          <h2 className="profile-name">{name}</h2>
          <p className="profile-email">{email}</p>

          <div className="profile-sections">
            {error && (
              <p className="auth-error" role="alert">
                {error}
              </p>
            )}
            {notice && (
              <p className="booking-success" role="status">
                {notice}
              </p>
            )}
            <div className="profile-section-block">
              <div className="section-title-header">
                <h3>Személyes adatok</h3>
                {canEditProfile && !isEditingData && (
                  <button
                    type="button"
                    className="secondary-btn"
                    onClick={() => {
                      setError("");
                      setNotice("");
                      setIsEditingData(true);
                    }}
                    disabled={saving}
                  >
                    Adatok módosítása
                  </button>
                )}
              </div>

              {isEditingData ? (
                <form onSubmit={handleSaveData} className="edit-form">
                  <div className="form-group">
                    <label htmlFor="edit-name">Teljes név</label>
                    <input
                      type="text"
                      id="edit-name"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="edit-email">E-mail cím</label>
                    <input
                      type="email"
                      id="edit-email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                    />
                  </div>
                  <div className="form-actions-inline">
                    <button
                      type="submit"
                      className="primary-btn"
                      disabled={saving}
                    >
                      {saving ? "Mentés..." : "Mentés"}
                    </button>
                    <button
                      type="button"
                      className="cancel-btn"
                      onClick={() => {
                        setName(user?.name || "");
                        setEmail(user?.email || "");
                        setIsEditingData(false);
                      }}
                      disabled={saving}
                    >
                      Mégse
                    </button>
                  </div>
                </form>
              ) : (
                <div className="profile-info-grid">
                  <div className="info-item">
                    <span className="info-label">Teljes név</span>
                    <span className="info-value">{name}</span>
                  </div>
                  <div className="info-item">
                    <span className="info-label">E-mail cím</span>
                    <span className="info-value">{email}</span>
                  </div>
                </div>
              )}
            </div>

            {canEditProfile ? (
              <div className="profile-section-block">
                <h3>Jelszó módosítása</h3>
                <form onSubmit={handlePasswordChange} className="password-form">
                  <div className="form-group">
                    <label htmlFor="current-pass">Jelenlegi jelszó</label>
                    <input
                      type="password"
                      id="current-pass"
                      value={currentPassword}
                      onChange={(e) => setCurrentPassword(e.target.value)}
                      required
                    />
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label htmlFor="new-pass">Új jelszó</label>
                      <input
                        type="password"
                        id="new-pass"
                        placeholder="Új jelszó"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        required
                      />
                    </div>
                    <div className="form-group">
                      <label htmlFor="confirm-pass">
                        Új jelszó megerősítése
                      </label>
                      <input
                        type="password"
                        id="confirm-pass"
                        placeholder="Új jelszó újra"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        required
                      />
                    </div>
                  </div>
                  <button
                    type="submit"
                    className="primary-btn align-self-start"
                    disabled={saving}
                  >
                    {saving ? "Frissítés..." : "Jelszó frissítése"}
                  </button>
                </form>
              </div>
            ) : (
              <p className="booking-role-note">
                Profiladatot és jelszót csak kiosztott szerepkörrel lehet
                módosítani.
              </p>
            )}

            <div className="profile-section-block">
              <h3>Bejelentett hibajegyek</h3>
              <p>A hibajegyek megjelenítése még nem érhető el.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
