
/*  MostRecentTable
this component displays the latest telemetry record for each turbine.
it works by fetching all rows from supabase, ordering them by timestamp descending,
then using a map to keep only the first (newest) row for each turbine id.
chose this approach because it's simple and runs entirely in the browser.
the table shows key metrics like wind speed, rotor speed, power output,
efficiency (benchmark) & fault status with coloured icons. */

import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import { FaExclamationTriangle, FaCheckCircle, FaBell } from 'react-icons/fa' 
import './Table.css'   //reuse the same styling as the full history table

function MostRecentTable() {
  const [recentData, setRecentData] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const getAlarmDisplay = (alarmLvl) => {
    if (alarmLvl === 1) {
      return { text: 'A1', colour: '#f0ad4e', icon: <FaExclamationTriangle /> }
    } else if (alarmLvl === 2) {
      return { text: 'A2', colour: '#d9534f', icon: <FaBell /> }
    } else {
      return { text: 'None', colour: '#82C340', icon: <FaCheckCircle /> }
    }
  }

  useEffect(() => {
    async function fetchLatestPerTurbine() {
      setLoading(true)
      setError('')

      //fetch all telemetry rows, sorted with the newest first
      const { data: telemetry, error: telemError } = await supabase
        .from('TurbineData')
        .select('*')
        .order('Timestamp', { ascending: false })

      if (telemError) {
        setError(telemError.message)
        setLoading(false)
        return
      }

      if (!telemetry || telemetry.length === 0) {
        setRecentData([])
        setLoading(false)
        return
      }

      //fetch failure detection results (safe declaration)
      let failures = []
      let failError = null
      try {
        const result = await supabase
          .from('FailureDetectionResult')
          .select('TurbineId, Timestamp, AlarmLvl, IsAcknowledged')
          .order('Timestamp', { ascending: false })
        if (result.error) {
          failError = result.error
          console.warn('Failure fetch error:', failError.message)
        } else {
          failures = result.data || []
        }
      } catch (err) {
        failError = err
        console.warn('Failure fetch exception:', err)
      }

      // keep only the latest failure per turbine (first because sorted desc)
      const latestFailureMap = new Map()
      for (const fail of failures) {
        if (!latestFailureMap.has(fail.TurbineId)) {
          latestFailureMap.set(fail.TurbineId, fail)
        }
      }

      //manually keep the first occurrence of each turbine id (which will be the newest because of the sort)
      const latestTeleMap = new Map()
      for (const row of telemetry) {
        if (!latestTeleMap.has(row.TurbineId)) {
          latestTeleMap.set(row.TurbineId, row)
      }}

      //combine telemetry with alarm level from failure map
      const combined = []
      for (const [turbineId, teleRow] of latestTeleMap.entries()) {
        const failure = latestFailureMap.get(turbineId)
        const alarmLvl = failure?.AlarmLvl ?? 0
        combined.push({ ...teleRow, alarmLevel: alarmLvl })
      }

      //sort by turbine id for consistent ordering
      combined.sort((a, b) => a.TurbineId.localeCompare(b.TurbineId))
      setRecentData(combined)
      setLoading(false)
    }

    fetchLatestPerTurbine()
  }, [])

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
              {/* replaced Fault Status with Alarm column */}
              <th>Alarm</th>
            </tr>
          </thead>
          <tbody>
            {recentData.map(row => {
              const alarm = getAlarmDisplay(row.alarmLevel)
              return (
                <tr key={row.TurbineId + row.Timestamp}>
                  <td>{row.TurbineId}</td>
                  <td>{new Date(row.Timestamp).toLocaleString()}</td>
                  <td>{row.WindSpeed?.toFixed(2) ?? '—'}</td>
                  <td>{row.RotorSpeed?.toFixed(2) ?? '—'}</td>
                  <td>{row.PowerOutput?.toFixed(2) ?? '—'}</td>
                  <td style={{
                    backgroundColor: (row.Efficiency ?? 0) < 70 ? '#ffdddd' : 'transparent'
                  }}>
                    {row.Efficiency?.toFixed(1) ?? '—'}%
                  </td>
                  {/* new alarm column with coloured text and icon */}
                  <td style={{ color: alarm.colour, fontWeight: 'bold' }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                      {alarm.icon} {alarm.text}
                    </span>
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

export default MostRecentTable