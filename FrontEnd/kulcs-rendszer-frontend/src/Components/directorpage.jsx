import { useCallback, useEffect, useState } from "react";
import { request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

const AUDIT_LOG_PAGE_SIZE = 50;

function getLocalDateBoundaryUtc(date, dayOffset = 0) {
  const boundary = new Date(`${date}T00:00:00`);
  boundary.setDate(boundary.getDate() + dayOffset);
  return boundary.toISOString();
}

function formatAuditTimestamp(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "Ismeretlen időpont"
    : new Intl.DateTimeFormat("hu-HU", {
        dateStyle: "medium",
        timeStyle: "medium",
      }).format(date);
}

export default function DirectorPage() {
  const [summary, setSummary] = useState({
    buildings: 0,
    floors: 0,
    rooms: 0,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [refreshCount, setRefreshCount] = useState(0);
  const [auditFilters, setAuditFilters] = useState({
    fromDate: "",
    toDate: "",
    method: "",
    statusCode: "",
  });
  const [appliedAuditFilters, setAppliedAuditFilters] = useState(auditFilters);
  const [auditPage, setAuditPage] = useState(1);
  const [auditRefreshCount, setAuditRefreshCount] = useState(0);
  const [auditLog, setAuditLog] = useState({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: AUDIT_LOG_PAGE_SIZE,
  });
  const [auditLoading, setAuditLoading] = useState(true);
  const [auditError, setAuditError] = useState("");

  const loadSummary = useCallback(async () => {
    const [buildingsResult, floorsResult, roomsResult] =
      await Promise.allSettled([
        request("/Building/getall"),
        request("/Floor/getall"),
        request("/Room/getall"),
      ]);

    const errors = [];
    const nextSummary = {};
    for (const [label, result, key] of [
      ["épületek", buildingsResult, "buildings"],
      ["emeletek", floorsResult, "floors"],
      ["termek", roomsResult, "rooms"],
    ]) {
      if (result.status === "fulfilled" && Array.isArray(result.value)) {
        nextSummary[key] = result.value.length;
      } else {
        nextSummary[key] = 0;
        errors.push(
          result.status === "rejected"
            ? `A(z) ${label} betöltése nem sikerült: ${result.reason.message}`
            : `A(z) ${label} API-válasza érvénytelen formátumú.`,
        );
      }
    }

    return { summary: nextSummary, error: errors.join(" ") };
  }, []);

  useEffect(() => {
    let cancelled = false;

    loadSummary().then(({ summary: nextSummary, error: nextError }) => {
      if (cancelled) return;

      setSummary(nextSummary);
      setError(nextError);
      setLoading(false);
    });

    return () => {
      cancelled = true;
    };
  }, [loadSummary, refreshCount]);

  useEffect(() => {
    let cancelled = false;
    const query = new URLSearchParams({
      page: String(auditPage),
      pageSize: String(AUDIT_LOG_PAGE_SIZE),
    });

    if (appliedAuditFilters.fromDate) {
      query.set(
        "fromUtc",
        getLocalDateBoundaryUtc(appliedAuditFilters.fromDate),
      );
    }
    if (appliedAuditFilters.toDate) {
      query.set(
        "toUtcExclusive",
        getLocalDateBoundaryUtc(appliedAuditFilters.toDate, 1),
      );
    }
    if (appliedAuditFilters.method) {
      query.set("method", appliedAuditFilters.method);
    }
    if (appliedAuditFilters.statusCode) {
      query.set("statusCode", appliedAuditFilters.statusCode);
    }

    request(`/audit-log?${query}`)
      .then((result) => {
        if (
          !result ||
          !Array.isArray(result.items) ||
          !Number.isInteger(result.totalCount)
        ) {
          throw new Error("Az auditnapló API-válasza érvénytelen formátumú.");
        }
        if (!cancelled) setAuditLog(result);
      })
      .catch((requestError) => {
        if (cancelled) return;
        setAuditLog({
          items: [],
          totalCount: 0,
          page: auditPage,
          pageSize: AUDIT_LOG_PAGE_SIZE,
        });
        setAuditError(
          `Az auditnapló betöltése nem sikerült: ${requestError.message}`,
        );
      })
      .finally(() => {
        if (!cancelled) setAuditLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [appliedAuditFilters, auditPage, auditRefreshCount]);

  const refreshSummary = () => {
    setLoading(true);
    setError("");
    setRefreshCount((count) => count + 1);
  };

  return (
    <main className="admin-page director-page">
      <ErrorToast message={error} onDismiss={() => setError("")} />
      <header className="admin-heading">
        <div>
          <p className="admin-eyebrow">VEZETŐI ÁTTEKINTÉS</p>
          <h1>Üzemeltetési igazgató</h1>
          <p className="admin-intro">
            Épületek és termek áttekintése, riportok és auditinformációk.
          </p>
        </div>
        <button
          type="button"
          className="admin-button"
          onClick={refreshSummary}
          disabled={loading}
        >
          {loading ? "Betöltés..." : "Adatok frissítése"}
        </button>
      </header>

      <section className="director-metrics" aria-label="Létesítményadatok">
        {[
          ["Épületek", summary.buildings],
          ["Emeletek", summary.floors],
          ["Termek", summary.rooms],
        ].map(([label, count]) => (
          <article className="admin-panel director-metric" key={label}>
            <p className="admin-eyebrow">{label.toLocaleUpperCase("hu")}</p>
            <strong>{loading ? "—" : count}</strong>
            <span>nyilvántartott elem</span>
          </article>
        ))}
      </section>

      <section className="admin-panel director-report">
        <div className="admin-panel-heading">
          <div>
            <p className="admin-eyebrow">KIMUTATÁSOK</p>
            <h2>Teremkihasználtság</h2>
          </div>
          <span className="director-unavailable-badge">
            Backend API szükséges
          </span>
        </div>
        <p>
          A teremkihasználtsági riport még nem jeleníthető meg: a foglalások
          összesített végpontja jelenleg Admin és Portás szerepkörrel érhető el,
          Igazgató szerepkörrel nem.
        </p>
      </section>

      <section className="admin-panel director-report">
        <div className="admin-panel-heading">
          <div>
            <p className="admin-eyebrow">BIZTONSÁG ÉS MEGFELELŐSÉG</p>
            <h2>Auditnapló</h2>
          </div>
          <button
            type="button"
            className="admin-button"
            onClick={() => {
              setAuditLoading(true);
              setAuditError("");
              setAuditRefreshCount((count) => count + 1);
            }}
            disabled={auditLoading}
          >
            {auditLoading ? "Betöltés..." : "Napló frissítése"}
          </button>
        </div>
        <form
          className="audit-log-filters"
          onSubmit={(event) => {
            event.preventDefault();
            setAuditLoading(true);
            setAuditError("");
            setAuditPage(1);
            setAppliedAuditFilters(auditFilters);
            setAuditRefreshCount((count) => count + 1);
          }}
        >
          <label>
            Kezdő dátum
            <input
              type="date"
              value={auditFilters.fromDate}
              onChange={(event) =>
                setAuditFilters((filters) => ({
                  ...filters,
                  fromDate: event.target.value,
                }))
              }
            />
          </label>
          <label>
            Záró dátum
            <input
              type="date"
              value={auditFilters.toDate}
              onChange={(event) =>
                setAuditFilters((filters) => ({
                  ...filters,
                  toDate: event.target.value,
                }))
              }
            />
          </label>
          <label>
            Művelet
            <select
              value={auditFilters.method}
              onChange={(event) =>
                setAuditFilters((filters) => ({
                  ...filters,
                  method: event.target.value,
                }))
              }
            >
              <option value="">Mindegyik</option>
              {["GET", "POST", "PUT", "PATCH", "DELETE"].map((method) => (
                <option key={method} value={method}>
                  {method}
                </option>
              ))}
            </select>
          </label>
          <label>
            HTTP státusz
            <select
              value={auditFilters.statusCode}
              onChange={(event) =>
                setAuditFilters((filters) => ({
                  ...filters,
                  statusCode: event.target.value,
                }))
              }
            >
              <option value="">Mindegyik</option>
              {[200, 201, 204, 400, 401, 403, 404, 409, 500].map(
                (statusCode) => (
                  <option key={statusCode} value={statusCode}>
                    {statusCode}
                  </option>
                ),
              )}
            </select>
          </label>
          <button type="submit" className="admin-button admin-button-primary">
            Szűrés
          </button>
        </form>

        {auditError && (
          <p className="audit-log-error" role="alert">
            {auditError}
          </p>
        )}

        <div className="audit-log-table-wrap" aria-live="polite">
          {auditLoading ? (
            <p className="admin-empty">Auditnapló betöltése...</p>
          ) : auditLog.items.length === 0 ? (
            <p className="admin-empty">Nincs megjeleníthető esemény.</p>
          ) : (
            <table className="audit-log-table">
              <thead>
                <tr>
                  <th scope="col">Időpont</th>
                  <th scope="col">Felhasználó</th>
                  <th scope="col">Művelet</th>
                  <th scope="col">Eredmény</th>
                  <th scope="col">Kliensadat</th>
                  <th scope="col">Időtartam</th>
                </tr>
              </thead>
              <tbody>
                {auditLog.items.map((entry) => (
                  <tr key={entry.eventId}>
                    <td>{formatAuditTimestamp(entry.occurredAtUtc)}</td>
                    <td>
                      <strong>{entry.userName || "Névtelen / rendszer"}</strong>
                      {entry.userId && (
                        <small className="audit-log-secondary">
                          Azonosító: {entry.userId}
                        </small>
                      )}
                      {entry.roles && (
                        <small className="audit-log-secondary">
                          Szerepkör: {entry.roles}
                        </small>
                      )}
                    </td>
                    <td>
                      <span className="audit-log-method">{entry.method}</span>
                      <code className="audit-log-path">{entry.path}</code>
                    </td>
                    <td>
                      <span
                        className={`audit-log-status${entry.statusCode >= 400 ? " audit-log-status-failure" : ""}`}
                      >
                        {entry.statusCode}
                      </span>
                    </td>
                    <td>
                      {entry.userAgent ? (
                        <small className="audit-log-secondary">
                          {entry.userAgent}
                        </small>
                      ) : (
                        "—"
                      )}
                    </td>
                    <td>{entry.durationMilliseconds} ms</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div className="audit-log-pagination">
          <span className="admin-count">
            {auditLog.totalCount} esemény · {auditLog.page} /{" "}
            {Math.max(1, Math.ceil(auditLog.totalCount / auditLog.pageSize))}
          </span>
          <div>
            <button
              type="button"
              className="admin-button"
              onClick={() => {
                setAuditLoading(true);
                setAuditError("");
                setAuditPage((page) => Math.max(1, page - 1));
              }}
              disabled={auditLoading || auditPage <= 1}
            >
              Előző
            </button>
            <button
              type="button"
              className="admin-button"
              onClick={() => {
                setAuditLoading(true);
                setAuditError("");
                setAuditPage((page) => page + 1);
              }}
              disabled={
                auditLoading ||
                auditPage >=
                  Math.max(
                    1,
                    Math.ceil(auditLog.totalCount / auditLog.pageSize),
                  )
              }
            >
              Következő
            </button>
          </div>
        </div>
      </section>
    </main>
  );
}
