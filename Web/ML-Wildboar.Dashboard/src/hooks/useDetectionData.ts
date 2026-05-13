import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../services/api';
import { GetDetectionsParams } from '../types/api.types';

export function useDetectionData(params: GetDetectionsParams) {
  return useQuery({
    queryKey: ['detections', params],
    queryFn: () => apiClient.getDetections(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    retry: 2,
  });
}
