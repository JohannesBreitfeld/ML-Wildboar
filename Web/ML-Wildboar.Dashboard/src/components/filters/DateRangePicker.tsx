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
  const [customMode, setCustomMode] = useState(false);

  const handlePresetClick = (days: number) => {
    const end = new Date();
    const start = new Date();
    start.setDate(start.getDate() - days);
    onDateRangeChange(start, end);
    setCustomMode(false);
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
      <h3>Date Range</h3>
      <div className="preset-buttons">
        <button
          onClick={() => handlePresetClick(7)}
          className={!customMode ? 'active' : ''}
        >
          Last 7 Days
        </button>
        <button
          onClick={() => handlePresetClick(30)}
          className={!customMode ? 'active' : ''}
        >
          Last 30 Days
        </button>
        <button
          onClick={() => setCustomMode(true)}
          className={customMode ? 'active' : ''}
        >
          Custom
        </button>
      </div>

      {customMode && (
        <div className="custom-date-inputs">
          <div className="date-input-group">
            <label>Start Date:</label>
            <input
              type="date"
              value={formatDate(startDate)}
              onChange={(e) => handleCustomDateChange('start', e.target.value)}
              max={formatDate(endDate)}
            />
          </div>
          <div className="date-input-group">
            <label>End Date:</label>
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
          Selected: {formatDate(startDate)} to {formatDate(endDate)}
        </small>
      </div>
    </div>
  );
}
