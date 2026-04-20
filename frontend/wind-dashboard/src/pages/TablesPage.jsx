import { useState } from 'react'
import { FiActivity, FiDatabase, FiTrendingUp } from 'react-icons/fi'
import MostRecentTable from '../components/MostRecentTable'
import FullHistoryTable from '../components/Table'

function TablesPage() {
  const [showFullHistory, setShowFullHistory] = useState(false)

  return (
    <section className="dashboard-section">
      <div className="section-title">
        <FiActivity color="#82C340" size={24} />
        <h2>{showFullHistory ? 'Complete Telemetry History' : 'Current Turbine Status – Most Recent'}</h2>
      </div>
      {!showFullHistory && (
        <p style={{ fontSize: '0.9rem', color: '#2c3e50', marginBottom: '1rem' }}>
          <strong>Latest telemetry per turbine:</strong> This table shows the single most recent SCADA record for each turbine (newest timestamp per Turbine ID). Use this to quickly check each turbine's current efficiency benchmark, fault status, power output, and other live metrics.
        </p>
      )}
      <button
        onClick={() => setShowFullHistory(!showFullHistory)}
        className="toggle-history-btn"
        style={{ marginBottom: '1.5rem' }}
      >
        {showFullHistory ? <FiTrendingUp /> : <FiDatabase />}
        {showFullHistory ? ' Show Most Recent Data' : ' Show Full History (All Rows)'}
      </button>
      {showFullHistory ? <FullHistoryTable /> : <MostRecentTable />}
    </section>
  )
}

export default TablesPage