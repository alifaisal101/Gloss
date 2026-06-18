export default function EmptyState({ icon: Icon, title, description, action }) {
  return (
    <div className="empty empty-page">
      {Icon && <Icon className="empty-icon" size={40} strokeWidth={1.5} aria-hidden="true" />}
      {title && <h2>{title}</h2>}
      {description && <p className="muted">{description}</p>}
      {action && <div className="empty-action">{action}</div>}
    </div>
  );
}
