import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../services/api';
import { GetDetectionsParams } from '../types/api.types';

export function useDetectionData(params: GetDetectionsParams) {
  return useQuery({
    queryKey: ['detections', params],
    queryFn: () => apiClient.getDetections(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes (formerly cacheTime)
    retry: 2,
  });
}
