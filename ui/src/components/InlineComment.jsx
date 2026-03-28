import { useState } from 'react';

export default function InlineComment({ comment, onEdit, onDelete }) {
  const [mode, setMode] = useState('view'); // 'view' | 'edit' | 'delete'
  const [editBody, setEditBody] = useState(comment.body);
  const [editReason, setEditReason] = useState('');
  const [deleteReason, setDeleteReason] = useState('');
  const [reasonVisible, setReasonVisible] = useState(false);
  const [reasoningVisible, setReasoningVisible] = useState(false);
  const [saving, setSaving] = useState(false);

  async function submitEdit() {
    setSaving(true);
    try {
      await onEdit(editBody, editReason.trim() || null);
      setMode('view');
      setEditReason('');
      setReasonVisible(false);
    } finally {
      setSaving(false);
    }
  }

  async function submitDelete() {
    setSaving(true);
    try {
      await onDelete(deleteReason.trim() || null);
    } finally {
      setSaving(false);
    }
  }

  const label = comment.state === 'UserAdded' ? 'You' : 'Gloss';
  const edited = comment.state === 'Edited';

  return (
    <div className={`inline-comment ${comment.state === 'UserAdded' ? 'user-added' : ''}`}>
      <div className="comment-header">
        <span className="comment-author">
          {label}
          {edited && <span className="edited-badge">edited</span>}
        </span>
        <div className="comment-header-actions">
          {comment.reasoning && (
            <button className="btn-ghost btn-sm" onClick={() => setReasoningVisible(v => !v)}>
              {reasoningVisible ? 'Hide reasoning' : 'Reasoning'}
            </button>
          )}
          {mode === 'view' && (
            <>
              <button className="btn-ghost btn-sm" onClick={() => { setMode('edit'); setReasonVisible(false); }}>Edit</button>
              <button className="btn-ghost btn-sm btn-danger" onClick={() => setMode('delete')}>Delete</button>
            </>
          )}
        </div>
      </div>

      {reasoningVisible && comment.reasoning && (
        <div className="reasoning-panel">{comment.reasoning}</div>
      )}

      {mode === 'view' && (
        <div className="comment-body">{comment.body}</div>
      )}

      {mode === 'edit' && (
        <div className="comment-edit">
          <textarea
            value={editBody}
            onChange={e => setEditBody(e.target.value)}
            rows={4}
            autoFocus
          />
          <button className="btn-ghost btn-sm reason-toggle" onClick={() => setReasonVisible(v => !v)}>
            {reasonVisible ? '− Reason' : '+ Reason for edit'}
          </button>
          {reasonVisible && (
            <input
              type="text"
              className="reason-input"
              placeholder="Why are you changing this? (helps Gloss learn)"
              value={editReason}
              onChange={e => setEditReason(e.target.value)}
            />
          )}
          <div className="form-actions">
            <button className="btn btn-sm" onClick={submitEdit} disabled={saving || !editBody.trim()}>
              {saving ? 'Saving…' : 'Save'}
            </button>
            <button className="btn-ghost btn-sm" onClick={() => { setMode('view'); setEditReason(''); setReasonVisible(false); }}>
              Cancel
            </button>
          </div>
        </div>
      )}

      {mode === 'delete' && (
        <div className="comment-delete">
          <input
            type="text"
            className="reason-input"
            placeholder="Reason for deleting? (helps Gloss learn — optional)"
            value={deleteReason}
            onChange={e => setDeleteReason(e.target.value)}
            autoFocus
          />
          <div className="form-actions">
            <button className="btn btn-sm btn-danger" onClick={submitDelete} disabled={saving}>
              {saving ? 'Deleting…' : 'Delete'}
            </button>
            <button className="btn-ghost btn-sm" onClick={() => { setMode('view'); setDeleteReason(''); }}>
              Cancel
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
