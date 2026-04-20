import { useEffect, useState } from 'react'
import { supabase } from './utils/supabase'
import './Table.css'

function Turbine({ onSelectTurbine }) {
  const [turbines, setTurbines] = useState([])
  const [loading, setLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    async function getTurbines() {
      setLoading(true)
      setErrorMessage('')

      const { data, error } = await supabase
        .from('Turbine')
        .select('*')
        .order('TurbineId', { ascending: true })

      if (error) {
        setErrorMessage(error.message)
        setLoading(false)
        return
      }

      if (data) {
        setTurbines(data)
      }

      setLoading(false)
    }

    getTurbines()
  }, [])

  return (
    <div className="table-page">
      <h1>Turbine Table</h1>

      {loading && <p>Loading data...</p>}
      {errorMessage && <p className="error-text">Error: {errorMessage}</p>}

      {!loading && !errorMessage && (
        <div className="table-container">
          <table className="telemetry-table">
            <thead>
              <tr>
                <th>TurbineId</th>
                <th>Name</th>
                <th>Location</th>
                <th>Status</th>
                <th>LastTelemetryTime</th>
              </tr>
            </thead>

            <tbody>
              {turbines.map((row) => (
                <tr
                  key={row.TurbineId}
                  onClick={() => onSelectTurbine(row.TurbineId)}
                  className="clickable-row"
                >
                  <td>{row.TurbineId}</td>
                  <td>{row.Name}</td>
                  <td>{row.Location}</td>
                  <td>{row.Status}</td>
                  <td>
                    {row.LastTelemetryTime
                      ? new Date(row.LastTelemetryTime).toLocaleString()
                      : 'N/A'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

export default Turbine