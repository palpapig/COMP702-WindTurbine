import { useState, useEffect } from 'react';
import { supabase } from '../utils/supabase';
import { FaCheck, FaBell, FaExclamationTriangle, FaHistory } from 'react-icons/fa';
import './AlertsPage.css';

//component to show all historical a1/a2 alarms
//each alarm has turbine id, timestamp, alarm level, status & an acknowledge button
//acknowledging an alarm sets isacknowledged = true in the database
function AlertsPage() {
  const [alarms, setAlarms] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showResolved, setShowResolved] = useState(false);   //whether to show acknowledged alarms

  //fetch alarms from supabase, then filter in javascript
  //do the filtering client-side because supabase's .gt('alarmlvl', 0) sometimes
  //has issues with integer columns and rls policies. this is simpler and reliable
  const fetchAlarms = async () => {
    setLoading(true);
    setError('');
    try {
      //get all failure detection results, newest first
      //we only need the columns shown in the table
      const { data, error: fetchError } = await supabase
        .from('FailureDetectionResult')
        .select('Id, TurbineId, Timestamp, AlarmLvl, IsAcknowledged')
        .order('Timestamp', { ascending: false });

      if (fetchError) throw fetchError;

      //only keep rows where alarm level is 1 or 2 (a1 or a2)
      let filtered = (data || []).filter(alarm => alarm.AlarmLvl > 0);

      //if we are not showing resolved alarms, filter out acknowledged ones
      if (!showResolved) {
        filtered = filtered.filter(alarm => !alarm.IsAcknowledged);
      }
      setAlarms(filtered);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  //refetch when 'show resolved' checkbox toggles
  useEffect(() => {
    fetchAlarms();
  }, [showResolved]);

  //mark a single alarm as acknowledged (resolved)
  const acknowledgeAlarm = async (alarmId) => {
    try {
      const { error } = await supabase
        .from('FailureDetectionResult')
        .update({ IsAcknowledged: true })
        .eq('Id', alarmId);
      if (error) throw error;
      //refresh the list so the alarm disappears (or moves to resolved section)
      fetchAlarms();
    } catch (err) {
      setError(`Failed to acknowledge: ${err.message}`);
    }
  };

  //helper to turn alarm level (1,2,0) into label, icon & colour
  const getAlarmLabel = (lvl) => {
    if (lvl === 1) return { text: 'A1', icon: <FaExclamationTriangle />, colour: '#f0ad4e' };
    if (lvl === 2) return { text: 'A2', icon: <FaBell />, colour: '#d9534f' };
    return { text: 'None', icon: <FaCheck />, colour: '#82C340' };
  };

  if (loading) return <div className="alerts-page"><p>loading alarms...</p></div>;
  if (error) return <div className="alerts-page"><p className="error-text">Error: {error}</p></div>;

  return (
    <div className="alerts-page">
      <div className="section-title">
        <FaHistory size={24} color="#06A2DF" />
        <h2>Historical Alarms</h2>
      </div>
      <div className="alerts-controls">
        <label style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <input
            type="checkbox"
            checked={showResolved}
            onChange={(e) => setShowResolved(e.target.checked)}
          />
          Show resolved alarms
        </label>
        <button onClick={fetchAlarms} className="refresh-btn">Refresh</button>
      </div>
      {alarms.length === 0 ? (
        <p>No alarms found {showResolved ? '(including resolved)' : '(unresolved)'}.</p>
      ) : (
        <div className="table-container">
          <table className="alerts-table">
            <thead>
              <tr>
                <th>Turbine ID</th>
                <th>Timestamp (UTC)</th>
                <th>Alarm Level</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {alarms.map(alarm => {
                const label = getAlarmLabel(alarm.AlarmLvl);
                return (
                  <tr key={alarm.Id}>
                    <td>{alarm.TurbineId}</td>
                    <td>{new Date(alarm.Timestamp).toLocaleString()}</td>
                    <td style={{ color: label.colour, fontWeight: 'bold' }}>
                      {label.icon} {label.text}
                    </td>
                    <td style={{ whiteSpace: 'nowrap' }}>
                      {alarm.IsAcknowledged ? (
                        <span><FaCheck color="#28a745" /> Acknowledged</span>
                      ) : (
                        'Active'
                      )}
                    </td>
                    <td>
                      {!alarm.IsAcknowledged && (
                        <button
                          onClick={() => acknowledgeAlarm(alarm.Id)}
                          className="acknowledge-btn"
                        >
                          Acknowledge
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default AlertsPage;