import React, { useState } from 'react';
import { formatDate } from '../../utils/dateHelpers';
import './DateRangePicker.css';

interface DateRangePickerProps {
  startDate: Date;
  endDate: Date;
  onDateRangeChange: (start: Date, end: Date) => void;
}

export function DateRangePicker({
  startDate,
  endDate,
  onDateRangeChange,
}: DateRangePickerProps) {
  const [selectedPreset, setSelectedPreset] = useState<number | 'custom'>(7);

  const handlePresetClick = (days: number) => {
    const end = new Date();
    const start = new Date();
    start.setDate(start.getDate() - days);
    onDateRangeChange(start, end);
    setSelectedPreset(days);
  };

  const handleCustomDateChange = (type: 'start' | 'end', value: string) => {
    const newDate = new Date(value);
    if (type === 'start') {
      onDateRangeChange(newDate, endDate);
    } else {
      onDateRangeChange(startDate, newDate);
    }
  };

  return (
    <div className="date-range-picker">
      <h3>Tidsspann</h3>
      <div className="preset-buttons">
        <button
          onClick={() => handlePresetClick(7)}
          className={selectedPreset === 7 ? 'active' : ''}
        >
          Senaste 7 dagarna
        </button>
        <button
          onClick={() => handlePresetClick(30)}
          className={selectedPreset === 30 ? 'active' : ''}
        >
          Senaste 30 dagarna
        </button>
        <button
          onClick={() => setSelectedPreset('custom')}
          className={selectedPreset === 'custom' ? 'active' : ''}
        >
          Anpassad
        </button>
      </div>

      {selectedPreset === 'custom' && (
        <div className="custom-date-inputs">
          <div className="date-input-group">
            <label>Startdatum:</label>
            <input
              type="date"
              value={formatDate(startDate)}
              onChange={(e) => handleCustomDateChange('start', e.target.value)}
              max={formatDate(endDate)}
            />
          </div>
          <div className="date-input-group">
            <label>Slutdatum:</label>
            <input
              type="date"
              value={formatDate(endDate)}
              onChange={(e) => handleCustomDateChange('end', e.target.value)}
              min={formatDate(startDate)}
              max={formatDate(new Date())}
            />
          </div>
        </div>
      )}

      <div className="date-range-display">
        <small>
          Valt tidsspann: {formatDate(startDate)} till {formatDate(endDate)}
        </small>
      </div>
    </div>
  );
}
