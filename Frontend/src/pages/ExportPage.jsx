import { useState } from 'react'
import { supabase } from '../utils/supabase'

function ExportPage() {
  const [loadingType, setLoadingType] = useState('')
  const [message, setMessage] = useState('')

  const convertToCSV = (rows) => {
    if (!rows || rows.length === 0) {
      return ''
    }

    const headers = Object.keys(rows[0])

    const escapeCSVValue = (value) => {
      if (value === null || value === undefined) {
        return ''
      }

      const stringValue = String(value)

      if (
        stringValue.includes(',') ||
        stringValue.includes('"') ||
        stringValue.includes('\n')
      ) {
        return `"${stringValue.replace(/"/g, '""')}"`
      }

      return stringValue
    }

    const csvRows = [
      headers.join(','),
      ...rows.map(row =>
        headers.map(header => escapeCSVValue(row[header])).join(',')
      )
    ]

    return csvRows.join('\n')
  }

  const downloadCSV = (csvContent, fileName) => {
    const blob = new Blob([csvContent], {
      type: 'text/csv;charset=utf-8;'
    })

    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')

    link.href = url
    link.setAttribute('download', fileName)
    document.body.appendChild(link)
    link.click()

    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  }

  const handleDownload = async (tableName, fileName, typeLabel) => {
    try {
      setLoadingType(typeLabel)
      setMessage('')

      const { data, error } = await supabase
        .from(tableName)
        .select('*')

      if (error) {
        throw error
      }

      if (!data || data.length === 0) {
        setMessage(`No data found in ${tableName}.`)
        return
      }

      const csvContent = convertToCSV(data)
      downloadCSV(csvContent, fileName)

      setMessage(`${typeLabel} CSV downloaded successfully.`)
    } catch (error) {
      console.error(`Error exporting ${typeLabel}:`, error)
      setMessage(`Failed to export ${typeLabel}. ${error.message}`)
    } finally {
      setLoadingType('')
    }
  }

  const handleDownloadAlerts = async () => {
    try {
      setLoadingType('Fault Detection Alerts')
      setMessage('')

      let { data, error } = await supabase
        .from('Alerts')
        .select('*')

      /*
        Fallback:
        If your Supabase table is named Alert instead of Alerts,
        this tries the singular table name.
      */
      if (error) {
        const fallbackResponse = await supabase
          .from('Alert')
          .select('*')

        data = fallbackResponse.data
        error = fallbackResponse.error
      }

      if (error) {
        throw error
      }

      if (!data || data.length === 0) {
        setMessage('No fault detection alerts found.')
        return
      }

      const csvContent = convertToCSV(data)
      downloadCSV(csvContent, 'fault_detection_alerts.csv')

      setMessage('Fault Detection Alerts CSV downloaded successfully.')
    } catch (error) {
      console.error('Error exporting Fault Detection Alerts:', error)
      setMessage(`Failed to export Fault Detection Alerts. ${error.message}`)
    } finally {
      setLoadingType('')
    }
  }

  return (
    <div className="export-page">
      <h1>Export Data</h1>

      <p className="export-description">
        Download key wind turbine datasets as CSV files.
      </p>

      <div className="export-card">
        <h2>Telemetry Data</h2>
        <p>
          Downloads all rows from the TurbineData table.
        </p>

        <button
          className="export-button"
          onClick={() =>
            handleDownload(
              'TurbineData',
              'telemetry_data.csv',
              'Telemetry Data'
            )
          }
          disabled={loadingType !== ''}
        >
          {loadingType === 'Telemetry Data'
            ? 'Downloading...'
            : 'Download Telemetry'}
        </button>
      </div>

      <div className="export-card">
        <h2>Benchmarking Insights</h2>
        <p>
          Downloads all rows from the BenchmarkResult table, including turbine ID,
          time range, and deviation score.
        </p>

        <button
          className="export-button"
          onClick={() =>
            handleDownload(
              'BenchmarkResult',
              'benchmark_results.csv',
              'Benchmarking Insights'
            )
          }
          disabled={loadingType !== ''}
        >
          {loadingType === 'Benchmarking Insights'
            ? 'Downloading...'
            : 'Download Benchmarking Insights'}
        </button>
      </div>

      <div className="export-card">
        <h2>Fault Detection Alerts</h2>
        <p>
          Downloads all rows from the Alerts table, including acknowledgement
          status and resolution timestamp.
        </p>

        <button
          className="export-button"
          onClick={handleDownloadAlerts}
          disabled={loadingType !== ''}
        >
          {loadingType === 'Fault Detection Alerts'
            ? 'Downloading...'
            : 'Download Fault Detection Alerts'}
        </button>
      </div>

      {message && (
        <p className="export-message">
          {message}
        </p>
      )}
    </div>
  )
}

export default ExportPage