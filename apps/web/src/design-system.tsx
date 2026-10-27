import type { ButtonHTMLAttributes, HTMLAttributes, ReactNode } from "react";

type CardVariant = "default" | "elevated" | "highlighted";
type ButtonVariant = "primary" | "secondary" | "ghost";
type BadgeVariant =
  | "neutral"
  | "success"
  | "warning"
  | "error"
  | "info"
  | "partial"
  | "insufficient";
type StatusVariant =
  "error" | "warning" | "information" | "success" | "partial" | "insufficient";
type ContainerSize = "narrow" | "standard" | "wide";

function cx(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

export function Button({
  variant = "primary",
  loading = false,
  disabled,
  type = "button",
  children,
  className,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  loading?: boolean;
}) {
  return (
    <button
      {...props}
      type={type}
      className={cx("ui-button", `ui-button--${variant}`, className)}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
    >
      {loading ? (
        <>
          <span className="ui-spinner" aria-hidden="true" />
          {children}
        </>
      ) : (
        children
      )}
    </button>
  );
}

export function Card({
  variant = "default",
  className,
  ...props
}: HTMLAttributes<HTMLDivElement> & { variant?: CardVariant }) {
  const variantClass =
    variant === "default" ? undefined : `ui-card--${variant}`;
  return <div {...props} className={cx("ui-card", variantClass, className)} />;
}

export function Badge({
  variant = "neutral",
  children,
  className,
  ...props
}: HTMLAttributes<HTMLSpanElement> & { variant?: BadgeVariant }) {
  return (
    <span
      {...props}
      className={cx("ui-badge", `ui-badge--${variant}`, className)}
    >
      {children}
    </span>
  );
}

export function StatCard({
  label,
  value,
  description,
  className,
}: {
  label: ReactNode;
  value: ReactNode;
  description?: ReactNode;
  className?: string;
}) {
  return (
    <dl className={cx("ui-stat-card", className)}>
      <dt className="ui-stat-card__label">{label}</dt>
      <dd className="ui-stat-card__value">{value}</dd>
      {description ? (
        <dd className="ui-stat-card__description">{description}</dd>
      ) : null}
    </dl>
  );
}

export function StatusMessage({
  variant,
  title,
  children,
  className,
  ...props
}: HTMLAttributes<HTMLDivElement> & {
  variant: StatusVariant;
  title: ReactNode;
  children: ReactNode;
}) {
  return (
    <div
      {...props}
      className={cx("ui-status", `ui-status--${variant}`, className)}
      role={variant === "error" ? "alert" : "status"}
    >
      <span className="ui-status__title">{title}</span>
      {children}
    </div>
  );
}

export function SectionHeader({
  title,
  titleId,
  description,
  action,
  className,
}: {
  title: ReactNode;
  titleId?: string;
  description?: ReactNode;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <header className={cx("ui-section-header", className)}>
      <div>
        <h2 id={titleId}>{title}</h2>
        {description ? <p>{description}</p> : null}
      </div>
      {action ? <div>{action}</div> : null}
    </header>
  );
}

export function LoadingState({
  message,
  className,
}: {
  message: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cx("ui-loading", className)}
      role="status"
      aria-live="polite"
    >
      <span className="ui-spinner" aria-hidden="true" />
      <span>{message}</span>
    </div>
  );
}

export function EmptyState({
  title,
  description,
  action,
  className,
}: {
  title: ReactNode;
  description: ReactNode;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cx("ui-empty", className)}>
      <h3>{title}</h3>
      <p>{description}</p>
      {action}
    </div>
  );
}

export function Divider(props: HTMLAttributes<HTMLHRElement>) {
  return <hr {...props} className={cx("ui-divider", props.className)} />;
}

export function Container({
  size = "standard",
  className,
  ...props
}: HTMLAttributes<HTMLDivElement> & { size?: ContainerSize }) {
  const sizeClass = size === "standard" ? undefined : `ui-container--${size}`;
  return (
    <div {...props} className={cx("ui-container", sizeClass, className)} />
  );
}
