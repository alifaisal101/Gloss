import { useState, useEffect } from 'react';
import { toast } from 'sonner';
import { AlertCircle } from 'lucide-react';
import { useConfig, useSaveConfig } from '../api/queries.js';
import Button from '../components/ui/Button.jsx';
import Skeleton from '../components/ui/Skeleton.jsx';

const GIT_PROVIDERS = ['gitlab', 'github'];
const LLM_PROVIDERS = ['anthropic', 'openai', 'ollama'];

// Models are tied to the provider that serves them. Hosted providers get a fixed list; Ollama runs
// arbitrary local models, so it falls back to free text. Keep in sync with LlmProvider.IsValidModel.
const MODELS_BY_PROVIDER = {
  anthropic: ['claude-opus-4-8', 'claude-sonnet-4-6', 'claude-haiku-4-5-20251001'],
  openai: ['gpt-5', 'gpt-4o', 'gpt-4-turbo'],
  ollama: [],
};

// Backend validation codes → the form field they belong to. Ambiguous secret errors fall through
// to a page-level banner instead.
const FIELD_FOR_CODE = {
  'Config.Validation.InvalidGitProvider': 'gitProvider',
  'Config.Validation.InvalidLlmProvider': 'llmProvider',
  'Config.Validation.InvalidLlmModel': 'llmModel',
  'Config.Validation.InvalidGitBaseUrl': 'gitBaseUrl',
};

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
  const { data: serverConfig, isLoading, isError, error: loadError, refetch } = useConfig();
  const saveConfig = useSaveConfig();
  const [settings, setSettings] = useState(null);
  const [form, setForm] = useState(null);
  const [saved, setSaved] = useState(false);
  const [fieldErrors, setFieldErrors] = useState({});
  const [formError, setFormError] = useState(null);

  useEffect(() => {
    if (serverConfig && form === null) {
      const s = normalize(serverConfig);
      setSettings(s);
      setForm(s);
    }
  }, [serverConfig, form]);

  function set(key, value) {
    setForm((f) => ({ ...f, [key]: value }));
    setSaved(false);
    setFormError(null);
    setFieldErrors((e) => {
      if (!e[key]) return e;
      const next = { ...e };
      delete next[key];
      return next;
    });
  }

  function setLlmProvider(provider) {
    const models = MODELS_BY_PROVIDER[provider] ?? [];
    setForm((f) => ({ ...f, llmProvider: provider, llmModel: models[0] ?? '' }));
    setSaved(false);
    setFormError(null);
    setFieldErrors({});
  }

  function handleSave() {
    setFieldErrors({});
    setFormError(null);
    saveConfig.mutate(
      {
        ...form,
        gitToken: form.gitToken || null,
        llmApiKey: form.llmApiKey || null,
        gitProjects: form.gitProjects.filter(Boolean),
      },
      {
        onSuccess: async () => {
          toast.success('Settings saved');
          const r = await refetch();
          if (r.data) {
            const s = normalize(r.data);
            setSettings(s);
            setForm(s);
          }
          setSaved(true);
        },
        onError: (err) => {
          const field = FIELD_FOR_CODE[err.code];
          if (field) setFieldErrors({ [field]: err.message });
          else setFormError(err.message);
          toast.error('Could not save settings', { description: err.message });
        },
      },
    );
  }

  if (isLoading || !form) {
    if (isError) {
      return (
        <div className="page">
          <div className="banner banner-error">
            <AlertCircle size={16} aria-hidden="true" />
            <span>{loadError.message}</span>
            <Button variant="ghost" size="sm" onClick={() => refetch()}>Retry</Button>
          </div>
        </div>
      );
    }
    return <SettingsSkeleton />;
  }

  const secretsDirty = form.gitToken !== null || form.llmApiKey !== null;
  const nonSecretsDirty = settings && JSON.stringify({
    ...form, gitToken: null, llmApiKey: null, gitTokenSet: null, llmApiKeySet: null,
  }) !== JSON.stringify({
    ...settings, gitToken: null, llmApiKey: null, gitTokenSet: null, llmApiKeySet: null,
  });
  const isDirty = secretsDirty || nonSecretsDirty;
  const modelOptions = MODELS_BY_PROVIDER[form.llmProvider] ?? [];

  return (
    <div className="page">
      <div className="page-header">
        <h1>Settings</h1>
        <div className="page-actions">
          {saved && !isDirty && <span className="saved-badge">Saved ✓</span>}
          <Button onClick={handleSave} loading={saveConfig.isPending} disabled={saveConfig.isPending || !isDirty}>
            Save Changes
          </Button>
        </div>
      </div>

      {formError && (
        <div className="banner banner-error">
          <AlertCircle size={16} aria-hidden="true" />
          <span>{formError}</span>
        </div>
      )}

      <div className="settings-sections">
        <section className="settings-section">
          <h2>Git Provider</h2>
          <div className="field-group">
            <Field label="Provider" error={fieldErrors.gitProvider}>
              <select value={form.gitProvider} onChange={(e) => set('gitProvider', e.target.value)}>
                {GIT_PROVIDERS.map((p) => <option key={p} value={p}>{p}</option>)}
              </select>
            </Field>
            <Field label="Base URL" hint="e.g. https://gitlab.example.com" error={fieldErrors.gitBaseUrl}>
              <input
                type="text"
                value={form.gitBaseUrl}
                onChange={(e) => set('gitBaseUrl', e.target.value)}
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
                onChange={(v) => set('gitToken', v)}
                placeholder="glpat-…"
              />
            </Field>
            <Field label="Projects to watch" hint="One project path per line (e.g. group/repo)">
              <textarea
                rows={4}
                value={(form.gitProjects ?? []).join('\n')}
                onChange={(e) => set('gitProjects', e.target.value.split('\n').map((s) => s.trim()))}
                placeholder={'group/project-one\ngroup/project-two'}
              />
            </Field>
          </div>
        </section>

        <section className="settings-section">
          <h2>LLM Provider</h2>
          <div className="field-group">
            <Field label="Provider" error={fieldErrors.llmProvider}>
              <select value={form.llmProvider} onChange={(e) => setLlmProvider(e.target.value)}>
                {LLM_PROVIDERS.map((p) => <option key={p} value={p}>{p}</option>)}
              </select>
            </Field>
            <Field label="API Key">
              <SecretInput
                value={form.llmApiKey}
                isSet={form.llmApiKeySet}
                onChange={(v) => set('llmApiKey', v)}
                placeholder="sk-ant-…"
              />
            </Field>
            <Field
              label="Model"
              error={fieldErrors.llmModel}
              hint={modelOptions.length > 0
                ? 'Models served by the selected provider'
                : 'Local model name as served by Ollama (e.g. llama3.1)'}
            >
              {modelOptions.length > 0 ? (
                <select value={form.llmModel} onChange={(e) => set('llmModel', e.target.value)}>
                  {!modelOptions.includes(form.llmModel) && (
                    <option value={form.llmModel}>
                      {form.llmModel ? `${form.llmModel} (unrecognized)` : 'Select a model…'}
                    </option>
                  )}
                  {modelOptions.map((m) => <option key={m} value={m}>{m}</option>)}
                </select>
              ) : (
                <input
                  type="text"
                  value={form.llmModel}
                  onChange={(e) => set('llmModel', e.target.value)}
                  placeholder="llama3.1"
                />
              )}
            </Field>
            <Field label="Enable reasoning">
              <label className="toggle">
                <input
                  type="checkbox"
                  checked={form.llmReasoningEnabled ?? true}
                  onChange={(e) => set('llmReasoningEnabled', e.target.checked)}
                />
                <span>Include reasoning trace in draft comments</span>
              </label>
            </Field>
            <Field label="Max tokens" hint="Maximum tokens in the LLM response (e.g. 16000)">
              <input
                type="number"
                min={1}
                value={form.llmMaxTokens}
                onChange={(e) => set('llmMaxTokens', Number(e.target.value))}
              />
            </Field>
            <Field label="Thinking budget" hint="Token budget for extended thinking (e.g. 10000); ignored when reasoning is disabled">
              <input
                type="number"
                min={1}
                value={form.llmThinkingBudget}
                onChange={(e) => set('llmThinkingBudget', Number(e.target.value))}
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
                onChange={(e) => set('defaultPollCron', e.target.value)}
                placeholder="0 */2 * * * ?"
              />
            </Field>
          </div>
        </section>
      </div>
    </div>
  );
}

function Field({ label, hint, error, children }) {
  return (
    <div className={`field${error ? ' field--error' : ''}`}>
      <label className="field-label">{label}</label>
      {children}
      {error ? <span className="field-error">{error}</span> : hint && <span className="field-hint">{hint}</span>}
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
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        autoFocus={isSet}
        autoComplete="off"
      />
      <button className="btn-ghost btn-sm" type="button" onClick={() => setRevealed((r) => !r)}>
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

function SettingsSkeleton() {
  return (
    <div className="page">
      <div className="page-header"><Skeleton width={120} height={26} /></div>
      <div className="settings-sections">
        {[0, 1, 2].map((s) => (
          <section className="settings-section" key={s}>
            <Skeleton width={140} height={18} />
            <div className="field-group" style={{ marginTop: 16 }}>
              {[0, 1].map((f) => (
                <div className="field" key={f}>
                  <Skeleton width={100} height={13} />
                  <Skeleton width="100%" height={34} radius={6} style={{ marginTop: 6 }} />
                </div>
              ))}
            </div>
          </section>
        ))}
      </div>
    </div>
  );
}
