export interface LoginUser {
  email: string
}

export interface LoginResponse {
  token: string
  user: LoginUser
}

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  const response = await fetch('/api/users/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })

  if (!response.ok) {
    throw new ApiError(response.status, await response.text())
  }

  return (await response.json()) as LoginResponse
}
