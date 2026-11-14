import { ApiError } from '../api/client'

export const errorMessage = (error: unknown) =>
  error instanceof ApiError ? error.message : 'Cannot reach the server.'
