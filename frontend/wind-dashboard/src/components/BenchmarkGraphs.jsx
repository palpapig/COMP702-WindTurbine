import { useState, useEffect } from 'react';
import { supabase } from '../utils/supabase';
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts';
import { FiZap } from 'react-icons/fi';

function BenchmarkGraphs() {
  //state for storing deviation data (power difference) grouped by year
  const [deviationSeries, setDeviationSeries] = useState([]);
  //state for deviation scores per year (displayed in the table)
  const [deviationScores, setDeviationScores] = useState([]);
  //loading and error states for user feedback
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  //statistics summary (min, max, best/worst year, etc.) computed from deviation data
  const [stats, setStats] = useState(null);

  //state for measured power data (actual power output) grouped by year
  const [measuredSeries, setMeasuredSeries] = useState([]);
  //expected power curve from the turbine model (same for all years)
  const [expectedCurve, setExpectedCurve] = useState([]);

  //the only turbine that currently has benchmark data in supabase
  const targetTurbineId = 'BK-TEST-4';

  //consistent colour palette for each year so lines are easy to tell apart
  const colourMap = {
    2018: '#1f77b4', 
    2019: '#00bcd4', 
    2020: '#8bc34a', 
    2021: '#ff9800', 
    2022: '#d32f2f', 
  };

  //this function computes summary statistics directly from the deviation data
  //i added it to give the user a quick interpretation without hovering every point
  const computeStatistics = (deviationSeries) => {
    if (!deviationSeries || deviationSeries.length === 0) return null;
    let allValues = [];
    let perYearStats = [];
    //loop through each year's deviation data
    for (const series of deviationSeries) {
      //extract all powerDifference values, ignoring nulls or nans
      const values = series.data.map(d => d.powerDifference).filter(v => v !== null && !isNaN(v));
      if (values.length === 0) continue;
      //calculate mean, min, max for this year
      const mean = values.reduce((a, b) => a + b, 0) / values.length;
      const min = Math.min(...values);
      const max = Math.max(...values);
      perYearStats.push({ year: series.year, mean, min, max });
      allValues.push(...values);
    }
    if (allValues.length === 0) return null;
    //overall statistics across all years
    const overallMin = Math.min(...allValues);
    const overallMax = Math.max(...allValues);
    const overallMean = allValues.reduce((a, b) => a + b, 0) / allValues.length;
    //find best and worst years by mean deviation (higher mean is better)
    const sortedByMean = [...perYearStats].sort((a, b) => b.mean - a.mean);
    const bestYear = sortedByMean[0];
    const worstYear = sortedByMean[sortedByMean.length - 1];
    //find the single highest and lowest powerDifference points across all years
    //this helps identify extreme events
    let highestPoint = { value: -Infinity, windSpeed: null, year: null };
    let lowestPoint = { value: Infinity, windSpeed: null, year: null };
    for (const series of deviationSeries) {
      for (const point of series.data) {
        if (point.powerDifference > highestPoint.value) {
          highestPoint = { value: point.powerDifference, windSpeed: point.windSpeed, year: series.year };
        }
        if (point.powerDifference < lowestPoint.value) {
          lowestPoint = { value: point.powerDifference, windSpeed: point.windSpeed, year: series.year };
        }
      }
    }
    //return nicely formatted numbers with one decimal place
    return {
      overallMean: overallMean.toFixed(1),
      overallMin: overallMin.toFixed(1),
      overallMax: overallMax.toFixed(1),
      bestYear: bestYear.year,
      bestYearMean: bestYear.mean.toFixed(1),
      worstYear: worstYear.year,
      worstYearMean: worstYear.mean.toFixed(1),
      highestDeviation: highestPoint.value.toFixed(1),
      highestWindSpeed: highestPoint.windSpeed?.toFixed(1),
      highestYear: highestPoint.year,
      lowestDeviation: lowestPoint.value.toFixed(1),
      lowestWindSpeed: lowestPoint.windSpeed?.toFixed(1),
      lowestYear: lowestPoint.year,
    };
  };

  //main data fetching – runs once when the component mounts
  useEffect(() => {
    async function fetchData() {
      setLoading(true);
      try {
        //step 1: get all benchmark results for the specific turbine
        const { data: benchmarks, error: benchError } = await supabase
          .from('BenchmarkResult')
          .select('Id, TimeRangeStart, DeviationScore')
          .eq('TurbineId', targetTurbineId)
          .order('TimeRangeStart', { ascending: true }); //oldest first so years are ordered

        if (benchError) throw benchError;
        if (!benchmarks || benchmarks.length === 0) {
          setError('No benchmark results found for this turbine');
          setLoading(false);
          return;
        }

        //prepare deviation scores for the table (extract year from timestamp)
        const scores = benchmarks.map(b => ({
          year: new Date(b.TimeRangeStart).getFullYear(),
          score: b.DeviationScore
        }));
        setDeviationScores(scores);

        //step 2: fetch expected power curve from PowerBinExpected (once)
        //first we need the TurbineModelId for BK-TEST-4
        const { data: turbine, error: turbineError } = await supabase
          .from('Turbine')
          .select('TurbineModelId')
          .eq('TurbineId', targetTurbineId)
          .single();
        if (turbineError) throw turbineError;
        const { data: expected, error: expError } = await supabase
          .from('PowerBinExpected')
          .select('WindSpeed, Power')
          .eq('TurbineModelId', turbine.TurbineModelId)
          .order('WindSpeed', { ascending: true });
        if (expError) throw expError;
        setExpectedCurve(expected.map(e => ({ windSpeed: e.WindSpeed, power: e.Power })));

        //step 3: fetch deviation data (PowerBinDeviation) and measured data (PowerBinMeasured)
        const devSeries = [];
        const measSeries = [];

        for (const bench of benchmarks) {
          const year = new Date(bench.TimeRangeStart).getFullYear();

          //deviation (power difference) – used in the first graph
          const { data: deviation, error: devError } = await supabase
            .from('PowerBinDeviation')
            .select('WindSpeed, PowerDifference')
            .eq('BenchmarkResultId', bench.Id)
            .order('WindSpeed', { ascending: true });
          if (devError) throw devError;
          if (deviation && deviation.length > 0) {
            devSeries.push({
              year,
              data: deviation.map(d => ({ windSpeed: d.WindSpeed, powerDifference: d.PowerDifference }))
            });
          }

          //measured power (actual power output) – used in the second graph
          const { data: measured, error: measError } = await supabase
            .from('PowerBinMeasured')
            .select('WindSpeed, Power')
            .eq('BenchmarkResultId', bench.Id) //foreign key column is BenchmarkResultId
            .order('WindSpeed', { ascending: true });
          if (measError) throw measError;
          if (measured && measured.length > 0) {
            measSeries.push({
              year,
              data: measured.map(m => ({ windSpeed: m.WindSpeed, power: m.Power }))
            });
          }
        }

        setDeviationSeries(devSeries);
        setMeasuredSeries(measSeries);
        const computedStats = computeStatistics(devSeries);
        setStats(computedStats);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    fetchData();
  }, []); //empty array means this effect runs only once when the component mounts

  //render loading, error, or empty states
  if (loading) return <p>Loading data for {targetTurbineId}...</p>;
  if (error) return <p className="error-text">Error: {error}</p>;
  if (deviationSeries.length === 0 && measuredSeries.length === 0)
    return <p>No data available for {targetTurbineId}.</p>;

  return (
    <div>
      {/*main page title (large) */}
      <h1 style={{ textAlign: 'center', fontSize: '2rem', marginBottom: '1rem' }}>
        Power Difference vs Wind Speed
      </h1>
      <p style={{ textAlign: 'center', fontSize: '1rem', color: '#666', marginBottom: '2rem' }}>
        Deviation from expected power curve (based on turbine model)
      </p>

      {/*========== First Graph: Power Deviation ==========*/}
      <div className="dashboard-section">
        <div className="section-title">
          <FiZap size={24} color="#06A2DF" style={{ marginRight: '8px' }} />
          <h3>Power Difference vs Wind Speed</h3>
        </div>
        <ResponsiveContainer width="100%" height={450}>
          <LineChart margin={{ top: 20, right: 30, left: 50, bottom: 60 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              type="number"
              dataKey="windSpeed"
              name="Wind Speed"
              unit=" m/s"
              domain={['dataMin', 'dataMax']}
              tickMargin={10} //pushes tick labels down to avoid overlap with y-axis labels
              label={{ value: 'Wind speed (m/s)', position: 'insideBottom', offset: -20 }}
            />
            <YAxis
              type="number"
              dataKey="powerDifference"
              name="Power Difference"
              unit=" kW"
              domain={[-100, 200]} //fixed scale based on actual data range (from sql output)
              label={{ value: 'Power (kW)', angle: -90, position: 'insideLeft', dx: -25 }}
            />
            <Tooltip />
            <Legend verticalAlign="bottom" align="center" wrapperStyle={{ paddingTop: 30 }} />
            {/*render one line per year, using the colour map for consistency*/}
            {deviationSeries.map(series => {
              const colour = colourMap[series.year] || '#888888';
              return (
                <Line
                  key={series.year}
                  type="monotone"
                  data={series.data}
                  dataKey="powerDifference"
                  stroke={colour}
                  strokeWidth={2}
                  dot={{ r: 3 }}
                  name={`D${series.year}`}
                />
              );
            })}
          </LineChart>
        </ResponsiveContainer>

        {/*statistics summary – computed directly from the same data, so always in sync*/}
        {stats && (
          <div style={{
            marginTop: '24px',
            padding: '16px',
            backgroundColor: '#f8f9fa',
            borderRadius: '8px',
            fontSize: '0.9rem',
            textAlign: 'left'
          }}>
            <h3 style={{ margin: '0 0 8px 0' }}>Key Statistics</h3>
            <ul style={{ margin: 0, paddingLeft: '20px' }}>
              <li><strong>Overall deviation</strong> ranges from <strong>{stats.overallMin} kW</strong> to <strong>{stats.overallMax} kW</strong> (mean = {stats.overallMean} kW).</li>
              <li><strong>Best performing year</strong>: {stats.bestYear} (average deviation = +{stats.bestYearMean} kW).</li>
              <li><strong>Worst performing year</strong>: {stats.worstYear} (average deviation = {stats.worstYearMean} kW).</li>
              <li><strong>Highest single deviation</strong>: +{stats.highestDeviation} kW at {stats.highestWindSpeed} m/s ({stats.highestYear}).</li>
              <li><strong>Lowest single deviation</strong>: {stats.lowestDeviation} kW at {stats.lowestWindSpeed} m/s ({stats.lowestYear}).</li>
            </ul>
          </div>
        )}
      </div>

      {/*========== Second Graph: Measured Power vs Wind Speed with Expected Curve ==========*/}
      {measuredSeries.length > 0 && expectedCurve.length > 0 && (
        <div className="dashboard-section">
          <div className="section-title">
            <FiZap size={24} color="#06A2DF" style={{ marginRight: '8px' }} />
            <h3>Measured Power vs Wind Speed (with Expected Curve)</h3>
          </div>
          <ResponsiveContainer width="100%" height={450}>
            <LineChart margin={{ top: 20, right: 30, left: 50, bottom: 60 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis
                type="number"
                dataKey="windSpeed"
                name="Wind Speed"
                unit=" m/s"
                domain={['dataMin', 'dataMax']}
                tickMargin={10}
                label={{ value: 'Wind speed (m/s)', position: 'insideBottom', offset: -20 }}
              />
              <YAxis
                type="number"
                dataKey="power"
                name="Power"
                unit=" kW"
                domain={[0, 2100]} //expected power reaches about 2050 kW, so 2100 gives headroom
                label={{ value: 'Power (kW)', angle: -90, position: 'insideLeft', dx: -25 }}
              />
              <Tooltip />
              <Legend verticalAlign="bottom" align="center" wrapperStyle={{ paddingTop: 30 }} />
              {/*expected curve (same for all years) – drawn as a black dashed line*/}
              <Line
                type="monotone"
                data={expectedCurve}
                dataKey="power"
                stroke="#000000"
                strokeWidth={3}
                strokeDasharray="5 5"
                dot={false}
                name="Expected Curve"
              />
              {/*measured lines per year – each year gets its own coloured line*/}
              {measuredSeries.map(series => {
                const colour = colourMap[series.year] || '#888888';
                return (
                  <Line
                    key={series.year}
                    type="monotone"
                    data={series.data}
                    dataKey="power"
                    stroke={colour}
                    strokeWidth={2}
                    dot={{ r: 3 }}
                    name={`Measured ${series.year}`}
                  />
                );
              })}
            </LineChart>
          </ResponsiveContainer>
          <p style={{ fontSize: '0.85rem', color: '#555', textAlign: 'center', marginTop: '12px' }}>
            Solid coloured lines are measured power per year. Black dashed line is the expected power curve from the turbine model.
          </p>
        </div>
      )}

      {/*simple table showing deviation score per year – directly from supabase*/}
      <div className="dashboard-section">
        <div className="section-title">
          <h3>Deviation Score Per Year</h3>
        </div>
        <table className="telemetry-table" style={{ minWidth: '300px' }}>
          <thead>
            <tr>
              <th>Year</th>
              <th>Deviation Score</th>
            </tr>
          </thead>
          <tbody>
            {deviationScores.map(item => (
              <tr key={item.year}>
                <td>{item.year}</td>
                <td>{item.score !== null && item.score !== undefined ? item.score.toFixed(2) : 'N/A'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default BenchmarkGraphs;