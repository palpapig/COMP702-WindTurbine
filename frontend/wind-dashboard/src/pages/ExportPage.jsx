import { FiDownload } from 'react-icons/fi';

function ExportPage() {
  return (
    <div className="dashboard-section">
      <div className="section-title">
        <FiDownload color="#82C340" size={24} />
        <h2>Export Data (Placeholder)</h2>
      </div>
      <p>CSV/JSON export options will be implemented here.</p>
    </div>
  );
}

export default ExportPage;