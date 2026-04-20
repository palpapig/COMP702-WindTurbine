import { FiSettings } from 'react-icons/fi';

function ConfigPage() {
  return (
    <div className="dashboard-section">
      <div className="section-title">
        <FiSettings color="#82C340" size={24} />
        <h2>Configuration (Placeholder)</h2>
      </div>
      <p>Threshold settings, power curve parameters, etc. will go here.</p>
    </div>
  );
}

export default ConfigPage;