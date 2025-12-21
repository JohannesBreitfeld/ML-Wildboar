// API Response Types

export interface DetectionDataResponse {
  detections: DetectionDataPoint[];
  totalImages: number;
  wildboarImages: number;
  dateRange: DateRange;
}

export interface DetectionDataPoint {
  timestamp: string;
  count: number;
  averageConfidence: number;
}

export interface DateRange {
  start: string;
  end: string;
}

export interface ImageGalleryResponse {
  images: ImageDto[];
  continuationToken?: string;
  totalCount: number;
}

export interface ImageDto {
  id: string;
  capturedAt: string;
  containsWildboar: boolean;
  confidenceScore: number;
  imageUrl: string;
}

export interface SasTokenResponse {
  imageUrl: string;
  expiresAt: string;
}

// API Request Parameters

export interface GetDetectionsParams {
  startDate?: string;
  endDate?: string;
  minConfidence?: number;
  groupBy?: 'hour' | 'day';
}

export interface GetImagesParams {
  date: string;
  startHour?: number;
  endHour?: number;
  containsWildboar?: boolean;
  minConfidence?: number;
  pageSize?: number;
  continuationToken?: string;
}

export interface GetSasTokenParams {
  blobUrl: string;
  expiryMinutes?: number;
}

// Error Types

export interface ApiError {
  error: string;
  statusCode?: number;
}
