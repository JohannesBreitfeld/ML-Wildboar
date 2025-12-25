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
  onDayClick?: (date: string, containsWildboar?: boolean) => void;
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
          Vildsvin: {wildboar}
        </p>
        <p style={{ margin: '5px 0 0 0', color: '#594AE2' }}>
          Inte vildsvin: {noWildboar}
        </p>
        <p style={{ margin: '5px 0 0 0', borderTop: '1px solid #eee', paddingTop: '5px' }}>
          Totalt: {total}
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
  const safeData = data || [];

  const handleWildboarBarClick = (data: any) => {
    if (data && data.date) {
      onDayClick?.(data.date, true);
    }
  };

  const handleNoWildboarBarClick = (data: any) => {
    if (data && data.date) {
      onDayClick?.(data.date, false);
    }
  };

  return (
    <div style={{ width: '100%', height: '300px' }}>
      <h3 style={{ textAlign: 'center', marginBottom: '1rem' }}>
        Bilder per dag
      </h3>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={safeData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis
            dataKey="dateLabel"
            tick={{ fontSize: 12 }}
            angle={safeData.length > 14 ? -45 : 0}
            textAnchor={safeData.length > 14 ? 'end' : 'middle'}
            height={safeData.length > 14 ? 80 : 60}
          />
          <YAxis label={{ value: 'Bilder', angle: -90, position: 'insideLeft' }} />
          <Tooltip content={<CustomTooltip />} />
          <Legend />
          <Bar
            dataKey="wildboar"
            stackId="a"
            fill="#00C853"
            name="Vildsvin"
            onClick={handleWildboarBarClick}
            cursor="pointer"
          />
          <Bar
            dataKey="noWildboar"
            stackId="a"
            fill="#594AE2"
            name="Inte vildsvin"
            onClick={handleNoWildboarBarClick}
            cursor="pointer"
          />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
