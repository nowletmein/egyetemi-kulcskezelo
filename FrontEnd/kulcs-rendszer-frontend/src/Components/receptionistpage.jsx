import { useCallback, useEffect, useMemo, useState } from "react";
import { jsonOptions, request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

const emptyTicketForm = { roomId: "", keyId: "", description: "" };

export default function ReceptionistPage() {
  const [section, setSection] = useState("bookings");
  const [rooms, setRooms] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [keys, setKeys] = useState([]);
  const [ticketForm, setTicketForm] = useState(emptyTicketForm);
  const [savingTicket, setSavingTicket] = useState(false);
  const [tickets, setTickets] = useState([]);
  const [ticketsLoading, setTicketsLoading] = useState(false);
  const [ticketsError, setTicketsError] = useState("");
  const [ticketRefreshKey, setTicketRefreshKey] = useState(0);

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

  useEffect(() => {
    let cancelled = false;

    request("/Key/getall")
      .then((result) => {
        if (!cancelled && Array.isArray(result)) setKeys(result);
      })
      .catch(() => {
        if (!cancelled) setKeys([]);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (section !== "issue-tickets") return undefined;

    let active = true;
    const loadTickets = async () => {
      setTicketsLoading(true);
      setTicketsError("");
      try {
        const result = await request("/issuetickets");
        if (!Array.isArray(result)) {
          throw new Error("A hibajegyek API-válasza érvénytelen formátumú.");
        }
        if (active) setTickets(result);
      } catch (loadError) {
        if (active) {
          setTickets([]);
          setTicketsError(
            `Nem sikerült betölteni a hibajegyeket: ${loadError.message}`,
          );
        }
      } finally {
        if (active) setTicketsLoading(false);
      }
    };

    loadTickets();
    return () => {
      active = false;
    };
  }, [section, ticketRefreshKey]);

  const refreshData = () => {
    setLoading(true);
    setError("");
    setTicketRefreshKey((current) => current + 1);
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

  const roomKeys = useMemo(
    () =>
      keys.filter((key) =>
        key.isMasterKey
          ? key.accessibleRoomIds?.includes(ticketForm.roomId)
          : key.roomId === ticketForm.roomId,
      ),
    [keys, ticketForm.roomId],
  );

  const submitIssueTicket = async (event) => {
    event.preventDefault();
    setSavingTicket(true);
    setError("");
    setNotice("");
    try {
      await request(
        "/issuetickets",
        jsonOptions("POST", {
          roomId: ticketForm.roomId,
          keyId: ticketForm.keyId ? Number(ticketForm.keyId) : null,
          description: ticketForm.description.trim(),
        }),
      );
      setTicketForm(emptyTicketForm);
      setNotice("A hibajegy rögzítve.");
      setTicketRefreshKey((current) => current + 1);
    } catch (saveError) {
      setError(`A hibajegy rögzítése nem sikerült: ${saveError.message}`);
    } finally {
      setSavingTicket(false);
    }
  };

  const formatDate = (value) => {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? "Ismeretlen időpont"
      : date.toLocaleString("hu-HU");
  };

  return (
    <main className="admin-page receptionist-page">
      <ErrorToast message={error} onDismiss={() => setError("")} />
      <ErrorToast
        message={notice}
        onDismiss={() => setNotice("")}
        variant="success"
      />
      <header className="admin-heading">
        <div>
          <p className="admin-eyebrow">ÜZEMELTETÉS</p>
          <h1>Portás felület</h1>
          <p className="admin-intro">
            Foglalások, termek és hibajegyek áttekintése.
          </p>
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

      <nav className="admin-tabs" aria-label="Portás területek">
        <button
          type="button"
          className={section === "bookings" ? "active" : ""}
          onClick={() => setSection("bookings")}
        >
          Foglalások
        </button>
        <button
          type="button"
          className={section === "rooms" ? "active" : ""}
          onClick={() => setSection("rooms")}
        >
          Teremlista
        </button>
        <button
          type="button"
          className={section === "issue-tickets" ? "active" : ""}
          onClick={() => setSection("issue-tickets")}
        >
          Hibajegyek
        </button>
      </nav>

      {section === "bookings" && (
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
      )}

      {section === "rooms" && (
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
      )}

      {section === "issue-tickets" && (
        <section className="admin-panel">
          <div className="admin-panel-heading">
            <div>
              <p className="admin-eyebrow">MEGHIBÁSODÁSOK</p>
              <h2>Hibajegyek</h2>
            </div>
            <span className="admin-count">
              {ticketsLoading ? "Betöltés..." : `${tickets.length} hibajegy`}
            </span>
          </div>

          <form className="admin-form" onSubmit={submitIssueTicket}>
            <h3>Új hibajegy</h3>
            <label>
              Terem
              <select
                required
                value={ticketForm.roomId}
                onChange={(event) =>
                  setTicketForm({
                    ...ticketForm,
                    roomId: event.target.value,
                    keyId: "",
                  })
                }
                disabled={savingTicket || loading}
              >
                <option value="">Válassz termet</option>
                {rooms.map((room) => (
                  <option key={room.id} value={room.id}>
                    {room.name || `Terem ${room.id}`} ({room.id})
                  </option>
                ))}
              </select>
            </label>
            <label>
              Érintett kulcs (nem kötelező)
              <select
                value={ticketForm.keyId}
                onChange={(event) =>
                  setTicketForm({ ...ticketForm, keyId: event.target.value })
                }
                disabled={savingTicket || !ticketForm.roomId}
              >
                <option value="">Nincs kulcs megadva</option>
                {roomKeys.map((key) => (
                  <option key={key.id} value={key.id}>
                    {key.isMasterKey ? key.masterKeyName : `Kulcs #${key.id}`}
                  </option>
                ))}
              </select>
            </label>
            <label>
              A hiba leírása
              <textarea
                required
                maxLength={500}
                value={ticketForm.description}
                onChange={(event) =>
                  setTicketForm({
                    ...ticketForm,
                    description: event.target.value,
                  })
                }
                placeholder="Pl. a kulcs eltört, a projektor nem kapcsol be"
                disabled={savingTicket}
              />
            </label>
            <div className="admin-form-actions">
              <button
                type="submit"
                className="admin-button admin-button-primary"
                disabled={savingTicket}
              >
                {savingTicket ? "Rögzítés..." : "Hibajegy rögzítése"}
              </button>
            </div>
          </form>

          {ticketsError && (
            <ErrorToast
              message={ticketsError}
              onDismiss={() => setTicketsError("")}
            />
          )}

          {ticketsLoading ? (
            <p className="admin-empty">A hibajegyek betöltése folyamatban...</p>
          ) : tickets.length === 0 ? (
            !ticketsError && (
              <p className="admin-empty">Nincs bejelentett hibajegy.</p>
            )
          ) : (
            <div className="booking-list">
              {tickets.map((ticket) => (
                <article className="booking-item" key={ticket.id}>
                  <div className="booking-item-heading">
                    <h3>
                      {roomNames[ticket.roomId] || `Terem ${ticket.roomId}`}{" "}
                      <small>#{ticket.id}</small>
                    </h3>
                    <span>
                      {ticket.status === "Open" ? "Nyitott" : ticket.status}
                    </span>
                  </div>
                  <p className="admin-booking-owner">
                    Bejelentő: Felhasználó #{ticket.reporterId}
                  </p>
                  <dl>
                    {ticket.keyId !== null && ticket.keyId !== undefined && (
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
        </section>
      )}
    </main>
  );
}
