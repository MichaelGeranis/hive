/**
 * The message the API sent with a failed request (`{ message }` in the response body), or
 * undefined when there is none: the request never reached the API, or it answered without one.
 */
export function apiErrorMessage(error: unknown): string | undefined {
  if (error && typeof error === 'object' && 'response' in error) {
    const response = (error as { response?: { data?: { message?: unknown } } }).response
    const message = response?.data?.message
    if (typeof message === 'string' && message.length > 0) {
      return message
    }
  }
  return undefined
}

/**
 * What to tell the manager about a failed request: the API's own message when it sent one.
 */
export function getErrorMessage(error: unknown): string {
  return apiErrorMessage(error) ?? 'An unexpected error occurred'
}
