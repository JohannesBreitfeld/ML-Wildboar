// Image caching — SAS tokens are now returned directly by the API with each ImageDto.
// This service is kept for potential future use but is no longer called by the dashboard.

class ImageCacheService {
  private cache = new Map<string, { url: string; expiresAt: Date }>();

  get(blobUrl: string): string | null {
    const cached = this.cache.get(blobUrl);
    if (cached && cached.expiresAt > new Date(Date.now() + 5 * 60 * 1000)) {
      return cached.url;
    }
    return null;
  }

  set(blobUrl: string, url: string, expiresAt: Date) {
    this.cache.set(blobUrl, { url, expiresAt });
  }
}

export const imageCache = new ImageCacheService();
