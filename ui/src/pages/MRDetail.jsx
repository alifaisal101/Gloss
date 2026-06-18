import { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { Trash2, EyeOff, Sparkles, Send, AlertCircle, Check } from 'lucide-react';
import {
  useMr, useReviewMr, usePublishMr, useDeleteMr, useIgnoreMr,
  useAddComment, useEditComment, useDeleteComment,
} from '../api/queries.js';
import DiffView from '../components/DiffView.jsx';
import Button from '../components/ui/Button.jsx';
import StateBadge from '../components/ui/StateBadge.jsx';
import Skeleton from '../components/ui/Skeleton.jsx';
import ConfirmDialog from '../components/ui/ConfirmDialog.jsx';

const LIFECYCLE = ['Pending', 'Reviewing', 'Ready', 'Published'];

export default function MRDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [selectedSha, setSelectedSha] = useState(null);
  const [commitsExpanded, setCommitsExpanded] = useState(true);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [confirmIgnore, setConfirmIgnore] = useState(false);

  const mrQuery = useMr(id, {
    refetchInterval: (q) => (q.state.data?.state === 'Reviewing' ? 4000 : false),
  });
  const review = useReviewMr(id);
  const publish = usePublishMr(id);
  const deleteMr = useDeleteMr();
  const ignoreMr = useIgnoreMr();
  const addComment = useAddComment(id);
  const editComment = useEditComment(id);
  const deleteComment = useDeleteComment(id);

  useEffect(() => { setSelectedSha(null); }, [id]);

  const mr = mrQuery.data;

  function handleReview() {
    review.mutate(undefined, { onSuccess: () => setSelectedSha(null) });
  }

  function handleAddComment(filePath, lineNumber, body, reasoning) {
    addComment.mutate({ filePath, line: lineNumber, body, reasoning });
  }

  function handleEditComment(commentId, body) {
    const comment = mr?.comments?.find((c) => c.id === commentId);
    if (!comment) return Promise.resolve();
    return editComment.mutateAsync({
      commentId,
      filePath: comment.filePath,
      line: comment.lineNumber,
      body,
      reasoning: comment.reasoning,
    });
  }

  function handleDeleteComment(commentId) {
    return deleteComment.mutateAsync({ commentId });
  }

  if (mrQuery.isLoading) return <MrDetailSkeleton />;
  if (mrQuery.isError && !mr) {
    return (
      <div className="mr-detail">
        <Link to="/" className="back-link">← Merge Requests</Link>
        <div className="banner banner-error" style={{ marginTop: 16 }}>
          <AlertCircle size={16} aria-hidden="true" />
          <span>{mrQuery.error.message}</span>
          <Button variant="ghost" size="sm" onClick={() => mrQuery.refetch()}>Retry</Button>
        </div>
      </div>
    );
  }

  const comments = mr.comments ?? [];
  // GitLab returns commits newest-first; reverse so index 0 = oldest
  const commits = [...(mr.commits ?? [])].reverse();
  // Selection is keyed by sha and resolved against the *current* commits, so it survives an mr
  // refetch and falls back to "All changes" (full mr.diff) if the sha is gone — never a stale commit.
  const selectedCommit = selectedSha ? commits.find((c) => c.sha === selectedSha) ?? null : null;
  const canReview = mr.state === 'Pending' || mr.state === 'Ready';
  const isReviewing = mr.state === 'Reviewing' || review.isPending;
  const canPublish = mr.state === 'Ready';

  const activeDiff = selectedCommit ? selectedCommit.diff : mr.diff;
  const activeComments = selectedCommit ? [] : comments;

  return (
    <div className="mr-detail">
      <div className="mr-detail-header">
        <div className="mr-detail-top">
          <Link to="/" className="back-link">← Merge Requests</Link>
          <div className="mr-actions">
            {canReview && (
              <Button variant="secondary" icon={Sparkles} onClick={handleReview} loading={review.isPending}
                disabled={isReviewing || publish.isPending}>
                {mr.state === 'Ready' ? 'Re-review' : 'Trigger Review'}
              </Button>
            )}
            {canPublish && (
              <Button variant="publish" icon={Send} onClick={() => publish.mutate()} loading={publish.isPending}
                disabled={isReviewing}
                title={!mr.hasShas ? 'Diff refs unavailable — comments will be posted as general notes' : undefined}>
                Publish to GitLab
              </Button>
            )}
            <Button variant="ghost" size="sm" icon={EyeOff} onClick={() => setConfirmIgnore(true)}
              loading={ignoreMr.isPending}
              disabled={deleteMr.isPending || isReviewing || publish.isPending}
              title="Hide this MR and stop it from being pulled again">
              Ignore
            </Button>
            <Button variant="dangerGhost" size="sm" icon={Trash2} onClick={() => setConfirmDelete(true)}
              disabled={deleteMr.isPending || ignoreMr.isPending || isReviewing || publish.isPending}>
              Delete
            </Button>
          </div>
        </div>

        <div className="mr-detail-title-area">
          <h1 className="mr-detail-title">{mr.title}</h1>
          <StateBadge state={mr.state} />
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

        <LifecycleStepper state={mr.state} />
      </div>

      {commits.length > 0 && (
        <div className="commits-panel">
          <button className="commits-panel-head" onClick={() => setCommitsExpanded((e) => !e)}>
            <span className="commits-panel-title">Commits</span>
            <span className="commits-panel-count muted">{commits.length}</span>
            {commitsExpanded && <span className="commits-panel-hint muted">oldest → newest</span>}
            <span className="commits-panel-chevron">{commitsExpanded ? '▲' : '▼'}</span>
          </button>
          {commitsExpanded && (
            <div className="commits-list">
              <button
                className={`commits-all-btn${!selectedCommit ? ' active' : ''}`}
                onClick={() => setSelectedSha(null)}
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
                  onClick={() => setSelectedSha(c.sha)}
                >
                  <span className="commit-row-num">{i + 1}</span>
                  <span className="commit-row-sha mono">{c.sha.slice(0, 7)}</span>
                  <span className="commit-row-msg">{c.title}</span>
                  <span className="commit-row-author muted">{c.authorName}</span>
                  {i === commits.length - 1 && <span className="commit-row-latest">latest</span>}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {selectedCommit && (
        <div className="commit-context-bar">
          <span className="commit-context-num">
            Commit {commits.findIndex((c) => c.sha === selectedCommit.sha) + 1} of {commits.length}
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

      <ConfirmDialog
        open={confirmDelete}
        onOpenChange={setConfirmDelete}
        title="Delete merge request?"
        description={`“${mr.title}” will be removed from Gloss. This does not affect the merge request on your Git platform.`}
        confirmLabel="Delete"
        destructive
        loading={deleteMr.isPending}
        onConfirm={() => deleteMr.mutate(id, { onSuccess: () => navigate('/') })}
      />

      <ConfirmDialog
        open={confirmIgnore}
        onOpenChange={setConfirmIgnore}
        title="Ignore merge request?"
        description={`“${mr.title}” will be hidden and won’t be pulled again, even if it stays open on your Git platform.`}
        confirmLabel="Ignore"
        loading={ignoreMr.isPending}
        onConfirm={() => ignoreMr.mutate(id, { onSuccess: () => navigate('/') })}
      />
    </div>
  );
}

function LifecycleStepper({ state }) {
  const currentIndex = LIFECYCLE.indexOf(state);
  if (currentIndex === -1) return null;
  return (
    <ol className="stepper" aria-label="Review lifecycle">
      {LIFECYCLE.map((step, i) => {
        const status = i < currentIndex ? 'done' : i === currentIndex ? 'current' : 'upcoming';
        return (
          <li key={step} className={`stepper-step stepper-${status}`}>
            <span className="stepper-dot">{status === 'done' ? <Check size={12} /> : i + 1}</span>
            <span className="stepper-label">{step}</span>
          </li>
        );
      })}
    </ol>
  );
}

function MrDetailSkeleton() {
  return (
    <div className="mr-detail">
      <Skeleton width={140} height={14} />
      <div style={{ marginTop: 20, display: 'flex', flexDirection: 'column', gap: 12 }}>
        <Skeleton width="60%" height={24} />
        <Skeleton width="40%" height={14} />
        <Skeleton width="100%" height={48} radius={8} />
      </div>
      <div style={{ marginTop: 24, display: 'flex', flexDirection: 'column', gap: 8 }}>
        <Skeleton width="100%" height={120} radius={8} />
        <Skeleton width="100%" height={200} radius={8} />
      </div>
    </div>
  );
}
