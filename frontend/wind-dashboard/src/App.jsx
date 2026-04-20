/*
this is the main dashboard component that ties everything together.
it manages the state for toggling between the most‑recent & full‑history tables,
and includes the efficiency graph & the power curve graph.
i used react‑icons for a clean, professional look with the client's brand colours.
the explanation paragraph under the table heading was added to help non‑technical
users understand what "most recent" means. 
*/
import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import { FiBarChart2, FiActivity, FiSettings, FiDownload } from 'react-icons/fi'
import DashboardPage from './pages/DashboardPage'
import TablesPage from './pages/TablesPage'
import ConfigPage from './pages/ConfigPage'
import ExportPage from './pages/ExportPage'
import './App.css'
import './Navigation.css'
import logo from './assets/WindTurbineLogo.png'

function App() {
  return (
    <BrowserRouter>
      <div className="dashboard-container">
        <header className="dashboard-header">
          <div className="header-logo-text">
            <img src={logo} alt="Wind Turbine Logo"/>
            <div className="header-text">
              <h1>Wind Turbine Monitoring Dashboard</h1>
              <p>Live benchmark & fault status | historical efficiency trends</p>
            </div>
          </div>
          <nav className="nav-bar">
            <NavLink to="/" className={({ isActive }) => "nav-link" + (isActive ? " active" : "")}>
              <FiBarChart2 /> Dashboard
            </NavLink>
            <NavLink to="/tables" className={({ isActive }) => "nav-link" + (isActive ? " active" : "")}>
              <FiActivity /> Tables
            </NavLink>
            <NavLink to="/config" className={({ isActive }) => "nav-link" + (isActive ? " active" : "")}>
              <FiSettings /> Config
            </NavLink>
            <NavLink to="/export" className={({ isActive }) => "nav-link" + (isActive ? " active" : "")}>
              <FiDownload /> Export
            </NavLink>
          </nav>
        </header>

        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/tables" element={<TablesPage />} />
          <Route path="/config" element={<ConfigPage />} />
          <Route path="/export" element={<ExportPage />} />
        </Routes>
      </div>
    </BrowserRouter>
  )
}

export default App