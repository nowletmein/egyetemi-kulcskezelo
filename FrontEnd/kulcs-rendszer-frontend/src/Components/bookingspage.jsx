import { useEffect, useState } from "react";
import { request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString("hu-HU");
}

function getBookingStatusLabel(status) {
  return status?.toLowerCase() === "pending"
    ? "A kulcs még nem lett felvéve"
    : status;
}

async function fetchBookings(userId) {
  let result;
  try {
    result = await request(`/bookings/user/${encodeURIComponent(userId)}`);
  } catch (error) {
    if (error.status === 404) return [];
    throw error;
  }

  if (!Array.isArray(result)) {
    throw new Error("A foglalások API-válasza érvénytelen formátumú.");
  }

  const now = Date.now();
  const currentBookings = [];
  const previousBookings = [];
  for (const booking of result) {
    (new Date(booking.endTime).getTime() >= now
      ? currentBookings
      : previousBookings
    ).push(booking);
  }
  currentBookings.sort(
    (first, second) =>
      new Date(first.startTime).getTime() -
      new Date(second.startTime).getTime(),
  );
  previousBookings.sort(
    (first, second) =>
      new Date(second.startTime).getTime() -
      new Date(first.startTime).getTime(),
  );
  return { currentBookings, previousBookings };
}

export default function BookingsPage({ user }) {
  const [currentBookings, setCurrentBookings] = useState([]);
  const [previousBookings, setPreviousBookings] = useState([]);
  const [roomNames, setRoomNames] = useState({});
  const [loading, setLoading] = useState(Boolean(user?.id));
  const [error, setError] = useState(
    user?.id
      ? ""
      : "A felhasználói azonosító nem található a bejelentkezési tokenben.",
  );
  const [notice, setNotice] = useState("");

  useEffect(() => {
    const userId = user?.id;
    if (!userId) return undefined;

    let active = true;
    const load = async () => {
      try {
        const result = await fetchBookings(userId);
        if (active) {
          setCurrentBookings(result.currentBookings);
          setPreviousBookings(result.previousBookings);
          setError("");
        }
      } catch (loadError) {
        if (active) {
          setError(`A foglalások betöltése nem sikerült: ${loadError.message}`);
        }
      } finally {
        if (active) setLoading(false);
      }
    };

    load();
    return () => {
      active = false;
    };
  }, [user?.id]);

  useEffect(() => {
    if (currentBookings.length + previousBookings.length === 0) {
      return undefined;
    }

    let active = true;
    const loadRoomNames = async () => {
      try {
        const floors = await request("/Floor/getall");
        const roomLists = await Promise.all(
          floors.map((floor) => request(`/Floor/${floor.id}/rooms`)),
        );
        if (!active) return;

        const names = Object.fromEntries(
          roomLists.flat().map((room) => [room.id, room.name || room.id]),
        );
        setRoomNames(names);
      } catch (loadError) {
        console.error(
          "A foglalásokhoz tartozó teremnevek betöltése nem sikerült.",
          loadError,
        );
      }
    };

    loadRoomNames();
    return () => {
      active = false;
    };
  }, [currentBookings.length, previousBookings.length]);

  const refreshBookings = async () => {
    if (!user?.id) {
      setError(
        "A felhasználói azonosító nem található a bejelentkezési tokenben.",
      );
      return;
    }

    setLoading(true);
    setError("");
    try {
      const result = await fetchBookings(user.id);
      setCurrentBookings(result.currentBookings);
      setPreviousBookings(result.previousBookings);
    } catch (loadError) {
      setError(`A foglalások betöltése nem sikerült: ${loadError.message}`);
    } finally {
      setLoading(false);
    }
  };

  const deleteBooking = async (booking) => {
    if (!window.confirm("Biztosan törlöd ezt a foglalást?")) return;

    setError("");
    setNotice("");
    try {
      await request(`/bookings?bookingId=${encodeURIComponent(booking.id)}`, {
        method: "DELETE",
      });
      setNotice("A foglalás törölve.");
      await refreshBookings();
    } catch (deleteError) {
      setError(`A foglalás törlése nem sikerült: ${deleteError.message}`);
    }
  };

  const renderBookings = (items) =>
    items.length === 0 ? (
      <p className="booking-empty">Nincs megjeleníthető foglalás.</p>
    ) : (
      <div className="booking-list">
        {items.map((booking) => (
          <article className="booking-item" key={booking.id}>
            <div className="booking-item-heading">
              <h3>{roomNames[booking.roomId] || `Terem ${booking.roomId}`}</h3>
              <span>{getBookingStatusLabel(booking.status)}</span>
            </div>
            <dl>
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
            <button
              type="button"
              className="admin-button"
              onClick={() => deleteBooking(booking)}
            >
              Foglalás törlése
            </button>
          </article>
        ))}
      </div>
    );

  if (loading) {
    return (
      <main className="admin-page">
        <p className="admin-loading">Foglalások betöltése...</p>
      </main>
    );
  }

  return (
    <main className="admin-page bookings-page">
      <ErrorToast message={error} onDismiss={() => setError("")} />
      <header className="admin-heading">
        <div>
          <h1>Lefoglalt termek</h1>
          <p className="admin-intro">
            A jelenlegi, közelgő és korábbi foglalásaid.
          </p>
        </div>
        <button
          type="button"
          className="admin-button"
          onClick={refreshBookings}
          disabled={loading}
        >
          Frissítés
        </button>
      </header>
      <ErrorToast
        message={notice}
        onDismiss={() => setNotice("")}
        variant="success"
      />
      <section className="admin-panel">
        <div className="admin-panel-heading">
          <div>
            <p className="admin-eyebrow">AKTÍV</p>
            <h2>Jelenlegi és közelgő foglalások</h2>
          </div>
          <span className="admin-count">{currentBookings.length} foglalás</span>
        </div>
        {renderBookings(currentBookings)}
      </section>
      <section className="admin-panel">
        <div className="admin-panel-heading">
          <div>
            <p className="admin-eyebrow">ELŐZMÉNYEK</p>
            <h2>Korábbi foglalások</h2>
          </div>
          <span className="admin-count">
            {previousBookings.length} foglalás
          </span>
        </div>
        {renderBookings(previousBookings)}
      </section>
    </main>
  );
}
