import { useState, type SubmitEvent } from 'react'
import { ApiError, login } from './api/login'

export default function App() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      const result = await login(email, password)
      setMessage(`Logged in as ${result.user.email}`)
    } catch (error) {
      setMessage(
        error instanceof ApiError
          ? `Backend answered ${error.status}: ${error.message}`
          : 'Cannot reach backend. Is it running on port 5185?',
      )
    }
  }

  return (
    <main>
      <h1>CryptoSimulator</h1>
      <form onSubmit={handleSubmit}>
        <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        <input type="password" placeholder="Password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        <button type="submit">Log in</button>
      </form>
      {message && <p>{message}</p>}
    </main>
  )
}
