import React, { useState, useEffect } from "react";

const API_BASE_URL = "http://localhost:5000/api";

export default function Dashboard() {
  const [buildings, setBuildings] = useState([]);
  const [floors, setFloors] = useState([]);
  const [rooms, setRooms] = useState([]);

  const [selectedBuildingId, setSelectedBuildingId] = useState(null);
  const [selectedFloorId, setSelectedFloorId] = useState(null);

  const [loading, setLoading] = useState(true);
  const [roomsLoading, setRoomsLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [buildingsRes, floorsRes] = await Promise.all([
          fetch(`${API_BASE_URL}/Building/getall`),
          fetch(`${API_BASE_URL}/Floor/getall`),
        ]);

        if (!buildingsRes.ok || !floorsRes.ok) {
          throw new Error("Hiba történt az adatok lekérdezésekor.");
        }

        const buildingsData = await buildingsRes.json();
        const floorsData = await floorsRes.json();

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
    if (!selectedFloorId) return;

    let isMounted = true;

    const fetchRooms = async () => {
      try {
        setRoomsLoading(true);
        const res = await fetch(
          `${API_BASE_URL}/Floor/${selectedFloorId}/rooms`,
        );
        if (!res.ok) {
          throw new Error("Nem sikerült betölteni a termeket.");
        }
        const roomsData = await res.json();
        if (isMounted) setRooms(roomsData);
      } catch (err) {
        console.error(err);
        if (isMounted) setRooms([]);
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
                {floor.name ||
                  floor.label ||
                  `${floor.level ?? floor.id}. emelet`}
              </button>
            ))
          )}
        </div>
      </section>

      <section className="step-section">
        <div className="step-header">
          <h3>
            Terem és kulcs foglalása ({getBuildingDisplayName(selectedBuilding)}
            , {selectedFloor?.name || selectedFloor?.label})
          </h3>
        </div>

        {roomsLoading ? (
          <p>Termek betöltése...</p>
        ) : currentRooms.length === 0 ? (
          <p>Ezen az emeleten nincs foglalható terem.</p>
        ) : (
          <div className="cards-grid rooms-grid">
            {currentRooms.map((room) => {
              const isAvailable = room.keyAvailable ?? room.isAvailable ?? true;
              return (
                <div key={room.id} className="room-card">
                  <div className="room-header">
                    <h4 className="room-title">
                      {room.name || room.roomNumber}
                    </h4>
                    <span
                      className={`key-badge ${
                        isAvailable ? "available" : "borrowed"
                      }`}
                    >
                      {isAvailable ? "Kulcs Felvehető" : "Kiadva"}
                    </span>
                  </div>

                  <div className="room-action">
                    <button
                      className={`book-key-btn ${!isAvailable ? "disabled" : ""}`}
                      disabled={!isAvailable}
                    >
                      {isAvailable ? "Book Key" : "Nincs Kulcs"}
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
