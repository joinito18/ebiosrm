import { BrowserRouter, Route, Routes, Navigate } from 'react-router-dom'
import AppLayout from './components/layout/AppLayout'
import RouteProtegee from './components/auth/RouteProtegee'
import Connexion from './pages/Connexion'
import Inscription from './pages/Inscription'
import MotDePasseOublie from './pages/MotDePasseOublie'
import ReinitialiserMotDePasse from './pages/ReinitialiserMotDePasse'
import Dashboard from './pages/Dashboard'
import Etudes from './pages/Etudes'
import AtelierPage from './pages/AtelierPage'
import Rapports from './pages/Rapports'
import Parametres from './pages/Parametres'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/connexion" element={<Connexion />} />
        <Route path="/inscription" element={<Inscription />} />
        <Route path="/mot-de-passe-oublie" element={<MotDePasseOublie />} />
        <Route path="/reinitialiser-mot-de-passe" element={<ReinitialiserMotDePasse />} />
        <Route element={<RouteProtegee />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/etudes" replace />} />
            <Route path="/etudes" element={<Etudes />} />
            <Route path="/etudes/:etudeId" element={<Dashboard />} />
            <Route path="/etudes/:etudeId/ateliers/:numero" element={<AtelierPage />} />
            <Route path="/rapports" element={<Rapports />} />
            <Route path="/parametres" element={<Parametres />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
