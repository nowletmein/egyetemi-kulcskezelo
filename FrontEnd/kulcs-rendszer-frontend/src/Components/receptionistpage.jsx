import { useCallback, useEffect, useMemo, useState } from "react";
import { request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

export default function ReceptionistPage() {
  const [rooms, setRooms] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadData = useCallback(async () => {
    const [roomsResult, bookingsResult] = await Promise.allSettled([
      request("/Room/getall"),
      request("/bookings/getall"),
    ]);

    const errors = [];
    let nextRooms = null;
    let nextBookings = null;
    if (roomsResult.status === "fulfilled") {
      if (Array.isArray(roomsResult.value)) {
        nextRooms = roomsResult.value;
      } else {
        errors.push("A teremlista API-válasza érvénytelen formátumú.");
      }
    } else {
      errors.push(
        `A termek betöltése nem sikerült: ${roomsResult.reason.message}`,
      );
    }

    if (bookingsResult.status === "fulfilled") {
      if (Array.isArray(bookingsResult.value)) {
        nextBookings = [...bookingsResult.value].sort(
          (first, second) =>
            new Date(first.startTime).getTime() -
            new Date(second.startTime).getTime(),
        );
      } else {
        errors.push("A foglalások API-válasza érvénytelen formátumú.");
      }
    } else {
      errors.push(
        `A foglalások betöltése nem sikerült: ${bookingsResult.reason.message}`,
      );
    }

    return {
      rooms: nextRooms,
      bookings: nextBookings,
      error: errors.join(" "),
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    loadData().then(
      ({ rooms: nextRooms, bookings: nextBookings, error: nextError }) => {
        if (cancelled) return;

        if (nextRooms !== null) setRooms(nextRooms);
        if (nextBookings !== null) setBookings(nextBookings);
        setError(nextError);
        setLoading(false);
      },
    );

    return () => {
      cancelled = true;
    };
  }, [loadData]);

  const refreshData = () => {
    setLoading(true);
    setError("");
    loadData().then(
      ({ rooms: nextRooms, bookings: nextBookings, error: nextError }) => {
        if (nextRooms !== null) setRooms(nextRooms);
        if (nextBookings !== null) setBookings(nextBookings);
        setError(nextError);
        setLoading(false);
      },
    );
  };

  const filteredRooms = useMemo(() => {
    const query = search.trim().toLocaleLowerCase("hu");
    if (!query) return rooms;

    return rooms.filter((room) =>
      [room.name, room.id, room.building, room.floorId]
        .filter((value) => value !== null && value !== undefined)
        .some((value) => String(value).toLocaleLowerCase("hu").includes(query)),
    );
  }, [rooms, search]);

  const roomNames = useMemo(
    () => Object.fromEntries(rooms.map((room) => [room.id, room.name])),
    [rooms],
  );

  const formatDate = (value) => {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? "Ismeretlen időpont"
      : date.toLocaleString("hu-HU");
  };

  return (
    <main className="admin-page receptionist-page">
      <ErrorToast message={error} onDismiss={() => setError("")} />
      <header className="admin-heading">
        <div>
          <p className="admin-eyebrow">ÜZEMELTETÉS</p>
          <h1>Portás felület</h1>
          <p className="admin-intro">Foglalások és termek áttekintése.</p>
        </div>
        <button
          type="button"
          className="admin-button"
          onClick={refreshData}
          disabled={loading}
        >
          {loading ? "Betöltés..." : "Adatok frissítése"}
        </button>
      </header>

      <section className="admin-panel receptionist-bookings">
        <div className="admin-panel-heading">
          <div>
            <p className="admin-eyebrow">FOGLALÁSOK</p>
            <h2>Foglalások</h2>
          </div>
          <span className="admin-count">
            {loading ? "Betöltés..." : `${bookings.length} foglalás`}
          </span>
        </div>
        {loading ? (
          <p className="admin-empty">A foglalások betöltése folyamatban...</p>
        ) : bookings.length === 0 ? (
          <p className="admin-empty">Nincs megjeleníthető foglalás.</p>
        ) : (
          <ul className="receptionist-booking-list">
            {bookings.map((booking) => (
              <li className="receptionist-booking-item" key={booking.id}>
                <div className="receptionist-booking-heading">
                  <strong>
                    {roomNames[booking.roomId] || `Terem ${booking.roomId}`}
                  </strong>
                  <span className="admin-count">{booking.status}</span>
                </div>
                <dl>
                  <div>
                    <dt>Foglaló</dt>
                    <dd>Felhasználó #{booking.userId}</dd>
                  </div>
                  <div>
                    <dt>Kezdés</dt>
                    <dd>{formatDate(booking.startTime)}</dd>
                  </div>
                  <div>
                    <dt>Befejezés</dt>
                    <dd>{formatDate(booking.endTime)}</dd>
                  </div>
                  {booking.specialRequest && (
                    <div>
                      <dt>Megjegyzés</dt>
                      <dd>{booking.specialRequest}</dd>
                    </div>
                  )}
                </dl>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="admin-panel receptionist-rooms">
        <div className="admin-panel-heading">
          <div>
            <p className="admin-eyebrow">ELÉRHETŐ ADATOK</p>
            <h2>Teremlista</h2>
          </div>
          <span className="admin-count">
            {loading ? "Betöltés..." : `${filteredRooms.length} terem`}
          </span>
        </div>
        <label className="admin-search-field">
          Terem keresése
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Név vagy azonosító"
          />
        </label>

        {loading ? (
          <p className="admin-empty">A termek betöltése folyamatban...</p>
        ) : filteredRooms.length === 0 ? (
          <p className="admin-empty">
            {rooms.length === 0
              ? "Nincs megjeleníthető terem."
              : "Nincs a keresésnek megfelelő terem."}
          </p>
        ) : (
          <ul className="receptionist-room-list">
            {filteredRooms.map((room) => (
              <li className="receptionist-room-item" key={room.id}>
                <span className="receptionist-room-mark" aria-hidden="true">
                  T
                </span>
                <span className="receptionist-room-details">
                  <strong>{room.name || `Terem ${room.id}`}</strong>
                  <small>
                    Azonosító: {room.id}
                    {room.building ? ` · ${room.building}` : ""}
                    {room.floorId !== null && room.floorId !== undefined
                      ? ` · Emelet #${room.floorId}`
                      : ""}
                    {Number.isFinite(room.capacity)
                      ? ` · ${room.capacity} fő`
                      : ""}
                  </small>
                  {room.equipment && <small>{room.equipment}</small>}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  );
}
