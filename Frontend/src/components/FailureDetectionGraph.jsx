import { useState, useEffect } from 'react'
import { supabase } from '../utils/supabase'
import {
    ResponsiveContainer,
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
} from 'recharts'
import { FaLightbulb } from 'react-icons/fa'

function FailureDetectionGraph({ turbineId }) {
    const [chartData, setChartData] = useState([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')

    useEffect(() => {
        if (!turbineId) {
            setChartData([])
            return
        }

        async function fetchFailureDetectionData() {
            setLoading(true)
            setError('')

            const { data, error } = await supabase
                .from('FailureDetectionResults')
                .select('Timestamp, PredictedValue, ActualValue, IsAbnormal, AlarmLvl, A1Triggered, A2Triggered')
                .eq('TurbineId', turbineId)
                .order('Timestamp', { ascending: true })

            if (error) {
                setError(error.message)
                setLoading(false)
                return
            }

            const formatted = (data || []) 
            .map((row) => {
                const date = new Date(row.Timestamp)
                return {
                    timestamp: row.Timestamp,
                    timestampLabel: date.toLocaleString(),
                    predictedValue: row.PredictedValue ?? null,
                    actualValue: row.ActualValue ?? null,
                    isAbnormal: row.IsAbnormal ?? false,
                    alarmLevel: row.AlarmLvl ?? 0,
                    a1Triggered: row.A1Triggered ?? false,
                    a2Triggered: row.A2Triggered ?? false,

                    a1Dot: row.A1Triggered ? row.ActualValue : null,
                    a2Dot: row.A2Triggered ? row.ActualValue : null,
                }
            })

            setChartData(formatted)
            setLoading(false)
        }

        fetchFailureDetectionData()
    }, [turbineId])

    if (loading) return <p>loading failure detection temperature data...</p>
    if (error) return <p className="error-text">error: {error}</p>
    if (!turbineId) return <p>select a turbine to view failure detection results.</p>
    if (chartData.length === 0) return <p>no failure detection results found for this turbine yet.</p>

    return (
        <div style={{ width: '100%', marginTop: '1rem' }}>
            <h3>Actual vs Predicted Temperature Over Time</h3>
            <p style={{ fontSize: '0.85rem', color: '#555' }}>
                Compares the actual measured temperature against the model-predicted temperature from the failure detection system.
            </p>

            <div style={{ width: '100%', height: 420 }}>
                <ResponsiveContainer width="100%" height="100%">
                    <LineChart
                        data={chartData}
                        margin={{ top: 20, right: 30, left: 20, bottom: 80 }}
                    >
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis
                            dataKey="timestampLabel"
                            angle={-45}
                            textAnchor="end"
                            height={90}
                            interval="preserveStartEnd"
                            tick={{ fontSize: 10 }}
                            label={{
                                value: 'Date and Time',
                                position: 'insideBottom',
                                offset: -55,
                                style: { textAnchor: 'middle', fill: '#666' }
                            }}
                        />
                        <YAxis
                            domain={[30, 70]}
                            label={{
                                value: 'Temperature',
                                angle: -90,
                                position: 'insideLeft',
                                style: { textAnchor: 'middle' }
                            }}
                        />
                        <Tooltip
                            labelFormatter={(label) => `Timestamp: ${label}`}
                            formatter={(value, name, props) => {
                                if (name === 'Predicted Temperature') return [value, name]

                                if (name === 'Actual Temperature') {
                                    const abnormalText = props?.payload?.isAbnormal ? 'Abnormal' : 'Normal'
                                    return [
                                        `${value} (${abnormalText}, Alarm ${props?.payload?.alarmLevel ?? 0})`,
                                        name
                                    ]
                                }

                                if (name === 'A1 Triggered') {
                                    return [`Actual Temperature: ${value}`, 'A1 Triggered']
                                }

                                if (name === 'A2 Triggered') {
                                    return [`Actual Temperature: ${value}`, 'A2 Triggered']
                                }

                                return [value, name]
                            }}
                        />
                        <Legend verticalAlign="top" height={36} />
                        <Line
                            type="monotone"
                            dataKey="predictedValue"
                            name="Predicted Temperature"
                            stroke="#06A2DF"
                            strokeWidth={2}
                            dot={false}
                            connectNulls
                        />
                        <Line
                            type="monotone"
                            dataKey="actualValue"
                            name="Actual Temperature"
                            stroke="#82C340"
                            strokeWidth={2}
                            dot={false}
                            connectNulls
                        />

                        <Line
                            type="monotone"
                            dataKey="a1Dot"
                            name="A1 Triggered"
                            stroke="#ff9800"
                            fill="#ff9800"
                            strokeWidth={0}
                            line={false}
                            dot={{ r: 5 }}
                            activeDot={{ r: 7 }}
                            connectNulls={false}
                        />

                        <Line
                            type="monotone"
                            dataKey="a2Dot"
                            name="A2 Triggered"
                            stroke="#d9534f"
                            fill="#d9534f"
                            strokeWidth={0}
                            line={false}
                            dot={{ r: 5 }}
                            activeDot={{ r: 7 }}
                            connectNulls={false}
                        />
                    </LineChart>
                </ResponsiveContainer>
            </div>

            <div style={{
                textAlign: 'center',
                fontSize: '0.8rem',
                color: '#666',
                fontStyle: 'italic',
                marginTop: '12px',
                borderTop: '1px solid #e0e0e0',
                paddingTop: '10px',
                maxWidth: '80%',
                marginLeft: 'auto',
                marginRight: 'auto'
            }}>
                <FaLightbulb
                    color="#FFD700"
                    style={{ fontSize: '0.9rem', marginRight: '6px', verticalAlign: 'middle' }}
                />
                When the green actual temperature line diverges noticeably from the blue predicted temperature line over time, it may indicate abnormal operating behaviour or a developing fault.
            </div>
        </div>
    )
}

export default FailureDetectionGraph