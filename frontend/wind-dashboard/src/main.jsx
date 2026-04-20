import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import DashboardTabs from './DashboardTabs.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <DashboardTabs />
  </StrictMode>,
)