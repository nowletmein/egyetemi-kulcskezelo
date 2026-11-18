import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { jsonOptions, request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

const emptyTicketForm = { roomId: "", keyId: "", description: "" };

const getBookingKeyMovements = (booking) => {
  const movements = booking?.keyMovements ?? booking?.KeyMovements;
  return Array.isArray(movements) ? movements : [];
};

const getLatestKeyMovement = (booking) =>
  [...getBookingKeyMovements(booking)].sort((first, second) => {
    const firstTime = new Date(
      first.timestamp ?? first.Timestamp ?? 0,
    ).getTime();
    const secondTime = new Date(
      second.timestamp ?? second.Timestamp ?? 0,
    ).getTime();
    return secondTime - firstTime;
  })[0] ?? null;

const getKeyMovementStatus = (movement) =>
  String(movement?.status ?? movement?.Status ?? "")
    .trim()
    .toLowerCase();

const getKeyMovementKeyId = (movement) => movement?.keyId ?? movement?.KeyId;

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

  const [keyDialogBooking, setKeyDialogBooking] = useState(null);
  const [selectedKeyId, setSelectedKeyId] = useState("");
  const [keyPin, setKeyPin] = useState("");
  const [keyPinSent, setKeyPinSent] = useState(false);
  const [keyActionLoading, setKeyActionLoading] = useState(false);
  const [processingBookingId, setProcessingBookingId] = useState(null);
  const keyDialogRef = useRef(null);

  useEffect(() => {
    if (!keyDialogBooking) return;
    const dialog = keyDialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
  }, [keyDialogBooking]);

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

  const refreshBookings = useCallback(async () => {
    const result = await request("/bookings/getall");
    if (!Array.isArray(result)) {
      throw new Error("A foglalások API-válasza érvénytelen formátumú.");
    }

    setBookings(
      [...result].sort(
        (first, second) =>
          new Date(first.startTime).getTime() -
          new Date(second.startTime).getTime(),
      ),
    );
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
      .catch((loadError) => {
        if (!cancelled) {
          setKeys([]);
          setError(`A kulcsok betöltése nem sikerült: ${loadError.message}`);
        }
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

  // A számként és szövegként érkező azonosítókat is helyesen hasonlítja össze.
  const getKeysForRoom = useCallback(
    (roomId) =>
      keys.filter((key) =>
        key.isMasterKey
          ? key.accessibleRoomIds?.some(
              (accessibleRoomId) => String(accessibleRoomId) === String(roomId),
            )
          : String(key.roomId) === String(roomId),
      ),
    [keys],
  );

  const roomKeys = useMemo(
    () => getKeysForRoom(ticketForm.roomId),
    [getKeysForRoom, ticketForm.roomId],
  );

  const openKeyDialog = (booking) => {
    const availableKeys = getKeysForRoom(booking.roomId);

    if (availableKeys.length === 0) {
      setError("Ehhez a teremhez nem található kiadható kulcs.");
      return;
    }

    setError("");
    setNotice("");
    setKeyDialogBooking(booking);
    setSelectedKeyId(
      availableKeys.length === 1 ? String(availableKeys[0].id) : "",
    );
    setKeyPin("");
    setKeyPinSent(false);
  };

  const closeKeyDialog = () => {
    if (keyActionLoading) return;
    setKeyDialogBooking(null);
    setSelectedKeyId("");
    setKeyPin("");
    setKeyPinSent(false);
  };

  const submitKeyDialog = async (event) => {
    event.preventDefault();
    if (!keyDialogBooking || !selectedKeyId || keyActionLoading) return;

    const booking = keyDialogBooking;
    const keyId = Number(selectedKeyId);

    if (!Number.isInteger(keyId) || keyId <= 0) {
      setError("A kiválasztott kulcs azonosítója érvénytelen.");
      return;
    }

    setKeyActionLoading(true);
    setError("");
    setNotice("");

    try {
      if (!keyPinSent) {
        // A végpont [FromBody] int userId értéket vár, ezért itt nyers szám a body.
        await request(
          `/bookings/${keyId}/generate-pin`,
          jsonOptions("POST", Number(booking.userId)),
        );

        setKeyPinSent(true);
        setNotice("A PIN-kódot elküldtük a felhasználónak.");
      } else {
        if (!keyPin.trim()) {
          setError("Add meg a felhasználó hitelesítő kódját.");
          return;
        }

        await request(
          "/bookings/authenticate-pin",
          jsonOptions("POST", {
            userId: Number(booking.userId),
            keyId,
            pin: keyPin.trim(),
            bookingId: Number(booking.id),
          }),
        );

        setNotice("A kulcs kiadása sikeres.");
        setKeyDialogBooking(null);
        setSelectedKeyId("");
        setKeyPin("");
        setKeyPinSent(false);
        try {
          await refreshBookings();
        } catch (refreshError) {
          setError(
            `A kulcs kiadása sikerült, de a foglalás frissítése nem: ${refreshError.message}`,
          );
        }
      }
    } catch (actionError) {
      const message = actionError.message || "Ismeretlen hiba történt.";
      setError(
        message.toLowerCase().includes("invalid or expired pin")
          ? "A megadott hitelesítő kód érvénytelen vagy lejárt."
          : `A kulcs művelet nem sikerült: ${message}`,
      );
    } finally {
      setKeyActionLoading(false);
    }
  };

  const returnKey = async (booking) => {
    const currentKeyMovement = getLatestKeyMovement(booking);
    const currentStatus = getKeyMovementStatus(currentKeyMovement);
    const keyId = Number(getKeyMovementKeyId(currentKeyMovement));

    if (
      !["given out", "unreturned"].includes(currentStatus) ||
      !Number.isInteger(keyId) ||
      keyId <= 0 ||
      processingBookingId !== null
    ) {
      return;
    }

    setProcessingBookingId(booking.id);
    setError("");
    setNotice("");

    try {
      await request(
        "/bookings/return-key",
        jsonOptions("POST", {
          userId: Number(booking.userId),
          keyId,
          bookingId: Number(booking.id),
        }),
      );

      setNotice("A kulcs visszavétele sikeres.");
      try {
        await refreshBookings();
      } catch (refreshError) {
        setError(
          `A kulcs visszavétele sikerült, de a foglalás frissítése nem: ${refreshError.message}`,
        );
      }
    } catch (actionError) {
      setError(`A kulcs visszavétele nem sikerült: ${actionError.message}`);
    } finally {
      setProcessingBookingId(null);
    }
  };

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

  const dialogKeys = keyDialogBooking
    ? getKeysForRoom(keyDialogBooking.roomId)
    : [];

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
              {bookings.map((booking) => {
                const keyMovements = getBookingKeyMovements(booking);
                const latestKeyMovement = getLatestKeyMovement(booking);
                const latestKeyStatus = getKeyMovementStatus(latestKeyMovement);
                const hasIssuedKey = ["given out", "unreturned"].includes(
                  latestKeyStatus,
                );
                const hasReturnedKey = latestKeyStatus === "returned";
                const isProcessing = processingBookingId === booking.id;

                return (
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
                      <div>
                        <dt>Kulcsmozgások</dt>
                        <dd>
                          {keyMovements.length === 0
                            ? "Még nem történt kulcsmozgás."
                            : [...keyMovements]
                                .sort((first, second) => {
                                  const firstTime = new Date(
                                    first.timestamp ?? first.Timestamp ?? 0,
                                  ).getTime();
                                  const secondTime = new Date(
                                    second.timestamp ?? second.Timestamp ?? 0,
                                  ).getTime();
                                  return firstTime - secondTime;
                                })
                                .map((movement, index) => {
                                  const movementStatus =
                                    getKeyMovementStatus(movement);
                                  const statusLabel =
                                    movementStatus === "given out" ||
                                    movementStatus === "unreturned"
                                      ? "Kiadva"
                                      : movementStatus === "returned"
                                        ? "Visszaadva"
                                        : movementStatus ||
                                          "Ismeretlen státusz";
                                  const movementKeyId =
                                    getKeyMovementKeyId(movement);
                                  const movementTimestamp =
                                    movement.timestamp ?? movement.Timestamp;
                                  const identificationMethod =
                                    movement.identificationMethod ??
                                    movement.IdentificationMethod;

                                  return (
                                    <span
                                      key={`${movement.bookingId ?? movement.BookingId ?? booking.id}-${movementKeyId ?? "key"}-${index}`}
                                    >
                                      {`Kulcs #${movementKeyId ?? "?"} · ${statusLabel}`}
                                      {identificationMethod
                                        ? ` · ${identificationMethod}`
                                        : ""}
                                      {movementTimestamp
                                        ? ` · ${formatDate(movementTimestamp)}`
                                        : ""}
                                      {index < keyMovements.length - 1
                                        ? "; "
                                        : ""}
                                    </span>
                                  );
                                })}
                        </dd>
                      </div>
                    </dl>
                    <div
                      className="admin-form-actions"
                      style={{ marginTop: "12px" }}
                    >
                      {hasReturnedKey ? (
                        <button type="button" className="admin-button" disabled>
                          Kulcs leadva
                        </button>
                      ) : hasIssuedKey ? (
                        <button
                          type="button"
                          className="admin-button admin-button-primary"
                          onClick={() => returnKey(booking)}
                          disabled={processingBookingId !== null}
                        >
                          {isProcessing ? "Visszavétel..." : "Kulcs leadva"}
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="admin-button admin-button-primary"
                          onClick={() => openKeyDialog(booking)}
                          disabled={
                            processingBookingId !== null || keys.length === 0
                          }
                        >
                          Kulcs kiadása
                        </button>
                      )}
                    </div>
                  </li>
                );
              })}
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

      {keyDialogBooking && (
        <dialog
          ref={keyDialogRef}
          className="admin-panel"
          aria-labelledby="receptionist-key-modal-title"
          onCancel={(event) => {
            event.preventDefault();
            closeKeyDialog();
          }}
          onClick={(event) => {
            if (event.target === event.currentTarget) closeKeyDialog();
          }}
          style={{
            width: "min(100%, 32rem)",
            maxHeight: "90vh",
            overflowY: "auto",
          }}
        >
          <div className="admin-panel-heading">
            <div>
              <p className="admin-eyebrow">KULCSKEZELÉS</p>
              <h2 id="receptionist-key-modal-title">Kulcs kiadása</h2>
            </div>
          </div>

          <p className="admin-intro">
            Foglaló: Felhasználó #{keyDialogBooking.userId}
            <br />
            Terem:{" "}
            {roomNames[keyDialogBooking.roomId] ||
              `#${keyDialogBooking.roomId}`}
          </p>

          <form className="admin-form" onSubmit={submitKeyDialog}>
            <label>
              Kiadandó kulcs
              <select
                required
                value={selectedKeyId}
                onChange={(event) => {
                  setSelectedKeyId(event.target.value);
                  setKeyPin("");
                  setKeyPinSent(false);
                  setNotice("");
                }}
                disabled={keyActionLoading || keyPinSent}
              >
                <option value="">Válassz kulcsot</option>
                {dialogKeys.map((key) => (
                  <option key={key.id} value={key.id}>
                    {key.isMasterKey
                      ? `${key.masterKeyName || "Mesterkulcs"} (#${key.id})`
                      : `Kulcs #${key.id}`}
                  </option>
                ))}
              </select>
            </label>

            {keyPinSent && (
              <label>
                A felhasználó hitelesítő kódja
                <input
                  type="text"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  value={keyPin}
                  onChange={(event) => setKeyPin(event.target.value)}
                  placeholder="Írd be a PIN-kódot"
                  required
                  disabled={keyActionLoading}
                  autoFocus
                />
              </label>
            )}

            <p className="admin-intro">
              {keyPinSent
                ? "Kérd el a felhasználótól az értesítésben kapott PIN-kódot, majd ellenőrizd."
                : "A PIN elküldése után a felhasználó értesítést kap a hitelesítő kóddal."}
            </p>

            <div className="admin-form-actions">
              <button
                type="button"
                className="admin-button"
                onClick={closeKeyDialog}
                disabled={keyActionLoading}
              >
                Mégse
              </button>
              <button
                type="submit"
                className="admin-button admin-button-primary"
                disabled={
                  keyActionLoading ||
                  !selectedKeyId ||
                  (keyPinSent && !keyPin.trim())
                }
              >
                {keyActionLoading
                  ? "Feldolgozás..."
                  : keyPinSent
                    ? "Kód ellenőrzése"
                    : "PIN küldése"}
              </button>
            </div>
          </form>
        </dialog>
      )}
    </main>
  );
}
