import { useState } from 'react';
import { Plus, ScrollText, AlertCircle, ChevronUp, ChevronDown } from 'lucide-react';
import {
  useConstitution, useAddDocument, useUpdateDocument, useDeleteDocument, useSeedProjection,
} from '../api/queries.js';
import Button from '../components/ui/Button.jsx';
import Skeleton from '../components/ui/Skeleton.jsx';
import EmptyState from '../components/ui/EmptyState.jsx';
import ConfirmDialog from '../components/ui/ConfirmDialog.jsx';

export default function Constitution() {
  const docsQuery = useConstitution();
  const docs = docsQuery.data ?? [];
  const addDoc = useAddDocument();
  const updateDoc = useUpdateDocument();
  const deleteDoc = useDeleteDocument();
  const seed = useSeedProjection();

  const [editingId, setEditingId] = useState(null);
  const [adding, setAdding] = useState(false);
  const [confirmDeleteId, setConfirmDeleteId] = useState(null);

  function handleAdd(title, body) {
    return addDoc.mutateAsync({ title, body, position: docs.length }).then(() => setAdding(false));
  }

  function handleUpdate(doc, title, body) {
    return updateDoc.mutateAsync({ id: doc.id, title, body, position: doc.position }).then(() => setEditingId(null));
  }

  function handleMove(id, dir) {
    const idx = docs.findIndex((d) => d.id === id);
    const swapIdx = idx + dir;
    if (swapIdx < 0 || swapIdx >= docs.length) return;
    const reordered = [...docs];
    [reordered[idx], reordered[swapIdx]] = [reordered[swapIdx], reordered[idx]];
    Promise.all(
      reordered
        .map((doc, i) => (doc.position !== i
          ? updateDoc.mutateAsync({ id: doc.id, title: doc.title, body: doc.body, position: i })
          : null))
        .filter(Boolean),
    );
  }

  const docToDelete = docs.find((d) => d.id === confirmDeleteId);

  return (
    <div className="page">
      <div className="page-header">
        <h1>Constitution</h1>
        <div className="page-actions">
          <Button variant="ghost" icon={Plus} onClick={() => { setAdding(true); setEditingId(null); }}>
            Add Document
          </Button>
          <Button onClick={() => seed.mutate()} loading={seed.isPending} disabled={seed.isPending || docs.length === 0}>
            Seed Projection
          </Button>
        </div>
      </div>

      <p className="page-desc">
        Constitution documents are injected into every review and never modified by the learning loop.
        Earlier documents take priority. Use <em>Seed Projection</em> to give the reviewer an informed baseline
        before any events have been recorded.
      </p>

      {docsQuery.isError && (
        <div className="banner banner-error">
          <AlertCircle size={16} aria-hidden="true" />
          <span>{docsQuery.error.message}</span>
          <Button variant="ghost" size="sm" onClick={() => docsQuery.refetch()}>Retry</Button>
        </div>
      )}

      {adding && <DocForm onSubmit={handleAdd} onCancel={() => setAdding(false)} />}

      {docsQuery.isLoading ? (
        <DocListSkeleton />
      ) : docs.length === 0 && !adding && !docsQuery.isError ? (
        <EmptyState
          icon={ScrollText}
          title="No constitution documents yet"
          description="Add review guidelines, coding standards, or architecture policies the reviewer should always follow."
          action={<Button icon={Plus} onClick={() => setAdding(true)}>Add Document</Button>}
        />
      ) : (
        <div className="doc-list">
          {docs.map((doc, idx) => (
            <div key={doc.id} className="doc-card">
              {editingId === doc.id ? (
                <DocForm
                  initial={doc}
                  onSubmit={(title, body) => handleUpdate(doc, title, body)}
                  onCancel={() => setEditingId(null)}
                />
              ) : (
                <>
                  <div className="doc-card-header">
                    <div className="doc-order-buttons">
                      <button onClick={() => handleMove(doc.id, -1)} disabled={idx === 0} title="Move up" aria-label="Move up">
                        <ChevronUp size={14} />
                      </button>
                      <button onClick={() => handleMove(doc.id, 1)} disabled={idx === docs.length - 1} title="Move down" aria-label="Move down">
                        <ChevronDown size={14} />
                      </button>
                    </div>
                    <h3 className="doc-title">{doc.title}</h3>
                    <div className="doc-actions">
                      <Button variant="ghost" size="sm" onClick={() => { setEditingId(doc.id); setAdding(false); }}>Edit</Button>
                      <Button variant="dangerGhost" size="sm" onClick={() => setConfirmDeleteId(doc.id)}>Delete</Button>
                    </div>
                  </div>
                  <pre className="doc-body">{doc.body}</pre>
                </>
              )}
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!confirmDeleteId}
        onOpenChange={(open) => !open && setConfirmDeleteId(null)}
        title="Delete document?"
        description={docToDelete ? `“${docToDelete.title}” will no longer be included in reviews.` : ''}
        confirmLabel="Delete"
        destructive
        loading={deleteDoc.isPending}
        onConfirm={() => deleteDoc.mutate(confirmDeleteId, { onSuccess: () => setConfirmDeleteId(null) })}
      />
    </div>
  );
}

function DocForm({ initial, onSubmit, onCancel }) {
  const [title, setTitle] = useState(initial?.title ?? '');
  const [body, setBody] = useState(initial?.body ?? '');
  const [saving, setSaving] = useState(false);

  async function handleSubmit() {
    if (!title.trim() || !body.trim()) return;
    setSaving(true);
    try {
      await onSubmit(title.trim(), body.trim());
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="doc-form">
      <input
        type="text"
        className="doc-form-title"
        placeholder="Document title (e.g. Code Review Guidelines)"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        autoFocus
      />
      <textarea
        className="doc-form-body"
        placeholder="Document content — guidelines, standards, policies…"
        value={body}
        onChange={(e) => setBody(e.target.value)}
        rows={10}
      />
      <div className="form-actions">
        <Button onClick={handleSubmit} loading={saving} disabled={saving || !title.trim() || !body.trim()}>
          {initial ? 'Save' : 'Add'}
        </Button>
        <Button variant="ghost" onClick={onCancel}>Cancel</Button>
      </div>
    </div>
  );
}

function DocListSkeleton() {
  return (
    <div className="doc-list">
      {[0, 1].map((i) => (
        <div className="doc-card" key={i}>
          <div className="doc-card-header">
            <Skeleton width="30%" height={16} />
          </div>
          <Skeleton width="100%" height={60} radius={6} style={{ marginTop: 12 }} />
        </div>
      ))}
    </div>
  );
}
