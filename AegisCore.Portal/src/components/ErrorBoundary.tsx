"use client";

import type { ErrorInfo, ReactNode } from "react";
import { Component } from "react";
import { ErrorDisplay, toWizardError } from "@/components/ErrorDisplay";

type Props = { children: ReactNode; fallback?: ReactNode };
type State = { error: Error | null };

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State { return { error }; }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("AegisCore page error", error, info.componentStack);
  }

  private reset = () => this.setState({ error: null });

  render() {
    if (!this.state.error) return this.props.children;
    if (this.props.fallback) return this.props.fallback;
    return <main className="error-page"><ErrorDisplay error={toWizardError(this.state.error)} onRetry={this.reset} /></main>;
  }
}
