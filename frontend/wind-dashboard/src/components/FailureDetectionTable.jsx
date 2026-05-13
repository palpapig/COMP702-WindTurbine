import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import { FaExclamationTriangle, FaCheckCircle } from 'react-icons/fa'
import './Table.css'

function formatNumber(value, decimals = 2) {
  return typeof value === 'number' ? value.toFixed(decimals) : '—'
}

function formatBoolean(value) {
  if (value === true) return 'TRUE'
  if (value === false) return 'FALSE'
  return '—'
}

function FailureDetectionMostRecentTable() {
  const [recentResults, setRecentResults] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    async function fetchLatestFailureResults() {
      setLoading(true)
      setError('')

      const { data, error: fetchError } = await supabase
        .from('FailureDetectionResult')
        .select('*')
        .order('Timestamp', { ascending: false })

      if (fetchError) {
        setError(fetchError.message)
        setLoading(false)
        return
      }

      if (!data || data.length === 0) {
        setRecentResults([])
        setLoading(false)
        return
      }

      const latestMap = new Map()

      for (const row of data) {
        if (!latestMap.has(row.TurbineId)) {
          latestMap.set(row.TurbineId, row)
        }
      }

      const latestRows = Array.from(latestMap.values())
      latestRows.sort((a, b) => a.TurbineId.localeCompare(b.TurbineId))

      setRecentResults(latestRows)
      setLoading(false)
    }

    fetchLatestFailureResults()
  }, [])

  if (loading) return <p>loading most recent failure detection data...</p>
  if (error) return <p className="error-text">error: {error}</p>
  if (recentResults.length === 0) return <p>no failure detection data found.</p>

  return (
    <div className="table-page">
      <h1>Failure Detection Results</h1>

      <div className="table-container">
        <table className="telemetry-table failure-detection-table">
          <thead>
            <tr>
              <th>Turbine ID</th>
              <th>Timestamp (utc)</th>
              <th>Residual</th>
              <th>Predicted Value</th>
              <th>Actual Value</th>
              <th>LCL</th>
              <th>UCL</th>
              <th>EWMA</th>
              <th>Alarm Level</th>
              <th>A1 Triggered</th>
              <th>A2 Triggered</th>
              <th>Fault Status</th>
            </tr>
          </thead>

          <tbody>
            {recentResults.map(row => {
              const hasFault =
                row.IsAbnormal ||
                row.A1Triggered ||
                row.A2Triggered ||
                row.AlarmLvl > 0

              return (
                <tr key={row.TurbineId + row.Timestamp}>
                  <td>{row.TurbineId}</td>
                  <td>{row.Timestamp ? new Date(row.Timestamp).toLocaleString() : '—'}</td>
                  <td>{formatNumber(row.Residual, 3)}</td>
                  <td>{formatNumber(row.PredictedValue, 2)}</td>
                  <td>{formatNumber(row.ActualValue, 2)}</td>
                  <td>{formatNumber(row.LCL, 2)}</td>
                  <td>{formatNumber(row.UCL, 2)}</td>
                  <td>{formatNumber(row.EWMA, 3)}</td>

                  <td
                    style={{
                      backgroundColor: (row.AlarmLvl ?? 0) > 0 ? '#ffdddd' : 'transparent'
                    }}
                  >
                    {row.AlarmLvl ?? '—'}
                  </td>

                  <td>{formatBoolean(row.A1Triggered)}</td>
                  <td>{formatBoolean(row.A2Triggered)}</td>

                  <td>
                    {hasFault ? (
                      <span style={{ color: '#d9534f', display: 'flex', alignItems: 'center', gap: '6px' }}>
                        <FaExclamationTriangle /> Abnormal
                      </span>
                    ) : (
                      <span style={{ color: '#82C340', display: 'flex', alignItems: 'center', gap: '6px' }}>
                        <FaCheckCircle /> Normal
                      </span>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export default FailureDetectionMostRecentTable