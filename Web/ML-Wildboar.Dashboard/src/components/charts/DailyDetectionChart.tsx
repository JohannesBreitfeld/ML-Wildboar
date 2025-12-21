import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { DailyChartData } from '../../utils/chartHelpers';

interface DailyDetectionChartProps {
  data: DailyChartData[];
  onDayClick?: (date: string) => void;
}

const CustomTooltip = (props: any) => {
  const { active, payload, label } = props;
  if (active && payload && payload.length) {
    const wildboar = payload.find((p: any) => p.dataKey === 'wildboar')?.value || 0;
    const noWildboar = payload.find((p: any) => p.dataKey === 'noWildboar')?.value || 0;
    const total = Number(wildboar) + Number(noWildboar);

    return (
      <div
        style={{
          backgroundColor: 'white',
          padding: '10px',
          border: '1px solid #ccc',
          borderRadius: '4px',
        }}
      >
        <p style={{ margin: '0 0 5px 0', fontWeight: 'bold' }}>{label}</p>
        <p style={{ margin: '0', color: '#00C853' }}>
          Wildboar: {wildboar}
        </p>
        <p style={{ margin: '5px 0 0 0', color: '#594AE2' }}>
          No Wildboar: {noWildboar}
        </p>
        <p style={{ margin: '5px 0 0 0', borderTop: '1px solid #eee', paddingTop: '5px' }}>
          Total: {total}
        </p>
      </div>
    );
  }
  return null;
};

export function DailyDetectionChart({
  data,
  onDayClick,
}: DailyDetectionChartProps) {
  const handleClick = (data: any) => {
    if (data && data.activePayload && data.activePayload.length > 0) {
      const point = data.activePayload[0].payload as DailyChartData;
      onDayClick?.(point.date);
    }
  };

  return (
    <div style={{ width: '100%', height: '300px' }}>
      <h3 style={{ textAlign: 'center', marginBottom: '1rem' }}>
        Images Per Day
      </h3>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart
          data={data}
          onClick={handleClick}
          style={{ cursor: onDayClick ? 'pointer' : 'default' }}
        >
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis
            dataKey="dateLabel"
            tick={{ fontSize: 12 }}
            angle={data.length > 14 ? -45 : 0}
            textAnchor={data.length > 14 ? 'end' : 'middle'}
            height={data.length > 14 ? 80 : 60}
          />
          <YAxis label={{ value: 'Images', angle: -90, position: 'insideLeft' }} />
          <Tooltip content={<CustomTooltip />} />
          <Legend />
          <Bar dataKey="wildboar" stackId="a" fill="#00C853" name="Wildboar" />
          <Bar dataKey="noWildboar" stackId="a" fill="#594AE2" name="No Wildboar" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
