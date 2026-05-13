import React, { useMemo } from 'react';
import { ImageDto, HourlyAgg } from '../../types/api.types';
import { HourlyDistributionChart } from '../charts/HourlyDistributionChart';
import { ImageGallery } from '../gallery/ImageGallery';
import { formatDateLongSv } from '../../utils/species';

interface Props {
  date: string;
  images: ImageDto[];
  speciesFilter: string[];
  onClose: () => void;
}

export function DailyDrilldown({ date, images, speciesFilter, onClose }: Props) {
  const withAnimals = images.filter(i => !i.isEmpty);
  const empty = images.length - withAnimals.length;
  const speciesCount = new Set(withAnimals.flatMap(i => i.detections.map(d => d.species))).size;

  const hourlyAgg = useMemo<HourlyAgg[]>(() => {
    const arr: HourlyAgg[] = Array.from({ length: 24 }, (_, h) => ({ hour: h, total: 0, bySpecies: {} }));
    for (const img of images) {
      if (img.isEmpty) continue;
      const h = new Date(img.capturedAt).getHours();
      const row = arr[h];
      for (const det of img.detections) {
        if (speciesFilter.length > 0 && !speciesFilter.includes(det.species)) continue;
        row.bySpecies[det.species] = (row.bySpecies[det.species] || 0) + det.count;
        row.total += det.count;
      }
    }
    return arr;
  }, [images, speciesFilter]);

  return (
    <div style={{
      marginTop: 18,
      padding: 20,
      background: 'var(--surface-2)',
      border: '1px solid var(--border-strong)',
      borderRadius: 'var(--radius-lg)',
      display: 'flex', flexDirection: 'column', gap: 16,
      animation: 'drilldown-in 0.22s ease',
    }}>
      <style>{`@keyframes drilldown-in { from { opacity:0; transform: translateY(-4px); } to { opacity:1; transform:none; } }`}</style>

      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', gap: 12 }}>
        <div>
          <div style={{ fontSize: 11, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.1em', fontWeight: 500 }}>
            Detaljer för dagen
          </div>
          <div className="display" style={{ fontSize: 28, color: 'var(--text)', marginTop: 4, lineHeight: 1 }}>
            {formatDateLongSv(date)}
          </div>
          <div style={{ display: 'flex', gap: 16, marginTop: 10, fontSize: 13, color: 'var(--text-2)', flexWrap: 'wrap' }}>
            <span><span className="mono" style={{ color: 'var(--text)', fontWeight: 500 }}>{images.length}</span> bilder</span>
            <span><span className="mono" style={{ color: 'var(--accent)', fontWeight: 500 }}>{withAnimals.length}</span> med djur</span>
            <span><span className="mono" style={{ fontWeight: 500 }}>{empty}</span> tomma</span>
            <span><span className="mono" style={{ fontWeight: 500 }}>{speciesCount}</span> arter</span>
          </div>
        </div>
        <button
          onClick={onClose}
          autoFocus
          style={{
            background: 'transparent', border: '1px solid var(--border)', borderRadius: 999,
            padding: '6px 12px', fontSize: 12, color: 'var(--text-2)',
            display: 'inline-flex', alignItems: 'center', gap: 6,
          }}
        >
          ✕ Stäng
        </button>
      </div>

      <HourlyDistributionChart data={hourlyAgg} speciesFilter={speciesFilter} />

      {withAnimals.length > 0 && (
        <>
          <div style={{ fontSize: 12, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.06em', fontWeight: 500, marginTop: 4 }}>
            Bilder från denna dag ({withAnimals.length})
          </div>
          <ImageGallery images={withAnimals} minThumbnailWidth={140} maxItems={12} />
        </>
      )}
    </div>
  );
}
