import { useState, useEffect } from 'react'
import { supabase } from './utils/supabase'
import './Table.css'

function Table() {
  const [turbineData, setTurbineData] = useState([])
  const [loading, setLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    async function getData() {
      setLoading(true)
      setErrorMessage('')

      const { data, error } = await supabase
        .from('TurbineData')
        .select('*')
        .order('Id', { ascending: true })
        .range(0, 29)

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

      {loading && <p>Loading data...</p>}
      {errorMessage && <p className="error-text">Error: {errorMessage}</p>}

      {!loading && !errorMessage && (
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
      )}
    </div>
  )
}

export default Table