import { useState } from 'react'
import { FiActivity, FiDatabase, FiTrendingUp, FiBell } from 'react-icons/fi'
import MostRecentTable from '../components/MostRecentTable'
import FullHistoryTable from '../components/Table'
import { Link, useNavigate } from 'react-router-dom';

function TablesPage() {
  const [showFullHistory, setShowFullHistory] = useState(false)
  const navigate = useNavigate();

  return (
    <section className="dashboard-section">
      <div className="section-title">
        <FiActivity color="#82C340" size={24} /> 
        <h2>{showFullHistory ? 'Complete Telemetry History' : 'Current Turbine Status – Most Recent'}</h2>
        <button
        onClick={() => navigate('/alerts')}
        style={{
          marginLeft: 'auto',
          backgroundColor: '#d9534f',
          color: 'white',
          border: 'none',
          padding: '8px 16px',
          borderRadius: '6px',
          cursor: 'pointer',
          fontWeight: 'bold',
          fontSize: '1rem',
          display: 'flex',
          alignItems: 'center',
          gap: '10px'
          }}
          >
          <FiBell /> Alerts
        </button>
      </div>
      {!showFullHistory && (
        <p style={{ fontSize: '0.9rem', color: '#2c3e50', marginBottom: '1rem', textAlign: 'left', width: '100%' }}>
          <strong>Latest telemetry per turbine:</strong> This table shows the single most recent SCADA record for each turbine (newest timestamp per Turbine ID). Use this to quickly check each turbine's current efficiency benchmark, fault status, power output, and other live metrics.
        </p>
      )}      
      <button
        onClick={() => setShowFullHistory(!showFullHistory)}
        className="toggle-history-btn"
        style={{ marginBottom: '1.5rem', display: 'block', textAlign: 'left', width: 'auto'  }}
      >
        {showFullHistory ? <FiTrendingUp /> : <FiDatabase />}
        {showFullHistory ? ' Show Most Recent Data' : ' Show Full History (All Rows)'}
      </button>
      {showFullHistory ? <FullHistoryTable /> : <MostRecentTable />}
    </section>
  )
}

export default TablesPage