import React, { useState } from 'react';

const buildings = [
  {
    id: 'I',
    name: 'Building I',
    desc: 'Műszaki Informatikai Kar',
    floorRoomCounts: [4, 6, 6, 6, 6, 6, 6, 6, 6, 6, 0],
    availableKeys: 18,
  },
  {
    id: 'A',
    name: 'Building A',
    desc: 'Gazdaságtudományi Kar',
    basementRooms: 2,
    floorRoomCounts: [2, 8, 8],
    availableKeys: 22,
  },
  {
    id: 'B',
    name: 'Building B',
    desc: 'Mérnöki Kar',
    floorRoomCounts: [0, 8, 8],
    availableKeys: 9,
  },
];

export default function Dashboard() {
  const [selectedBuilding, setSelectedBuilding] = useState('I');
  const [selectedFloor, setSelectedFloor] = useState('0');

  const building = buildings.find((item) => item.id === selectedBuilding);
  const floors = [
    ...(building.basementRooms
      ? [{ id: 'basement', label: 'Alagsor', roomCount: building.basementRooms }]
      : []),
    ...building.floorRoomCounts.map((roomCount, floorNumber) => ({
      id: String(floorNumber),
      label: floorNumber === 0 ? 'Földszint' : `${floorNumber}. emelet`,
      roomCount,
    })),
  ];
  const currentFloor = floors.find((floor) => floor.id === selectedFloor);
  const rooms = Array.from({ length: currentFloor.roomCount }, (_, index) => {
    const roomNumber = String(index + 1).padStart(2, '0');
    const floorPrefix = selectedFloor === 'basement' ? 'A' : selectedFloor;
    const name = `${building.id}.${floorPrefix}${roomNumber}`;

    return { id: name, name, keyAvailable: true };
  });

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
              className={`clickable-card building-card ${selectedBuilding === b.id ? 'selected' : ''}`}
              onClick={() => {
                setSelectedBuilding(b.id);
                setSelectedFloor('0');
              }}
            >
              <h4>{b.name}</h4>
              <p className="card-desc">{b.desc}</p>
              <div className="card-footer-info">
                <span>
                  Összesen: {b.floorRoomCounts.reduce((total, count) => total + count, b.basementRooms ?? 0)} terem
                </span>
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="step-section">
        <div className="step-header">
          <h3>Emelet kiválasztása</h3>
        </div>
        <div className="floor-buttons-group">
          {floors.map((floor) => (
            <button
              key={floor.id}
              className={`floor-btn ${selectedFloor === floor.id ? 'active' : ''}`}
              onClick={() => setSelectedFloor(floor.id)}
            >
              {floor.label}
            </button>
          ))}
        </div>
      </section>

      <section className="step-section">
        <div className="step-header">
          <h3>Terem és kulcs foglalása ({building.id} Épület, {currentFloor.label})</h3>
        </div>
        {rooms.length === 0 ? (
          <p>
            {building.id === 'I' && selectedFloor === '10'
              ? 'A 10. emeleten nincs foglalható terem.'
              : 'Ezen az emeleten nincs foglalható terem.'}
          </p>
        ) : (
          <div className="cards-grid rooms-grid">
            {rooms.map((room) => (
              <div key={room.id} className="room-card">
                <div className="room-header">
                  <h4 className="room-title">{room.name}</h4>
                  <span className={`key-badge ${room.keyAvailable ? 'available' : 'borrowed'}`}>
                    {room.keyAvailable ? 'Kulcs Felvehető' : 'Kiadva'}
                  </span>
                </div>

                <div className="room-action">
                  <button
                    className={`book-key-btn ${!room.keyAvailable ? 'disabled' : ''}`}
                    disabled={!room.keyAvailable}
                  >
                    {room.keyAvailable ? 'Book Key' : 'Nincs Kulcs'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}