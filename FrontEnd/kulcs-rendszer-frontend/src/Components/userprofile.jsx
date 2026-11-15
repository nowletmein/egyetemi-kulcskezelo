import React, { useEffect, useState } from "react";
import { jsonOptions, request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

export default function UserProfile({ user, onUserUpdated }) {
  const [isEditingData, setIsEditingData] = useState(false);
  const [editedName, setEditedName] = useState("");
  const [editedEmail, setEditedEmail] = useState("");

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [myTickets, setMyTickets] = useState([]);
  const [ticketRooms, setTicketRooms] = useState({});
  const [ticketsLoading, setTicketsLoading] = useState(true);
  const [ticketsError, setTicketsError] = useState("");

  useEffect(() => {
    let cancelled = false;

    Promise.allSettled([
      request("/issuetickets/mine"),
      request("/Room/getall"),
    ]).then(([ticketsResult, roomsResult]) => {
      if (cancelled) return;

      if (
        ticketsResult.status === "fulfilled" &&
        Array.isArray(ticketsResult.value)
      ) {
        setMyTickets(ticketsResult.value);
      } else {
        setTicketsError(
          ticketsResult.status === "rejected"
            ? `A hibajegyek betöltése nem sikerült: ${ticketsResult.reason.message}`
            : "A hibajegyek API-válasza érvénytelen formátumú.",
        );
      }

      if (
        roomsResult.status === "fulfilled" &&
        Array.isArray(roomsResult.value)
      ) {
        setTicketRooms(
          Object.fromEntries(
            roomsResult.value.map((room) => [room.id, room.name]),
          ),
        );
      }

      setTicketsLoading(false);
    });

    return () => {
      cancelled = true;
    };
  }, []);
  const canEditProfile =
    user &&
    (!user.roles?.length ||
      user.roles.some((role) =>
        ["Oktato", "Portas", "Admin", "Igazgato"].includes(role),
      ));

  const name = user?.name || "";
  const email = user?.email || "";

  const handleSaveData = async (e) => {
    e.preventDefault();
    setSaving(true);
    setError("");
    setNotice("");
    try {
      const updatedUser = await request(
        "/User/update",
        jsonOptions("PUT", {
          name: editedName.trim(),
          email: editedEmail.trim(),
          password: "",
        }),
      );
      const updatedDetails = {
        name: updatedUser?.name || editedName.trim(),
        email: updatedUser?.email || editedEmail.trim(),
      };
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
          <ErrorToast message={error} onDismiss={() => setError("")} />
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
            <ErrorToast
              message={notice}
              onDismiss={() => setNotice("")}
              variant="success"
            />
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
                      setEditedName(name);
                      setEditedEmail(email);
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
                      value={editedName}
                      onChange={(e) => setEditedName(e.target.value)}
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="edit-email">E-mail cím</label>
                    <input
                      type="email"
                      id="edit-email"
                      value={editedEmail}
                      onChange={(e) => setEditedEmail(e.target.value)}
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
              {ticketsError && (
                <ErrorToast
                  message={ticketsError}
                  onDismiss={() => setTicketsError("")}
                />
              )}
              {ticketsLoading ? (
                <p>A hibajegyek betöltése folyamatban...</p>
              ) : myTickets.length === 0 ? (
                !ticketsError && <p>Még nem jelentettél be hibát.</p>
              ) : (
                <div className="booking-list">
                  {myTickets.map((ticket) => (
                    <article className="booking-item" key={ticket.id}>
                      <div className="booking-item-heading">
                        <h3>
                          {ticketRooms[ticket.roomId] ||
                            `Terem ${ticket.roomId}`}{" "}
                          <small>#{ticket.id}</small>
                        </h3>
                        <span>
                          {ticket.status === "Open" ? "Nyitott" : ticket.status}
                        </span>
                      </div>
                      <dl>
                        {ticket.keyId !== null &&
                          ticket.keyId !== undefined && (
                            <div>
                              <dt>Kulcs</dt>
                              <dd>Kulcs #{ticket.keyId}</dd>
                            </div>
                          )}
                        <div>
                          <dt>Leírás</dt>
                          <dd>{ticket.description}</dd>
                        </div>
                      </dl>
                    </article>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
