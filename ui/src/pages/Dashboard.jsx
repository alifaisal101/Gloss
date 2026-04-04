import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client.js';

const STATE_ORDER = ['Pending', 'Ready', 'Seen', 'Staged', 'Published'];
const STATE_LABEL = {
  Pending: 'Awaiting review',
  Ready: 'Reviewed — not yet opened',
  Seen: 'Opened, no changes made',
  Staged: 'Edited or commented',
  Published: 'Posted to Git platform',
};

export default function Dashboard() {
  const [mrs, setMrs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [polling, setPolling] = useState(false);
  const [error, setError] = useState(null);

  const load = useCallback(() => {
    return api.listMrs()
      .then(setMrs)
      .catch(setError);
  }, []);

  useEffect(() => {
    load().finally(() => setLoading(false));
  }, [load]);

  async function handlePollNow() {
    setPolling(true);
    setError(null);
    try {
      await api.pollAll();
      await load();
    } catch (err) {
      setError(err);
    } finally {
      setPolling(false);
    }
  }

  if (loading) return <div className="loading">Loading…</div>;

  const grouped = STATE_ORDER.reduce((acc, state) => {
    acc[state] = mrs.filter(mr => mr.state === state);
    return acc;
  }, {});

  const total = mrs.length;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Merge Requests</h1>
        <div className="page-actions">
          {total > 0 && <span className="muted">{total} total</span>}
          <button className="btn" onClick={handlePollNow} disabled={polling}>
            {polling ? 'Polling…' : 'Poll now'}
          </button>
        </div>
      </div>

      {error && <div className="error" style={{ marginBottom: 16 }}>{error.message}</div>}

      {STATE_ORDER.map(state =>
        grouped[state].length > 0 ? (
          <section key={state} className="mr-group">
            <div className="mr-group-header">
              <span className={`state-badge state-${state.toLowerCase()}`}>{state}</span>
              <span className="muted">{STATE_LABEL[state]}</span>
              <span className="mr-count">{grouped[state].length}</span>
            </div>
            <div className="mr-list">
              {grouped[state].map(mr => (
                <Link key={mr.id} to={`/mr/${mr.id}`} className="mr-card">
                  <div className="mr-card-title">{mr.title}</div>
                  <div className="mr-card-meta">
                    <span>{mr.projectPath}</span>
                    <span className="branch">{mr.sourceBranch} → {mr.targetBranch}</span>
                  </div>
                </Link>
              ))}
            </div>
          </section>
        ) : null
      )}

      {total === 0 && (
        <div className="empty">
          No merge requests yet.{' '}
          <button className="btn-ghost" onClick={handlePollNow} disabled={polling}>
            {polling ? 'Polling…' : 'Poll now'}
          </button>{' '}
          to fetch from configured projects.
        </div>
      )}
    </div>
  );
}
