/* PowerCurveGraph
this file creates a scatter plot of power output vs wind speed.
it overlays the theoretical power curve (blue line) so we can see
how actual turbine performance compares to the ideal.
green if normal. the graph helps spot underperformance visually.
we fetch the last 500 telemetry rows from supabase for performance.
the expected curve uses the same piecewise linear parameters as
our benchmarker in the windows service. */

import { useState, useEffect } from 'react'
import { supabase } from './utils/supabase'
import {
  ScatterChart, Scatter, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ComposedChart, Line,
  ResponsiveContainer
} from 'recharts'
import { FaLightbulb } from 'react-icons/fa'

function PowerCurveGraph() {
  //state for the scatter data points, loading flag & any error messages
  const [data, setData] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  //power curve parameters – these come from the same config as the benchmarker
  //i decided to hardcode them here so the graph is self‑contained.
  //cut‑in: wind speed where turbine starts generating (2.75 m/s)
  const cutIn = 2.75
  //rated wind: wind speed where it first reaches rated power (11.25 m/s)
  const ratedWind = 11.25
  //rated power: max power output (2050 kW) from the turbine specs
  const ratedPower = 2050.0
  //cut‑out: wind speed where turbine shuts down (25 m/s)
  const cutOut = 25.0

  //function that calculates expected power for a given wind speed
  //using the piecewise linear model (same as in benchmarker)
  const expectedPower = (windSpeed) => {
    //below cut‑in or above cut‑out: no power
    if (windSpeed < cutIn || windSpeed > cutOut) return 0
    //between cut‑in and rated wind: linear interpolation
    if (windSpeed <= ratedWind) {
      const ratio = (windSpeed - cutIn) / (ratedWind - cutIn)
      return ratio * ratedPower
    }
    //between rated wind and cut‑out: constant rated power
    return ratedPower
  }

  //generate a set of points for the blue line (every 0.5 m/s)
  //we do this so the line is smooth and covers the whole domain.
  const generateCurvePoints = () => {
    const points = []
    for (let ws = 0; ws <= 25; ws += 0.5) {
      points.push({ windSpeed: ws, expectedPower: expectedPower(ws) })
    }
    return points
  }

  const curveData = generateCurvePoints()

  //fetch telemetry from supabase when the component mounts
  useEffect(() => {
    async function fetchData() {
      setLoading(true)
      setError('')
      //get the 500 most recent rows – this is enough for a good scatter plot
      //and avoids loading thousands of rows unnecessarily.
      const { data: telemetry, error: fetchError } = await supabase
        .from('TurbineData')
        .select('WindSpeed, PowerOutput, StartedAlert')
        .order('Timestamp', { ascending: false })
        .limit(500)

      if (fetchError) {
        setError(fetchError.message)
        setLoading(false)
        return
      }

      //format each row for recharts: wind speed, power output, and a readable fault status
      //i use the nullish coalescing operator to default missing values to 0.
      const formatted = telemetry.map(row => ({
        windSpeed: row.WindSpeed ?? 0,
        powerOutput: row.PowerOutput ?? 0,
        faultStatus: row.StartedAlert ? 'Fault' : 'Normal'
      }))
      setData(formatted)
      setLoading(false)
    }
    fetchData()
  }, []) //empty dependency array means this runs only once on mount

  //simple loading & error states
  if (loading) return <p>Loading power curve data...</p>
  if (error) return <p className="error-text">Error: {error}</p>

  return (
    <div style={{ width: '100%', marginTop: '1rem' }}>
      <h3>Power Curve: Actual vs Expected</h3>
      <p style={{ fontSize: '0.85rem', color: '#555' }}>
        Points below the curve indicate underperformance.
      </p>
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
            tick={{ fontSize: 9 }}                     //smaller font so labels dont overlap
            tickFormatter={(value) => value.toFixed(1)} //1 decimal is enough, reduces clutter
            height={50}                                //reserve space for rotated labels (though didnt rotate)
            tickMargin={10}                            //extra space between tick mark and label
          />
          <YAxis
            type="number"
            dataKey="powerOutput"
            name="Power Output"
            unit=" kW"
            domain={[0, 2500]}   //safe upper limit (rated power is 2050 kW, 2500 gives headroom)
            label={{ value: 'Power Output (kW)', angle: -90, position: 'insideLeft', dy: 70 }}
            tick={{ fontSize: 10 }}
            tickFormatter={(value) => {
              //round to nearest integer & format without scientific notation
              if (isNaN(value)) return '0';
              return Math.round(value).toLocaleString();
            }}
          />
          <Tooltip cursor={{ strokeDasharray: '3 3' }} />

          {/* scatter series for fault and normal points – split them so they can have different colours */}
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

          {/* blue line for the expected power curve */}
          <Line
            type="monotone"
            data={curveData}
            dataKey="expectedPower"
            stroke="#06A2DF"
            strokeWidth={2}
            dot={false}
            name="Expected Power Curve"
          />
        </ComposedChart>
      </ResponsiveContainer>

      {/* custom legend – placed outside the chart to avoid overlapping with axes */}
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
          <span>Expected Power Curve</span>
        </div>
      </div>

      {/* explanatory note for users – helps them interpret the graph correctly */}
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
        The blue line is the ideal power curve. Normal operation (green) can still be below this line due to real‑world conditions. Significant and persistent underperformance (red) indicates faults.
      </div>
    </div>
  )
}

export default PowerCurveGraph