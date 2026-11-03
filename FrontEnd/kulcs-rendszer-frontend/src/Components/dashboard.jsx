import React, { useState, useEffect } from "react";
import { jsonOptions, request } from "../api.js";

function getFloorDisplayName(floor) {
  if (!Number.isInteger(floor?.number)) return "Ismeretlen emelet";
  return floor.number === 0 ? "Földszint" : `${floor.number}. emelet`;
}

export default function Dashboard({ user, onBookingCreated }) {
  const [buildings, setBuildings] = useState([]);
  const [floors, setFloors] = useState([]);
  const [rooms, setRooms] = useState([]);

  const [selectedBuildingId, setSelectedBuildingId] = useState(null);
  const [selectedFloorId, setSelectedFloorId] = useState(null);

  const [loading, setLoading] = useState(true);
  const [roomsLoading, setRoomsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [roomsError, setRoomsError] = useState("");
  const [bookingRoom, setBookingRoom] = useState(null);
  const [startTime, setStartTime] = useState("");
  const [endTime, setEndTime] = useState("");
  const [specialRequest, setSpecialRequest] = useState("");
  const [bookingError, setBookingError] = useState("");
  const [bookingNotice, setBookingNotice] = useState("");
  const [bookingSaving, setBookingSaving] = useState(false);
  const canBook =
    user &&
    (!user.roles?.length || user.roles.includes("Oktato"));

  const submitBooking = async (event) => {
    event.preventDefault();
    setBookingError("");
    setBookingNotice("");

    if (!user?.id || !bookingRoom || !startTime || !endTime) {
      setBookingError("Válassz időpontot a foglaláshoz.");
      return;
    }
    if (new Date(startTime) >= new Date(endTime)) {
      setBookingError("A foglalás kezdete legyen korábbi a végénél.");
      return;
    }

    setBookingSaving(true);
    try {
      await request(
        "/bookings",
        jsonOptions("POST", {
          userId: Number(user.id),
          roomId: bookingRoom.id,
          startTime,
          endTime,
          specialRequest: specialRequest.trim(),
        }),
      );
      setBookingNotice("A foglalási kérelmet rögzítettük.");
      setBookingRoom(null);
      setStartTime("");
      setEndTime("");
      setSpecialRequest("");
      onBookingCreated?.();
    } catch (bookingSubmitError) {
      setBookingError(bookingSubmitError.message);
    } finally {
      setBookingSaving(false);
    }
  };

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [buildingsData, floorsData] = await Promise.all([
          request("/Building/getall"),
          request("/Floor/getall"),
        ]);

        setBuildings(buildingsData);
        setFloors(floorsData);

        if (buildingsData.length > 0) {
          const firstBuildingId = buildingsData[0].id;
          setSelectedBuildingId(firstBuildingId);

          const buildingFloors = floorsData.filter(
            (f) => f.buildingId === firstBuildingId,
          );
          if (buildingFloors.length > 0) {
            setSelectedFloorId(buildingFloors[0].id);
          }
        }
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  useEffect(() => {
    if (!selectedFloorId) {
      setRooms([]);
      setRoomsError("");
      return;
    }

    let isMounted = true;

    const fetchRooms = async () => {
      try {
        setRoomsLoading(true);
        setRoomsError("");
        const roomsData = await request(`/Floor/${selectedFloorId}/rooms`);
        if (isMounted) setRooms(roomsData);
      } catch (err) {
        console.error(err);
        if (isMounted) {
          setRooms([]);
          setRoomsError(`A termek betöltése nem sikerült: ${err.message}`);
        }
      } finally {
        if (isMounted) setRoomsLoading(false);
      }
    };

    fetchRooms();

    return () => {
      isMounted = false;
    };
  }, [selectedFloorId]);

  const handleBuildingChange = (buildingId) => {
    setSelectedBuildingId(buildingId);
    const availableFloors = floors.filter((f) => f.buildingId === buildingId);
    if (availableFloors.length > 0) {
      setSelectedFloorId(availableFloors[0].id);
    } else {
      setSelectedFloorId(null);
      setRooms([]);
    }
  };

  const getBuildingDisplayName = (b) => {
    if (!b) return "-";

    const code = b.code || b.buildingCode || b.shortName;
    const name = b.name || b.buildingName || b.title;

    if (code && name) {
      if (name.startsWith(code)) {
        return name;
      }
      return `${code} - ${name}`;
    }

    return name || code || `Épület #${b.id}`;
  };

  if (loading) {
    return (
      <div className="dashboard-container">
        <p>Adatok betöltése...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="dashboard-container">
        <p>Hiba történt: {error}</p>
      </div>
    );
  }

  const selectedBuilding = buildings.find((b) => b.id === selectedBuildingId);
  const availableFloors = floors.filter(
    (f) => f.buildingId === selectedBuildingId,
  );
  const selectedFloor = floors.find((f) => f.id === selectedFloorId);

  const currentRooms = selectedFloorId ? rooms : [];

  return (
    <div className="dashboard-container">
      <section className="step-section">
        <div className="step-header">
          <h3>Épület kiválasztása</h3>
        </div>
        <div className="cards-grid buildings-grid">
          {buildings.map((b) => (
            <div
              key={b.id}
              className={`clickable-card building-card ${
                selectedBuildingId === b.id ? "selected" : ""
              }`}
              onClick={() => handleBuildingChange(b.id)}
            >
              <h4>{getBuildingDisplayName(b)}</h4>
              <p className="card-desc">{b.description || b.desc || ""}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="step-section">
        <div className="step-header">
          <h3>Emelet kiválasztása</h3>
        </div>
        <div className="floor-buttons-group">
          {availableFloors.length === 0 ? (
            <p>Ehhez az épülethez nem található emelet.</p>
          ) : (
            availableFloors.map((floor) => (
              <button
                key={floor.id}
                className={`floor-btn ${
                  selectedFloorId === floor.id ? "active" : ""
                }`}
                onClick={() => setSelectedFloorId(floor.id)}
              >
                {getFloorDisplayName(floor)}
              </button>
            ))
          )}
        </div>
      </section>

      <section className="step-section">
        <div className="step-header">
          <h3>
            Termek ({getBuildingDisplayName(selectedBuilding)},{" "}
            {selectedFloor
              ? getFloorDisplayName(selectedFloor)
              : "nincs emelet"}
            )
          </h3>
        </div>

        {roomsLoading ? (
          <p>Termek betöltése...</p>
        ) : roomsError ? (
          <p className="auth-error" role="alert">
            {roomsError}
          </p>
        ) : currentRooms.length === 0 ? (
          <p>Ezen az emeleten nincs terem.</p>
        ) : (
          <div className="cards-grid rooms-grid">
            {currentRooms.map((room) => (
              <div key={room.id} className="room-card">
                <div className="room-header">
                  <h4 className="room-title">{room.name}</h4>
                </div>
                <div className="room-details">
                  <div className="detail-item">
                    <span className="detail-label">Kapacitás</span>
                    <span className="detail-value">{room.capacity}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Felszereltség</span>
                    <span className="detail-value">
                      {room.equipment || "Nincs megadva"}
                    </span>
                  </div>
                </div>
                {canBook && (
                  <button
                    type="button"
                    className="primary-btn"
                    onClick={() => {
                      setBookingRoom(room);
                      setBookingError("");
                      setBookingNotice("");
                    }}
                  >
                    Terem foglalása
                  </button>
                )}
                {bookingRoom?.id === room.id && (
                  <form className="booking-form" onSubmit={submitBooking}>
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
                        value={endTime}
                        onChange={(event) => setEndTime(event.target.value)}
                        required
                      />
                    </label>
                    <label className="form-group">
                      Megjegyzés
                      <textarea
                        value={specialRequest}
                        onChange={(event) =>
                          setSpecialRequest(event.target.value)
                        }
                        maxLength={1000}
                      />
                    </label>
                    {bookingError && (
                      <p className="auth-error" role="alert">
                        {bookingError}
                      </p>
                    )}
                    <div className="booking-form-actions">
                      <button
                        type="button"
                        className="cancel-btn"
                        onClick={() => setBookingRoom(null)}
                        disabled={bookingSaving}
                      >
                        Mégse
                      </button>
                      <button
                        type="submit"
                        className="primary-btn"
                        disabled={bookingSaving}
                      >
                        {bookingSaving ? "Küldés..." : "Foglalás beküldése"}
                      </button>
                    </div>
                  </form>
                )}
              </div>
            ))}
          </div>
        )}
        {bookingNotice && (
          <p className="booking-success" role="status">
            {bookingNotice}
          </p>
        )}
      </section>
    </div>
  );
}
