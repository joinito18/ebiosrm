import { lazy, Suspense } from 'react'
import { BrowserRouter, Route, Routes, Navigate } from 'react-router-dom'
import AppLayout from './components/layout/AppLayout'
import RouteProtegee from './components/auth/RouteProtegee'
import ErrorBoundary from './components/shared/ErrorBoundary'
import Toaster from './components/shared/Toaster'
import { ProviderLangue } from './lib/i18n'
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
const ConformiteEtude = lazy(function () { return import('./pages/ConformiteEtude') })
const Portefeuille = lazy(function () { return import('./pages/Portefeuille') })
const SuiviEtude = lazy(function () { return import('./pages/SuiviEtude') })
const Aide = lazy(function () { return import('./pages/Aide') })
const Conditions = lazy(function () { return import('./pages/Conditions') })

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
      <ProviderLangue>
      <Toaster />
      <BrowserRouter>
        <Suspense fallback={<Chargement />}>
          <Routes>
            <Route path="/connexion" element={<Connexion />} />
            <Route path="/inscription" element={<Inscription />} />
            <Route path="/conditions" element={<Conditions />} />
            <Route element={<RouteProtegee />}>
              <Route element={<AppLayout />}>
                <Route path="/" element={<Navigate to="/etudes" replace />} />
                <Route path="/etudes" element={<Etudes />} />
                <Route path="/etudes/:etudeId" element={<Dashboard />} />
                <Route path="/etudes/:etudeId/journal" element={<JournalEtude />} />
                <Route path="/etudes/:etudeId/membres" element={<MembresEtude />} />
                <Route path="/etudes/:etudeId/conformite" element={<ConformiteEtude />} />
                <Route path="/etudes/:etudeId/suivi" element={<SuiviEtude />} />
                <Route path="/portefeuille" element={<Portefeuille />} />
                <Route path="/etudes/:etudeId/ateliers/:numero" element={<AtelierPage />} />
                <Route path="/bibliotheque" element={<Bibliotheque />} />
                <Route path="/aide" element={<Aide />} />
                <Route path="/aide/:slug" element={<Aide />} />
                <Route path="/rapports" element={<Rapports />} />
                <Route path="/parametres" element={<Parametres />} />
              </Route>
            </Route>
          </Routes>
        </Suspense>
      </BrowserRouter>
      </ProviderLangue>
    </ErrorBoundary>
  )
}
