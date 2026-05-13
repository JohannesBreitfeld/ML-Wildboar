import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../services/api';
import { GetImagesParams } from '../types/api.types';

export function useImageGallery(params: GetImagesParams, enabled = true) {
  return useQuery({
    queryKey: ['images', params],
    queryFn: () => apiClient.getImages(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    enabled,
    retry: 2,
  });
}
