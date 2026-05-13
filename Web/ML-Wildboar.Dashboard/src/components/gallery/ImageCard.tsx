import React, { useState } from 'react';
import { ImageDto } from '../../types/api.types';
import { speciesById, formatDateSv, formatTimeSv } from '../../utils/species';

interface Props {
  image: ImageDto;
  onClick: () => void;
}

export function ImageCard({ image, onClick }: Props) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div style={{
      background: 'var(--surface)',
      border: '1px solid var(--border)',
      borderRadius: 'var(--radius)',
      overflow: 'hidden',
      transition: 'border-color 0.15s',
    }}
      onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--border-strong)')}
      onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border)')}
    >
      {/* Image thumbnail */}
      <button
        onClick={onClick}
        style={{
          display: 'block', width: '100%', aspectRatio: '4 / 3',
          position: 'relative', border: 'none', padding: 0, cursor: 'pointer',
          overflow: 'hidden', background: 'var(--bg-2)',
        }}
      >
        <img
          src={image.blobUrl}
          alt={image.description || 'Viltkamerabild'}
          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
          loading="lazy"
        />
        {/* Detection badges */}
        {!image.isEmpty && image.detections.length > 0 && (
          <div style={{ position: 'absolute', bottom: 8, left: 8, display: 'flex', gap: 4, flexWrap: 'wrap' }}>
            {image.detections.slice(0, 2).map(d => {
              const sp = speciesById(d.species);
              if (!sp) return null;
              return (
                <span key={d.species} className="mono" style={{
                  background: 'rgba(0,0,0,0.55)', backdropFilter: 'blur(8px)',
                  color: 'white', fontSize: 11, padding: '3px 8px',
                  borderRadius: 999, display: 'inline-flex', alignItems: 'center', gap: 5,
                  fontWeight: 500,
                }}>
                  <span style={{ width: 6, height: 6, borderRadius: '50%', background: sp.color }} />
                  {sp.label} ×{d.count}
                </span>
              );
            })}
          </div>
        )}
        {image.isEmpty && (
          <div style={{
            position: 'absolute', bottom: 8, left: 8,
            background: 'rgba(0,0,0,0.45)', color: 'white',
            padding: '3px 8px', borderRadius: 999, fontSize: 11,
            backdropFilter: 'blur(6px)',
          }}>
            Tom scen
          </div>
        )}
      </button>

      {/* Info panel */}
      <div style={{ padding: '10px 12px 12px', display: 'flex', flexDirection: 'column', gap: 6 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
          <span className="mono" style={{ fontSize: 11, color: 'var(--text-3)' }}>
            {formatDateSv(image.partitionKey)} · {formatTimeSv(image.capturedAt)}
          </span>
          <button onClick={() => setExpanded(e => !e)} style={{
            background: 'transparent', border: 'none', color: 'var(--text-3)',
            fontSize: 12, padding: 0, fontFamily: 'var(--font-mono)',
          }}>
            {expanded ? '− info' : '+ info'}
          </button>
        </div>
        {!expanded ? (
          <div style={{
            fontSize: 12, color: 'var(--text-2)', lineHeight: 1.4,
            display: '-webkit-box', WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical', overflow: 'hidden',
          }}>
            {image.description}
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8, paddingTop: 4 }}>
            {image.description && (
              <div style={{ fontSize: 12, color: 'var(--text-2)', lineHeight: 1.45 }}>
                {image.description}
              </div>
            )}
            {image.weather && <InfoRow label="Väder" value={image.weather} />}
            <InfoRow label="Detektioner" value={
              image.isEmpty ? 'Inga djur' : (
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
                  {image.detections.map(d => {
                    const sp = speciesById(d.species);
                    return (
                      <div key={d.species} style={{
                        display: 'flex', flexDirection: 'column', gap: 2,
                        padding: '4px 8px', borderRadius: 4,
                        background: 'var(--surface-2)', border: '1px solid var(--border)',
                        fontSize: 11,
                      }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                          {sp && <span style={{ width: 6, height: 6, borderRadius: 2, background: sp.color, flexShrink: 0 }} />}
                          <span style={{ fontWeight: 500 }}>{sp?.label ?? d.species} ×{d.count}</span>
                          <span className="mono" style={{ color: 'var(--text-3)', fontSize: 10 }}>{d.confidence}</span>
                        </div>
                        {d.reasoning && (
                          <div style={{ color: 'var(--text-2)', fontStyle: 'italic', lineHeight: 1.4, paddingLeft: 11 }}>
                            {d.reasoning}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )
            } />
          </div>
        )}
      </div>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8, fontSize: 12 }}>
      <span style={{ color: 'var(--text-3)', flexShrink: 0, fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.06em', paddingTop: 2 }}>
        {label}
      </span>
      <span style={{ color: 'var(--text-2)', textAlign: 'right', flex: 1 }}>{value}</span>
    </div>
  );
}
