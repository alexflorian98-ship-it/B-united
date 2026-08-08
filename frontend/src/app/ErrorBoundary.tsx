import { Component, type ErrorInfo, type ReactNode } from "react";
import { withTranslation, type WithTranslation } from "react-i18next";
import { Alert } from "../shared/design-system/Alert";
import { Button } from "../shared/design-system/Button";

interface ErrorBoundaryProps extends WithTranslation {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
}

/**
 * Application-level crash guard. Never renders the caught error's message (stack traces/raw
 * exception text must never reach the client, docs/DEVELOPMENT_INSTRUCTIONS.md §6) — only a
 * localized, generic recovery message and a reload action.
 */
class ErrorBoundaryImpl extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // eslint-disable-next-line no-console
    console.error("Unhandled render error", error, info.componentStack);
  }

  handleReload = () => {
    window.location.assign("/");
  };

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    const { t } = this.props;

    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <div className="w-full max-w-md">
          <Alert tone="danger" title={t("common:errors.internalServerError")}>
            <Button variant="primary" className="mt-3" onClick={this.handleReload}>
              {t("common:actions.retry")}
            </Button>
          </Alert>
        </div>
      </div>
    );
  }
}

export const ErrorBoundary = withTranslation()(ErrorBoundaryImpl);
