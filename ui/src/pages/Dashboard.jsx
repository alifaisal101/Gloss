import { useState } from 'react';
import { Link } from 'react-router-dom';
import { RefreshCw, Trash2, RotateCcw, GitPullRequest, AlertCircle } from 'lucide-react';
import {
  useMrs, useConfig, usePollAll, useDeleteMr, useIgnoredMrs, useUnignoreMr,
} from '../api/queries.js';
import Button from '../components/ui/Button.jsx';
import StateBadge from '../components/ui/StateBadge.jsx';
import Skeleton from '../components/ui/Skeleton.jsx';
import EmptyState from '../components/ui/EmptyState.jsx';
import ConfirmDialog from '../components/ui/ConfirmDialog.jsx';

const STATE_TABS = ['Pending', 'Reviewing', 'Ready', 'Published'];

export default function Dashboard() {
  const [tab, setTab] = useState('All');
  const [search, setSearch] = useState('');

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
  const ignoredQuery = useIgnoredMrs();
  const pollAll = usePollAll();

  const mrs = mrsQuery.data ?? [];
  const ignored = ignoredQuery.data ?? [];
  const isPolling = pollAll.isPending || config?.isPolling === true;

  const q = search.trim().toLowerCase();
  const hit = (...fields) => !q || fields.some((f) => (f ?? '').toLowerCase().includes(q));

  const counts = {
    All: mrs.length,
    Pending: mrs.filter((m) => m.state === 'Pending').length,
    Reviewing: mrs.filter((m) => m.state === 'Reviewing').length,
    Ready: mrs.filter((m) => m.state === 'Ready').length,
    Published: mrs.filter((m) => m.state === 'Published').length,
    Ignored: ignored.length,
  };
  const tabs = ['All', ...STATE_TABS, 'Ignored'];

  const visibleMrs = mrs
    .filter((m) => tab === 'All' || m.state === tab)
    .filter((m) => hit(m.title, m.projectPath, m.authorUsername, m.sourceBranch));
  const visibleIgnored = ignored.filter((i) => hit(i.title, i.projectPath));

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Merge Requests</h1>
          {counts.Pending + counts.Ready > 0 && (
            <div className="page-subtitle">{counts.Pending + counts.Ready} need attention</div>
          )}
        </div>
        <div className="page-actions">
          <Button icon={RefreshCw} onClick={() => pollAll.mutate()} loading={isPolling} disabled={isPolling}>
            {isPolling ? 'Polling…' : 'Poll now'}
          </Button>
        </div>
      </div>

      <div className="mr-toolbar">
        <div className="tabs" role="tablist">
          {tabs.map((t) => (
            <button
              key={t}
              type="button"
              role="tab"
              aria-selected={tab === t}
              className={`tab${tab === t ? ' tab-active' : ''}`}
              onClick={() => setTab(t)}
            >
              {t}
              <span className="tab-count">{counts[t] ?? 0}</span>
            </button>
          ))}
        </div>
        <input
          className="search-input"
          type="search"
          placeholder="Search title, project, author…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {(mrsQuery.isError || ignoredQuery.isError) && (
        <div className="banner banner-error">
          <AlertCircle size={16} aria-hidden="true" />
          <span>{(mrsQuery.error ?? ignoredQuery.error)?.message}</span>
          <Button variant="ghost" size="sm" onClick={() => { mrsQuery.refetch(); ignoredQuery.refetch(); }}>Retry</Button>
        </div>
      )}

      {tab === 'Ignored' ? (
        <IgnoredList loading={ignoredQuery.isLoading} items={visibleIgnored} total={ignored.length} hasSearch={!!q} />
      ) : (
        <ActiveList
          loading={mrsQuery.isLoading}
          items={visibleMrs}
          total={mrs.length}
          hasSearch={!!q}
          isPolling={isPolling}
          onPoll={() => pollAll.mutate()}
        />
      )}
    </div>
  );
}

function ActiveList({ loading, items, total, hasSearch, isPolling, onPoll }) {
  if (loading) return <MrListSkeleton />;
  if (total === 0) {
    return (
      <EmptyState
        icon={GitPullRequest}
        title="No merge requests yet"
        description="Poll your configured repositories to pull in open merge requests for review."
        action={<Button icon={RefreshCw} onClick={onPoll} loading={isPolling} disabled={isPolling}>Poll now</Button>}
      />
    );
  }
  if (items.length === 0) {
    return (
      <EmptyState
        icon={GitPullRequest}
        title="Nothing here"
        description={hasSearch ? 'No merge requests match your search.' : 'No merge requests in this state.'}
      />
    );
  }
  return <div className="mr-list">{items.map((mr) => <MrCard key={mr.id} mr={mr} />)}</div>;
}

function IgnoredList({ loading, items, total, hasSearch }) {
  if (loading) return <MrListSkeleton />;
  if (total === 0) {
    return (
      <EmptyState
        icon={GitPullRequest}
        title="No ignored merge requests"
        description="Merge requests you ignore are hidden from polling and listed here so you can restore them."
      />
    );
  }
  if (items.length === 0) {
    return <EmptyState icon={GitPullRequest} title="Nothing here" description="No ignored merge requests match your search." />;
  }
  return <div className="mr-list">{items.map((i) => <IgnoredCard key={i.id} item={i} />)}</div>;
}

function MrCard({ mr }) {
  const [confirmOpen, setConfirmOpen] = useState(false);
  const deleteMr = useDeleteMr();

  return (
    <div className="mr-card">
      <Link to={`/mr/${mr.id}`} className="mr-card-link">
        <div className="mr-card-title">
          <StateBadge state={mr.state} />
          <span>{mr.title}</span>
        </div>
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

function IgnoredCard({ item }) {
  const unignore = useUnignoreMr();

  return (
    <div className="mr-card">
      <div className="mr-card-link">
        <div className="mr-card-title"><span>{item.title}</span></div>
        <div className="mr-card-meta">
          <span className="muted">{item.projectPath}</span>
          <span className="mono">!{item.providerIid}</span>
          <span className="muted">ignored {new Date(item.ignoredAt).toLocaleDateString()}</span>
        </div>
      </div>
      <Button
        variant="secondary"
        size="sm"
        icon={RotateCcw}
        loading={unignore.isPending}
        onClick={() => unignore.mutate(item.id)}
      >
        Unignore
      </Button>
    </div>
  );
}

function MrListSkeleton() {
  return (
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
  );
}
