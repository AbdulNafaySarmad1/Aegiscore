"use client";

import { useState, useCallback, useRef, useEffect } from "react";
import { CHART_ZOOM_SESSION_KEY } from "./types";
import type { NChartZoomState } from "./types";

interface NChartZoomLayerProps {
  children: React.ReactNode;
  preserveKey?: string;
  onZoomChange?: (state: NChartZoomState) => void;
}

const DEFAULT_ZOOM: NChartZoomState = {
  scale: 1,
  translateX: 0,
  translateY: 0,
  brushStart: null,
  brushEnd: null,
};

function readSessionZoom(preserveKey?: string): NChartZoomState {
  if (!preserveKey) return { ...DEFAULT_ZOOM };
  try {
    const raw = sessionStorage.getItem(`${CHART_ZOOM_SESSION_KEY}:${preserveKey}`);
    if (!raw) return { ...DEFAULT_ZOOM };
    const parsed = JSON.parse(raw);
    return {
      scale: parsed.scale ?? 1,
      translateX: parsed.translateX ?? 0,
      translateY: parsed.translateY ?? 0,
      brushStart: parsed.brushStart ?? null,
      brushEnd: parsed.brushEnd ?? null,
    };
  } catch {
    return { ...DEFAULT_ZOOM };
  }
}

function writeSessionZoom(preserveKey: string | undefined, state: NChartZoomState) {
  if (!preserveKey) return;
  try {
    sessionStorage.setItem(`${CHART_ZOOM_SESSION_KEY}:${preserveKey}`, JSON.stringify(state));
  } catch {
    /* quota exceeded — silently drop */
  }
}

export function NChartZoomLayer({ children, preserveKey, onZoomChange }: NChartZoomLayerProps) {
  const [zoom, setZoom] = useState<NChartZoomState>(() => readSessionZoom(preserveKey));
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState<{ x: number; y: number } | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const lastTapRef = useRef<number>(0);

  const notify = useCallback(
    (state: NChartZoomState) => {
      onZoomChange?.(state);
      writeSessionZoom(preserveKey, state);
    },
    [onZoomChange, preserveKey]
  );

  const handleWheel = useCallback(
    (e: React.WheelEvent) => {
      if (!e.ctrlKey && !e.metaKey) return;
      e.preventDefault();
      setZoom((prev) => {
        const delta = e.deltaY < 0 ? 0.1 : -0.1;
        const next = Math.max(0.5, Math.min(3, prev.scale + delta));
        const newState = { ...prev, scale: next };
        notify(newState);
        return newState;
      });
    },
    [notify]
  );

  const handleDoubleClick = useCallback(() => {
    setZoom({ ...DEFAULT_ZOOM });
    notify({ ...DEFAULT_ZOOM });
    lastTapRef.current = 0;
  }, [notify]);

  const handleClick = useCallback(() => {
    const now = Date.now();
    if (now - lastTapRef.current < 300) {
      handleDoubleClick();
    }
    lastTapRef.current = now;
  }, [handleDoubleClick]);

  const startDrag = useCallback(
    (clientX: number, clientY: number) => {
      if (zoom.scale <= 1) return;
      setIsDragging(true);
      setDragStart({ x: clientX - zoom.translateX, y: clientY - zoom.translateY });
    },
    [zoom.scale, zoom.translateX, zoom.translateY]
  );

  const onDrag = useCallback(
    (clientX: number, clientY: number) => {
      if (!isDragging || !dragStart) return;
      const tx = clientX - dragStart.x;
      const ty = clientY - dragStart.y;
      const newState = { ...zoom, translateX: tx, translateY: ty };
      setZoom(newState);
      notify(newState);
    },
    [isDragging, dragStart, zoom, notify]
  );

  const endDrag = useCallback(() => {
    setIsDragging(false);
    setDragStart(null);
  }, []);

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      startDrag(e.clientX, e.clientY);
    },
    [startDrag]
  );

  const handleMouseMove = useCallback(
    (e: React.MouseEvent) => {
      onDrag(e.clientX, e.clientY);
    },
    [onDrag]
  );

  const handleMouseUp = useCallback(() => {
    endDrag();
  }, [endDrag]);

  const handleMouseLeave = useCallback(() => {
    endDrag();
  }, [endDrag]);

  const handleTouchStart = useCallback(
    (e: React.TouchEvent) => {
      if (e.touches.length === 1) {
        startDrag(e.touches[0].clientX, e.touches[0].clientY);
      }
    },
    [startDrag]
  );

  const handleTouchMove = useCallback(
    (e: React.TouchEvent) => {
      if (e.touches.length === 1) {
        onDrag(e.touches[0].clientX, e.touches[0].clientY);
      }
      if (e.touches.length === 2) {
        e.preventDefault();
        const dx = Math.abs(e.touches[0].clientX - e.touches[1].clientX);
        const dy = Math.abs(e.touches[0].clientY - e.touches[1].clientY);
        const dist = Math.sqrt(dx * dx + dy * dy);
        const scale = Math.max(0.5, Math.min(3, dist / 200));
        const newState = { ...zoom, scale };
        setZoom(newState);
        notify(newState);
      }
    },
    [zoom, onDrag, notify]
  );

  const handleTouchEnd = useCallback(() => {
    endDrag();
  }, [endDrag]);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;
    const handler = (e: WheelEvent) => {
      if (e.ctrlKey || e.metaKey) e.preventDefault();
    };
    container.addEventListener("wheel", handler, { passive: false });
    return () => container.removeEventListener("wheel", handler);
  }, []);

  return (
    <div
      ref={containerRef}
      role="region"
      aria-label="Zoomable chart area"
      onClick={handleClick}
      onWheel={handleWheel}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseLeave}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      style={{
        overflow: "hidden",
        cursor: isDragging ? "grabbing" : zoom.scale > 1 ? "grab" : "default",
        userSelect: "none",
        touchAction: zoom.scale > 1 ? "none" : "pan-x pan-y",
      }}
    >
      <div
        style={{
          transform: `scale(${zoom.scale}) translate(${zoom.translateX}px, ${zoom.translateY}px)`,
          transformOrigin: "top left",
          transition: isDragging ? "none" : "transform 0.15s ease",
          width: "100%",
          height: "100%",
        }}
      >
        {children}
      </div>
      {zoom.scale !== 1 && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            handleDoubleClick();
          }}
          style={{
            position: "absolute",
            top: "4px",
            right: "4px",
            padding: "0.2rem 0.45rem",
            border: "1px solid var(--line)",
            borderRadius: "5px",
            background: "var(--surface)",
            color: "var(--muted)",
            fontSize: "0.7rem",
            cursor: "pointer",
            zIndex: 10,
          }}
          aria-label="Reset zoom"
        >
          {(zoom.scale * 100).toFixed(0)}%
        </button>
      )}
    </div>
  );
}
