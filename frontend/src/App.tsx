import { Navigate, Route, Routes } from 'react-router'
import { RequireAuth } from './auth/RequireAuth'
import { AppLayout } from './layout/AppLayout'
import { CryptosPage } from './pages/CryptosPage'
import { LoginPage } from './pages/LoginPage'
import { PriceHistoryPage } from './pages/PriceHistoryPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route path="/cryptos" element={<CryptosPage />} />
          <Route path="/cryptos/:cryptoId/price-history" element={<PriceHistoryPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/cryptos" replace />} />
    </Routes>
  )
}
