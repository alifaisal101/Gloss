import { useState, useEffect } from 'react';
import { api } from '../api/client.js';

export default function Constitution() {
  const [docs, setDocs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [editingId, setEditingId] = useState(null);
  const [adding, setAdding] = useState(false);
  const [seeding, setSeeding] = useState(false);
  const [seedDone, setSeedDone] = useState(false);

  useEffect(() => {
    api.listConstitution()
      .then(d => setDocs(d.sort((a, b) => a.position - b.position)))
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);

  async function handleAdd(title, body) {
    const added = await api.addDocument(title, body, docs.length);
    setDocs(d => [...d, added]);
    setAdding(false);
  }

  async function handleUpdate(doc, title, body) {
    const updated = await api.updateDocument(doc.id, title, body, doc.position);
    setDocs(d => d.map(x => x.id === doc.id ? updated : x));
    setEditingId(null);
  }

  async function handleDelete(id) {
    await api.deleteDocument(id);
    setDocs(d => d.filter(x => x.id !== id));
  }

  async function handleMove(id, dir) {
    const sorted = [...docs].sort((a, b) => a.position - b.position);
    const idx = sorted.findIndex(d => d.id === id);
    const swapIdx = idx + dir;
    if (swapIdx < 0 || swapIdx >= sorted.length) return;

    const reordered = [...sorted];
    [reordered[idx], reordered[swapIdx]] = [reordered[swapIdx], reordered[idx]];

    const updated = await Promise.all(
      reordered.map((doc, i) =>
        doc.position !== i
          ? api.updateDocument(doc.id, doc.title, doc.body, i)
          : Promise.resolve({ ...doc, position: i }),
      ),
    );
    setDocs(updated.sort((a, b) => a.position - b.position));
  }

  async function handleSeedProjection() {
    setSeeding(true);
    setSeedDone(false);
    try {
      await api.seedProjection();
      setSeedDone(true);
    } finally {
      setSeeding(false);
    }
  }

  if (loading) return <div className="loading">Loading…</div>;
  if (error) return <div className="error">Failed to load: {error.message}</div>;

  const sorted = [...docs].sort((a, b) => a.position - b.position);

  return (
    <div className="page">
      <div className="page-header">
        <h1>Constitution</h1>
        <div className="page-actions">
          <button className="btn-ghost" onClick={() => { setAdding(true); setEditingId(null); }}>
            + Add Document
          </button>
          <button className="btn" onClick={handleSeedProjection} disabled={seeding || docs.length === 0}>
            {seeding ? 'Seeding…' : seedDone ? 'Seeded ✓' : 'Seed Projection'}
          </button>
        </div>
      </div>

      <p className="page-desc">
        Constitution documents are injected into every review and never modified by the learning loop.
        Earlier documents take priority. Use <em>Seed Projection</em> to give the reviewer an informed baseline
        before any events have been recorded.
      </p>

      {adding && (
        <DocForm onSubmit={handleAdd} onCancel={() => setAdding(false)} />
      )}

      <div className="doc-list">
        {sorted.map((doc, idx) => (
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
                    <button onClick={() => handleMove(doc.id, -1)} disabled={idx === 0} title="Move up">↑</button>
                    <button onClick={() => handleMove(doc.id, 1)} disabled={idx === sorted.length - 1} title="Move down">↓</button>
                  </div>
                  <h3 className="doc-title">{doc.title}</h3>
                  <div className="doc-actions">
                    <button className="btn-ghost" onClick={() => { setEditingId(doc.id); setAdding(false); }}>Edit</button>
                    <button className="btn-ghost btn-danger" onClick={() => handleDelete(doc.id)}>Delete</button>
                  </div>
                </div>
                <pre className="doc-body">{doc.body}</pre>
              </>
            )}
          </div>
        ))}

        {docs.length === 0 && !adding && (
          <div className="empty">
            No constitution documents yet. Add review guidelines, coding standards, or architecture policies.
          </div>
        )}
      </div>
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
        onChange={e => setTitle(e.target.value)}
        autoFocus
      />
      <textarea
        className="doc-form-body"
        placeholder="Document content — guidelines, standards, policies…"
        value={body}
        onChange={e => setBody(e.target.value)}
        rows={10}
      />
      <div className="form-actions">
        <button className="btn" onClick={handleSubmit} disabled={saving || !title.trim() || !body.trim()}>
          {saving ? 'Saving…' : initial ? 'Save' : 'Add'}
        </button>
        <button className="btn-ghost" onClick={onCancel}>Cancel</button>
      </div>
    </div>
  );
}
