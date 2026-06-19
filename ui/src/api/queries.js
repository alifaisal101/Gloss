import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from './client.js';

export const keys = {
  mrs: ['mrs'],
  mr: (id) => ['mr', id],
  config: ['config'],
  repositories: ['repositories'],
  constitution: ['constitution'],
  ignoredMrs: ['ignored-mrs'],
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

export function useIgnoreMr() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => api.ignoreMr(id),
    onSuccess: () => {
      toast.success('Merge request ignored', { description: 'It won’t be pulled again.' });
      qc.invalidateQueries({ queryKey: keys.mrs });
      qc.invalidateQueries({ queryKey: keys.ignoredMrs });
    },
    onError: (err) => toast.error('Could not ignore merge request', { description: err.message }),
  });
}

export function useIgnoredMrs(options = {}) {
  return useQuery({ queryKey: keys.ignoredMrs, queryFn: api.listIgnoredMrs, ...options });
}

export function useUnignoreMr() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => api.unignoreMr(id),
    onSuccess: () => {
      toast.success('Merge request restored', { description: 'It will be pulled again on the next poll.' });
      qc.invalidateQueries({ queryKey: keys.ignoredMrs });
      qc.invalidateQueries({ queryKey: keys.mrs });
    },
    onError: (err) => toast.error('Could not restore merge request', { description: err.message }),
  });
}

export function useRepositories(options = {}) {
  return useQuery({ queryKey: keys.repositories, queryFn: api.listRepositories, ...options });
}

export function useUpdateRepository() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, fields }) => api.updateRepository(id, fields),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.repositories }),
    onError: (err) => toast.error('Could not update repository', { description: err.message }),
  });
}

export function useDeleteRepository() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => api.deleteRepository(id),
    onSuccess: () => {
      toast.success('Repository removed');
      qc.invalidateQueries({ queryKey: keys.repositories });
    },
    onError: (err) => toast.error('Could not remove repository', { description: err.message }),
  });
}

export function useConstitution(options = {}) {
  return useQuery({
    queryKey: keys.constitution,
    queryFn: api.listConstitution,
    select: (docs) => [...docs].sort((a, b) => a.position - b.position),
    ...options,
  });
}

export function useAddDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ title, body, position }) => api.addDocument(title, body, position),
    onSuccess: () => { toast.success('Document added'); qc.invalidateQueries({ queryKey: keys.constitution }); },
    onError: (err) => toast.error('Could not add document', { description: err.message }),
  });
}

export function useUpdateDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, title, body, position }) => api.updateDocument(id, title, body, position),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.constitution }),
    onError: (err) => toast.error('Could not save document', { description: err.message }),
  });
}

export function useDeleteDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => api.deleteDocument(id),
    onSuccess: () => { toast.success('Document deleted'); qc.invalidateQueries({ queryKey: keys.constitution }); },
    onError: (err) => toast.error('Could not delete document', { description: err.message }),
  });
}

export function useSeedProjection() {
  return useMutation({
    mutationFn: api.seedProjection,
    onSuccess: () => toast.success('Projection seeded', { description: 'The reviewer now has a baseline.' }),
    onError: (err) => toast.error('Could not seed projection', { description: err.message }),
  });
}

export function useSaveConfig() {
  return useMutation({ mutationFn: (config) => api.updateConfig(config) });
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
