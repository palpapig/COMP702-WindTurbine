//dashboard page – now shows benchmark graphs & failure detection graph
//hardcoded all turbine ids because supabase was only returning bk-test-4
// (probably due to rls or permissions?). the list comes from the actual database query
import { useState } from 'react';
import BenchmarkGraphs from '../components/BenchmarkGraphs';
import FailureDetectionGraph from '../components/FailureDetectionGraph';
import DegradationResultTable from '../components/DegradationResultTable';
import { FiActivity } from 'react-icons/fi';

function DashboardPage() {
  // bk-test-4 is first because it's the one with failure data
  const turbineIds = [
    'BK-TEST-4',
    'WT-001', 'WT-002', 'WT-003', 'WT-004', 'WT-005', 'WT-006', 'WT-007', 'WT-008', 'WT-009',
    'WT-010', 'WT-011', 'WT-012', 'WT-013', 'WT-014', 'WT-015', 'WT-016', 'WT-017', 'WT-018', 'WT-019',
    'WT-020', 'WT-021', 'WT-022', 'WT-023', 'WT-024', 'WT-025', 'WT-026', 'WT-027', 'WT-028', 'WT-029',
    'WT-030', 'WT-031', 'WT-032', 'WT-033', 'WT-034', 'WT-035', 'WT-036', 'WT-037', 'WT-038', 'WT-039',
    'WT-040', 'WT-041', 'WT-042', 'WT-043', 'WT-044', 'WT-045', 'WT-046', 'WT-047', 'WT-048', 'WT-049',
    'WT-050', 'WT-051', 'WT-052', 'WT-053', 'WT-054', 'WT-055', 'WT-056', 'WT-057', 'WT-058', 'WT-059',
    'WT-060', 'WT-061', 'WT-062', 'WT-063', 'WT-064', 'WT-065', 'WT-066', 'WT-067', 'WT-068', 'WT-069',
    'WT-070', 'WT-071', 'WT-072', 'WT-073', 'WT-074', 'WT-075', 'WT-076', 'WT-077', 'WT-078', 'WT-079',
    'WT-080', 'WT-081', 'WT-082', 'WT-083', 'WT-084', 'WT-085', 'WT-086', 'WT-087', 'WT-088', 'WT-089',
    'WT-090', 'WT-091', 'WT-092', 'WT-093', 'WT-094', 'WT-095', 'WT-096', 'WT-097', 'WT-098', 'WT-099'
  ];

  const [selectedTurbine, setSelectedTurbine] = useState('BK-TEST-4');

  return (
    <div className="dashboard-container">
      {/* turbine selector – now shows all ids */}
      <div style={{ marginTop: '0.5rem', marginBottom: '20px', padding: '10px', borderRadius: '8px' }}>
        <label style={{ marginRight: '10px', fontWeight: 'bold', color: '#1f2937' }}>Select Turbine: </label>
        <select
          value={selectedTurbine}
          onChange={(e) => setSelectedTurbine(e.target.value)}
          style={{ padding: '6px 12px', borderRadius: '4px', border: '1px solid #ccc' }}
        >
          {turbineIds.map(turbineId => (
            <option key={turbineId} value={turbineId}>{turbineId}</option>
          ))}
        </select>
      </div>

      {/* benchmark graphs – shows power difference vs wind speed etc. */}
      <BenchmarkGraphs />

      {/* fred's failure detection graph – uses selected turbine */}
      <section className="dashboard-section">
        <div className="section-title">
          <FiActivity color="#06A2DF" size={24} />
          <h2>Failure Detection</h2>
        </div>
        <div className="graph-wrapper">
          <FailureDetectionGraph turbineId={selectedTurbine} />
        </div>
      </section>

      <section className="dashboard-section">
        <div className="section-title">
          <FiActivity color="#06A2DF" size={24} />
          <h2>Degradation Results</h2>
        </div>

        <div className="graph-wrapper">
          <DegradationResultTable turbineId={selectedTurbine} />
        </div>
      </section>
    </div>


  );
}

export default DashboardPage;