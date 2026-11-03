import { useState } from "react";
import { jsonOptions, request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

export default function RoomInfo({ room, user, onBack, onBookingCreated }) {
  const [startTime, setStartTime] = useState("");
  const [endTime, setEndTime] = useState("");
  const [specialRequest, setSpecialRequest] = useState("");
  const [error, setError] = useState(
    room ? "" : "A terem adatai nem találhatók. Lépj vissza a teremlistához.",
  );
  const [saving, setSaving] = useState(false);
  const canBook =
    user && (!user.roles?.length || user.roles.includes("Oktato"));

  const submitBooking = async (event) => {
    event.preventDefault();
    setError("");

    const userId = Number(user?.id);
    if (!Number.isInteger(userId) || userId < 1) {
      setError("A bejelentkezett felhasználó azonosítója nem érvényes.");
      return;
    }

    const start = new Date(startTime);
    const end = new Date(endTime);
    if (
      !startTime ||
      !endTime ||
      Number.isNaN(start.getTime()) ||
      Number.isNaN(end.getTime()) ||
      start >= end
    ) {
      setError("A foglalás kezdete legyen érvényes és korábbi a végénél.");
      return;
    }

    setSaving(true);
    try {
      await request(
        "/bookings",
        jsonOptions("POST", {
          userId,
          roomId: room.id,
          startTime,
          endTime,
          specialRequest: specialRequest.trim(),
        }),
      );
      onBookingCreated?.();
    } catch (bookingError) {
      setError(bookingError.message);
    } finally {
      setSaving(false);
    }
  };

  if (!room) {
    return (
      <section className="room-info-page">
        <ErrorToast message={error} onDismiss={() => setError("")} />
        <button type="button" className="cancel-btn" onClick={onBack}>
          Vissza a termekhez
        </button>
      </section>
    );
  }

  return (
    <section className="room-info-page">
      <ErrorToast message={error} onDismiss={() => setError("")} />
      <button type="button" className="room-info-back" onClick={onBack}>
        ← Vissza a termekhez
      </button>

      <article className="room-info-card">
        <header className="room-info-heading">
          <div>
            <p className="admin-eyebrow">TEREMFOGLALÁS</p>
            <h1>{room.name}</h1>
            <p>
              {room.buildingName || room.building || "Épület"} ·{" "}
              {room.floorName || `Emelet ${room.floorId}`}
            </p>
          </div>
          <span className="room-info-id">{room.id}</span>
        </header>

        <dl className="room-info-details">
          <div>
            <dt>Férőhely</dt>
            <dd>{room.capacity}</dd>
          </div>
          <div>
            <dt>Felszereltség</dt>
            <dd>{room.equipment || "Nincs megadva"}</dd>
          </div>
        </dl>

        {canBook ? (
          <form
            className="booking-form room-info-booking"
            onSubmit={submitBooking}
          >
            <h2>Foglalási kérelem</h2>
            <label className="form-group">
              Kezdés
              <input
                type="datetime-local"
                value={startTime}
                onChange={(event) => setStartTime(event.target.value)}
                required
              />
            </label>
            <label className="form-group">
              Befejezés
              <input
                type="datetime-local"
                min={startTime || undefined}
                value={endTime}
                onChange={(event) => setEndTime(event.target.value)}
                required
              />
            </label>
            <label className="form-group">
              Megjegyzés
              <textarea
                value={specialRequest}
                onChange={(event) => setSpecialRequest(event.target.value)}
                maxLength={1000}
                rows={4}
              />
            </label>
            <div className="booking-form-actions">
              <button
                type="button"
                className="cancel-btn"
                onClick={onBack}
                disabled={saving}
              >
                Mégse
              </button>
              <button type="submit" className="primary-btn" disabled={saving}>
                {saving ? "Küldés..." : "Foglalás beküldése"}
              </button>
            </div>
          </form>
        ) : (
          <p className="room-info-role-note">
            A foglalás nem érhető el ehhez a fiókhoz.
          </p>
        )}
      </article>
    </section>
  );
}
