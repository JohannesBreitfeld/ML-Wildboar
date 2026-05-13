import React, { useState, useMemo } from 'react';
import { DateRange } from '../types/api.types';
import { useDetectionData } from '../hooks/useDetectionData';
import { useImageGallery } from '../hooks/useImageGallery';
import { DateRangePicker } from '../components/filters/DateRangePicker';
import { SpeciesChips } from '../components/filters/SpeciesChips';
import { StatCard } from '../components/dashboard/StatCard';
import { DailyDetectionChart } from '../components/charts/DailyDetectionChart';
import { HourlyDistributionChart } from '../components/charts/HourlyDistributionChart';
import { DailyDrilldown } from '../components/dashboard/DailyDrilldown';
import { ImageGallery } from '../components/gallery/ImageGallery';
import { SPECIES, formatDateLongSv, dateKey } from '../utils/species';

function defaultRange(): DateRange {
  const end = new Date();
  const start = new Date();
  start.setDate(start.getDate() - 13);
  return { from: dateKey(start), to: dateKey(end), preset: '14d' };
}

function LegendChip({ items }: { items: { color: string; label: string }[] }) {
  return (
    <div style={{ display: 'flex', gap: 12, fontSize: 12, color: 'var(--text-2)', flexWrap: 'wrap' }}>
      {items.map(it => (
        <div key={it.label} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ width: 10, height: 10, borderRadius: 3, background: it.color }} />
          {it.label}
        </div>
      ))}
    </div>
  );
}

function SpeciesLegend({ speciesIds }: { speciesIds: string[] }) {
  const items = speciesIds.length > 0
    ? SPECIES.filter(s => speciesIds.includes(s.id))
    : SPECIES;
  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, fontSize: 12, color: 'var(--text-2)' }}>
      {items.map(sp => (
        <div key={sp.id} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ width: 10, height: 10, borderRadius: 3, background: sp.color }} />
          {sp.label}
        </div>
      ))}
    </div>
  );
}

function Card({ children }: { children: React.ReactNode }) {
  return (
    <section style={{
      background: 'var(--surface)',
      border: '1px solid var(--border)',
      borderRadius: 'var(--radius-lg)',
      padding: 'var(--pad-section)',
    }}>
      {children}
    </section>
  );
}

function CardHeader({ title, subtitle, right }: { title: string; subtitle?: string; right?: React.ReactNode }) {
  return (
    <div style={{
      display: 'flex', justifyContent: 'space-between', alignItems: 'start',
      gap: 12, marginBottom: 16, flexWrap: 'wrap',
    }}>
      <div>
        <h2 style={{ fontSize: 17, fontWeight: 600, letterSpacing: '-0.01em' }}>{title}</h2>
        {subtitle && <div style={{ fontSize: 13, color: 'var(--text-3)', marginTop: 2 }}>{subtitle}</div>}
      </div>
      {right && <div>{right}</div>}
    </div>
  );
}

