import React, { useRef, useState, useEffect, useMemo } from 'react';
import { DailyAgg } from '../../types/api.types';
import { SPECIES, formatDateSv, formatDateLongSv } from '../../utils/species';

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
  data: DailyAgg[];
  mode: 'empty-vs-animal' | 'by-species';
  speciesFilter: string[];
  selectedDate: string | null;
  onSelectDay: (date: string | null) => void;
}

export function DailyDetectionChart({ data, mode, speciesFilter, selectedDate, onSelectDay }: Props) {
  const wrapRef = useRef<HTMLDivElement>(null);
  const width = useContainerWidth(wrapRef);
  const height = 280;
  const innerW = width - PAD.left - PAD.right;
  const innerH = height - PAD.top - PAD.bottom;

  const speciesOrder = speciesFilter.length > 0
    ? SPECIES.filter(s => speciesFilter.includes(s.id))
    : SPECIES;

  const maxVal = useMemo(() => {
    if (!data.length) return 4;
    if (mode === 'empty-vs-animal') {
      return niceMax(Math.max(...data.map(r => r.total)));
    }
    return niceMax(Math.max(...data.map(r =>
      speciesOrder.reduce((s, sp) => s + (r.bySpecies[sp.id] || 0), 0)
    ), 1));
  }, [data, mode, speciesOrder]);

  if (!data.length) {
    return (
      <div ref={wrapRef} style={{ minHeight: height, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-3)', fontSize: 13 }}>
        Ingen data
      </div>
    );
  }

  const bandW = innerW / data.length;
  const barPad = Math.max(2, bandW * 0.18);
  const barW = bandW - barPad * 2;
  const labelStride = Math.max(1, Math.ceil(44 / bandW));

  const yPos = (v: number) => PAD.top + innerH * (1 - v / maxVal);
  const hVal = (v: number) => innerH * (v / maxVal);

  return (
    <div ref={wrapRef} style={{ width: '100%', minHeight: height }}>
      <svg width="100%" height={height} role="img" aria-label="Bilder per dag">
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

        {data.map((r, i) => {
          const x = PAD.left + i * bandW + barPad;
          const isSelected = selectedDate === r.date;
          const dim = !!selectedDate && !isSelected;
          const showLabel = i === data.length - 1 || i % labelStride === 0;

          let bars: React.ReactNode[];
          if (mode === 'empty-vs-animal') {
            const animalH = hVal(r.withAnimals);
            const emptyH = hVal(r.empty);
            bars = [
              <rect key="animal" x={x} y={PAD.top + innerH - animalH}
                width={barW} height={animalH} rx="3"
                fill="var(--has-animal)" opacity={dim ? 0.35 : 1} />,
              <rect key="empty" x={x} y={PAD.top + innerH - animalH - emptyH}
                width={barW} height={emptyH} rx="3"
                fill="var(--empty)" opacity={dim ? 0.25 : 0.65} />,
            ];
          } else {
            let acc = 0;
            bars = speciesOrder.map(sp => {
              const v = r.bySpecies[sp.id] || 0;
              const seg = (
                <rect key={sp.id} x={x} y={PAD.top + innerH - hVal(acc + v)}
                  width={barW} height={hVal(v)} rx={acc === 0 ? 3 : 0}
                  fill={sp.color} opacity={dim ? 0.35 : 1} />
              );
              acc += v;
              return seg;
            });
          }

          return (
            <g key={r.date}
              onClick={() => onSelectDay(isSelected ? null : r.date)}
              style={{ cursor: 'pointer' }}>
              <rect x={x - barPad} y={PAD.top} width={bandW} height={innerH} fill="transparent">
                <title>{`${formatDateLongSv(r.date)} – ${r.total} bilder (${r.withAnimals} med djur)`}</title>
              </rect>
              {bars}
              {isSelected && (
                <rect x={x - 3} y={PAD.top - 3} width={barW + 6} height={innerH + 6}
                  fill="none" stroke="var(--accent)" strokeWidth="1.5" rx="6" />
              )}
              {showLabel && (
                <text x={x + barW / 2} y={height - 10} textAnchor="middle"
                  fontSize="11" fill="var(--text-3)" fontFamily="var(--font-mono)">
                  {formatDateSv(r.date)}
                </text>
              )}
            </g>
          );
        })}
      </svg>
    </div>
  );
}
