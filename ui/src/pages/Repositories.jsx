import { useState, useEffect } from 'react';
import { api } from '../api/client.js';

export default function Repositories() {
  const [repos, setRepos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [edits, setEdits] = useState({});
  const [saving, setSaving] = useState({});

  useEffect(() => {
    api.listRepositories()
      .then(setRepos)
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);

  function edit(id, value) {
    setEdits(e => ({ ...e, [id]: value }));
  }

  async function save(repo) {
    setSaving(s => ({ ...s, [repo.id]: true }));
    try {
      const updated = await api.updateRepository(repo.id, edits[repo.id]);
      setRepos(rs => rs.map(r => r.id === repo.id ? updated : r));
      setEdits(e => { const n = { ...e }; delete n[repo.id]; return n; });
    } catch (err) {
      setError(err);
    } finally {
      setSaving(s => { const n = { ...s }; delete n[repo.id]; return n; });
    }
  }

  if (loading) return <div className="loading">Loading…</div>;
  if (error) return <div className="error">Failed to load: {error.message}</div>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Repositories</h1>
      </div>
      <p className="page-desc">
        Gloss tracks repositories automatically as MRs are discovered.
        The <em>Poll schedule</em> column is reserved for future per-repository scheduling — it has no effect right now.
        All repositories are polled on the global schedule configured in Settings.
      </p>

      {repos.length === 0 ? (
        <div className="empty">
          No repositories tracked yet. They appear here as Gloss discovers MRs.
        </div>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Project</th>
              <th>Clone path</th>
              <th>Last fetch</th>
              <th>Poll schedule (cron)</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {repos.map(repo => {
              const isDirty = edits[repo.id] !== undefined;
              return (
                <tr key={repo.id}>
                  <td className="mono">{repo.projectPath}</td>
                  <td className="mono muted">{repo.clonePath ?? '—'}</td>
                  <td className="muted">{repo.lastFetchAt ? new Date(repo.lastFetchAt).toLocaleString() : '—'}</td>
                  <td>
                    <input
                      className="cron-input"
                      value={isDirty ? edits[repo.id] : (repo.pollCron ?? '')}
                      onChange={e => edit(repo.id, e.target.value)}
                      placeholder="Default from env"
                    />
                  </td>
                  <td>
                    {isDirty && (
                      <button className="btn" onClick={() => save(repo)} disabled={saving[repo.id]}>
                        {saving[repo.id] ? 'Saving…' : 'Save'}
                      </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
