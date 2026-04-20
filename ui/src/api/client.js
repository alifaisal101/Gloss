const BASE = '/api';

async function request(method, path, body) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: body != null ? { 'Content-Type': 'application/json' } : {},
    body: body != null ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status} ${res.statusText}`);
  if (res.status === 204) return null;
  return res.json();
}

export const api = {
  // MRs
  listMrs: () => request('GET', '/merge-requests'),
  getMr: (id) => request('GET', `/merge-requests/${id}`),
  reviewMr: (id) => request('POST', `/merge-requests/${id}/review`),
  publishMr: (id) => request('POST', `/merge-requests/${id}/publish`),
  deleteMr: (id) => request('DELETE', `/merge-requests/${id}`),
  pollAll: () => request('POST', '/repositories/poll-all'),

  // Comments
  addComment: (mrId, filePath, line, body, reasoning) =>
    request('POST', `/merge-requests/${mrId}/comments`, { filePath, line, body, reasoning }),
  editComment: (mrId, commentId, filePath, line, body, reasoning) =>
    request('PUT', `/merge-requests/${mrId}/comments/${commentId}`, { filePath, line, body, reasoning }),
  deleteComment: (mrId, commentId) =>
    request('DELETE', `/merge-requests/${mrId}/comments/${commentId}`),

  // Repositories
  listRepositories: () => request('GET', '/repositories'),
  updateRepository: (id, fields) =>
    request('PATCH', `/repositories/${id}`, fields),
  deleteRepository: (id) => request('DELETE', `/repositories/${id}`),

  // Config
  getConfig: () => request('GET', '/config'),
  updateConfig: (config) => request('PUT', '/config', config),

  // Constitution
  listConstitution: () => request('GET', '/constitution'),
  addDocument: (title, body, position) =>
    request('POST', '/constitution', { title, body, position }),
  updateDocument: (id, title, body, position) =>
    request('PUT', `/constitution/${id}`, { title, body, position }),
  deleteDocument: (id) => request('DELETE', `/constitution/${id}`),
  seedProjection: () => request('POST', '/constitution/seed-projection'),
};
