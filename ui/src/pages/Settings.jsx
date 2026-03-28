import { useState, useEffect } from 'react';
import { api } from '../api/client.js';

const GIT_PROVIDERS = ['gitlab', 'github'];
const LLM_PROVIDERS = ['anthropic', 'openai', 'ollama'];

export default function Settings() {
  const [settings, setSettings] = useState(null);
  const [form, setForm] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.getSettings()
      .then(s => { setSettings(s); setForm(s); })
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);

  function set(key, value) {
    setForm(f => ({ ...f, [key]: value }));
    setSaved(false);
  }

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      const updated = await api.updateSettings(form);
      setSettings(updated);
      setForm(updated);
      setSaved(true);
    } catch (err) {
      setError(err);
    } finally {
      setSaving(false);
    }
  }

  const isDirty = form && settings && JSON.stringify(form) !== JSON.stringify(settings);

  if (loading) return <div className="loading">Loading…</div>;
  if (error && !form) return <div className="error">Failed to load: {error.message}</div>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Settings</h1>
        <div className="page-actions">
          {saved && !isDirty && <span className="saved-badge">Saved ✓</span>}
          <button className="btn" onClick={handleSave} disabled={saving || !isDirty}>
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      </div>

      {error && <div className="error" style={{ marginBottom: 16 }}>{error.message}</div>}

      <div className="settings-sections">
        <section className="settings-section">
          <h2>Git Provider</h2>
          <div className="field-group">
            <Field label="Provider">
              <select value={form.gitProvider} onChange={e => set('gitProvider', e.target.value)}>
                {GIT_PROVIDERS.map(p => <option key={p} value={p}>{p}</option>)}
              </select>
            </Field>
            <Field label="Base URL" hint="e.g. https://gitlab.example.com">
              <input
                type="text"
                value={form.gitBaseUrl}
                onChange={e => set('gitBaseUrl', e.target.value)}
                placeholder="https://gitlab.example.com"
              />
            </Field>
            <Field label="Personal Access Token">
              <SecretInput
                value={form.gitToken}
                onChange={v => set('gitToken', v)}
                placeholder="glpat-…"
              />
            </Field>
            <Field label="Projects to watch" hint="One project path per line (e.g. group/repo)">
              <textarea
                rows={4}
                value={(form.gitProjects ?? []).join('\n')}
                onChange={e => set('gitProjects', e.target.value.split('\n').map(s => s.trim()).filter(Boolean))}
                placeholder={'group/project-one\ngroup/project-two'}
              />
            </Field>
          </div>
        </section>

        <section className="settings-section">
          <h2>LLM Provider</h2>
          <div className="field-group">
            <Field label="Provider">
              <select value={form.llmProvider} onChange={e => set('llmProvider', e.target.value)}>
                {LLM_PROVIDERS.map(p => <option key={p} value={p}>{p}</option>)}
              </select>
            </Field>
            <Field label="API Key">
              <SecretInput
                value={form.llmApiKey}
                onChange={v => set('llmApiKey', v)}
                placeholder="sk-ant-…"
              />
            </Field>
            <Field label="Model" hint="e.g. claude-sonnet-4-6">
              <input
                type="text"
                value={form.llmModel}
                onChange={e => set('llmModel', e.target.value)}
                placeholder="claude-sonnet-4-6"
              />
            </Field>
            <Field label="Enable reasoning">
              <label className="toggle">
                <input
                  type="checkbox"
                  checked={form.llmReasoningEnabled ?? true}
                  onChange={e => set('llmReasoningEnabled', e.target.checked)}
                />
                <span>Include reasoning trace in draft comments</span>
              </label>
            </Field>
          </div>
        </section>

        <section className="settings-section">
          <h2>Polling</h2>
          <div className="field-group">
            <Field
              label="Default poll schedule"
              hint="Cron expression applied to all repositories unless overridden per-repository on the Repositories page."
            >
              <input
                type="text"
                className="mono"
                value={form.defaultPollCron}
                onChange={e => set('defaultPollCron', e.target.value)}
                placeholder="0 */2 * * * ?"
              />
            </Field>
          </div>
        </section>
      </div>
    </div>
  );
}

function Field({ label, hint, children }) {
  return (
    <div className="field">
      <label className="field-label">{label}</label>
      {children}
      {hint && <span className="field-hint">{hint}</span>}
    </div>
  );
}

function SecretInput({ value, onChange, placeholder }) {
  const [revealed, setRevealed] = useState(false);
  return (
    <div className="secret-input-wrap">
      <input
        type={revealed ? 'text' : 'password'}
        value={value ?? ''}
        onChange={e => onChange(e.target.value)}
        placeholder={placeholder}
      />
      <button className="btn-ghost btn-sm" onClick={() => setRevealed(r => !r)}>
        {revealed ? 'Hide' : 'Show'}
      </button>
    </div>
  );
}
