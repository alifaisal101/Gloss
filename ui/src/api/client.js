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
  listMrs: () => request('GET', '/mrs'),
  getMr: (id) => request('GET', `/mrs/${id}`),
  publishMr: (id) => request('POST', `/mrs/${id}/publish`),

  // Comments
  editComment: (mrId, commentId, body, reason) =>
    request('PATCH', `/mrs/${mrId}/comments/${commentId}`, { body, reason }),
  deleteComment: (mrId, commentId, reason) =>
    request('DELETE', `/mrs/${mrId}/comments/${commentId}`, { reason }),
  addComment: (mrId, filePath, lineNumber, body, reason) =>
    request('POST', `/mrs/${mrId}/comments`, { filePath, lineNumber, body, reason }),

  // Repositories
  listRepositories: () => request('GET', '/repositories'),
  updateRepository: (id, pollCron) =>
    request('PATCH', `/repositories/${id}`, { pollCron }),

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
