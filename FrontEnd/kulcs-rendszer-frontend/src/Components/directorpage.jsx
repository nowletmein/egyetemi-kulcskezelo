import { useCallback, useEffect, useState } from "react";
import { request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

export default function DirectorPage() {
  const [summary, setSummary] = useState({
    buildings: 0,
    floors: 0,
    rooms: 0,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [refreshCount, setRefreshCount] = useState(0);

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
          <span className="director-unavailable-badge">
            Még nincs megvalósítva
          </span>
        </div>
        <p>
          Auditnapló-lekérő API nem érhető el, ezért az események elemzése még
          nincs megvalósítva.
        </p>
      </section>
    </main>
  );
}
