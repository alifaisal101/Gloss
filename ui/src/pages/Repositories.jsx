import { useState } from 'react';
import { toast } from 'sonner';
import { FolderGit2, Trash2, AlertCircle } from 'lucide-react';
import { useRepositories, useUpdateRepository, useDeleteRepository } from '../api/queries.js';
import Button from '../components/ui/Button.jsx';
import Skeleton from '../components/ui/Skeleton.jsx';
import EmptyState from '../components/ui/EmptyState.jsx';
import ConfirmDialog from '../components/ui/ConfirmDialog.jsx';

export default function Repositories() {
  const reposQuery = useRepositories();
  const repos = reposQuery.data ?? [];

  return (
    <div className="page">
      <div className="page-header">
        <h1>Repositories</h1>
      </div>
      <p className="page-desc">
        Gloss tracks repositories automatically as MRs are discovered.
        The <em>Poll schedule</em> column is reserved for future per-repository scheduling — it has no effect right now.
        All repositories are polled on the global schedule configured in Settings.
      </p>

      {reposQuery.isError && (
        <div className="banner banner-error">
          <AlertCircle size={16} aria-hidden="true" />
          <span>{reposQuery.error.message}</span>
          <Button variant="ghost" size="sm" onClick={() => reposQuery.refetch()}>Retry</Button>
        </div>
      )}

      {reposQuery.isLoading ? (
        <TableSkeleton />
      ) : repos.length === 0 && !reposQuery.isError ? (
        <EmptyState
          icon={FolderGit2}
          title="No repositories tracked yet"
          description="Repositories appear here automatically as Gloss discovers merge requests on the projects configured in Settings."
        />
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Project</th>
              <th>Auto-review</th>
              <th>Poll schedule (cron)</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {repos.map((repo) => <RepoRow key={repo.id} repo={repo} />)}
          </tbody>
        </table>
      )}
    </div>
  );
}

function RepoRow({ repo }) {
  const [cronEdit, setCronEdit] = useState(undefined);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const update = useUpdateRepository();
  const remove = useDeleteRepository();

  const isCronDirty = cronEdit !== undefined && cronEdit !== (repo.pollCron ?? '');
  const busy = update.isPending || remove.isPending;

  function saveCron() {
    update.mutate(
      { id: repo.id, fields: { pollCron: cronEdit } },
      { onSuccess: () => { setCronEdit(undefined); toast.success('Poll schedule saved'); } },
    );
  }

  function toggleAutoReview() {
    const next = !repo.autoReviewEnabled;
    update.mutate(
      { id: repo.id, fields: { autoReviewEnabled: next } },
      { onSuccess: () => toast.success(next ? 'Auto-review enabled' : 'Auto-review disabled') },
    );
  }

  return (
    <tr>
      <td className="mono">{repo.projectPath}</td>
      <td>
        <label className="toggle" title="Automatically review new MRs when they are pulled">
          <input type="checkbox" checked={repo.autoReviewEnabled} onChange={toggleAutoReview} disabled={busy} />
          {repo.autoReviewEnabled ? 'On' : 'Off'}
        </label>
      </td>
      <td>
        <input
          className="cron-input"
          value={cronEdit !== undefined ? cronEdit : (repo.pollCron ?? '')}
          onChange={(e) => setCronEdit(e.target.value)}
          placeholder="Not used yet"
        />
      </td>
      <td>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center', justifyContent: 'flex-end' }}>
          {isCronDirty && (
            <Button size="sm" onClick={saveCron} loading={update.isPending} disabled={busy}>Save</Button>
          )}
          <Button
            variant="dangerGhost"
            size="sm"
            icon={Trash2}
            aria-label="Remove repository"
            onClick={() => setConfirmDelete(true)}
            disabled={busy}
          />
        </div>
        <ConfirmDialog
          open={confirmDelete}
          onOpenChange={setConfirmDelete}
          title="Remove repository?"
          description={`Gloss will stop tracking ${repo.projectPath}. It will reappear if a new merge request is discovered there.`}
          confirmLabel="Remove"
          destructive
          loading={remove.isPending}
          onConfirm={() => remove.mutate(repo.id, { onSuccess: () => setConfirmDelete(false) })}
        />
      </td>
    </tr>
  );
}

function TableSkeleton() {
  return (
    <table className="data-table">
      <thead>
        <tr><th>Project</th><th>Auto-review</th><th>Poll schedule (cron)</th><th></th></tr>
      </thead>
      <tbody>
        {[0, 1, 2].map((i) => (
          <tr key={i}>
            <td><Skeleton width="60%" height={14} /></td>
            <td><Skeleton width={50} height={14} /></td>
            <td><Skeleton width={140} height={28} radius={6} /></td>
            <td><Skeleton width={60} height={28} radius={6} /></td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
