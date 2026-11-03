import { useEffect, useRef } from "react";
import { createPortal } from "react-dom";

const DISMISS_DELAY = 7000;

export default function ErrorToast({ message, onDismiss }) {
  const dismissRef = useRef(onDismiss);

  useEffect(() => {
    dismissRef.current = onDismiss;
  }, [onDismiss]);

  useEffect(() => {
    if (!message) return undefined;
    const timeoutId = window.setTimeout(() => {
      dismissRef.current?.();
    }, DISMISS_DELAY);

    return () => window.clearTimeout(timeoutId);
  }, [message]);

  const toastRegion = document.getElementById("error-toast-region");
  if (!message || !toastRegion) return null;

  const dismiss = () => {
    dismissRef.current?.();
  };

  return createPortal(
    <div className="error-toast" role="alert">
      <span>{message}</span>
      <button type="button" onClick={dismiss} aria-label="Hibaüzenet bezárása">
        ×
      </button>
    </div>,
    toastRegion,
  );
}
