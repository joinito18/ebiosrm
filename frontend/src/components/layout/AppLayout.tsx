import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import Header from './Header'

export default function AppLayout() {
  var [menuMobileOuvert, setMenuMobileOuvert] = useState(false)

  return (
    <div className="flex min-h-screen bg-paper">
      <Sidebar ouvert={menuMobileOuvert} onFermer={function () { setMenuMobileOuvert(false) }} />
      <div className="flex min-w-0 flex-1 flex-col">
        <Header onOuvrirMenu={function () { setMenuMobileOuvert(true) }} />
        <main className="flex-1 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
