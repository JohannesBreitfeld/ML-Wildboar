import React from 'react';
import { SPECIES } from '../../utils/species';

interface Props {
  selected: string[];
  onChange: (selected: string[]) => void;
}

function chipStyle(active: boolean): React.CSSProperties {
  return {
    display: 'inline-flex',
    alignItems: 'center',
    gap: 6,
    padding: '7px 12px',
    fontSize: 13,
    fontWeight: 500,
    background: active ? 'var(--text)' : 'var(--surface)',
    color: active ? 'var(--bg)' : 'var(--text-2)',
    border: '1px solid',
    borderColor: active ? 'var(--text)' : 'var(--border)',
    borderRadius: 999,
    cursor: 'pointer',
    fontFamily: 'var(--font-sans)',
    letterSpacing: '-0.005em',
    transition: 'all 0.12s',
  };
}

export function SpeciesChips({ selected, onChange }: Props) {
  const toggle = (id: string) => {
    const set = new Set(selected);
    if (set.has(id)) set.delete(id); else set.add(id);
    onChange(Array.from(set));
  };

  const allActive = selected.length === 0;

  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
      <button onClick={() => onChange([])} style={chipStyle(allActive)}>
        Alla arter
        <span className="mono" style={{ opacity: 0.6, fontSize: 11 }}>
          {allActive ? SPECIES.length : selected.length}
        </span>
      </button>
      {SPECIES.map(sp => {
        const active = selected.includes(sp.id);
        return (
          <button key={sp.id} onClick={() => toggle(sp.id)} style={chipStyle(active)}>
            <span style={{
              width: 8, height: 8, borderRadius: 2,
              background: sp.color,
              display: 'inline-block',
              flexShrink: 0,
              boxShadow: active ? '0 0 0 2px var(--surface)' : 'none',
            }} />
            {sp.label}
          </button>
        );
      })}
    </div>
  );
}
