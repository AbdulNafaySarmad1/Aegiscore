"use client";

// ─── Async UX Primitives ───
// Shared loading / error / empty / forbidden states so no module page
// renders a blank screen or a one-off copy of these patterns.

import type { LucideIcon } from "lucide-react";
import { AlertTriangle, Inbox, Lock, RefreshCw } from "lucide-react";
import { isApiError } from "@/lib/api-client";

export function Spinner({ size = 18, label }: { size?: number; label?: string }) {
  return (
    <span className="spinner-wrap" role="status" aria-live="polite">
      <span className="spinner" style={{ width: size, height: size }} />
      {label && <span className="spinner-label">{label}</span>}
    </span>
  );
}

export function LoadingState({ label = "Loading…", lines = 0 }: { label?: string; lines?: number }) {
  if (lines > 0) {
    return (
      <div className="skeleton-stack" aria-busy="true" aria-label={label}>
        {Array.from({ length: lines }, (_, i) => (
          <div key={i} className="skeleton" style={{ height: 16, width: `${88 - (i % 3) * 14}%` }} />
        ))}
      </div>
    );
  }
  return (
    <div className="loading-state">
      <Spinner label={label} />
    </div>
  );
}

export function TableSkeleton({ rows = 6, columns = 4 }: { rows?: number; columns?: number }) {
  return (
    <div className="table-wrap" aria-busy="true" aria-label="Loading table">
      <table>
        <thead>
          <tr>{Array.from({ length: columns }, (_, i) => <th key={i}><div className="skeleton" style={{ height: 12, width: 72 }} /></th>)}</tr>
        </thead>
        <tbody>
          {Array.from({ length: rows }, (_, r) => (
            <tr key={r}>{Array.from({ length: columns }, (_, c) => <td key={c}><div className="skeleton" style={{ height: 12, width: `${70 + ((r + c) % 3) * 10}%` }} /></td>)}</tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type ErrorStateProps = {
  error?: unknown;
  title?: string;
  onRetry?: () => void;
};

export function ErrorState({ error, title = "Something went wrong", onRetry }: ErrorStateProps) {
  if (isApiError(error) && error.isForbidden) {
    return <ForbiddenState message={error.message} />;
  }
  const message = isApiError(error)
    ? error.isRateLimited
      ? "Too many requests. Please wait a moment and try again."
      : error.message
    : "The request could not be completed.";
  return (
    <div className="error-state" role="alert">
      <AlertTriangle size={20} />
      <div className="state-body">
        <strong>{title}</strong>
        <p>{message}</p>
      </div>
      {onRetry && (
        <button type="button" className="btn btn-secondary btn-sm" onClick={onRetry}>
          <RefreshCw size={14} /> Retry
        </button>
      )}
    </div>
  );
}

export function ForbiddenState({ message }: { message?: string }) {
  return (
    <div className="empty-state forbidden-state" role="alert">
      <Lock size={40} />
      <h3>Access restricted</h3>
      <p>{message ?? "Your role does not include access to this workspace. If you believe this is a mistake, contact your administrator."}</p>
    </div>
  );
}

type EmptyStateProps = {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
};

export function EmptyState({ icon: Icon = Inbox, title, description, action }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <Icon size={40} />
      <h3>{title}</h3>
      {description && <p>{description}</p>}
      {action}
    </div>
  );
}
