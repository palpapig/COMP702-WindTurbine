import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import './Table.css'

function DegradationResultTable({ turbineId }) {
  const [degradationData, setDegradationData] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!turbineId) {
      setDegradationData([])
      return
    }

    async function fetchDegradationData() {
      setLoading(true)
      setError('')

      const { data, error } = await supabase
        .from('DegradationResult')
        .select('Id, Region2Score, Region2Point5Score, TimeRangeStart, TimeRangeEnd, TurbineId')
        .eq('TurbineId', turbineId)
        .order('TimeRangeStart', { ascending: true })

      if (error) {
        setError(error.message)
        setLoading(false)
        return
      }

      setDegradationData(data || [])
      setLoading(false)
    }

    fetchDegradationData()
  }, [turbineId])

  if (loading) return <p>loading degradation result data...</p>
  if (error) return <p className="error-text">error: {error}</p>
  if (!turbineId) return <p>select a turbine to view degradation results.</p>
  if (degradationData.length === 0) {
    return <p>no degradation results found for {turbineId}.</p>
  }

  return (
    <div className="table-page">
      <h3>Degradation Result Table</h3>
      <p style={{ fontSize: '0.85rem', color: '#555', marginBottom: '1rem' }}>
        Shows degradation scores for the selected turbine only.
      </p>

      <div className="table-container">
        <table className="telemetry-table">
          <thead>
            <tr>
              <th>Id</th>
              <th>TurbineId</th>
              <th>Region 2 Score</th>
              <th>Region 2.5 Score</th>
              <th>Time Range Start</th>
              <th>Time Range End</th>
            </tr>
          </thead>

          <tbody>
            {degradationData.map((row) => (
              <tr key={row.Id}>
                <td>{row.Id}</td>
                <td>{row.TurbineId}</td>
                <td>{row.Region2Score?.toFixed(4) ?? '—'}</td>
                <td>{row.Region2Point5Score?.toFixed(4) ?? '—'}</td>
                <td>
                  {row.TimeRangeStart
                    ? new Date(row.TimeRangeStart).toLocaleString()
                    : '—'}
                </td>
                <td>
                  {row.TimeRangeEnd
                    ? new Date(row.TimeRangeEnd).toLocaleString()
                    : '—'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export default DegradationResultTable