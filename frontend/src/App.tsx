import { lazy, Suspense } from 'react'
import { BrowserRouter, Route, Routes, Navigate } from 'react-router-dom'
import AppLayout from './components/layout/AppLayout'
import RouteProtegee from './components/auth/RouteProtegee'
import ErrorBoundary from './components/shared/ErrorBoundary'
import Toaster from './components/shared/Toaster'
import Connexion from './pages/Connexion'
import Inscription from './pages/Inscription'
import Etudes from './pages/Etudes'

// Pages lourdes chargees a la demande (l'Atelier surtout : ~la moitie du bundle).
const Dashboard = lazy(function () { return import('./pages/Dashboard') })
const JournalEtude = lazy(function () { return import('./pages/JournalEtude') })
const MembresEtude = lazy(function () { return import('./pages/MembresEtude') })
const AtelierPage = lazy(function () { return import('./pages/AtelierPage') })
const Rapports = lazy(function () { return import('./pages/Rapports') })
const Parametres = lazy(function () { return import('./pages/Parametres') })
const Bibliotheque = lazy(function () { return import('./pages/Bibliotheque') })

function Chargement() {
  return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <div className="font-mono text-[11px] tracking-wide text-steel-light">CHARGEMENT...</div>
    </div>
  )
}

export default function App() {
  return (
    <ErrorBoundary>
      <Toaster />
      <BrowserRouter>
        <Suspense fallback={<Chargement />}>
          <Routes>
            <Route path="/connexion" element={<Connexion />} />
            <Route path="/inscription" element={<Inscription />} />
            <Route element={<RouteProtegee />}>
              <Route element={<AppLayout />}>
                <Route path="/" element={<Navigate to="/etudes" replace />} />
                <Route path="/etudes" element={<Etudes />} />
                <Route path="/etudes/:etudeId" element={<Dashboard />} />
                <Route path="/etudes/:etudeId/journal" element={<JournalEtude />} />
                <Route path="/etudes/:etudeId/membres" element={<MembresEtude />} />
                <Route path="/etudes/:etudeId/ateliers/:numero" element={<AtelierPage />} />
                <Route path="/bibliotheque" element={<Bibliotheque />} />
                <Route path="/rapports" element={<Rapports />} />
                <Route path="/parametres" element={<Parametres />} />
              </Route>
            </Route>
          </Routes>
        </Suspense>
      </BrowserRouter>
    </ErrorBoundary>
  )
}
