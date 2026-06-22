export interface ParsedApiError {
  displayKey: string;
  errorMessage?: string;
}

interface ServiceErrorBody {
  errorKey?: string;
  errorMessage?: string;
}

interface AuthErrorBody {
  error?: string;
}

export function parseApiErrors(body: unknown, status = 0): ParsedApiError[] {
  const parsed = coerceBody(body);
  if (!parsed) {
    return [{ displayKey: fallbackKey(status) }];
  }

  if (Array.isArray(parsed)) {
    const items = parsed
      .map((item) => mapItem(item, status))
      .filter((item): item is ParsedApiError => item !== null);

    return items.length > 0 ? items : [{ displayKey: fallbackKey(status) }];
  }

  const single = mapItem(parsed, status);
  return single ? [single] : [{ displayKey: fallbackKey(status) }];
}

function coerceBody(body: unknown): unknown | null {
  if (body === null || body === undefined || body === '') {
    return null;
  }

  if (typeof body === 'string') {
    try {
      return JSON.parse(body) as unknown;
    } catch {
      return { error: body };
    }
  }

  return body;
}

function mapItem(item: unknown, status: number): ParsedApiError | null {
  if (!item || typeof item !== 'object') {
    return null;
  }

  const serviceError = item as ServiceErrorBody;
  if (serviceError.errorKey) {
    return {
      displayKey: serviceError.errorKey,
      errorMessage: serviceError.errorMessage,
    };
  }

  const authError = item as AuthErrorBody;
  if (authError.error) {
    return {
      displayKey: authError.error,
      errorMessage: authError.error,
    };
  }

  if (serviceError.errorMessage) {
    return {
      displayKey: serviceError.errorMessage,
      errorMessage: serviceError.errorMessage,
    };
  }

  return { displayKey: fallbackKey(status) };
}

function fallbackKey(status: number): string {
  return status > 0 ? `http_${status}` : 'unknown_error';
}
