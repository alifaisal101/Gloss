import { Component } from 'react';

export default class ErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error) {
    return { error };
  }

  render() {
    if (!this.state.error) return this.props.children;
    return (
      <div className="error-boundary">
        <div className="error-boundary-icon">!</div>
        <h1>Something went wrong</h1>
        <p className="muted">{this.state.error.message || 'An unexpected error occurred while rendering this page.'}</p>
        <div className="error-boundary-actions">
          <button className="btn" onClick={() => window.location.reload()}>Reload page</button>
        </div>
      </div>
    );
  }
}
