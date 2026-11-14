export function makeJwt(expSeconds: number, extraClaims: Record<string, unknown> = {}): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_')
  return `${encode({ alg: 'HS256' })}.${encode({ exp: expSeconds, ...extraClaims })}.sig`
}
