import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client.js';

const STATE_ORDER = ['Pending', 'Ready', 'Seen', 'Staged', 'Published'];
const STATE_LABEL = {
  Pending: 'Review in progress or queued',
  Ready: 'Reviewed — not yet opened',
  Seen: 'Opened, no changes made',
  Staged: 'Edited or commented',
  Published: 'Posted to Git platform',
};

export default function Dashboard() {
  const [mrs, setMrs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.listMrs()
      .then(setMrs)
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading…</div>;
  if (error) return <div className="error">Failed to load: {error.message}</div>;

  const grouped = STATE_ORDER.reduce((acc, state) => {
    acc[state] = mrs.filter(mr => mr.state === state);
    return acc;
  }, {});

  const total = mrs.length;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Merge Requests</h1>
        {total > 0 && <span className="muted">{total} total</span>}
      </div>

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
                    <span className="muted">{formatDate(mr.updatedAt)}</span>
                  </div>
                </Link>
              ))}
            </div>
          </section>
        ) : null
      )}

      {total === 0 && (
        <div className="empty">
          No merge requests yet. Gloss will start polling configured projects shortly.
        </div>
      )}
    </div>
  );
}

function formatDate(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  const now = new Date();
  const diff = now - d;
  if (diff < 60_000) return 'just now';
  if (diff < 3_600_000) return `${Math.floor(diff / 60_000)}m ago`;
  if (diff < 86_400_000) return `${Math.floor(diff / 3_600_000)}h ago`;
  return d.toLocaleDateString();
}
