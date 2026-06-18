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

export function useReviewMr(mrId) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.reviewMr(mrId),
    onSuccess: () => {
      toast.success('Review requested');
      qc.invalidateQueries({ queryKey: keys.mr(mrId) });
      qc.invalidateQueries({ queryKey: keys.mrs });
    },
    onError: (err) => {
      toast.error('Review failed', { description: err.message });
      qc.invalidateQueries({ queryKey: keys.mr(mrId) });
    },
  });
}

export function usePublishMr(mrId) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.publishMr(mrId),
    onSuccess: () => {
      toast.success('Published to Git platform');
      qc.invalidateQueries({ queryKey: keys.mr(mrId) });
      qc.invalidateQueries({ queryKey: keys.mrs });
    },
    onError: (err) => toast.error('Publish failed', { description: err.message }),
  });
}

function patchComments(qc, mrId, updater) {
  qc.setQueryData(keys.mr(mrId), (old) => (old ? { ...old, comments: updater(old.comments ?? []) } : old));
}

export function useAddComment(mrId) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ filePath, line, body, reasoning }) => api.addComment(mrId, filePath, line, body, reasoning),
    onMutate: async (vars) => {
      await qc.cancelQueries({ queryKey: keys.mr(mrId) });
      const prev = qc.getQueryData(keys.mr(mrId));
      patchComments(qc, mrId, (comments) => [
        ...comments,
        {
          id: `temp-${Date.now()}`,
          filePath: vars.filePath,
          lineNumber: vars.line,
          body: vars.body,
          reasoning: vars.reasoning ?? null,
          state: 'UserAdded',
        },
      ]);
      return { prev };
    },
    onError: (err, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData(keys.mr(mrId), ctx.prev);
      toast.error('Could not add comment', { description: err.message });
    },
    onSettled: () => qc.invalidateQueries({ queryKey: keys.mr(mrId) }),
  });
}

export function useEditComment(mrId) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ commentId, filePath, line, body, reasoning }) =>
      api.editComment(mrId, commentId, filePath, line, body, reasoning),
    onMutate: async ({ commentId, body }) => {
      await qc.cancelQueries({ queryKey: keys.mr(mrId) });
      const prev = qc.getQueryData(keys.mr(mrId));
      patchComments(qc, mrId, (comments) =>
        comments.map((c) => (c.id === commentId ? { ...c, body, state: 'Edited' } : c)));
      return { prev };
    },
    onError: (err, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData(keys.mr(mrId), ctx.prev);
      toast.error('Could not save edit', { description: err.message });
    },
    onSettled: () => qc.invalidateQueries({ queryKey: keys.mr(mrId) }),
  });
}

export function useDeleteComment(mrId) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ commentId }) => api.deleteComment(mrId, commentId),
    onMutate: async ({ commentId }) => {
      await qc.cancelQueries({ queryKey: keys.mr(mrId) });
      const prev = qc.getQueryData(keys.mr(mrId));
      patchComments(qc, mrId, (comments) =>
        comments.map((c) => (c.id === commentId ? { ...c, state: 'Deleted' } : c)));
      return { prev };
    },
    onError: (err, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData(keys.mr(mrId), ctx.prev);
      toast.error('Could not delete comment', { description: err.message });
    },
    onSettled: () => qc.invalidateQueries({ queryKey: keys.mr(mrId) }),
  });
}
