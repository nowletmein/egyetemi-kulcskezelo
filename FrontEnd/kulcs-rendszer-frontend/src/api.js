const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api";
const TOKEN_STORAGE_KEY = "kulcs-rendszer-token";
const NAME_IDENTIFIER_CLAIM =
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
const NAME_CLAIM =
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
const EMAIL_CLAIM =
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
const ROLE_CLAIM =
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

function getErrorMessage(result) {
  const rawMessage =
    typeof result === "string"
      ? result
      : result?.detail || result?.message || result?.title || result?.error;

  if (typeof rawMessage !== "string" || !rawMessage.trim()) return "";

  const message = /<\/?[a-z][\s\S]*>/i.test(rawMessage)
    ? new DOMParser().parseFromString(rawMessage, "text/html").body.textContent
    : rawMessage;
  const withoutStack = (message || rawMessage)
    .replace(
      /\s+at\s+(?:[\w.$`<>]+\.)+[\w.$`<>]+\s*\([\s\S]*$/i,
      "",
    )
    .trim();

  return withoutStack.replace(/^[\w.]+Exception:\s*/i, "").trim();
}

export async function request(path, options = {}) {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  let response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...options,
      headers,
    });
  } catch (error) {
    console.error("API hálózati hiba", {
      method: options.method || "GET",
      path,
      error,
    });
    throw error;
  }
  const responseText = await response.text();
  let result = null;

  if (responseText) {
    try {
      result = JSON.parse(responseText);
    } catch {
      result = responseText;
    }
  }

  if (!response.ok) {
    console.error("API kérés sikertelen", {
      method: options.method || "GET",
      path,
      status: response.status,
      statusText: response.statusText,
      response: result,
    });
    const message = getErrorMessage(result);
    const error = new Error(
      message || `A kérés sikertelen (${response.status} ${response.statusText}).`,
    );
    error.status = response.status;
    throw error;
  }

  return result;
}

export function jsonOptions(method, body) {
  return {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  };
}

export function getUserFromToken(token) {
  const parts = token.split(".");
  if (parts.length !== 3) {
    throw new Error("A szerver érvénytelen tokent adott vissza.");
  }

  let claims;
  try {
    const payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const paddedPayload = payload.padEnd(Math.ceil(payload.length / 4) * 4, "=");
    const binaryPayload = atob(paddedPayload);
    const bytes = Uint8Array.from(binaryPayload, (character) =>
      character.charCodeAt(0),
    );
    claims = JSON.parse(new TextDecoder().decode(bytes));
  } catch {
    throw new Error("A szerver érvénytelen tokent adott vissza.");
  }

  if (typeof claims !== "object" || claims === null) {
    throw new Error("A szerver érvénytelen tokent adott vissza.");
  }
  if (claims.exp && Number(claims.exp) * 1000 <= Date.now()) {
    throw new Error("A munkamenet lejárt. Jelentkezz be újra.");
  }

  const role = claims[ROLE_CLAIM] ?? claims.role;

  return {
    id: claims[NAME_IDENTIFIER_CLAIM] ?? claims.nameid ?? claims.sub ?? null,
    name: claims[NAME_CLAIM] ?? claims.name ?? "",
    email: claims[EMAIL_CLAIM] ?? claims.email ?? "",
    roles: Array.isArray(role) ? role : role ? [role] : [],
  };
}

export function restoreSession() {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (!token) return null;

  try {
    return { token, user: getUserFromToken(token) };
  } catch {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    return null;
  }
}

export function storeToken(token) {
  localStorage.setItem(TOKEN_STORAGE_KEY, token);
}

export function removeToken() {
  localStorage.removeItem(TOKEN_STORAGE_KEY);
}
