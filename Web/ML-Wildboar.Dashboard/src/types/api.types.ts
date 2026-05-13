// API Response Types

export interface AnimalDetection {
  reasoning: string;  // Claude's Swedish justification for the species identification
  species: string;    // "vildsvin", "rådjur", "räv", "älg", "hare", "grävling", "kronvilt", "dovhjort"
  count: number;
  confidence: 'hög' | 'medel' | 'låg';
}

export interface ImageDto {
  id: string;
  partitionKey: string;    // "yyyy-MM-dd"
  capturedAt: string;      // ISO
  isEmpty: boolean;
  weather: string | null;
  description: string | null;
  detections: AnimalDetection[];
  blobUrl: string;
}

export interface ImageGalleryResponse {
  images: ImageDto[];
  continuationToken?: string;
  totalCount: number;
}

export interface DailyAgg {
  date: string;            // "yyyy-MM-dd"
  total: number;
  empty: number;
  withAnimals: number;
  bySpecies: Record<string, number>;
}

export interface HourlyAgg {
  hour: number;            // 0..23
  total: number;
  bySpecies: Record<string, number>;
}

export interface DashboardDataResponse {
  dailyAgg: DailyAgg[];
  hourlyAgg: HourlyAgg[];
  totalImages: number;
  withAnimals: number;
  empty: number;
}

// Request Parameters

export type PresetId = '7d' | '14d' | '30d';

export interface DateRange {
  from: string;   // "yyyy-MM-dd"
  to: string;     // "yyyy-MM-dd"
  preset?: PresetId;
}

export interface GetDetectionsParams {
  from: string;
  to: string;
  species?: string;   // comma-separated
}

export interface GetImagesParams {
  date?: string;
  from?: string;
  to?: string;
  species?: string;
  withAnimals?: boolean;
  pageSize?: number;
  continuationToken?: string;
}
