/*  EfficiencyGraph
this component shows a line graph of efficiency (%) over time for a single turbine.
the user selects a turbine from a dropdown & the graph fetches all efficiency
records for that turbine from supabase. i chose a line chart because it clearly
shows trends and degradation over time. the x‑axis labels are rotated 45 degrees
to avoid overlap. also set a fixed width of 800px & horizontal scroll for
responsiveness. */

import { useState, useEffect } from 'react'
import { supabase } from './utils/supabase'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend
} from 'recharts'

function EfficiencyGraph() {
  //list of all turbine ids, currently selected turbine, chart data, loading & error states
  const [turbines, setTurbines] = useState([])
  const [selectedTurbine, setSelectedTurbine] = useState('')
  const [chartData, setChartData] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  //fetch distinct turbine ids from supabase on component mount
  useEffect(() => {
    async function fetchTurbineIds() {
      const { data, error } = await supabase
        .from('TurbineData')
        .select('TurbineId')
        .order('TurbineId')
      if (error) {
        setError(error.message)
        return
      }
      //supabase may return duplicates? but .select('TurbineId') should be distinct
      //i used a map anyway to be safe
      const unique = [...new Map(data.map(row => [row.TurbineId, row])).values()]
      setTurbines(unique)
      //automatically select the first turbine as default
      if (unique.length > 0) setSelectedTurbine(unique[0].TurbineId)
    }
    fetchTurbineIds()
  }, [])

  //when selected turbine changes, fetch its efficiency history
  useEffect(() => {
    if (!selectedTurbine) return
    async function fetchEfficiencyHistory() {
      setLoading(true)
      setError('')
      const { data, error } = await supabase
        .from('TurbineData')
        .select('Timestamp, Efficiency')
        .eq('TurbineId', selectedTurbine)
        .order('Timestamp', { ascending: true }) //oldest first so the line goes left to right
      if (error) {
        setError(error.message)
        setLoading(false)
        return
      }
      //to format timestamp as a readable string for the x‑axis
      const formatted = (data || []).map(row => ({
        timestamp: new Date(row.Timestamp).toLocaleString(),
        efficiency: row.Efficiency ?? 0
      }))
      setChartData(formatted)
      setLoading(false)
    }
    fetchEfficiencyHistory()
  }, [selectedTurbine])

  const handleTurbineChange = (e) => {
    setSelectedTurbine(e.target.value)
  }

  return (
    <div className="table-page">
      {/* dropdown for turbine selection */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="turbineSelect" style={{ fontWeight: 500, marginRight: '0.5rem' }}>
          Select Turbine:
        </label>
        <select
          id="turbineSelect"
          value={selectedTurbine}
          onChange={handleTurbineChange}
          style={{ padding: '0.3rem 0.8rem', borderRadius: '8px', border: '1px solid #ccc' }}
        >
          {turbines.map(t => (
            <option key={t.TurbineId} value={t.TurbineId}>{t.TurbineId}</option>
          ))}
        </select>
      </div>

      {/* loading, error & empty states */}
      {loading && <p>Loading efficiency history...</p>}
      {error && <p className="error-text">Error: {error}</p>}
      {!loading && !error && chartData.length === 0 && (
        <p>No efficiency data for this turbine yet.</p>
      )}
      {!loading && !error && chartData.length > 0 && (
        //scrollable container in case the chart is wider than the viewport
        <div style={{ width: '100%', overflowX: 'auto' }}>
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
      )}
    </div>
  )
}

export default EfficiencyGraph