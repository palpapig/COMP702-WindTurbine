import { useState, useEffect } from 'react'
import { supabase } from './utils/supabase'
import './Table.css'

function Table() {
  const [turbineData, setTurbineData] = useState([])

  useEffect(() => {
    //Make GET request to Supabase
    
    async function getData() {

      const { data, error } = await supabase
      .from('TurbineData') //get from TurbineData table
      .select('*') //get all columns
      .range(0,29) //only get first 30 rows
      
      if (data){
        setTurbineData(data)
      }
    }
    
    getData()
  }, [])

  return (
    <>
      <h1>Turbine Telemetry Data</h1>
      <ul> 
      {/* iterate over each row of data and make a list item for each */}
      {turbineData.map((row, index) => (
        <li key={row.Id}>Entry {row.Id}: Turbine={row.TurbineId} Power={row.PowerOutput} Time={row.Timestamp}</li>
      ))}
    </ul>


    </>
  )
}

export default Table
