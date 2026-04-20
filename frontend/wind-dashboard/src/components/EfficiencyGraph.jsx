/*  EfficiencyGraph
this component shows a line graph of efficiency (%) over time for a single turbine.
the user selects a turbine from a dropdown & the graph fetches all efficiency
records for that turbine from supabase. i chose a line chart because it clearly
shows trends and degradation over time. the x‑axis labels are rotated 45 degrees
to avoid overlap. 
*/

import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend
} from 'recharts'
import { FaLightbulb } from 'react-icons/fa'

function EfficiencyGraph({ turbineId }) {
  //accept turbineId as a prop
  const [chartData, setChartData] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  // power curve parameters (from appsettings.json) – used for efficiency calculation explanation
  const cutIn = 2.75
  const ratedWind = 11.25
  const ratedPower = 2050.0

  //when turbineId changes, fetch its efficiency history
  useEffect(() => {
    if (!turbineId) return
    async function fetchEfficiencyHistory() {
      setLoading(true)
      setError('')
      const { data, error } = await supabase
        .from('TurbineData')
        .select('Timestamp, Efficiency')
        .eq('TurbineId', turbineId)
        .order('Timestamp', { ascending: true })
      if (error) {
        setError(error.message)
        setLoading(false)
        return
      }
      const formatted = (data || []).map(row => ({
        timestamp: new Date(row.Timestamp).toLocaleString(),
        efficiency: row.Efficiency ?? 0
      }))
      setChartData(formatted)
      setLoading(false)
    }
    fetchEfficiencyHistory()
  }, [turbineId])  //depend on prop

  //simple loading, error & empty states
  if (loading) return <p>loading efficiency history...</p>
  if (error) return <p className="error-text">error: {error}</p>
  if (!turbineId) return <p>select a turbine to view efficiency trend.</p>
  if (chartData.length === 0) return <p>no efficiency data for this turbine yet.</p>

      return (        
        <div style={{ width: '100%' }}>
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
              angle={-45}               //rotate labels to prevent overlap
              textAnchor="end"
              height={80}               //give enough room for rotated labels
              interval="preserveStartEnd" //show first and last labels even if crowded
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
                value: 'Efficiency (%)',
                angle: -90,
                position: 'insideTop',
                offset: 120,
                dx: -20,
                style: { textAnchor: 'middle' }
              }}
            />
            <Tooltip />
            <Legend verticalAlign="top" align="right" />
            <Line
              type="monotone"           //smooth line between points
              dataKey="efficiency"
              stroke="#06A2DF"          //brand blue colour
              strokeWidth={2}
            />
          </LineChart>
        </div>
      {/* explanatory note – explains efficiency calculation and what to look for */}
      <div style={{
        textAlign: 'left',
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
        <ul style={{ marginTop: '8px', marginBottom: '0', paddingLeft: '20px' }}>
          <li>Efficiency = (actual power / expected power) × 100%.</li>
          <li>Expected power comes from the theoretical power curve (cut‑in {cutIn} m/s, rated wind {ratedWind} m/s, rated power {ratedPower} kW).</li>
          <li>Values below 70% in the tables – persistent low efficiency may indicate blade wear, pitch misalignment, or other performance degradation.</li>
        </ul>
        </div>
      </div>
    )
}


export default EfficiencyGraph