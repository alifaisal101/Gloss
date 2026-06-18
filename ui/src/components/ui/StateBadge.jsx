export default function StateBadge({ state }) {
  if (!state) return null;
  return <span className={`state-badge state-${state.toLowerCase()}`}>{state}</span>;
}
