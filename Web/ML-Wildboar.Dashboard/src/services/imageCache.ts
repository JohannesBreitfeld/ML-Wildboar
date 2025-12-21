import { apiClient } from './api';

interface CachedImage {
  url: string;
  expiresAt: Date;
}

class ImageCacheService {
  private cache = new Map<string, CachedImage>();

  async getImageUrl(blobUrl: string, expiryMinutes: number = 60): Promise<string> {
    const cached = this.cache.get(blobUrl);

    // Check if cached and not expired (with 5 min buffer)
    if (cached && cached.expiresAt > new Date(Date.now() + 5 * 60 * 1000)) {
      return cached.url;
    }

    // Fetch new SAS token
    try {
      const response = await apiClient.getSasToken({ blobUrl, expiryMinutes });

      this.cache.set(blobUrl, {
        url: response.imageUrl,
        expiresAt: new Date(response.expiresAt),
      });

      return response.imageUrl;
    } catch (error) {
      console.error('Failed to get SAS token for image:', error);

      // Return cached URL if available (even if expired) as fallback
      if (cached) {
        return cached.url;
      }

      // Return original URL as last resort
      return blobUrl;
    }
  }

  clearCache(): void {
    this.cache.clear();
  }

  removeExpired(): void {
    const now = new Date();
    Array.from(this.cache.entries()).forEach(([key, value]) => {
      if (value.expiresAt <= now) {
        this.cache.delete(key);
      }
    });
  }
}

// Export singleton instance
export const imageCacheService = new ImageCacheService();

// Periodically clean up expired entries (every 10 minutes)
setInterval(() => {
  imageCacheService.removeExpired();
}, 10 * 60 * 1000);
