import React from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { HourlyChartData } from '../../utils/chartHelpers';

interface HourlyDistributionChartProps {
  data: HourlyChartData[];
}

const CustomTooltip = (props: any) => {
  const { active, payload } = props;
  if (active && payload && payload.length) {
    const data = payload[0].payload as HourlyChartData;
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
          Timme: {data.hourLabel}
        </p>
        <p style={{ margin: '0', color: '#00C853' }}>
          Detektioner: {data.count}
        </p>
      </div>
    );
  }
  return null;
};

export function HourlyDistributionChart({ data }: HourlyDistributionChartProps) {
  const safeData = data || [];

  return (
    <div style={{ width: '100%', height: '300px' }}>
      <h3 style={{ textAlign: 'center', marginBottom: '1rem' }}>
        Tidsfördelning av detektioner
      </h3>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={safeData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis
            dataKey="hour"
            tick={{ fontSize: 12 }}
            interval={1}
          />
          <YAxis label={{ value: 'Detektioner', angle: -90, position: 'insideLeft' }} />
          <Tooltip content={<CustomTooltip />} />
          <Line
            type="monotone"
            dataKey="count"
            stroke="#00C853"
            strokeWidth={2}
            dot={{ fill: '#00C853', r: 4 }}
            activeDot={{ r: 6 }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
