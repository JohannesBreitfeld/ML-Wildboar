import {
  DetectionDataResponse,
  GetDetectionsParams,
  GetImagesParams,
  GetSasTokenParams,
  ImageGalleryResponse,
  SasTokenResponse,
  ApiError,
} from '../types/api.types';

class ApiClient {
  private baseUrl: string;

  constructor() {
    // In development, use local API; in production, use relative path for Static Web Apps
    this.baseUrl = process.env.REACT_APP_API_URL || '/api';
  }

  private async request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;

    try {
      const response = await fetch(url, {
        ...options,
        headers: {
          'Content-Type': 'application/json',
          ...options?.headers,
        },
      });

      if (!response.ok) {
        const errorData: ApiError = await response.json().catch(() => ({
          error: `HTTP ${response.status}: ${response.statusText}`,
        }));

        throw new Error(errorData.error || 'Request failed');
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        console.error(`API Error [${endpoint}]:`, error.message);
        throw error;
      }
      throw new Error('Unknown error occurred');
    }
  }

  private buildQueryString(params: Record<string, any>): string {
    const queryParams = new URLSearchParams();

    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        queryParams.append(key, String(value));
      }
    });

    const queryString = queryParams.toString();
    return queryString ? `?${queryString}` : '';
  }

  async getDetections(params: GetDetectionsParams = {}): Promise<DetectionDataResponse> {
    const queryString = this.buildQueryString(params);
    return this.request<DetectionDataResponse>(`/detections${queryString}`);
  }

  async getImages(params: GetImagesParams): Promise<ImageGalleryResponse> {
    const queryString = this.buildQueryString(params);
    return this.request<ImageGalleryResponse>(`/images${queryString}`);
  }

  async getSasToken(params: GetSasTokenParams): Promise<SasTokenResponse> {
    const queryString = this.buildQueryString(params);
    return this.request<SasTokenResponse>(`/images/sas${queryString}`);
  }
}

// Export singleton instance
export const apiClient = new ApiClient();
