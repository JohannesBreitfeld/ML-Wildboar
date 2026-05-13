import React from 'react';
import { DateRange, PresetId } from '../../types/api.types';
import { dateKey, formatDateSv } from '../../utils/species';

interface Props {
  range: DateRange;
  onChange: (range: DateRange) => void;
  compact?: boolean;
}

const PRESETS: { id: PresetId; label: string; shortLabel: string; days: number }[] = [
  { id: '7d',  label: '7 dagar',  shortLabel: '7d',  days: 7  },
  { id: '14d', label: '14 dagar', shortLabel: '14d', days: 14 },
  { id: '30d', label: '30 dagar', shortLabel: '30d', days: 30 },
];

export function DateRangePicker({ range, onChange, compact = false }: Props) {
  const apply = (days: number, id: PresetId) => {
    const end = new Date();
    const start = new Date();
    start.setDate(start.getDate() - (days - 1));
    onChange({ from: dateKey(start), to: dateKey(end), preset: id });
  };

  return (
    <div style={{
      display: compact ? 'flex' : 'inline-flex',
      alignItems: 'center',
      background: 'var(--surface)',
      border: '1px solid var(--border)',
      borderRadius: 999,
      padding: 3,
      gap: 2,
      width: compact ? '100%' : 'auto',
    }}>
      {!compact && (
        <div style={{
          display: 'inline-flex', alignItems: 'center', gap: 6,
          padding: '5px 12px', fontSize: 12, color: 'var(--text-3)',
          borderRight: '1px solid var(--border)', marginRight: 4,
          whiteSpace: 'nowrap',
        }}>
          <span style={{ fontSize: 13 }}>📅</span>
          <span className="mono">{formatDateSv(range.from)} – {formatDateSv(range.to)}</span>
        </div>
      )}
      {PRESETS.map(p => (
        <button
          key={p.id}
          onClick={() => apply(p.days, p.id)}
          style={{
            border: 'none',
            background: range.preset === p.id ? 'var(--text)' : 'transparent',
            color: range.preset === p.id ? 'var(--bg)' : 'var(--text-2)',
            padding: '7px 12px',
            borderRadius: 999,
            fontSize: 13,
            fontWeight: 500,
            cursor: 'pointer',
            fontFamily: 'var(--font-sans)',
            flex: compact ? 1 : '0 0 auto',
          }}
        >
          {compact ? p.shortLabel : p.label}
        </button>
      ))}
    </div>
  );
}
