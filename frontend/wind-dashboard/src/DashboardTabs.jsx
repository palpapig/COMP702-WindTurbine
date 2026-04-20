import { useState } from 'react'
import Table from './Table.jsx'
import Turbine from './Turbine.jsx'
import TurbineDetail from './TurbineDetail.jsx'
import './DashboardTabs.css'

function DashboardTabs() {
  const [activeTab, setActiveTab] = useState('turbineData')
  const [selectedTurbineId, setSelectedTurbineId] = useState(null)

  const handleOpenTurbineDetail = (turbineId) => {
    setSelectedTurbineId(turbineId)
  }

  const handleBackToTurbines = () => {
    setSelectedTurbineId(null)
  }

  return (
    <div className="dashboard-shell">
      <main className="dashboard-content">
        {activeTab === 'turbineData' && <Table />}

        {activeTab === 'turbine' && !selectedTurbineId && (
          <Turbine onSelectTurbine={handleOpenTurbineDetail} />
        )}

        {activeTab === 'turbine' && selectedTurbineId && (
          <TurbineDetail
            turbineId={selectedTurbineId}
            onBack={handleBackToTurbines}
          />
        )}
      </main>

      <nav className="bottom-tab-bar">
        <button
          className={activeTab === 'turbineData' ? 'tab-btn active' : 'tab-btn'}
          onClick={() => {
            setActiveTab('turbineData')
            setSelectedTurbineId(null)
          }}
        >
          TurbineData
        </button>

        <button
          className={activeTab === 'turbine' ? 'tab-btn active' : 'tab-btn'}
          onClick={() => setActiveTab('turbine')}
        >
          Turbine
        </button>
      </nav>
    </div>
  )
}

export default DashboardTabs