import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../services/api';
import { GetImagesParams } from '../types/api.types';

export function useImageGallery(params: GetImagesParams, enabled: boolean = true) {
  return useQuery({
    queryKey: ['images', params],
    queryFn: () => apiClient.getImages(params),
    staleTime: 3 * 60 * 1000, // 3 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    enabled, // Only fetch when enabled
    retry: 2,
  });
}
