const BASE = '/api';

export class ApiError extends Error {
  constructor(status, code, message, fieldErrors, body) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.fieldErrors = fieldErrors ?? null;
    this.body = body ?? null;
  }
}

async function readError(res) {
  let payload = null;
  try {
    payload = await res.json();
  } catch {
    payload = null;
  }

  // BuildingBlocks envelope: { status, error: { code, message }, traceId }
  if (payload?.error?.message) {
    return new ApiError(res.status, payload.error.code ?? null, payload.error.message, null, payload);
  }
  // ASP.NET ProblemDetails: { title, detail, errors: { field: [msg] } }
  if (payload?.detail || payload?.title) {
    return new ApiError(res.status, payload.type ?? null, payload.detail || payload.title, payload.errors ?? null, payload);
  }
  // Bare status (e.g. Results.NotFound() with no body)
  const fallback = `${res.status} ${res.statusText}`.trim() || `Request failed (${res.status})`;
  return new ApiError(res.status, null, fallback, null, payload);
}

async function request(method, path, body) {
  let res;
  try {
    res = await fetch(`${BASE}${path}`, {
      method,
      headers: body != null ? { 'Content-Type': 'application/json' } : {},
      body: body != null ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'network', 'Could not reach the server. Check that the API is running and reachable.', null, null);
  }
  if (!res.ok) throw await readError(res);
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
