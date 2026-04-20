import { useEffect, useState } from 'react'
import { supabase } from './utils/supabase'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer
} from 'recharts'
import './TurbineDetail.css'

function TurbineDetail({ turbineId, onBack }) {
  const [telemetry, setTelemetry] = useState([])
  const [loading, setLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    async function getTelemetry() {
      setLoading(true)
      setErrorMessage('')

      const { data, error } = await supabase
        .from('TurbineData')
        .select('*')
        .eq('TurbineId', turbineId)
        .order('Timestamp', { ascending: true })

      if (error) {
        setErrorMessage(error.message)
        setLoading(false)
        return
      }

      if (data) {
        const formattedData = data.map((row) => ({
          ...row,
          timeLabel: new Date(row.Timestamp).toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit',
          }),
        }))

        setTelemetry(formattedData)
      }

      setLoading(false)
    }

    getTelemetry()
  }, [turbineId])

  return (
    <div className="detail-page">
      <div className="detail-header">
        <button className="back-btn" onClick={onBack}>
          Back
        </button>
        <h1>{turbineId} Details</h1>
      </div>

      {loading && <p>Loading turbine telemetry...</p>}
      {errorMessage && <p className="error-text">Error: {errorMessage}</p>}

      {!loading && !errorMessage && telemetry.length === 0 && (
        <p>No telemetry found for this turbine.</p>
      )}

      {!loading && !errorMessage && telemetry.length > 0 && (
        <div className="charts-grid">
          <div className="chart-card">
            <h2>Wind Speed Over Time</h2>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={telemetry}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="timeLabel" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="WindSpeed" />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="chart-card">
            <h2>Rotor Speed Over Time</h2>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={telemetry}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="timeLabel" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="RotorSpeed" />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="chart-card">
            <h2>Power Output Over Time</h2>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={telemetry}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="timeLabel" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="PowerOutput" />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="chart-card">
            <h2>Vibration Over Time</h2>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={telemetry}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="timeLabel" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="Vibration" />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}
    </div>
  )
}

export default TurbineDetail