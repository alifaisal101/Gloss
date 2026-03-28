import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { api } from '../api/client.js';
import DiffView from '../components/DiffView.jsx';

export default function MRDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [mr, setMr] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [publishing, setPublishing] = useState(false);

  useEffect(() => {
    api.getMr(id)
      .then(setMr)
      .catch(setError)
      .finally(() => setLoading(false));
  }, [id]);

  async function handleEditComment(commentId, body, reason) {
    const updated = await api.editComment(id, commentId, body, reason);
    setMr(prev => ({
      ...prev,
      comments: prev.comments.map(c => c.id === commentId ? updated : c),
    }));
  }

  async function handleDeleteComment(commentId, reason) {
    await api.deleteComment(id, commentId, reason);
    setMr(prev => ({
      ...prev,
      comments: prev.comments.map(c =>
        c.id === commentId ? { ...c, state: 'Deleted' } : c,
      ),
    }));
  }

  async function handleAddComment(filePath, lineNumber, body, reason) {
    const newComment = await api.addComment(id, filePath, lineNumber, body, reason);
    setMr(prev => ({ ...prev, comments: [...prev.comments, newComment] }));
  }

  async function handlePublish() {
    setPublishing(true);
    try {
      await api.publishMr(id);
      navigate('/');
    } catch (err) {
      setError(err);
      setPublishing(false);
    }
  }

  if (loading) return <div className="loading">Loading…</div>;
  if (error) return <div className="error">Failed to load: {error.message}</div>;

  const activeComments = mr.comments.filter(c => c.state !== 'Deleted');
  const canPublish = ['Ready', 'Seen', 'Staged'].includes(mr.state);

  return (
    <div className="mr-detail">
      <div className="mr-detail-header">
        <div className="mr-detail-breadcrumb">
          <Link to="/" className="back-link">← Dashboard</Link>
        </div>
        <div className="mr-detail-title-row">
          <div>
            <h1>{mr.title}</h1>
            <div className="mr-detail-meta">
              <span className={`state-badge state-${mr.state.toLowerCase()}`}>{mr.state}</span>
              <span className="muted">{mr.projectPath}</span>
              <span className="branch">{mr.sourceBranch} → {mr.targetBranch}</span>
              {activeComments.length > 0 && (
                <span className="comment-count">{activeComments.length} comment{activeComments.length !== 1 ? 's' : ''}</span>
              )}
            </div>
          </div>
          {canPublish && (
            <button className="btn btn-publish" onClick={handlePublish} disabled={publishing}>
              {publishing ? 'Publishing…' : `Publish${activeComments.length > 0 ? ` (${activeComments.length})` : ''}`}
            </button>
          )}
        </div>
      </div>

      <DiffView
        diff={mr.diff}
        comments={mr.comments}
        onEditComment={handleEditComment}
        onDeleteComment={handleDeleteComment}
        onAddComment={handleAddComment}
      />
    </div>
  );
}
