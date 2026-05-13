import React from 'react';
import { Sparkline } from './Sparkline';

interface Props {
  label: string;
  value: string | number;
  sub?: string;
  trend?: number;
  accent?: string;
  sparkData?: number[];
  sparkColor?: string;
}

export function StatCard({ label, value, sub, trend, accent, sparkData, sparkColor }: Props) {
  return (
    <div
      aria-label={`${label}: ${value}`}
      style={{
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
        padding: 'var(--pad-card)',
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <span style={{ fontSize: 12, fontWeight: 500, textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--text-3)' }}>
          {label}
        </span>
      </div>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
        <span className="mono" style={{
          fontSize: 32, fontWeight: 600, lineHeight: 1, letterSpacing: '-0.02em',
          color: accent || 'var(--text)',
          fontVariantNumeric: 'tabular-nums',
        }}>
          {value}
        </span>
        {sub && (
          <span style={{ fontSize: 13, color: 'var(--text-3)' }}>{sub}</span>
        )}
      </div>
      {(trend != null || sparkData) && (
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8, marginTop: 4 }}>
          {trend != null ? (
            <span style={{
              fontSize: 12,
              color: trend >= 0 ? 'var(--accent)' : 'var(--warn)',
              fontWeight: 500,
              whiteSpace: 'nowrap',
            }}>
              {trend >= 0 ? '↑' : '↓'} {Math.abs(trend).toFixed(0)}%
              <span style={{ color: 'var(--text-3)', fontWeight: 400, marginLeft: 6 }}>vs. förra</span>
            </span>
          ) : <span />}
          {sparkData && (
            <Sparkline data={sparkData} color={sparkColor || accent || 'var(--accent)'} width={70} height={24} />
          )}
        </div>
      )}
    </div>
  );
}
