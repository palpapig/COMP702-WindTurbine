/*  MostRecentTable
this component displays the latest telemetry record for each turbine.
it works by fetching all rows from supabase, ordering them by timestamp descending,
then using a map to keep only the first (newest) row for each turbine id.
chose this approach because it's simple and runs entirely in the browser.
the table shows key metrics like wind speed, rotor speed, power output,
efficiency (benchmark) & fault status with coloured icons. */

import { useState, useEffect } from 'react'
import { supabase } from './utils/supabase'
import { FaExclamationTriangle, FaCheckCircle } from 'react-icons/fa' 
import './Table.css'   //reuse the same styling as the full history table

function MostRecentTable() {
  const [recentData, setRecentData] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    async function fetchLatestPerTurbine() {
      setLoading(true)
      setError('')

      //fetch all telemetry rows, sorted with the newest first
      const { data, error: fetchError } = await supabase
        .from('TurbineData')
        .select('*')
        .order('Timestamp', { ascending: false })

      if (fetchError) {
        setError(fetchError.message)
        setLoading(false)
        return
      }

      if (!data || data.length === 0) {
        setRecentData([])
        setLoading(false)
        return
      }

      //manually keep the first occurrence of each turbine id (which will be the newest because of the sort)
      const latestMap = new Map()
      for (const row of data) {
        if (!latestMap.has(row.TurbineId)) {
          latestMap.set(row.TurbineId, row)
        }
      }

      //convert map values to an array & sort by turbine id for consistent ordering
      const latestRows = Array.from(latestMap.values())
      latestRows.sort((a, b) => a.TurbineId.localeCompare(b.TurbineId))
      setRecentData(latestRows)
      setLoading(false)
    }

    fetchLatestPerTurbine()
  }, []) //runs once when component mounts

  //simple loading, error and empty state messages
  if (loading) return <p>loading most recent turbine data...</p>
  if (error) return <p className="error-text">error: {error}</p>
  if (recentData.length === 0) return <p>no turbine data found.</p>

  return (
    <div className="table-page">
      <h1>Turbine Telemetry Data</h1>
      <div className="table-container">
        <table className="telemetry-table">
          <thead>
            <tr>
              <th>Turbine ID</th>
              <th>Timestamp (utc)</th>
              <th>Wind Speed (m/s)</th>
              <th>Rotor Speed (rpm)</th>
              <th>Power Output (kw)</th>
              <th>Efficiency (%)<br/><span style={{fontWeight:'normal'}}>(benchmark)</span></th>
              <th>Fault Status</th>
            </tr>
          </thead>
          <tbody>
            {recentData.map(row => (
              <tr key={row.TurbineId + row.Timestamp}>
                <td>{row.TurbineId}</td>
                <td>{new Date(row.Timestamp).toLocaleString()}</td>
                <td>{row.WindSpeed?.toFixed(2) ?? '—'}</td>
                <td>{row.RotorSpeed?.toFixed(2) ?? '—'}</td>
                <td>{row.PowerOutput?.toFixed(2) ?? '—'}</td>
                {/* highlight low efficiency with a light red background */}
                <td style={{
                  backgroundColor: (row.Efficiency ?? 0) < 70 ? '#ffdddd' : 'transparent'
                }}>
                  {row.Efficiency?.toFixed(1) ?? '—'}%
                </td>
                <td>
                  {row.StartedAlert ? (
                    <span style={{ color: '#d9534f', display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <FaExclamationTriangle /> Fault Detected
                    </span>
                  ) : (
                    <span style={{ color: '#82C340', display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <FaCheckCircle /> Normal
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export default MostRecentTable