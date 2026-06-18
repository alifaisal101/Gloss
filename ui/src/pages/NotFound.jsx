import { Link } from 'react-router-dom';

export default function NotFound() {
  return (
    <div className="page">
      <div className="empty empty-page">
        <div className="empty-code">404</div>
        <h1>Page not found</h1>
        <p className="muted">The page you’re looking for doesn’t exist or has moved.</p>
        <Link className="btn" to="/">Back to Merge Requests</Link>
      </div>
    </div>
  );
}
