import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts'
import { FaLightbulb } from 'react-icons/fa'

function VibrationGraph({ turbineId }) {
  const [chartData, setChartData] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!turbineId) return

    async function fetchVibrationData() {
      setLoading(true)
      setError('')

      const { data, error } = await supabase
        .from('TurbineData')
        .select('Timestamp, Vibration')
        .eq('TurbineId', turbineId)
        .order('Timestamp', { ascending: true })

      if (error) {
        setError(error.message)
        setLoading(false)
        return
      }

      const formatted = (data || []).map((row) => ({
        timestamp: new Date(row.Timestamp).toLocaleString(),
        vibration: row.Vibration ?? 0,
      }))

      setChartData(formatted)
      setLoading(false)
    }

    fetchVibrationData()
  }, [turbineId])

  if (loading) return <p>loading vibration history...</p>
  if (error) return <p className="error-text">error: {error}</p>
  if (!turbineId) return <p>select a turbine to view vibration trend.</p>
  if (chartData.length === 0) return <p>no vibration data for this turbine yet.</p>

  return (
    <div style={{ width: '100%', marginTop: '1rem' }}>
      <h3>Vibration Over Time</h3>
      <p style={{ fontSize: '0.85rem', color: '#555' }}>
        Monitors vibration levels across time for early signs of abnormal behaviour.
      </p>

      <div style={{ width: '100%', overflowX: 'auto', display: 'flex', justifyContent: 'center' }}>
        <LineChart
          width={800}
          height={400}
          data={chartData}
          margin={{ top: 20, right: 30, left: 20, bottom: 70 }}
        >
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis
            dataKey="timestamp"
            angle={-45}
            textAnchor="end"
            height={80}
            interval="preserveStartEnd"
            tick={{ fontSize: 10 }}
            label={{
              value: 'Date and Time',
              position: 'insideBottom',
              offset: -50,
              style: { textAnchor: 'middle', fill: '#666' }
            }}
          />
          <YAxis
            label={{
              value: 'Vibration',
              angle: -90,
              position: 'insideLeft',
              style: { textAnchor: 'middle' }
            }}
          />
          <Tooltip />
          <Line
            type="monotone"
            dataKey="vibration"
            stroke="#06A2DF"
            strokeWidth={2}
            dot={false}
          />
        </LineChart>
      </div>

      <div style={{
        textAlign: 'center',
        fontSize: '0.8rem',
        color: '#666',
        fontStyle: 'italic',
        marginTop: '12px',
        borderTop: '1px solid #e0e0e0',
        paddingTop: '10px',
        maxWidth: '80%',
        marginLeft: 'auto',
        marginRight: 'auto'
      }}>
        <FaLightbulb
          color="#FFD700"
          style={{ fontSize: '0.9rem', marginRight: '6px', verticalAlign: 'middle' }}
        />
        Persistent increases in vibration may indicate imbalance, wear, or developing mechanical faults.
      </div>
    </div>
  )
}

export default VibrationGraph