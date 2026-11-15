import { Navigate, Route, Routes } from 'react-router'
import { RequireAuth } from './auth/RequireAuth'
import { AppLayout } from './layout/AppLayout'
import { CryptosPage } from './pages/CryptosPage'
import { DepositPage } from './pages/DepositPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { LoginPage } from './pages/LoginPage'
import { MarketPage } from './pages/MarketPage'
import { PriceHistoryPage } from './pages/PriceHistoryPage'
import { RegisterPage } from './pages/RegisterPage'
import { TradePage } from './pages/TradePage'
import { TransactionsPage } from './pages/TransactionsPage'
import { UserPage } from './pages/UserPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route path="/cryptos" element={<CryptosPage />} />
          <Route path="/cryptos/:cryptoId/price-history" element={<PriceHistoryPage />} />
          <Route path="/market" element={<MarketPage />} />
          <Route path="/market/transactions" element={<TransactionsPage />} />
          <Route path="/trade" element={<TradePage />} />
          <Route path="/deposit" element={<DepositPage />} />
          <Route path="/profile" element={<UserPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/cryptos" replace />} />
    </Routes>
  )
}
