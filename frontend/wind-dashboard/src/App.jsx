/*
this is the main dashboard component that ties everything together.
it manages the state for toggling between the most‑recent & full‑history tables,
and includes the efficiency graph & the power curve graph.
i used react‑icons for a clean, professional look with the client's brand colours.
the explanation paragraph under the table heading was added to help non‑technical
users understand what "most recent" means. 
*/

import { useState } from 'react'
import { WiDayWindy, WiStrongWind } from 'react-icons/wi'
import { FiActivity, FiDatabase, FiTrendingUp } from 'react-icons/fi'
import MostRecentTable from './MostRecentTable'
import EfficiencyGraph from './EfficiencyGraph'
import FullHistoryTable from './Table'
import './App.css'
import PowerCurveGraph from './PowerCurveGraph'

function App() {
  //state to determine whether we show the full history table or the most recent one
  const [showFullHistory, setShowFullHistory] = useState(false)

  return (
    <div className="dashboard-container">
      <header className="dashboard-header">
        <div className="header-icon">
          <WiDayWindy size={48} color="#06A2DF" />
        </div>
        <div className="header-text">
          <h1>Wind Turbine Monitoring Dashboard</h1>
          <p>Live benchmark & fault status | historical efficiency trends</p>
        </div>
        {/* button toggles between showing most recent & full history */}
        <button
          onClick={() => setShowFullHistory(!showFullHistory)}
          className="toggle-history-btn"
        >
          {showFullHistory ? <FiDatabase /> : <FiTrendingUp />}
          {showFullHistory ? ' Show Most Recent Data' : ' Show Full History (All Rows)'}
        </button>
      </header>

      {/* section that contains either the most recent table or the full history table */}
      <section className="dashboard-section">
        <div className="section-title">
          <FiActivity color="#82C340" size={24} />
          <h2>{showFullHistory ? 'Complete Telemetry History' : 'Current Turbine Status – Most Recent'}</h2>
        </div>
        {/* added a plain‑english explanation of how "most recent" is determined */}
        <p style={{ fontSize: '0.9rem', color: '#2c3e50', marginBottom: '1rem' }}>
          <strong>Most recent data per turbine:</strong> For each turbine, this table shows the very latest measurement – the newest timestamp available in the database.
          It automatically updates as new data arrives, giving a live snapshot of every turbine's current status.
        </p>
        {showFullHistory ? <FullHistoryTable /> : <MostRecentTable />}
      </section>

      {/* efficiency graph section */}
      <section className="dashboard-section">
        <div className="section-title">
          <WiStrongWind color="#06A2DF" size={24} />
          <h2>Efficiency Over Time</h2>
        </div>
        <div className="graph-wrapper">
          <EfficiencyGraph />
        </div>
      </section>

      {/* power curve graph section – added as an extra diagnostic tool */}
      <section className="dashboard-section">
        <div className="section-title">
          <WiStrongWind color="#06A2DF" size={24} />
          <h2>Power Curve Analysis</h2>
        </div>
        <PowerCurveGraph />
      </section>
    </div>
  )
}

export default App