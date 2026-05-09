//DashboardPage - contains the shared turbine selector and all three graphs
import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import EfficiencyGraph from '../components/EfficiencyGraph'
import PowerCurveGraph from '../components/PowerCurveGraph'
import RotorSpeedCurveGraph from '../components/RotorSpeedCurveGraph'
import WindSpeedGraph from '../components/WindSpeedGraph'
import RotorSpeedGraph from '../components/RotorSpeedGraph'
import PowerOutputGraph from '../components/PowerOutputGraph'
import VibrationGraph from '../components/VibrationGraph'
import BenchmarkGraphs from '../components/BenchmarkGraphs'
import { FiTrendingUp, FiZap } from 'react-icons/fi'
import { GiGears } from 'react-icons/gi'

function DashboardPage() {
  //state for turbine list & currently selected turbine
  const [turbines, setTurbines] = useState([])
  const [selectedTurbine, setSelectedTurbine] = useState('')
  const [graphView, setGraphView] = useState('dashboard') //adding secondary tab

  //fetch distinct turbine ids on mount
  useEffect(() => {
    async function fetchTurbineIds() {
      const { data, error } = await supabase
        .from('TurbineData')
        .select('TurbineId')
        .order('TurbineId')
      if (error) {
        console.error(error.message)
        return
      }
      //deduplicate just in case
      const unique = [...new Map(data.map(row => [row.TurbineId, row])).values()]
      setTurbines(unique)
      if (unique.length > 0) setSelectedTurbine(unique[0].TurbineId)
    }
    fetchTurbineIds()
  }, [])

  const handleTurbineChange = (e) => {
    setSelectedTurbine(e.target.value)
  }

  return (
    <>
      {/* turbine selector – placed outside all graphs, above them */}
      <div className="dashboard-section" style={{ padding: '1rem 1.8rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', flexWrap: 'wrap' }}>
          <label htmlFor="dashboardTurbineSelect" style={{ fontWeight: 500 }}>
            Select Turbine:
          </label>

          <select
            id="dashboardTurbineSelect"
            value={selectedTurbine}
            onChange={handleTurbineChange}
            style={{ padding: '0.3rem 0.8rem', borderRadius: '8px', border: '1px solid #ccc' }}
          >
            {turbines.map(t => (
              <option key={t.TurbineId} value={t.TurbineId}>
                {t.TurbineId}
              </option>
            ))}
          </select>
        </div>

        <div className="graph-tabs">
          <button
            className={graphView === 'dashboard' ? 'graph-tab active' : 'graph-tab'}
            onClick={() => setGraphView('dashboard')}
          >
            Telemertry
          </button>

          <button
            className={graphView === 'benchmarking' ? 'graph-tab active' : 'graph-tab'}
            onClick={() => setGraphView('benchmarking')}
          >
            Benchmarking
          </button>
        </div>
      </div>

      {graphView === 'dashboard' && (
        <>

          {/* efficiency graph – receives selectedTurbine as prop */}
          <section className="dashboard-section">
            <div className="section-title">
              <FiTrendingUp color="#06A2DF" size={24} />
              <h2>Efficiency Over Time</h2>
            </div>
            <div className="graph-wrapper">
              <EfficiencyGraph turbineId={selectedTurbine} />
            </div>
          </section>

          {/* power curve graph – receives prop */}
          <section className="dashboard-section">
            <div className="section-title">
              <FiZap color="#06A2DF" size={24} />
              <h2>Power Curve Analysis</h2>
            </div>
            <div className="graph-wrapper">
              <PowerCurveGraph turbineId={selectedTurbine} />
            </div>
          </section>

          {/* rotor speed curve graph – receives prop */}
          <section className="dashboard-section">
            <div className="section-title">
              <GiGears color="#06A2DF" size={24} />
              <h2>Rotor Speed vs Wind Speed</h2>
            </div>
            <div className="graph-wrapper">
              <RotorSpeedCurveGraph turbineId={selectedTurbine} />
            </div>
          </section>

          <section className="dashboard-section">
            <div className="section-title">
              <GiGears color="#06A2DF" size={24} />
              <h2>Turbine Details</h2>
            </div>

            <div className="graph-wrapper">
              <WindSpeedGraph turbineId={selectedTurbine} />
            </div>

            <div className="graph-wrapper">
              <RotorSpeedGraph turbineId={selectedTurbine} />
            </div>

            <div className="graph-wrapper">
              <PowerOutputGraph turbineId={selectedTurbine} />
            </div>

            <div className="graph-wrapper">
              <VibrationGraph turbineId={selectedTurbine} />
            </div>
          </section>
        </>
      )}

      {/* show benchmarking graphs only when Benchmarking tab is selected */}
      {graphView === 'benchmarking' && (
        <BenchmarkGraphs turbineId={selectedTurbine} />
      )}
    </>
  )
}

export default DashboardPage