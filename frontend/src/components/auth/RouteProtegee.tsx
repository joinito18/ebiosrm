import { Navigate, Outlet } from 'react-router-dom'
import { estConnecte } from '../../lib/api'

export default function RouteProtegee() {
  if (!estConnecte()) {
    return <Navigate to="/connexion" replace />
  }
  return <Outlet />
}
