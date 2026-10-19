// Owns only the current request's cancellation token, never its response data.
export class RequestLifetime {
  private controller: AbortController | null = null;

  begin(parentSignal?: AbortSignal): AbortSignal {
    this.cancel();
    this.controller = new AbortController();
    return parentSignal
      ? AbortSignal.any([this.controller.signal, parentSignal])
      : this.controller.signal;
  }

  cancel(): void {
    this.controller?.abort();
    this.controller = null;
  }
}
