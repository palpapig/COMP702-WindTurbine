import { useState } from 'react'
import { FiActivity, FiDatabase, FiTrendingUp, FiAlertTriangle } from 'react-icons/fi'
import MostRecentTable from '../components/MostRecentTable'
import FullHistoryTable from '../components/Table'
import FailureDetectiontTable from '../components/FailureDetectionTable'

function TablesPage() {
  const [tableView, setTableView] = useState('telemetry')
  const [showFullHistory, setShowFullHistory] = useState(false)

  const isTelemetry = tableView === 'telemetry'

  return (
    <section className="dashboard-section">
      <div className="section-title">
        {isTelemetry ? (
          <FiActivity color="#82C340" size={24} />
        ) : (
          <FiAlertTriangle color="#82C340" size={24} />
        )}

        <h2>
          {isTelemetry
            ? showFullHistory
              ? 'Complete Telemetry History'
              : 'Current Turbine Status – Most Recent'
            : 'Failure Detection Results – Most Recent'}
        </h2>
      </div>

      <div className="graph-tabs" style={{ marginBottom: '1rem' }}>
        <button
          className={tableView === 'telemetry' ? 'graph-tab active' : 'graph-tab'}
          onClick={() => setTableView('telemetry')}
        >
          Telemetry
        </button>

        <button
          className={tableView === 'failureDetection' ? 'graph-tab active' : 'graph-tab'}
          onClick={() => setTableView('failureDetection')}
        >
          Failure Detection
        </button>
      </div>

      {isTelemetry ? (
        <>
          {!showFullHistory && (
            <p style={{ fontSize: '0.9rem', color: '#2c3e50', marginBottom: '1rem' }}>
              <strong>Latest telemetry per turbine:</strong> This table shows the single most recent SCADA record for each turbine
              (newest timestamp per Turbine ID). Use this to quickly check each turbine's current efficiency benchmark, fault status,
              power output, and other live metrics.
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
        </>
      ) : (
        <>
          <p style={{ fontSize: '0.9rem', color: '#2c3e50', marginBottom: '1rem' }}>
            <strong>Latest failure detection result per turbine:</strong> This table shows the newest row from the
            FailureDetectionResult table for each turbine, including residual, EWMA, control limits, alarm level,
            and A1/A2 trigger status.
          </p>

          <FailureDetectiontTable />
        </>
      )}
    </section>
  )
}

export default TablesPage