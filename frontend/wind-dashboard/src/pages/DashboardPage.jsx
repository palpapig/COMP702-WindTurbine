//DashboardPage now shows only benchmarking graphs for 1 turbine
import BenchmarkGraphs from '../components/BenchmarkGraphs';

function DashboardPage() {
  return (
    <div className="dashboard-container">
      <BenchmarkGraphs />
    </div>
  );
}

export default DashboardPage;