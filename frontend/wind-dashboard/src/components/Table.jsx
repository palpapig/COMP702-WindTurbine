/*    
this component shows the complete telemetry history (all rows).
it fetches all records from supabase without any limit and orders them
by timestamp ascending (oldest first). i removed the previous .range(0,29)
because we need the full history for the "Complete Telemetry History" view.
the table includes all columns stored in supabase, including the internal Id. 
*/

import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import './Table.css'

function Table() {
  const [turbineData, setTurbineData] = useState([])
  const [loading, setLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    async function getData() {
      setLoading(true)
      setErrorMessage('')

      //fetch all rows, ordered by timestamp oldest first
      //removed .range(0,29) to get the complete history
      const { data, error } = await supabase
        .from('TurbineData')
        .select('*')
        .order('Timestamp', { ascending: true })

      if (error) {
        setErrorMessage(error.message)
        setLoading(false)
        return
      }

      if (data) {
        setTurbineData(data)
      }

      setLoading(false)
    }

    getData()
  }, [])

  return (
    <div className="table-page">
      <h1>Turbine Telemetry Data</h1>
      <h2>Complete History</h2>
      <p>{turbineData.length} rows total</p>

      {loading && <p>Loading data...</p>}
      {errorMessage && <p className="error-text">Error: {errorMessage}</p>}

      {!loading && !errorMessage && (
        <>
        <div className="table-container">
          <table className="telemetry-table">
            <thead>
              <tr>
                <th>Id</th>
                <th>TurbineId</th>
                <th>Timestamp</th>
                <th>WindSpeed</th>
                <th>RotorSpeed</th>
                <th>PowerOutput</th>
                <th>Vibration</th>
                <th>Temperature</th>
                <th>Efficiency</th>
                <th>StartedAlert</th>
                <th>GearboxOilTemp</th>
                <th>PitchAngle</th>
              </tr>
            </thead>
            <tbody>
              {turbineData.map((row) => (
                <tr key={row.Id}>
                  <td>{row.Id}</td>
                  <td>{row.TurbineId}</td>
                  <td>{row.Timestamp ? new Date(row.Timestamp).toLocaleString() : 'N/A'}</td>
                  <td>{row.WindSpeed?.toFixed(2)}</td>
                  <td>{row.RotorSpeed?.toFixed(2)}</td>
                  <td>{row.PowerOutput?.toFixed(2)}</td>
                  <td>{row.Vibration?.toFixed(2)}</td>
                  <td>{row.Temperature?.toFixed(2)}</td>
                  <td>{row.Efficiency?.toFixed(2)}</td>
                  <td>{row.StartedAlert ? 'TRUE' : 'FALSE'}</td>
                  <td>{row.GearboxOilTemp?.toFixed(2)}</td>
                  <td>{row.PitchAngle?.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <button
            onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
            style={{
              display: 'block',
              margin: '1.5rem auto 0',
              padding: '0.5rem 1rem',
              backgroundColor: 'var(--brand-blue)',
              color: 'white',
              border: 'none',
              borderRadius: '40px',
              cursor: 'pointer',
              fontSize: '0.9rem'
            }}
          >
            ↑ Back to Top
          </button>
        </>
      )}        
    </div>
  )
}

export default Table