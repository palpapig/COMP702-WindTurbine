/* RotorSpeedCurveGraph
this component creates a scatter plot of rotor speed (rpm) vs wind speed (m/s)
for a single turbine. it overlays a theoretical rotor speed curve so we can
see if the turbine's rotor is behaving as expected.
anomalies like stuck pitch or incorrect yaw can cause rotor speed deviations.
our outlier detection document flagged rotor speed below 11 rpm as an extreme outlier,
so this graph helps visualise those edge cases. */

import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import {
  ComposedChart, Scatter, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend,
  ResponsiveContainer
} from 'recharts'
import { FaLightbulb } from 'react-icons/fa'

function RotorSpeedCurveGraph({ turbineId }) {
  const [data, setData] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  //from appsettings.json where each figure is theoretical behaviour of the simulated turbine 
  //saturation wind: wind speed where rotor speed stops increasing (8.25 m/s)
  const saturationWind = 8.25
  //maximum rotor speed at saturation (approx 15.2 rpm)
  const maxRotorSpeed = 15.21
  //cut‑in wind speed where rotor starts spinning (approx 2.75 m/s)
  const cutInRotor = 2.75

  //theoretical rotor speed for a given wind speed (piecewise linear)
  //below cut‑in: 0 rpm; between cut‑in and saturation: linear ramp;
  //above saturation: constant max rpm.
  const expectedRotorSpeed = (windSpeed) => {
    if (windSpeed < cutInRotor) return 0
    if (windSpeed <= saturationWind) {
      const ratio = (windSpeed - cutInRotor) / (saturationWind - cutInRotor)
      return ratio * maxRotorSpeed
    }
    return maxRotorSpeed
  }

  //generate points for the blue line (every 0.2 m/s for smoothness)
  const generateCurvePoints = () => {
    const points = []
    for (let ws = 0; ws <= 25; ws += 0.2) {
      points.push({ windSpeed: ws, expectedRotor: expectedRotorSpeed(ws) })
    }
    return points
  }
  const curveData = generateCurvePoints()

  //fetch real rotor speed data from Supabase when the selected turbine changes
  useEffect(() => {
    //if no turbine selected, clear any previous data and stop loading
    if (!turbineId) {
      setData([])
      setLoading(false)
      return
    }

    async function fetchRotorData() {
      setLoading(true)
      setError('')
      const { data: telemetry, error: fetchError } = await supabase
        .from('TurbineData')
        .select('WindSpeed, RotorSpeed, StartedAlert')
        .eq('TurbineId', turbineId)   //only rows for this turbine
        .order('Timestamp', { ascending: false })
        .limit(1000)   

      if (fetchError) {
        setError(fetchError.message)
        setLoading(false)
        return
      }

      //format each row for recharts: wind speed, rotor speed, fault status
      const formatted = (telemetry || []).map(row => ({
        windSpeed: row.WindSpeed ?? 0,
        rotorSpeed: row.RotorSpeed ?? 0,
        faultStatus: row.StartedAlert ? 'Fault' : 'Normal'
      }))
      setData(formatted)
      setLoading(false)
    }
    fetchRotorData()
  }, [turbineId])   //re‑run when turbineId changes

  // ---- render states ----
  if (loading) return <p>loading rotor speed data...</p>
  if (error) return <p className="error-text">error: {error}</p>
  if (!turbineId) return <p>select a turbine to view rotor speed curve.</p>
  if (data.length === 0) return <p>no rotor speed data for this turbine yet.</p>

  return (
    <div style={{ width: '100%', marginTop: '1rem' }}>
      <ResponsiveContainer width="100%" height={400}>
        <ComposedChart margin={{ top: 20, right: 30, left: 20, bottom: 20 }}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis
            type="number"
            dataKey="windSpeed"
            name="Wind Speed"
            unit=" m/s"
            domain={[0, 25]}
            label={{ value: 'Wind Speed (m/s)', position: 'insideBottom', offset: -10 }}
            tick={{ fontSize: 12 }}
            tickFormatter={(value) => Math.round(value)}
          />
          <YAxis
            type="number"
            dataKey="rotorSpeed"
            name="Rotor Speed"
            unit=" rpm"
            domain={[0, 18]}   //give a little headroom above max expected (15.2)
            label={{ value: 'Rotor Speed (rpm)', angle: -90, position: 'insideLeft', dy: 70, dx: -10 }}
            tick={{ fontSize: 12 }}
            tickFormatter={(value) => Math.round(value)}
          />
          <Tooltip cursor={{ strokeDasharray: '3 3' }} />

          {/* red = fault, green = normal – same colour scheme as power curve */}
          <Scatter
            name="Fault Detected"
            data={data.filter(d => d.faultStatus === 'Fault')}
            fill="#d9534f"
            shape="circle"
          />
          <Scatter
            name="Normal"
            data={data.filter(d => d.faultStatus === 'Normal')}
            fill="#82C340"
            shape="circle"
          />

          {/* theoretical rotor speed curve (blue line) */}
          <Line
            type="monotone"
            data={curveData}
            dataKey="expectedRotor"
            stroke="#06A2DF"
            strokeWidth={2}
            dot={false}
            name="Expected Rotor Speed"
          />
        </ComposedChart>
      </ResponsiveContainer>

      {/* custom legend – matches power curve graph for consistency */}
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        gap: '30px',
        marginTop: '15px',
        fontSize: '14px'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ width: '14px', height: '14px', backgroundColor: '#d9534f', borderRadius: '50%', display: 'inline-block' }}></span>
          <span>Fault Detected</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ width: '14px', height: '14px', backgroundColor: '#82C340', borderRadius: '50%', display: 'inline-block' }}></span>
          <span>Normal</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ width: '24px', height: '2px', backgroundColor: '#06A2DF', display: 'inline-block' }}></span>
          <span>Expected Rotor Speed Curve</span>
        </div>
      </div>

      {/* explanatory note – references the outlier detection document */}
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
          style={{ fontSize: '0.9rem', marginRight: '6px', verticalAlign: 'left' }}
        />
        The blue line shows the theoretical rotor speed curve (linear up to {saturationWind} m/s, then constant at ~{maxRotorSpeed.toFixed(1)} rpm). Points significantly below the curve may indicate pitch or yaw issues – our outlier detection flags rotor speeds below 11 rpm.
      </div>
    </div>
  )
}

export default RotorSpeedCurveGraph