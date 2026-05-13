export interface Species {
  id: string;
  label: string;
  color: string;
}

// IDs must match the Swedish species strings returned by the backend exactly.
export const SPECIES: Species[] = [
  { id: 'vildsvin', label: 'Vildsvin',  color: 'var(--sp-vildsvin)' },
  { id: 'rådjur',   label: 'Rådjur',    color: 'var(--sp-radjur)'   },
  { id: 'räv',      label: 'Räv',       color: 'var(--sp-rav)'      },
  { id: 'älg',      label: 'Älg',       color: 'var(--sp-alg)'      },
  { id: 'hare',     label: 'Hare',      color: 'var(--sp-hare)'     },
  { id: 'grävling', label: 'Grävling',  color: 'var(--sp-gravling)' },
  { id: 'kronvilt', label: 'Kronvilt',  color: 'var(--sp-kronvilt)' },
  { id: 'dovhjort', label: 'Dovhjort',  color: 'var(--sp-dovhjort)' },
  { id: 'fågel',    label: 'Fågel',     color: 'var(--sp-fagel)'    },
  { id: 'lo',       label: 'Lo',        color: 'var(--sp-lo)'       },
  { id: 'varg',     label: 'Varg',      color: 'var(--sp-varg)'     },
  { id: 'okänt',    label: 'Okänt',     color: 'var(--sp-okant)'    },
];

export function speciesById(id: string): Species | undefined {
  return SPECIES.find(s => s.id === id);
}

export function formatDateSv(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString('sv-SE', { day: 'numeric', month: 'short' });
}

export function formatDateLongSv(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString('sv-SE', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
}

export function formatTimeSv(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
}

export function dateKey(d: Date): string {
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}
