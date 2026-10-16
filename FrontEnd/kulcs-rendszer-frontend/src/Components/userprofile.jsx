import React, { useState } from 'react';

export default function UserProfile({ user }) {
  const [isEditingData, setIsEditingData] = useState(false);
  const [name, setName] = useState('Kovács Balázs');
  const [email, setEmail] = useState(user?.email || 'user@uni-pannon.hu');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const tickets = [
    { id: '#TK-1024', room: 'I.101', issue: 'Sérült HDMI kábel a projektorhoz', status: 'Folyamatban', date: '2026.10.12' },
    { id: '#TK-0891', room: 'A.204', issue: 'Hiányzó projektor távirányító', status: 'Megoldva', date: '2026.09.28' },
  ];

  const handleSaveData = (e) => {
    e.preventDefault();
    setIsEditingData(false);
    alert('Személyes adatok sikeresen frissítve!');
  };

  const handlePasswordChange = (e) => {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      alert('A megadott új jelszavak nem egyeznek!');
      return;
    }
    alert('Jelszó sikeresen megváltoztatva!');
    setCurrentPassword('');
    setNewPassword('');
    setConfirmPassword('');
  };

  return (
    <div className="profile-container">
      <div className="profile-card">
        <div className="profile-header-bg"></div>
        <div className="profile-body">
          <div className="profile-avatar-large">KB</div>
          <h2 className="profile-name">{name}</h2>
          <p className="profile-email">{email}</p>

          <div className="profile-sections">
            <div className="profile-section-block">
              <div className="section-title-header">
                <h3>Személyes adatok</h3>
                {!isEditingData && (
                  <button type="button" className="secondary-btn" onClick={() => setIsEditingData(true)}>Adatok módosítása</button>
                )}
              </div>

              {isEditingData ? (
                <form onSubmit={handleSaveData} className="edit-form">
                  <div className="form-group">
                    <label htmlFor="edit-name">Teljes név</label>
                    <input type="text" id="edit-name" value={name} onChange={(e) => setName(e.target.value)} required/>
                  </div>
                  <div className="form-group">
                    <label htmlFor="edit-email">E-mail cím</label>
                    <input type="email" id="edit-email" value={email} onChange={(e) => setEmail(e.target.value)} required/>
                  </div>
                  <div className="form-actions-inline">
                    <button type="submit" className="primary-btn">Mentés</button>
                    <button type="button" className="cancel-btn" onClick={() => setIsEditingData(false)}>Mégse</button>
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

            <div className="profile-section-block">
              <h3>Jelszó módosítása</h3>
              <form onSubmit={handlePasswordChange} className="password-form">
                <div className="form-group">
                  <label htmlFor="current-pass">Jelenlegi jelszó</label>
                  <input type="password" id="current-pass" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} required/>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="new-pass">Új jelszó</label>
                    <input type="password" id="new-pass" placeholder="Új jelszó" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required/>
                  </div>
                  <div className="form-group">
                    <label htmlFor="confirm-pass">Új jelszó megerősítése</label>
                    <input type="password" id="confirm-pass" placeholder="Új jelszó újra" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required/>
                  </div>
                </div>
                <button type="submit" className="primary-btn align-self-start">Jelszó frissítése</button>
              </form>
            </div>

            <div className="profile-section-block">
              <h3>Bejelentett hibajegyek</h3>
              <div className="tickets-list">
                {tickets.map((ticket) => (
                  <div key={ticket.id} className="ticket-item">
                    <div className="ticket-top">
                      <span className="ticket-id">{ticket.id}</span>
                      <span className="ticket-room">{ticket.room}</span>
                      <span className={`ticket-status ${ticket.status === 'Megoldva' ? 'resolved' : 'pending'}`}>
                        {ticket.status}
                      </span>
                    </div>
                    <p className="ticket-issue">{ticket.issue}</p>
                    <span className="ticket-date">{ticket.date}</span>
                  </div>
                ))}
              </div>
            </div>

          </div>
        </div>
      </div>
    </div>
  );
}