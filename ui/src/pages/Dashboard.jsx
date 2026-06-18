import { useState } from 'react';
import { Link } from 'react-router-dom';
import { RefreshCw, Trash2, GitPullRequest, AlertCircle } from 'lucide-react';
import { useMrs, useConfig, usePollAll, useDeleteMr } from '../api/queries.js';
import Button from '../components/ui/Button.jsx';
import StateBadge from '../components/ui/StateBadge.jsx';
import Skeleton from '../components/ui/Skeleton.jsx';
import EmptyState from '../components/ui/EmptyState.jsx';
import ConfirmDialog from '../components/ui/ConfirmDialog.jsx';

const STATE_ORDER = ['Reviewing', 'Pending', 'Ready', 'Published'];
const STATE_LABEL = {
  Reviewing: 'Being reviewed by AI',
  Pending: 'Awaiting review',
  Ready: 'Reviewed — ready to publish',
  Published: 'Published to Git platform',
};

export default function Dashboard() {
  const { data: config } = useConfig({
    refetchInterval: (q) => (q.state.data?.isPolling ? 3000 : false),
  });
  const mrsQuery = useMrs({
    refetchInterval: (q) => {
      const data = q.state.data ?? [];
      const reviewing = data.some((m) => m.state === 'Reviewing');
      return reviewing || config?.isPolling ? 4000 : false;
    },
  });
  const pollAll = usePollAll();

  const mrs = mrsQuery.data ?? [];
  const isPolling = pollAll.isPending || config?.isPolling === true;

  const grouped = STATE_ORDER.reduce((acc, state) => {
    acc[state] = mrs.filter((mr) => mr.state === state);
    return acc;
  }, {});
  const actionable = (grouped.Pending?.length ?? 0) + (grouped.Ready?.length ?? 0);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Merge Requests</h1>
          {actionable > 0 && <div className="page-subtitle">{actionable} need attention</div>}
        </div>
        <div className="page-actions">
          <Button icon={RefreshCw} onClick={() => pollAll.mutate()} loading={isPolling} disabled={isPolling}>
            {isPolling ? 'Polling…' : 'Poll now'}
          </Button>
        </div>
      </div>

      {mrsQuery.isError && (
        <div className="banner banner-error">
          <AlertCircle size={16} aria-hidden="true" />
          <span>{mrsQuery.error.message}</span>
          <Button variant="ghost" size="sm" onClick={() => mrsQuery.refetch()}>Retry</Button>
        </div>
      )}

      {mrsQuery.isLoading ? (
        <MrListSkeleton />
      ) : mrs.length === 0 && !mrsQuery.isError ? (
        <EmptyState
          icon={GitPullRequest}
          title="No merge requests yet"
          description="Poll your configured repositories to pull in open merge requests for review."
          action={
            <Button icon={RefreshCw} onClick={() => pollAll.mutate()} loading={isPolling} disabled={isPolling}>
              Poll now
            </Button>
          }
        />
      ) : (
        STATE_ORDER.map((state) =>
          grouped[state].length > 0 ? (
            <section key={state} className="mr-group">
              <div className="mr-group-header">
                <StateBadge state={state} />
                <span className="muted">{STATE_LABEL[state]}</span>
                <span className="mr-count">{grouped[state].length}</span>
              </div>
              <div className="mr-list">
                {grouped[state].map((mr) => <MrCard key={mr.id} mr={mr} />)}
              </div>
            </section>
          ) : null,
        )
      )}
    </div>
  );
}

function MrCard({ mr }) {
  const [confirmOpen, setConfirmOpen] = useState(false);
  const deleteMr = useDeleteMr();

  return (
    <div className="mr-card">
      <Link to={`/mr/${mr.id}`} className="mr-card-link">
        <div className="mr-card-title">{mr.title}</div>
        <div className="mr-card-meta">
          <span className="muted">{mr.projectPath}</span>
          <span className="branch mono">{mr.sourceBranch} → {mr.targetBranch}</span>
          <span className="muted">{mr.authorUsername}</span>
        </div>
      </Link>
      <Button
        variant="dangerGhost"
        size="sm"
        icon={Trash2}
        aria-label="Delete merge request"
        onClick={() => setConfirmOpen(true)}
      />
      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title="Delete merge request?"
        description={`“${mr.title}” will be removed from Gloss. This does not affect the merge request on your Git platform.`}
        confirmLabel="Delete"
        destructive
        loading={deleteMr.isPending}
        onConfirm={() => deleteMr.mutate(mr.id, { onSuccess: () => setConfirmOpen(false) })}
      />
    </div>
  );
}

function MrListSkeleton() {
  return (
    <div className="mr-group">
      <div className="mr-group-header"><Skeleton width={90} height={20} radius={12} /></div>
      <div className="mr-list">
        {[0, 1, 2].map((i) => (
          <div className="mr-card" key={i}>
            <div className="mr-card-link">
              <Skeleton width="55%" height={15} />
              <div style={{ display: 'flex', gap: 12, marginTop: 10 }}>
                <Skeleton width={120} height={12} />
                <Skeleton width={160} height={12} />
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
