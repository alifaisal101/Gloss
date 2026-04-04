import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api } from '../api/client.js';
import DiffView from '../components/DiffView.jsx';

export default function MRDetail() {
  const { id } = useParams();
  const [mr, setMr] = useState(null);
  const [loading, setLoading] = useState(true);
  const [reviewing, setReviewing] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.getMr(id)
      .then(setMr)
      .catch(setError)
      .finally(() => setLoading(false));
  }, [id]);

  async function handleReview() {
    setReviewing(true);
    setError(null);
    try {
      await api.reviewMr(id);
      const updated = await api.getMr(id);
      setMr(updated);
    } catch (err) {
      setError(err);
    } finally {
      setReviewing(false);
    }
  }

  if (loading) return <div className="loading">Loading…</div>;
  if (error && !mr) return <div className="error">Failed to load: {error.message}</div>;

  const comments = mr.comments ?? [];
  const canReview = mr.state === 'Pending';

  return (
    <div className="mr-detail">
      <div className="mr-detail-header">
        <div className="mr-detail-breadcrumb">
          <Link to="/" className="back-link">← Merge Requests</Link>
        </div>
        <div className="mr-detail-title-row">
          <div>
            <h1>{mr.title}</h1>
            <div className="mr-detail-meta">
              <span className={`state-badge state-${mr.state.toLowerCase()}`}>{mr.state}</span>
              <span className="muted">{mr.projectPath}</span>
              <span className="branch">{mr.sourceBranch} → {mr.targetBranch}</span>
              {comments.length > 0 && (
                <span className="comment-count">{comments.length} comment{comments.length !== 1 ? 's' : ''}</span>
              )}
            </div>
          </div>
          {canReview && (
            <button className="btn btn-publish" onClick={handleReview} disabled={reviewing}>
              {reviewing ? 'Reviewing…' : 'Trigger Review'}
            </button>
          )}
        </div>
      </div>

      {error && <div className="error" style={{ marginBottom: 16 }}>{error.message}</div>}

      <DiffView
        diff={mr.diff}
        comments={comments}
        onEditComment={() => {}}
        onDeleteComment={() => {}}
        onAddComment={() => {}}
      />
    </div>
  );
}
