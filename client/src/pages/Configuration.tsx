/*
purpose: a react page where users can view & edit system configuration. it talks to the configuration api endpoints
what will be added later:
- fetch config on load (via api)
- render an editable table
- on save, send the updated config through PUT
*/

import React from 'react';

//editable key-value table. fetches config from GET /api/config & updates via PUT
const Configuration: React.FC = () => {
    return <div>Configuration placeholder</div>; //just a placeholder - later will contain the actual ui
};

export default Configuration;