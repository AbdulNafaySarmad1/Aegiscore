"use client";

import { useState, useEffect, useRef, useMemo, useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import type { NChartDataPoint, NChartConfig, NChartGaugeConfig } from "./types";

interface NChartGaugeProps {
  data: NChartDataPoint[];
  config: NChartConfig;
  height: number;
}

const DEFAULT_ZONES: Array<{ color: string; from: number; to: number; label: string }> = [
  { color: "var(--green)", from: 0, to: 30, label: "nchart.gaugeLow" },
  { color: "var(--green)", from: 30, to: 60, label: "nchart.gaugeMedium" },
  { color: "var(--amber)", from: 60, to: 80, label: "nchart.gaugeHigh" },
  { color: "var(--red)", from: 80, to: 100, label: "nchart.gaugeCritical" },
];

function polarToCartesian(
  cx: number,
  cy: number,
  r: number,
  angleDeg: number
): { x: number; y: number } {
  const rad = ((angleDeg - 90) * Math.PI) / 180;
  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
}

function describeArc(
  cx: number,
  cy: number,
  innerR: number,
  outerR: number,
  startAngle: number,
  endAngle: number
): string {
  const startOuter = polarToCartesian(cx, cy, outerR, endAngle);
  const endOuter = polarToCartesian(cx, cy, outerR, startAngle);
  const startInner = polarToCartesian(cx, cy, innerR, startAngle);
  const endInner = polarToCartesian(cx, cy, innerR, endAngle);

  const largeArc = endAngle - startAngle > 180 ? 1 : 0;

  return [
    `M ${startOuter.x} ${startOuter.y}`,
    `A ${outerR} ${outerR} 0 ${largeArc} 0 ${endOuter.x} ${endOuter.y}`,
    `L ${endInner.x} ${endInner.y}`,
    `A ${innerR} ${innerR} 0 ${largeArc} 1 ${startInner.x} ${startInner.y}`,
    "Z",
  ].join(" ");
}

export function NChartGauge({ data, config, height }: NChartGaugeProps) {
  const t = useTranslations(config.i18nDomain ?? "nchart");
  const [animated, setAnimated] = useState(false);
  const mountedRef = useRef(false);

  const reducedMotion = useSyncExternalStore(
    (callback) => {
      const mql = window.matchMedia("(prefers-reduced-motion: reduce)");
      mql.addEventListener("change", callback);
      return () => mql.removeEventListener("change", callback);
    },
    () => window.matchMedia("(prefers-reduced-motion: reduce)").matches,
    () => false
  );

  useEffect(() => {
    const delay = reducedMotion ? 0 : 400;
    const id = setTimeout(() => setAnimated(true), delay);
    mountedRef.current = true;
    return () => clearTimeout(id);
  }, [reducedMotion]);

  const gaugeConfig: NChartGaugeConfig = config.gauge ?? {};
  const zones = gaugeConfig.zones ?? DEFAULT_ZONES;
  const max = gaugeConfig.max ?? 100;

  const value = useMemo(() => {
    if (data.length === 0) return 0;
    const key = config.valueKey ?? config.dataKey ?? config.yKey;
    if (!key) return 0;
    const raw = Number(data[0][key]);
    return Math.max(0, Math.min(max, isNaN(raw) ? 0 : raw));
  }, [data, config, max]);

  const normalizedValue = (value / max) * 100;
  const needleAngle = -90 + (normalizedValue / 100) * 270;

  const svgSize = Math.min(height, 320);
  const cx = svgSize / 2;
  const cy = svgSize * 0.6;
  const outerR = svgSize * 0.4;
  const innerR = outerR * 0.65;

  const needleLength = outerR * 0.75;
  const needleBase = outerR * 0.12;

  const needlePoint = polarToCartesian(cx, cy, needleLength, needleAngle);
  const needleLeft = polarToCartesian(cx, cy, needleBase, needleAngle - 90);
  const needleRight = polarToCartesian(cx, cy, needleBase, needleAngle + 90);

  const isAnimated = animated;

  return (
    <div
      role="meter"
      aria-valuenow={value}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-label={gaugeConfig.label ?? t("nchart.gauge")}
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        height,
        gap: "0.75rem",
      }}
    >
      <svg
        viewBox={`0 0 ${svgSize} ${svgSize}`}
        width={svgSize}
        height={svgSize}
        style={{ overflow: "visible" }}
      >
        {zones.map((zone, idx) => {
          const startAngle = -90 + (zone.from / 100) * 270;
          const endAngle = -90 + (zone.to / 100) * 270;
          const path = describeArc(cx, cy, innerR, outerR, startAngle, endAngle);
          return (
            <path
              key={idx}
              d={path}
              fill={zone.color}
              opacity={0.3}
              style={{ transition: isAnimated ? "opacity 0.6s ease" : "none" }}
            >
              {zone.label && <title>{t(zone.label)}</title>}
            </path>
          );
        })}

        <g
          style={{
            transformOrigin: `${cx}px ${cy}px`,
            transform: isAnimated
              ? `rotate(${needleAngle}deg)`
              : "rotate(-90deg)",
            transition: reducedMotion ? "none" : "transform 0.8s cubic-bezier(0.34, 1.56, 0.64, 1)",
          }}
        >
          <polygon
            points={`${needlePoint.x},${needlePoint.y} ${needleLeft.x},${needleLeft.y} ${needleRight.x},${needleRight.y}`}
            fill="var(--ink)"
          />
        </g>

        <circle
          cx={cx}
          cy={cy}
          r={innerR * 0.25}
          fill="var(--ink)"
          style={{
            transition: isAnimated ? "r 0.3s ease" : "none",
          }}
        />

        <text
          x={cx}
          y={cy + svgSize * 0.22}
          textAnchor="middle"
          fill="var(--ink)"
          fontSize={svgSize * 0.14}
          fontWeight={700}
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          style={{
            opacity: isAnimated ? 1 : 0,
            transition: "opacity 0.4s ease 0.3s",
          }}
        >
          {gaugeConfig.format ? gaugeConfig.format(value) : `${value.toFixed(0)}%`}
        </text>

        <text
          x={cx}
          y={cy + svgSize * 0.31}
          textAnchor="middle"
          fill="var(--muted)"
          fontSize={svgSize * 0.06}
          style={{
            opacity: isAnimated ? 1 : 0,
            transition: "opacity 0.4s ease 0.4s",
          }}
        >
          {gaugeConfig.label ?? t("nchart.gaugeLabel")}
        </text>
      </svg>
    </div>
  );
}
