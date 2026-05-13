// Legacy chart helpers — no longer used by the main Dashboard.
// Kept to avoid deleting files that may be referenced in tests or future pages.

export interface DailyChartData {
  date: string;
  dateLabel: string;
  wildboar: number;
  noWildboar: number;
}

export interface HourlyChartData {
  hour: number;
  hourLabel: string;
  count: number;
}
