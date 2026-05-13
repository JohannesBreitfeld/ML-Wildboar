import {
  DashboardDataResponse,
  GetDetectionsParams,
  GetImagesParams,
  ImageGalleryResponse,
} from '../types/api.types';

class ApiClient {
  private baseUrl: string;

  constructor() {
    this.baseUrl = process.env.REACT_APP_API_URL || '/api';
  }

  private async request<T>(endpoint: string): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const response = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ error: `HTTP ${response.status}` }));
      throw new Error(errorData.error || 'Request failed');
    }

    return response.json();
  }

  private qs(params: Record<string, unknown>): string {
    const p = new URLSearchParams();
    for (const [k, v] of Object.entries(params)) {
      if (v !== undefined && v !== null && v !== '') p.append(k, String(v));
    }
    const s = p.toString();
    return s ? `?${s}` : '';
  }

  async getDetections(params: GetDetectionsParams): Promise<DashboardDataResponse> {
    return this.request<DashboardDataResponse>(`/detections${this.qs({ ...params })}`);
  }

  async getImages(params: GetImagesParams): Promise<ImageGalleryResponse> {
    return this.request<ImageGalleryResponse>(`/images${this.qs({ ...params })}`);
  }
}

export const apiClient = new ApiClient();
