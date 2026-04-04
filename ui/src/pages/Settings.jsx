import { useState, useEffect } from 'react';
import { api } from '../api/client.js';

const GIT_PROVIDERS = ['gitlab', 'github'];
const LLM_PROVIDERS = ['anthropic', 'openai', 'ollama'];

function normalize(c) {
  return {
    gitProvider: c.gitProvider ?? 'gitlab',
    gitBaseUrl: c.gitBaseUrl ?? '',
    gitToken: null,
    gitTokenSet: c.gitTokenSet ?? false,
    gitProjects: c.gitProjects ?? [],
    llmProvider: c.llmProvider ?? 'anthropic',
    llmApiKey: null,
    llmApiKeySet: c.llmApiKeySet ?? false,
    llmModel: c.llmModel ?? '',
    llmReasoningEnabled: c.llmReasoningEnabled ?? true,
    llmMaxTokens: c.llmMaxTokens ?? 16000,
    llmThinkingBudget: c.llmThinkingBudget ?? 10000,
    defaultPollCron: c.defaultPollCron ?? '',
  };
}

export default function Settings() {
  const [settings, setSettings] = useState(null);
  const [form, setForm] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.getConfig()
      .then(c => { const s = normalize(c); setSettings(s); setForm(s); })
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
      await api.updateConfig({
        ...form,
        gitToken: form.gitToken || null,
        llmApiKey: form.llmApiKey || null,
        gitProjects: form.gitProjects.filter(Boolean),
      });
      const updated = normalize(await api.getConfig());
      setSettings(updated);
      setForm(updated);
      setSaved(true);
    } catch (err) {
      setError(err);
    } finally {
      setSaving(false);
    }
  }

  const secretsDirty = form?.gitToken !== null || form?.llmApiKey !== null;
  const nonSecretsDirty = form && settings && JSON.stringify({
    ...form, gitToken: null, llmApiKey: null, gitTokenSet: null, llmApiKeySet: null,
  }) !== JSON.stringify({
    ...settings, gitToken: null, llmApiKey: null, gitTokenSet: null, llmApiKeySet: null,
  });
  const isDirty = secretsDirty || nonSecretsDirty;

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
            <Field
              label="Personal Access Token"
              hint={form.gitProvider === 'gitlab' ? 'Required scope: api (read_api to fetch MRs, api to publish comments)' : undefined}
            >
              <SecretInput
                value={form.gitToken}
                isSet={form.gitTokenSet}
                onChange={v => set('gitToken', v)}
                placeholder="glpat-…"
              />
            </Field>
            <Field label="Projects to watch" hint="One project path per line (e.g. group/repo)">
              <textarea
                rows={4}
                value={(form.gitProjects ?? []).join('\n')}
                onChange={e => set('gitProjects', e.target.value.split('\n').map(s => s.trim()))}
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
                isSet={form.llmApiKeySet}
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
            <Field label="Max tokens" hint="Maximum tokens in the LLM response (e.g. 16000)">
              <input
                type="number"
                min={1}
                value={form.llmMaxTokens}
                onChange={e => set('llmMaxTokens', Number(e.target.value))}
              />
            </Field>
            <Field label="Thinking budget" hint="Token budget for extended thinking (e.g. 10000); ignored when reasoning is disabled">
              <input
                type="number"
                min={1}
                value={form.llmThinkingBudget}
                onChange={e => set('llmThinkingBudget', Number(e.target.value))}
              />
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

function SecretInput({ value, isSet, onChange, placeholder }) {
  const editing = value !== null || !isSet;
  const [revealed, setRevealed] = useState(false);

  if (!editing) {
    return (
      <div className="secret-input-wrap">
        <span className="secret-set-indicator">••••••••••••</span>
        <button className="btn-ghost btn-sm" type="button" onClick={() => onChange('')}>
          Change
        </button>
      </div>
    );
  }

  return (
    <div className="secret-input-wrap">
      <input
        type={revealed ? 'text' : 'password'}
        value={value ?? ''}
        onChange={e => onChange(e.target.value)}
        placeholder={placeholder}
        autoFocus={isSet}
        autoComplete="off"
      />
      <button className="btn-ghost btn-sm" type="button" onClick={() => setRevealed(r => !r)}>
        {revealed ? 'Hide' : 'Show'}
      </button>
      {isSet && (
        <button className="btn-ghost btn-sm" type="button" onClick={() => { onChange(null); setRevealed(false); }}>
          Cancel
        </button>
      )}
    </div>
  );
}
