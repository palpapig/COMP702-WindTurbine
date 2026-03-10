/*
purpose: a detailed view for a single turbine, showing historical graphs (e.g. power vs time, temperature trends)
what will be added later:
- fetch historical telemetry for the turbine from the api
- use chart.js to plot the data
- allow zoom/pan

*/
import React from 'react';

//displays the historical graphs for a single turbine using chart.js
//fetches telemetry data from /api/telemetry/{id}
const TurbineDetail: React.FC = () => {
    return <div>Turbine Detail placeholder</div>; //placeholder - later will contain charts
};

export default TurbineDetail; 