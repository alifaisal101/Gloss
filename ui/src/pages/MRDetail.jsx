import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api } from '../api/client.js';
import DiffView from '../components/DiffView.jsx';

export default function MRDetail() {
  const { id } = useParams();
  const [mr, setMr] = useState(null);
  const [loading, setLoading] = useState(true);
  const [reviewing, setReviewing] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [error, setError] = useState(null);
  const [selectedCommit, setSelectedCommit] = useState(null);

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
      setSelectedCommit(null);
    } catch (err) {
      setError(err);
    } finally {
      setReviewing(false);
    }
  }

  async function handlePublish() {
    setPublishing(true);
    setError(null);
    try {
      await api.publishMr(id);
      setMr(await api.getMr(id));
    } catch (err) {
      setError(err);
    } finally {
      setPublishing(false);
    }
  }

  if (loading) return <div className="loading">Loading…</div>;
  if (error && !mr) return <div className="error">Failed to load: {error.message}</div>;

  const comments = mr.comments ?? [];
  const commits = mr.commits ?? [];
  const canReview = mr.state === 'Pending' || mr.state === 'Ready';
  const canPublish = mr.state === 'Ready';

  const activeDiff = selectedCommit ? selectedCommit.diff : mr.diff;
  const activeComments = selectedCommit ? [] : comments;

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
              <span className="branch mono">{mr.sourceBranch} → {mr.targetBranch}</span>
              {!selectedCommit && comments.length > 0 && (
                <span className="comment-count">{comments.length} comment{comments.length !== 1 ? 's' : ''}</span>
              )}
            </div>
          </div>
          <div className="mr-actions">
            {canReview && (
              <button className="btn btn-secondary" onClick={handleReview} disabled={reviewing || publishing}>
                {reviewing ? 'Reviewing…' : mr.state === 'Ready' ? 'Re-review' : 'Trigger Review'}
              </button>
            )}
            {canPublish && (
              <button className="btn btn-publish" onClick={handlePublish} disabled={publishing || reviewing}>
                {publishing ? 'Publishing…' : 'Publish to GitLab'}
              </button>
            )}
            {mr.state === 'Published' && (
              <span className="published-indicator">Published to GitLab</span>
            )}
          </div>
        </div>
      </div>

      {error && <div className="error" style={{ marginBottom: 16 }}>{error.message}</div>}

      {commits.length > 0 && (
        <div className="commit-selector">
          <button
            className={`commit-btn${!selectedCommit ? ' active' : ''}`}
            onClick={() => setSelectedCommit(null)}
          >
            All changes
          </button>
          {commits.map(c => (
            <button
              key={c.sha}
              className={`commit-btn${selectedCommit?.sha === c.sha ? ' active' : ''}`}
              onClick={() => setSelectedCommit(c)}
              title={c.title}
            >
              <span className="commit-sha">{c.sha.slice(0, 7)}</span>
              <span className="commit-title-text">{c.title}</span>
            </button>
          ))}
        </div>
      )}

      {!selectedCommit && activeComments.length === 0 && mr.state !== 'Pending' && (
        <div className="empty" style={{ marginBottom: 16 }}>No comments generated.</div>
      )}

      {selectedCommit && (
        <div className="commit-info">
          <span className="commit-info-sha mono">{selectedCommit.sha.slice(0, 7)}</span>
          <span className="commit-info-title">{selectedCommit.title}</span>
          <span className="muted">by {selectedCommit.authorName}</span>
        </div>
      )}

      <DiffView
        diff={activeDiff}
        comments={activeComments}
        onEditComment={() => {}}
        onDeleteComment={() => {}}
        onAddComment={() => {}}
      />
    </div>
  );
}
