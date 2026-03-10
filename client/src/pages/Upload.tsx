/*
purpose: a page with a file input button for uploading historical turbine data files. corresponds to the manual upload path in our architecture
what will be added later:
- a file picker
- on submit, send the file to the /api/upload endpoint
- show upload progress/success message
*/

import React from 'react';

//file input form. calls POST /api/upload with the selected file
const Upload: React.FC = () => {
    return <div>Upload placeholder</div>; //placehplder - will later contain a file input and upload logic
};

export default Upload;