export function Dashboard() {
  const [range, setRange] = useState<DateRange>(defaultRange);
  const [speciesFilter, setSpeciesFilter] = useState<string[]>([]);
  const [selectedDate, setSelectedDate] = useState<string | null>(null);

  const speciesParam = speciesFilter.length > 0 ? speciesFilter.join(',') : undefined;

  const { data: dashData, isLoading, error } = useDetectionData({
    from: range.from,
    to: range.to,
    species: speciesParam,
  });

  // Recent images with animals
  const { data: recentData } = useImageGallery({
    from: range.from,
    to: range.to,
    species: speciesParam,
    withAnimals: true,
    pageSize: 8,
  });

  // Drilldown images for selected day
  const { data: drilldownData } = useImageGallery(
    { date: selectedDate!, species: speciesParam, pageSize: 100 },
    !!selectedDate,
  );

  const dailyMode: 'empty-vs-animal' | 'by-species' = speciesFilter.length > 0 ? 'by-species' : 'empty-vs-animal';

  const sparkTotal = useMemo(() => dashData?.dailyAgg.map(d => d.total) ?? [], [dashData]);
  const sparkAnimals = useMemo(() => dashData?.dailyAgg.map(d => d.withAnimals) ?? [], [dashData]);

  const uniqueSpecies = useMemo(() => {
    if (!dashData) return 0;
    const ids = new Set<string>();
    dashData.dailyAgg.forEach(d => Object.keys(d.bySpecies).forEach(k => ids.add(k)));
    return ids.size;
  }, [dashData]);

  const isNarrow = window.innerWidth < 640;

  const handleRangeChange = (newRange: DateRange) => {
    setRange(newRange);
    if (selectedDate && (selectedDate < newRange.from || selectedDate > newRange.to)) {
      setSelectedDate(null);
    }
  };

  if (error) {
    return (
      <main style={{ maxWidth: 1500, margin: '0 auto', padding: '28px', display: 'flex', flexDirection: 'column', gap: 'var(--gap)' }}>
        <div style={{ padding: 24, background: 'var(--surface)', border: '1px solid var(--border)', borderRadius: 'var(--radius-lg)', color: 'var(--danger)' }}>
          <p>Fel vid inläsning: {(error as Error).message}</p>
          <button onClick={() => window.location.reload()} style={{ marginTop: 12, padding: '8px 16px', background: 'var(--text)', color: 'var(--bg)', border: 'none', borderRadius: 999, cursor: 'pointer' }}>
            Försök igen
          </button>
        </div>
      </main>
    );
  }

  return (
    <main style={{ maxWidth: 1500, margin: '0 auto', padding: '28px', display: 'flex', flexDirection: 'column', gap: 'var(--gap)' }}>

      {/* Header row */}
      <header style={{ display: 'flex', flexDirection: 'column', gap: 18, paddingBottom: 4 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 24, flexWrap: 'wrap' }}>
          <div style={{ minWidth: 0, flex: '1 1 300px' }}>
            <div style={{ fontSize: 12, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.1em', fontWeight: 500 }}>
              Översikt
            </div>
            <h1 className="display" style={{ fontSize: 'clamp(28px, 4.5vw, 44px)', lineHeight: 1.05, marginTop: 4 }}>
              Viltkamera-aktivitet
            </h1>
            <p style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 10 }}>
              {formatDateLongSv(range.from)} – {formatDateLongSv(range.to)}
            </p>
          </div>
          <DateRangePicker range={range} onChange={handleRangeChange} compact={isNarrow} />
        </div>

        {/* Species filter */}
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
            <div style={{
              display: 'flex', alignItems: 'center', gap: 6,
              color: 'var(--text-3)', fontSize: 12, fontWeight: 500,
              paddingRight: 8, borderRight: '1px solid var(--border)',
              textTransform: 'uppercase', letterSpacing: '0.06em',
            }}>
              ⊃ Filtrera
            </div>
            <SpeciesChips selected={speciesFilter} onChange={setSpeciesFilter} />
          </div>
        </Card>
      </header>

      {/* Stat grid */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 'var(--gap)' }}>
        <StatCard
          label="Totala bilder"
          value={(dashData?.totalImages ?? 0).toLocaleString('sv-SE')}
          trend={undefined}
          sparkData={sparkTotal}
          sparkColor="var(--text-3)"
        />
        <StatCard
          label="Med djur"
          value={(dashData?.withAnimals ?? 0).toLocaleString('sv-SE')}
          sub={dashData && dashData.totalImages > 0 ? `${((dashData.withAnimals / dashData.totalImages) * 100).toFixed(0)}%` : undefined}
          accent="var(--accent)"
          sparkData={sparkAnimals}
          sparkColor="var(--accent)"
        />
        <StatCard
          label="Tomma"
          value={(dashData?.empty ?? 0).toLocaleString('sv-SE')}
          sub={dashData && dashData.totalImages > 0 ? `${((dashData.empty / dashData.totalImages) * 100).toFixed(0)}%` : undefined}
        />
        <StatCard
          label="Arter sedda"
          value={uniqueSpecies}
          sub={speciesFilter.length === 0 ? 'alla arter' : `av ${speciesFilter.length} valda`}
        />
      </div>

      {/* Daily chart */}
      <Card>
        <CardHeader
          title="Bilder per dag"
          subtitle={speciesFilter.length > 0
            ? `Stack per art – ${speciesFilter.length} ${speciesFilter.length === 1 ? 'art' : 'arter'} valda`
            : 'Tomma scener vs. bilder med djur'}
          right={speciesFilter.length > 0
            ? <SpeciesLegend speciesIds={speciesFilter} />
            : <LegendChip items={[{ color: 'var(--has-animal)', label: 'Med djur' }, { color: 'var(--empty)', label: 'Tomma' }]} />}
        />
        {isLoading ? (
          <div style={{ height: 280, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-3)' }}>
            Laddar…
          </div>
        ) : (
          <>
            <DailyDetectionChart
              data={dashData?.dailyAgg ?? []}
              mode={dailyMode}
              speciesFilter={speciesFilter}
              selectedDate={selectedDate}
              onSelectDay={setSelectedDate}
            />
            {!selectedDate && (
              <div style={{ fontSize: 12, color: 'var(--text-3)', marginTop: 8, display: 'flex', alignItems: 'center', gap: 6 }}>
                ○ Klicka på en dag för att se timfördelning
              </div>
            )}
            {selectedDate && (
              <DailyDrilldown
                date={selectedDate}
                images={drilldownData?.images ?? []}
                speciesFilter={speciesFilter}
                onClose={() => setSelectedDate(null)}
              />
            )}
          </>
        )}
      </Card>

      {/* Hourly chart */}
      <Card>
        <CardHeader
          title="Tidsfördelning"
          subtitle="Detektioner per timme på dygnet, en linje per art"
          right={<SpeciesLegend speciesIds={speciesFilter} />}
        />
        {isLoading ? (
          <div style={{ height: 240, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-3)' }}>
            Laddar…
          </div>
        ) : (
          <HourlyDistributionChart
            data={dashData?.hourlyAgg ?? []}
            speciesFilter={speciesFilter}
          />
        )}
      </Card>

      {/* Recent events */}
      <Card>
        <CardHeader
          title="Senaste händelser"
          subtitle={`${recentData?.totalCount ?? 0} bilder med djur i nuvarande val`}
          right={
            <button style={{
              display: 'inline-flex', alignItems: 'center', gap: 6,
              padding: '7px 14px', fontSize: 13, fontWeight: 500,
              border: '1px solid var(--border)', background: 'var(--surface)',
              color: 'var(--text-2)', borderRadius: 999,
            }}>
              Visa alla →
            </button>
          }
        />
        {recentData?.images && recentData.images.length > 0 ? (
          <ImageGallery images={recentData.images} minThumbnailWidth={200} />
        ) : (
          <div style={{ color: 'var(--text-3)', fontSize: 13, padding: '24px 0', textAlign: 'center' }}>
            Inga bilder med djur i nuvarande val
          </div>
        )}
      </Card>
    </main>
  );
}
