import React, { useEffect } from 'react';
import { ImageDto } from '../../types/api.types';
import { speciesById, formatDateLongSv, formatTimeSv } from '../../utils/species';

interface Props {
  images: ImageDto[];
  index: number;
  onClose: () => void;
  onPrev: () => void;
  onNext: () => void;
}

export function ImageLightbox({ images, index, onClose, onPrev, onNext }: Props) {
  const image = images[index];

  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
      if (e.key === 'ArrowLeft') onPrev();
      if (e.key === 'ArrowRight') onNext();
    };
    window.addEventListener('keydown', handleKey);
    return () => window.removeEventListener('keydown', handleKey);
  }, [onClose, onPrev, onNext]);

  if (!image) return null;

  const navBtn = (side: 'left' | 'right'): React.CSSProperties => ({
    position: 'absolute', top: '50%', transform: 'translateY(-50%)',
    [side]: -8, zIndex: 2,
    width: 40, height: 40, borderRadius: '50%',
    background: 'rgba(255,255,255,0.92)', color: 'var(--text)',
    border: 'none', cursor: 'pointer',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    boxShadow: '0 4px 12px rgba(0,0,0,0.2)',
    fontSize: 18,
  });

  return (
    <div
      style={{
        position: 'fixed', inset: 0, background: 'rgba(8,12,10,0.85)',
        backdropFilter: 'blur(8px)', zIndex: 100,
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: 32,
      }}
      onClick={onClose}
    >
      <div
        onClick={e => e.stopPropagation()}
        style={{
          display: 'grid', gridTemplateColumns: 'minmax(0,1fr) 360px', gap: 16,
          maxWidth: 1400, width: '100%', maxHeight: '90vh',
        }}
      >
        {/* Image area */}
        <div style={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <button onClick={onPrev} style={navBtn('left')}>‹</button>
          <div style={{ width: '100%', aspectRatio: '4/3', maxHeight: '85vh', position: 'relative', overflow: 'hidden', borderRadius: 'var(--radius-lg)' }}>
            <img
              src={image.blobUrl}
              alt={image.description || 'Viltkamerabild'}
              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
            />
            {!image.isEmpty && image.detections.length > 0 && (
              <div style={{ position: 'absolute', top: 14, right: 14, display: 'flex', flexDirection: 'column', gap: 6, alignItems: 'flex-end' }}>
                {image.detections.map(d => {
                  const sp = speciesById(d.species);
                  return (
                    <div key={d.species} className="mono" style={{
                      background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(10px)',
                      color: 'white', padding: '6px 12px', borderRadius: 999,
                      fontSize: 12, fontWeight: 500,
                      display: 'inline-flex', alignItems: 'center', gap: 8,
                      border: `1px solid ${sp?.color ?? 'rgba(255,255,255,0.3)'}`,
                    }}>
                      <span style={{ width: 8, height: 8, borderRadius: '50%', background: sp?.color }} />
                      {sp?.label ?? d.species} ×{d.count}
                      <span style={{ opacity: 0.55, fontSize: 10, textTransform: 'uppercase' }}>{d.confidence}</span>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
          <button onClick={onNext} style={navBtn('right')}>›</button>
        </div>

        {/* Detail panel */}
        <div style={{
          background: 'var(--surface)', borderRadius: 'var(--radius-lg)',
          padding: 24, display: 'flex', flexDirection: 'column', gap: 18,
          overflowY: 'auto',
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', gap: 12 }}>
            <div>
              <div style={{ fontSize: 12, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                Bild {index + 1} / {images.length}
              </div>
              <h3 className="display" style={{ fontSize: 24, marginTop: 4, color: 'var(--text)', lineHeight: 1.1 }}>
                {image.isEmpty ? 'Tom scen' : image.detections.map(d => speciesById(d.species)?.label ?? d.species).join(' + ')}
              </h3>
            </div>
            <button onClick={onClose} style={{
              background: 'var(--surface-2)', border: '1px solid var(--border)',
              borderRadius: '50%', width: 32, height: 32,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: 'var(--text-2)', fontSize: 16,
            }}>
              ✕
            </button>
          </div>

          <div style={{
            display: 'grid', gridTemplateColumns: '1fr 1fr',
            gap: 1, background: 'var(--border)',
            border: '1px solid var(--border)', borderRadius: 'var(--radius)',
            overflow: 'hidden',
          }}>
            <KV label="Datum" value={formatDateLongSv(image.partitionKey)} />
            <KV label="Tid" value={formatTimeSv(image.capturedAt)} mono />
            {image.weather && <KV label="Väder" value={image.weather} />}
            <KV label="Status" value={image.isEmpty ? 'Tom' : 'Djur upptäckt'} accent={!image.isEmpty} />
          </div>

          {image.description && (
            <Section title="Beskrivning">
              <p style={{ fontSize: 14, lineHeight: 1.55, color: 'var(--text-2)' }}>{image.description}</p>
            </Section>
          )}

          {!image.isEmpty && image.detections.length > 0 && (
            <Section title={`Detektioner (${image.detections.length})`}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                {image.detections.map(d => {
                  const sp = speciesById(d.species);
                  return (
                    <div key={d.species} style={{
                      display: 'flex', alignItems: 'start', gap: 12,
                      padding: '10px 12px',
                      background: 'var(--surface-2)',
                      border: '1px solid var(--border)',
                      borderRadius: 'var(--radius)',
                    }}>
                      <span style={{ width: 24, height: 24, borderRadius: 6, background: sp?.color, flexShrink: 0, marginTop: 2 }} />
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ fontSize: 14, fontWeight: 500 }}>{sp?.label ?? d.species}</div>
                        <div style={{ fontSize: 12, color: 'var(--text-3)' }}>Konfidens: {d.confidence}</div>
                        {d.reasoning && (
                          <div style={{ fontSize: 12, color: 'var(--text-2)', marginTop: 4, lineHeight: 1.45, fontStyle: 'italic' }}>
                            {d.reasoning}
                          </div>
                        )}
                      </div>
                      <span className="mono" style={{ fontSize: 20, fontWeight: 500, color: sp?.color, flexShrink: 0 }}>×{d.count}</span>
                    </div>
                  );
                })}
              </div>
            </Section>
          )}

          <Section title="Rådata">
            <div style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--text-3)', lineHeight: 1.6 }}>
              <div>id: {image.id}</div>
              <div>partition: {image.partitionKey}</div>
            </div>
          </Section>
        </div>
      </div>
    </div>
  );
}

function KV({ label, value, mono, accent }: { label: string; value: string; mono?: boolean; accent?: boolean }) {
  return (
    <div style={{ background: 'var(--surface)', padding: '10px 12px' }}>
      <div style={{ fontSize: 10, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>{label}</div>
      <div className={mono ? 'mono' : ''} style={{ fontSize: 14, marginTop: 2, color: accent ? 'var(--accent)' : 'var(--text)', fontWeight: 500 }}>
        {value}
      </div>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <div style={{ fontSize: 11, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: 8, fontWeight: 500 }}>
        {title}
      </div>
      {children}
    </div>
  );
}
