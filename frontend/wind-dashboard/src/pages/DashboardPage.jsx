//DashboardPage - contains the shared turbine selector and selected benchmark/failure graphs
import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import BenchmarkGraphs from '../components/BenchmarkGraphs'

function DashboardPage() {
  const [turbines, setTurbines] = useState([])
  const [selectedTurbine, setSelectedTurbine] = useState('')

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

      const unique = [...new Map(data.map(row => [row.TurbineId, row])).values()]
      setTurbines(unique)

      if (unique.length > 0) {
        setSelectedTurbine(unique[0].TurbineId)
      }
    }

    fetchTurbineIds()
  }, [])

  const handleTurbineChange = (e) => {
    setSelectedTurbine(e.target.value)
  }

  return (
    <>
      {/* keep the turbine selector UI */}
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
      </div>

      {/* this shows your 2 benchmark graphs + table, and we add failure detection inside BenchmarkGraphs */}
      <BenchmarkGraphs turbineId={selectedTurbine} />
    </>
  )
}

export default DashboardPage