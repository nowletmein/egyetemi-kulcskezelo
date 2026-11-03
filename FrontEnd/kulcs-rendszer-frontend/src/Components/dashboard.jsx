import { useEffect, useState } from "react";
import { request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

function getFloorDisplayName(floor) {
  if (!Number.isInteger(floor?.number)) return "Ismeretlen emelet";
  return floor.number === 0 ? "Földszint" : `${floor.number}. emelet`;
}

export default function Dashboard({ user, onSelectRoom }) {
  const [buildings, setBuildings] = useState([]);
  const [floors, setFloors] = useState([]);

  const [selectedBuildingId, setSelectedBuildingId] = useState(null);
  const [selectedFloorId, setSelectedFloorId] = useState(null);

  const [loading, setLoading] = useState(true);
  const [roomsLoading, setRoomsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [roomsLoadResult, setRoomsLoadResult] = useState({
    floorId: null,
    rooms: [],
    error: "",
  });
  const canBook =
    user && (!user.roles?.length || user.roles.includes("Oktato"));

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
    if (!selectedFloorId) return undefined;

    let isMounted = true;

    const fetchRooms = async () => {
      try {
        setRoomsLoading(true);
        const roomsData = await request(`/Floor/${selectedFloorId}/rooms`);
        if (isMounted) {
          setRoomsLoadResult({
            floorId: selectedFloorId,
            rooms: roomsData,
            error: "",
          });
        }
      } catch (err) {
        console.error(err);
        if (isMounted) {
          setRoomsLoadResult({
            floorId: selectedFloorId,
            rooms: [],
            error: `A termek betöltése nem sikerült: ${err.message}`,
          });
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
        <ErrorToast message={error} onDismiss={() => setError(null)} />
      </div>
    );
  }

  const selectedBuilding = buildings.find((b) => b.id === selectedBuildingId);
  const availableFloors = floors.filter(
    (f) => f.buildingId === selectedBuildingId,
  );
  const selectedFloor = floors.find((f) => f.id === selectedFloorId);

  const currentRooms =
    roomsLoadResult.floorId === selectedFloorId ? roomsLoadResult.rooms : [];
  const roomsError =
    roomsLoadResult.floorId === selectedFloorId ? roomsLoadResult.error : "";

  return (
    <div className="dashboard-container">
      <ErrorToast
        message={roomsError}
        onDismiss={() =>
          setRoomsLoadResult((current) => ({ ...current, error: "" }))
        }
      />
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
        ) : roomsError ? null : currentRooms.length === 0 ? (
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
                    onClick={() =>
                      onSelectRoom?.({
                        ...room,
                        buildingName: getBuildingDisplayName(selectedBuilding),
                        floorName: getFloorDisplayName(selectedFloor),
                      })
                    }
                  >
                    Terem foglalása
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
