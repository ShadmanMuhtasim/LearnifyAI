import type { ButtonHTMLAttributes, HTMLAttributes, ReactNode } from 'react';

type Tone = 'default' | 'primary' | 'success' | 'warning' | 'danger' | 'muted';

interface PageHeaderProps {
  title: string;
  eyebrow?: string;
  subtitle?: string;
  actions?: ReactNode;
}

export function PageHeader({ title, eyebrow, subtitle, actions }: PageHeaderProps) {
  return (
    <div className="page-header">
      <div>
        {eyebrow && <div className="eyebrow">{eyebrow}</div>}
        <h1>{title}</h1>
        {subtitle && <p>{subtitle}</p>}
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </div>
  );
}

export function Card({ className = '', ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={`ui-card ${className}`.trim()} {...props} />;
}

export function SectionPanel({ className = '', ...props }: HTMLAttributes<HTMLElement>) {
  return <section className={`section-panel ${className}`.trim()} {...props} />;
}

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: Tone;
}

export function Badge({ tone = 'default', className = '', ...props }: BadgeProps) {
  return <span className={`ui-badge ui-badge-${tone} ${className}`.trim()} {...props} />;
}

interface AppButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
}

export function AppButton({ variant = 'primary', className = '', ...props }: AppButtonProps) {
  return <button className={`ui-button ui-button-${variant} ${className}`.trim()} {...props} />;
}

interface EmptyStateProps {
  title: string;
  message: string;
  action?: ReactNode;
}

export function EmptyState({ title, message, action }: EmptyStateProps) {
  return (
    <Card className="empty-state">
      <div className="empty-state-icon" aria-hidden="true">+</div>
      <h2>{title}</h2>
      <p>{message}</p>
      {action && <div className="empty-state-action">{action}</div>}
    </Card>
  );
}

export function LoadingState({ label = 'Loading...' }: { label?: string }) {
  return (
    <div className="state-wrap" role="status" aria-live="polite">
      <div className="spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}

export function ErrorState({ message }: { message: string }) {
  return (
    <div className="alert alert-danger" role="alert">
      {message}
    </div>
  );
}

interface StatCardProps {
  label: string;
  value: ReactNode;
  detail?: string;
  tone?: Tone;
}

export function StatCard({ label, value, detail, tone = 'default' }: StatCardProps) {
  return (
    <Card className={`stat-card stat-card-${tone}`}>
      <span className="stat-label">{label}</span>
      <strong>{value}</strong>
      {detail && <span className="stat-detail">{detail}</span>}
    </Card>
  );
}
