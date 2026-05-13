import React, { useRef, useState, useEffect, useMemo } from 'react';
import { HourlyAgg } from '../../types/api.types';
import { SPECIES } from '../../utils/species';

const PAD = { top: 16, right: 12, bottom: 28, left: 36 };

function niceMax(v: number): number {
  if (v <= 0) return 4;
  const exp = Math.pow(10, Math.floor(Math.log10(v)));
  const m = v / exp;
  let n: number;
  if (m <= 1) n = 1; else if (m <= 2) n = 2; else if (m <= 5) n = 5; else n = 10;
  return n * exp;
}

function useContainerWidth(ref: React.RefObject<HTMLDivElement | null>): number {
  const [w, setW] = useState(640);
  useEffect(() => {
    if (!ref.current) return;
    const ro = new ResizeObserver(entries => {
      for (const e of entries) setW(Math.max(280, Math.floor(e.contentRect.width)));
    });
    ro.observe(ref.current);
    return () => ro.disconnect();
  }, [ref]);
  return w;
}

interface Props {
  data: HourlyAgg[];
  speciesFilter: string[];
}

export function HourlyDistributionChart({ data, speciesFilter }: Props) {
  const wrapRef = useRef<HTMLDivElement>(null);
  const width = useContainerWidth(wrapRef);
  const height = 240;
  const innerW = width - PAD.left - PAD.right;
  const innerH = height - PAD.top - PAD.bottom;
  const [hoverHour, setHoverHour] = useState<number | null>(null);

  const speciesOrder = speciesFilter.length > 0
    ? SPECIES.filter(s => speciesFilter.includes(s.id))
    : SPECIES;

  const rows = useMemo(() => {
    if (data.length === 24) return data;
    return Array.from({ length: 24 }, (_, h) => data.find(d => d.hour === h) ?? { hour: h, total: 0, bySpecies: {} });
  }, [data]);

  const maxVal = useMemo(() => {
    return niceMax(Math.max(...rows.flatMap(r => speciesOrder.map(s => r.bySpecies[s.id] || 0)), 1));
  }, [rows, speciesOrder]);

  const xStep = innerW / 23;
  const xPos = (h: number) => PAD.left + h * xStep;
  const yPos = (v: number) => PAD.top + innerH * (1 - v / maxVal);

  return (
    <div ref={wrapRef} style={{ width: '100%', minHeight: height, position: 'relative' }}>
      <svg width="100%" height={height} onMouseLeave={() => setHoverHour(null)}>
        {[0, 1, 2, 3, 4].map(i => {
          const v = (maxVal / 4) * i;
          const y = yPos(v);
          return (
            <g key={i}>
              <line x1={PAD.left} x2="100%" y1={y} y2={y}
                stroke="var(--border)" strokeDasharray={i === 0 ? '0' : '3 4'} />
              <text x={PAD.left - 8} y={y + 4} textAnchor="end"
                fontSize="11" fill="var(--text-3)" fontFamily="var(--font-mono)">
                {Math.round(v)}
              </text>
            </g>
          );
        })}

        {speciesOrder.map(sp => {
          const pts = rows.map(r => `${xPos(r.hour)},${yPos(r.bySpecies[sp.id] || 0)}`).join(' ');
          return (
            <g key={sp.id}>
              <polyline points={pts} fill="none" stroke={sp.color}
                strokeWidth={1.75} strokeLinecap="round" strokeLinejoin="round" opacity={0.95} />
              {rows.map(r => {
                const v = r.bySpecies[sp.id] || 0;
                if (v === 0) return null;
                return <circle key={r.hour} cx={xPos(r.hour)} cy={yPos(v)} r={2.5} fill={sp.color} />;
              })}
            </g>
          );
        })}

        {rows.map(r => {
          if (r.hour % 3 !== 0) return null;
          return (
            <text key={r.hour} x={xPos(r.hour)} y={height - 10} textAnchor="middle"
              fontSize="11" fill="var(--text-3)" fontFamily="var(--font-mono)">
              {String(r.hour).padStart(2, '0')}
            </text>
          );
        })}

        {rows.map(r => (
          <rect key={`h${r.hour}`}
            x={xPos(r.hour) - xStep / 2} y={PAD.top}
            width={xStep} height={innerH}
            fill="transparent"
            onMouseEnter={() => setHoverHour(r.hour)} />
        ))}

        {hoverHour != null && (
          <line x1={xPos(hoverHour)} x2={xPos(hoverHour)}
            y1={PAD.top} y2={PAD.top + innerH}
            stroke="var(--border-strong)" strokeDasharray="3 3" />
        )}
      </svg>

      {hoverHour != null && (() => {
        const r = rows[hoverHour];
        const items = speciesOrder
          .map(sp => ({ sp, v: r.bySpecies[sp.id] || 0 }))
          .filter(x => x.v > 0)
          .sort((a, b) => b.v - a.v);
        if (!items.length) return null;
        const xP = xPos(hoverHour);
        const placeLeft = xP > width * 0.6;
        return (
          <div style={{
            position: 'absolute',
            top: PAD.top + 4,
            left: placeLeft ? 'auto' : xP + 8,
            right: placeLeft ? width - xP + 8 : 'auto',
            background: 'var(--surface)',
            border: '1px solid var(--border-strong)',
            borderRadius: 'var(--radius)',
            padding: '8px 10px',
            fontSize: 12,
            boxShadow: 'var(--shadow)',
            pointerEvents: 'none',
            minWidth: 140,
            zIndex: 2,
          }}>
            <div className="mono" style={{ color: 'var(--text-3)', fontSize: 11, marginBottom: 4 }}>
              {String(hoverHour).padStart(2, '0')}:00
            </div>
            {items.map(({ sp, v }) => (
              <div key={sp.id} style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '2px 0' }}>
                <span style={{ width: 8, height: 8, borderRadius: 2, background: sp.color, flexShrink: 0 }} />
                <span style={{ flex: 1 }}>{sp.label}</span>
                <span className="mono" style={{ fontWeight: 500 }}>{v}</span>
              </div>
            ))}
          </div>
        );
      })()}
    </div>
  );
}
