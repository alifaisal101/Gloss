import Spinner from './Spinner.jsx';

const VARIANT_CLASS = {
  primary: 'btn',
  secondary: 'btn btn-secondary',
  publish: 'btn btn-publish',
  ghost: 'btn-ghost',
  danger: 'btn btn-danger-solid',
  dangerGhost: 'btn-ghost btn-danger',
};

export default function Button({
  variant = 'primary',
  size,
  loading = false,
  disabled = false,
  icon: Icon,
  children,
  className = '',
  ...rest
}) {
  const classes = [VARIANT_CLASS[variant] ?? VARIANT_CLASS.primary, size === 'sm' ? 'btn-sm' : '', className]
    .filter(Boolean)
    .join(' ');
  return (
    <button className={classes} disabled={disabled || loading} {...rest}>
      {loading ? <Spinner size={14} /> : Icon && <Icon size={size === 'sm' ? 14 : 16} aria-hidden="true" />}
      {children}
    </button>
  );
}
