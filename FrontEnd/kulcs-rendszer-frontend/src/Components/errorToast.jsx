import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";

const DISMISS_DELAY = 5000;

export default function ErrorToast({
  message,
  onDismiss,
  variant = "error",
  inline = false,
}) {
  const toastRegion = inline
    ? null
    : document.getElementById("error-toast-region");
  if (!message || (!inline && !toastRegion)) return null;

  const toast = (
    <TimedToast
      key={message}
      message={message}
      onDismiss={onDismiss}
      variant={variant}
      inline={inline}
    />
  );

  return inline ? toast : createPortal(toast, toastRegion);
}

function TimedToast({ message, onDismiss, variant, inline }) {
  const dismissRef = useRef(onDismiss);
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    dismissRef.current = onDismiss;
  }, [onDismiss]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setVisible(false);
      dismissRef.current?.();
    }, DISMISS_DELAY);

    return () => window.clearTimeout(timeoutId);
  }, []);

  const dismiss = () => {
    setVisible(false);
    dismissRef.current?.();
  };

  if (!visible) return null;

  return (
    <div
      className={`error-toast error-toast-${variant}${inline ? " error-toast-inline" : ""}`}
      role={variant === "error" ? "alert" : "status"}
    >
      <span>{message}</span>
      <button
        type="button"
        onClick={dismiss}
        aria-label={
          variant === "error" ? "Hibaüzenet bezárása" : "Üzenet bezárása"
        }
      >
        ×
      </button>
      <span className="error-toast-progress" aria-hidden="true">
        <span />
      </span>
    </div>
  );
}
