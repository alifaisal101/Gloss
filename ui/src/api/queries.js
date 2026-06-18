import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from './client.js';

export const keys = {
  mrs: ['mrs'],
  mr: (id) => ['mr', id],
  config: ['config'],
  repositories: ['repositories'],
  constitution: ['constitution'],
};

export function useMrs(options = {}) {
  return useQuery({ queryKey: keys.mrs, queryFn: api.listMrs, ...options });
}

export function useMr(id, options = {}) {
  return useQuery({ queryKey: keys.mr(id), queryFn: () => api.getMr(id), enabled: !!id, ...options });
}

export function useConfig(options = {}) {
  return useQuery({ queryKey: keys.config, queryFn: api.getConfig, ...options });
}

export function usePollAll() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: api.pollAll,
    onSuccess: () => {
      toast.success('Polling triggered', { description: 'Fetching new merge requests…' });
      qc.invalidateQueries({ queryKey: keys.mrs });
      qc.invalidateQueries({ queryKey: keys.config });
    },
    onError: (err) => toast.error('Could not start polling', { description: err.message }),
  });
}

export function useDeleteMr() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => api.deleteMr(id),
    onSuccess: () => {
      toast.success('Merge request deleted');
      qc.invalidateQueries({ queryKey: keys.mrs });
    },
    onError: (err) => toast.error('Could not delete merge request', { description: err.message }),
  });
}
