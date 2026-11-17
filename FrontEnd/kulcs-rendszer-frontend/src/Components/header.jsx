import { useEffect, useRef, useState } from "react";
import { request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

function getMonogram(name, email) {
  const parts = (name || "").trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return (email || "?").charAt(0).toUpperCase();

  const first = parts[0].charAt(0);
  const last = parts.length > 1 ? parts[parts.length - 1].charAt(0) : "";
  return (first + last).toLocaleUpperCase("hu");
}

export default function Header({
  user,
  onLogout,
  currentPage,
  setCurrentPage,
}) {
  const [notifications, setNotifications] = useState([]);
  const [notificationsError, setNotificationsError] = useState("");
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [isClearingNotifications, setIsClearingNotifications] = useState(false);
  const [isLoadingNotifications, setIsLoadingNotifications] = useState(true);
  const [notificationToastVisible, setNotificationToastVisible] =
    useState(false);
  const notificationsMenuRef = useRef(null);
  const loadNotificationsRef = useRef(() => {});
  const notificationKeysRef = useRef(null);

  useEffect(() => {
    if (user?.id == null) return undefined;

    let active = true;
    let requestPending = false;
    const loadNotifications = async () => {
      if (requestPending || document.visibilityState === "hidden") return;
      requestPending = true;
      try {
        const result = await request("/User/notifications");
        if (!Array.isArray(result)) {
          throw new Error("Az értesítések betöltése nem sikerült.");
        }
        if (active) {
          const currentKeys = result.map((notification) =>
            JSON.stringify([
              notification.createdAt ?? notification.CreatedAt ?? "",
              notification.message ?? notification.Message ?? "",
            ]),
          );
          const previousKeys = notificationKeysRef.current;
          let hasNewNotification =
            previousKeys === null ? currentKeys.length > 0 : false;
          if (previousKeys) {
            const previousKeyCounts = new Map();
            previousKeys.forEach((key) => {
              previousKeyCounts.set(key, (previousKeyCounts.get(key) || 0) + 1);
            });
            hasNewNotification = currentKeys.some((key) => {
              const previousCount = previousKeyCounts.get(key) || 0;
              if (previousCount > 0) {
                previousKeyCounts.set(key, previousCount - 1);
                return false;
              }
              return true;
            });
          }
          if (hasNewNotification) setNotificationToastVisible(true);
          notificationKeysRef.current = currentKeys;
          setNotifications(result);
          setNotificationsError("");
        }
      } catch (error) {
        if (active) {
          setNotificationsError(
            error.message || "Az értesítések betöltése nem sikerült.",
          );
        }
      } finally {
        requestPending = false;
        if (active) setIsLoadingNotifications(false);
      }
    };

    const refreshWhenVisible = () => {
      if (document.visibilityState === "visible") loadNotifications();
    };

    loadNotificationsRef.current = loadNotifications;
    loadNotifications();
    const intervalId = window.setInterval(loadNotifications, 5_000);
    window.addEventListener("focus", refreshWhenVisible);
    document.addEventListener("visibilitychange", refreshWhenVisible);

    return () => {
      active = false;
      loadNotificationsRef.current = () => {};
      window.clearInterval(intervalId);
      window.removeEventListener("focus", refreshWhenVisible);
      document.removeEventListener("visibilitychange", refreshWhenVisible);
    };
  }, [user?.id]);

  useEffect(() => {
    if (!notificationsOpen) return undefined;

    const closeOnOutsideClick = (event) => {
      if (!notificationsMenuRef.current?.contains(event.target)) {
        setNotificationsOpen(false);
      }
    };
    const closeOnEscape = (event) => {
      if (event.key === "Escape") setNotificationsOpen(false);
    };

    document.addEventListener("mousedown", closeOnOutsideClick);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("mousedown", closeOnOutsideClick);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [notificationsOpen]);

  const clearNotifications = async () => {
    setIsClearingNotifications(true);
    setNotificationsError("");
    try {
      await request("/User/notifications", { method: "DELETE" });
      setNotifications([]);
    } catch (error) {
      setNotificationsError(
        error.message || "Az értesítések törlése nem sikerült.",
      );
    } finally {
      setIsClearingNotifications(false);
    }
  };

  const formatNotificationDate = (notification) => {
    const createdAt = notification.createdAt ?? notification.CreatedAt;
    if (!createdAt) return "";
    const date = new Date(createdAt);
    return Number.isNaN(date.getTime())
      ? String(createdAt)
      : date.toLocaleString("hu-HU");
  };

  return (
    <header className="header">
      <div className="header-brand">
        <div>
          <span className="header-title">UniKey</span>
          <span className="header-subtitle">Egyetemi Kulcskezelő</span>
        </div>
      </div>

      {user ? (
        <div className="header-user-section">
          {user.roles?.includes("Admin") ? (
            <button
              className={`nav-btn ${currentPage === "admin" ? "active" : ""}`}
              onClick={() => setCurrentPage("admin")}
            >
              Adminisztráció
            </button>
          ) : user.roles?.includes("Portas") ? (
            <button
              className={`nav-btn ${currentPage === "receptionist" ? "active" : ""}`}
              onClick={() => setCurrentPage("receptionist")}
            >
              Portás felület
            </button>
          ) : user.roles?.includes("Igazgato") ? (
            <button
              className={`nav-btn ${currentPage === "director" ? "active" : ""}`}
              onClick={() => setCurrentPage("director")}
            >
              Igazgatói áttekintés
            </button>
          ) : (
            <>
              <button
                className={`nav-btn ${currentPage === "dashboard" ? "active" : ""}`}
                onClick={() => setCurrentPage("dashboard")}
              >
                Foglalás
              </button>
              <button
                className={`nav-btn ${currentPage === "bookings" ? "active" : ""}`}
                onClick={() => setCurrentPage("bookings")}
              >
                Lefoglalt termek
              </button>
            </>
          )}

          <div className="notification-menu" ref={notificationsMenuRef}>
            <button
              type="button"
              className="notification-trigger"
              aria-label={`Értesítések${notifications.length ? `, ${notifications.length} értesítés` : ""}`}
              aria-expanded={notificationsOpen}
              aria-controls="notification-panel"
              onClick={() => {
                setNotificationsOpen((open) => !open);
                loadNotificationsRef.current();
              }}
              title="Értesítések"
            >
              <svg
                aria-hidden="true"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.8"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
                <path d="M10 21h4" />
              </svg>
              {notifications.length > 0 && (
                <span className="notification-count" aria-hidden="true">
                  {notifications.length > 99 ? "99+" : notifications.length}
                </span>
              )}
            </button>
            {notificationsOpen && (
              <section
                className="notification-panel"
                id="notification-panel"
                aria-label="Értesítések"
              >
                <div className="notification-panel-header">
                  <h2>Értesítések</h2>
                  {notifications.length > 0 && (
                    <button
                      type="button"
                      className="notification-clear"
                      onClick={clearNotifications}
                      disabled={isClearingNotifications}
                    >
                      {isClearingNotifications ? "Törlés..." : "Összes törlése"}
                    </button>
                  )}
                </div>
                {notificationsError ? (
                  <p
                    className="notification-message notification-error"
                    role="alert"
                  >
                    {notificationsError}
                  </p>
                ) : isLoadingNotifications ? (
                  <p className="notification-message">
                    Értesítések betöltése...
                  </p>
                ) : notifications.length === 0 ? (
                  <p className="notification-message">
                    Nincsenek értesítéseid.
                  </p>
                ) : (
                  <ul className="notification-list">
                    {notifications.map((notification, index) => (
                      <li
                        className="notification-item"
                        key={`${notification.createdAt ?? notification.CreatedAt ?? "notification"}-${index}`}
                      >
                        <p>{notification.message ?? notification.Message}</p>
                        <time>{formatNotificationDate(notification)}</time>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            )}
          </div>

          <div
            className={`user-profile-clickable ${currentPage === "profile" ? "active-profile" : ""}`}
            onClick={() => setCurrentPage("profile")}
            style={{ cursor: "pointer" }}
            title="Saját profil"
          >
            <div className="avatar" aria-hidden="true">
              {getMonogram(user.name, user.email)}
            </div>
          </div>

          <button className="logout-btn" onClick={onLogout}>
            Kijelentkezés
          </button>
        </div>
      ) : (
        <div className="header-auth-buttons">
          <button
            className={`auth-tab ${currentPage === "login" ? "active" : ""}`}
            onClick={() => setCurrentPage("login")}
          >
            Bejelentkezés
          </button>
          <button
            className={`auth-tab ${currentPage === "register" ? "active" : ""}`}
            onClick={() => setCurrentPage("register")}
          >
            Regisztráció
          </button>
        </div>
      )}
      {notificationToastVisible && (
        <div className="notification-toast-region">
          <ErrorToast
            message="Értesítése érkezett"
            onDismiss={() => setNotificationToastVisible(false)}
            variant="notification"
            inline
          />
        </div>
      )}
    </header>
  );
}
