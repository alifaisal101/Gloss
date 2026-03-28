import { useState } from 'react';

export default function CommentForm({ onSubmit, onCancel }) {
  const [body, setBody] = useState('');
  const [reason, setReason] = useState('');
  const [reasonVisible, setReasonVisible] = useState(false);
  const [saving, setSaving] = useState(false);

  async function handleSubmit() {
    if (!body.trim()) return;
    setSaving(true);
    try {
      await onSubmit(body.trim(), reason.trim() || null);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="comment-form">
      <textarea
        value={body}
        onChange={e => setBody(e.target.value)}
        placeholder="Add a comment…"
        rows={3}
        autoFocus
      />
      <button className="btn-ghost btn-sm reason-toggle" onClick={() => setReasonVisible(v => !v)}>
        {reasonVisible ? '− Reason' : '+ Reason'}
      </button>
      {reasonVisible && (
        <input
          type="text"
          className="reason-input"
          placeholder="Why are you adding this? (helps Gloss learn — optional)"
          value={reason}
          onChange={e => setReason(e.target.value)}
        />
      )}
      <div className="form-actions">
        <button className="btn btn-sm" onClick={handleSubmit} disabled={saving || !body.trim()}>
          {saving ? 'Adding…' : 'Add Comment'}
        </button>
        <button className="btn-ghost btn-sm" onClick={onCancel}>Cancel</button>
      </div>
    </div>
  );
}
