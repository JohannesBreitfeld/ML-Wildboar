import { DetectionDataPoint } from '../types/api.types';

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

export function processDetectionDataByDay(
  detections: DetectionDataPoint[],
  totalImagesByDay: Map<string, number>
): DailyChartData[] {
  const dailyMap = new Map<string, number>();

  // Aggregate detections by day
  detections.forEach((d) => {
    const date = new Date(d.timestamp);
    const dateKey = date.toISOString().split('T')[0];
    dailyMap.set(dateKey, (dailyMap.get(dateKey) || 0) + d.count);
  });

  // Convert to chart data
  const chartData: DailyChartData[] = [];
  dailyMap.forEach((wildboarCount, dateKey) => {
    const totalForDay = totalImagesByDay.get(dateKey) || wildboarCount;
    const date = new Date(dateKey);
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    chartData.push({
      date: dateKey,
      dateLabel: `${month}-${day}`,
      wildboar: wildboarCount,
      noWildboar: Math.max(0, totalForDay - wildboarCount),
    });
  });

  return chartData.sort((a, b) => a.date.localeCompare(b.date));
}

export function processDetectionDataByHour(
  detections: DetectionDataPoint[]
): HourlyChartData[] {
  const hourlyMap = new Map<number, number>();

  // Initialize all hours with 0
  for (let i = 0; i < 24; i++) {
    hourlyMap.set(i, 0);
  }

  // Aggregate detections by hour
  detections.forEach((d) => {
    const date = new Date(d.timestamp);
    const hour = date.getHours();
    hourlyMap.set(hour, (hourlyMap.get(hour) || 0) + d.count);
  });

  // Convert to chart data
  const chartData: HourlyChartData[] = [];
  hourlyMap.forEach((count, hour) => {
    chartData.push({
      hour,
      hourLabel: `${String(hour).padStart(2, '0')}:00`,
      count,
    });
  });

  return chartData;
}
