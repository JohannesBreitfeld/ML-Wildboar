import React from 'react';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { DetectionDataPoint } from '../../types/api.types';
import { formatDateTime } from '../../utils/dateHelpers';

interface DetectionTimeSeriesChartProps {
  data: DetectionDataPoint[];
  onPointClick?: (timestamp: string) => void;
}

interface ChartDataPoint {
  timestamp: string;
  displayTime: string;
  count: number;
  confidence: number;
}

const CustomTooltip = (props: any) => {
  const { active, payload } = props;
  if (active && payload && payload.length) {
    const data = payload[0].payload as ChartDataPoint;
    return (
      <div
        style={{
          backgroundColor: 'white',
          padding: '10px',
          border: '1px solid #ccc',
          borderRadius: '4px',
        }}
      >
        <p style={{ margin: '0 0 5px 0', fontWeight: 'bold' }}>
          {data.displayTime}
        </p>
        <p style={{ margin: '0', color: '#00C853' }}>
          Detections: {data.count}
        </p>
        <p style={{ margin: '5px 0 0 0', color: '#666', fontSize: '0.9em' }}>
          Avg Confidence: {(data.confidence * 100).toFixed(1)}%
        </p>
      </div>
    );
  }
  return null;
};

export function DetectionTimeSeriesChart({
  data,
  onPointClick,
}: DetectionTimeSeriesChartProps) {
  const chartData: ChartDataPoint[] = data.map((d) => ({
    timestamp: d.timestamp,
    displayTime: formatDateTime(new Date(d.timestamp)),
    count: d.count,
    confidence: d.averageConfidence,
  }));

  const handleClick = (data: any) => {
    if (data && data.activePayload && data.activePayload.length > 0) {
      const point = data.activePayload[0].payload as ChartDataPoint;
      onPointClick?.(point.timestamp);
    }
  };

  return (
    <div style={{ width: '100%', height: '300px' }}>
      <h3 style={{ textAlign: 'center', marginBottom: '1rem' }}>
        Wildboar Detections Over Time
      </h3>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart
          data={chartData}
          onClick={handleClick}
          style={{ cursor: onPointClick ? 'pointer' : 'default' }}
        >
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis
            dataKey="displayTime"
            tick={{ fontSize: 12 }}
            interval="preserveStartEnd"
          />
          <YAxis
            label={{ value: 'Detections', angle: -90, position: 'insideLeft' }}
          />
          <Tooltip content={<CustomTooltip />} />
          <Area
            type="monotone"
            dataKey="count"
            stroke="#00C853"
            fill="#00C853"
            fillOpacity={0.6}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
