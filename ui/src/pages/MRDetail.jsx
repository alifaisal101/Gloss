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
  const [commitsExpanded, setCommitsExpanded] = useState(true);

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

  async function handleAddComment(filePath, lineNumber, body, reasoning) {
    await api.addComment(id, filePath, lineNumber, body, reasoning);
    setMr(await api.getMr(id));
  }

  async function handleEditComment(commentId, body, editReason) {
    const comment = mr.comments.find(c => c.id === commentId);
    if (!comment) return;
    await api.editComment(id, commentId, comment.filePath, comment.lineNumber, body, comment.reasoning);
    setMr(await api.getMr(id));
  }

  async function handleDeleteComment(commentId, deleteReason) {
    await api.deleteComment(id, commentId);
    setMr(await api.getMr(id));
  }

  if (loading) return <div className="loading">Loading…</div>;
  if (error && !mr) return <div className="error">Failed to load: {error.message}</div>;

  const comments = mr.comments ?? [];
  // GitLab returns commits newest-first; reverse so index 0 = oldest
  const commits = [...(mr.commits ?? [])].reverse();
  const canReview = mr.state === 'Pending' || mr.state === 'Ready';
  const isReviewing = mr.state === 'Reviewing';
  const canPublish = mr.state === 'Ready';

  const activeDiff = selectedCommit ? selectedCommit.diff : mr.diff;
  const activeComments = selectedCommit ? [] : comments;

  return (
    <div className="mr-detail">
      <div className="mr-detail-header">
        <div className="mr-detail-top">
          <Link to="/" className="back-link">← Merge Requests</Link>
          <div className="mr-actions">
            {isReviewing && (
              <span className="reviewing-indicator">Review in progress…</span>
            )}
            {canReview && (
              <button className="btn btn-secondary" onClick={handleReview} disabled={reviewing || publishing}>
                {reviewing ? 'Reviewing…' : mr.state === 'Ready' ? 'Re-review' : 'Trigger Review'}
              </button>
            )}
            {canPublish && (
              <button className="btn btn-publish" onClick={handlePublish} disabled={publishing || reviewing}
                title={!mr.hasShas ? 'Diff refs unavailable — comments will be posted as general notes' : undefined}>
                {publishing ? 'Publishing…' : 'Publish to GitLab'}
              </button>
            )}
            {mr.state === 'Published' && (
              <span className="published-indicator">Published to GitLab</span>
            )}
          </div>
        </div>

        <div className="mr-detail-title-area">
          <h1 className="mr-detail-title">{mr.title}</h1>
          <span className={`state-badge state-${mr.state.toLowerCase()}`}>{mr.state}</span>
        </div>

        <div className="mr-detail-meta">
          <span className="muted">{mr.projectPath}</span>
          <span className="meta-sep">·</span>
          <span className="mono">{mr.sourceBranch} → {mr.targetBranch}</span>
          {!selectedCommit && comments.length > 0 && (
            <>
              <span className="meta-sep">·</span>
              <span className="comment-count">{comments.length} comment{comments.length !== 1 ? 's' : ''}</span>
            </>
          )}
        </div>
      </div>

      {error && <div className="error mr-error">{error.message}</div>}

      {commits.length > 0 && (
        <div className="commits-panel">
          <button className="commits-panel-head" onClick={() => setCommitsExpanded(e => !e)}>
            <span className="commits-panel-title">Commits</span>
            <span className="commits-panel-count muted">{commits.length}</span>
            {commitsExpanded && <span className="commits-panel-hint muted">oldest → newest</span>}
            <span className="commits-panel-chevron">{commitsExpanded ? '▲' : '▼'}</span>
          </button>
          {commitsExpanded && (
            <div className="commits-list">
              <button
                className={`commits-all-btn${!selectedCommit ? ' active' : ''}`}
                onClick={() => setSelectedCommit(null)}
              >
                <span className="commits-all-label">All changes</span>
                {comments.length > 0 && (
                  <span className={`commits-all-badge${!selectedCommit ? ' active' : ''}`}>
                    {comments.length} comment{comments.length !== 1 ? 's' : ''}
                  </span>
                )}
              </button>
              <div className="commits-divider" />
              {commits.map((c, i) => (
                <button
                  key={c.sha}
                  className={`commit-row${selectedCommit?.sha === c.sha ? ' active' : ''}`}
                  onClick={() => setSelectedCommit(c)}
                >
                  <span className="commit-row-num">{i + 1}</span>
                  <span className="commit-row-sha mono">{c.sha.slice(0, 7)}</span>
                  <span className="commit-row-msg">{c.title}</span>
                  <span className="commit-row-author muted">{c.authorName}</span>
                  {i === commits.length - 1 && (
                    <span className="commit-row-latest">latest</span>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {selectedCommit && (
        <div className="commit-context-bar">
          <span className="commit-context-num">
            Commit {commits.findIndex(c => c.sha === selectedCommit.sha) + 1} of {commits.length}
          </span>
          <span className="meta-sep">·</span>
          <span className="mono commit-context-sha">{selectedCommit.sha.slice(0, 7)}</span>
          <span className="commit-context-msg">{selectedCommit.title}</span>
          <span className="muted">by {selectedCommit.authorName}</span>
        </div>
      )}

      {!selectedCommit && comments.length === 0 && mr.state !== 'Pending' && (
        <div className="empty" style={{ marginBottom: 16 }}>No comments generated.</div>
      )}

      <DiffView
        diff={activeDiff}
        comments={activeComments}
        onEditComment={handleEditComment}
        onDeleteComment={handleDeleteComment}
        onAddComment={handleAddComment}
      />
    </div>
  );
}
