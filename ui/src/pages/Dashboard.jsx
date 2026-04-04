import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client.js';

const STATE_ORDER = ['Pending', 'Ready', 'Published'];
const STATE_LABEL = {
  Pending: 'Awaiting review',
  Ready: 'Reviewed — ready to publish',
  Published: 'Published to Git platform',
};

export default function Dashboard() {
  const [mrs, setMrs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [polling, setPolling] = useState(false);
  const [error, setError] = useState(null);

  const load = useCallback(() =>
    api.listMrs().then(setMrs).catch(setError)
  , []);

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

  const actionable = (grouped['Pending']?.length ?? 0) + (grouped['Ready']?.length ?? 0);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Merge Requests</h1>
          {actionable > 0 && (
            <div className="page-subtitle">{actionable} need attention</div>
          )}
        </div>
        <div className="page-actions">
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
                <MrCard key={mr.id} mr={mr} />
              ))}
            </div>
          </section>
        ) : null
      )}

      {mrs.length === 0 && (
        <div className="empty">
          No merge requests yet.{' '}
          <button className="btn-ghost btn-sm" onClick={handlePollNow} disabled={polling}>
            {polling ? 'Polling…' : 'Poll now'}
          </button>
        </div>
      )}
    </div>
  );
}

function MrCard({ mr }) {
  return (
    <Link to={`/mr/${mr.id}`} className="mr-card">
      <div className="mr-card-title">{mr.title}</div>
      <div className="mr-card-meta">
        <span className="muted">{mr.projectPath}</span>
        <span className="branch mono">{mr.sourceBranch} → {mr.targetBranch}</span>
        <span className="muted">{mr.authorUsername}</span>
      </div>
    </Link>
  );
}
