import { useState, useEffect } from 'react';
import { api } from '../api/client.js';

export default function Repositories() {
  const [repos, setRepos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [cronEdits, setCronEdits] = useState({});
  const [saving, setSaving] = useState({});
  const [deleting, setDeleting] = useState({});

  useEffect(() => {
    api.listRepositories()
      .then(setRepos)
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);

  function editCron(id, value) {
    setCronEdits(e => ({ ...e, [id]: value }));
  }

  async function saveCron(repo) {
    setSaving(s => ({ ...s, [repo.id]: true }));
    try {
      const updated = await api.updateRepository(repo.id, { pollCron: cronEdits[repo.id] });
      setRepos(rs => rs.map(r => r.id === repo.id ? updated : r));
      setCronEdits(e => { const n = { ...e }; delete n[repo.id]; return n; });
    } catch (err) {
      setError(err);
    } finally {
      setSaving(s => { const n = { ...s }; delete n[repo.id]; return n; });
    }
  }

  async function deleteRepo(repo) {
    setDeleting(d => ({ ...d, [repo.id]: true }));
    try {
      await api.deleteRepository(repo.id);
      setRepos(rs => rs.filter(r => r.id !== repo.id));
    } catch (err) {
      setError(err);
      setDeleting(d => { const n = { ...d }; delete n[repo.id]; return n; });
    }
  }

  async function toggleAutoReview(repo) {
    setSaving(s => ({ ...s, [repo.id]: true }));
    try {
      const updated = await api.updateRepository(repo.id, { autoReviewEnabled: !repo.autoReviewEnabled });
      setRepos(rs => rs.map(r => r.id === repo.id ? updated : r));
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
              <th>Auto-review</th>
              <th>Poll schedule (cron)</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {repos.map(repo => {
              const isCronDirty = cronEdits[repo.id] !== undefined;
              return (
                <tr key={repo.id}>
                  <td className="mono">{repo.projectPath}</td>
                  <td>
                    <label className="toggle" title="Automatically review new MRs when they are pulled">
                      <input
                        type="checkbox"
                        checked={repo.autoReviewEnabled}
                        onChange={() => toggleAutoReview(repo)}
                        disabled={saving[repo.id]}
                      />
                      {repo.autoReviewEnabled ? 'On' : 'Off'}
                    </label>
                  </td>
                  <td>
                    <input
                      className="cron-input"
                      value={isCronDirty ? cronEdits[repo.id] : (repo.pollCron ?? '')}
                      onChange={e => editCron(repo.id, e.target.value)}
                      placeholder="Not used yet"
                    />
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                      {isCronDirty && (
                        <button className="btn btn-sm" onClick={() => saveCron(repo)} disabled={saving[repo.id] || deleting[repo.id]}>
                          {saving[repo.id] ? 'Saving…' : 'Save'}
                        </button>
                      )}
                      <button className="btn-ghost btn-danger btn-sm" onClick={() => deleteRepo(repo)} disabled={saving[repo.id] || deleting[repo.id]}>
                        {deleting[repo.id] ? '…' : 'Delete'}
                      </button>
                    </div>
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
