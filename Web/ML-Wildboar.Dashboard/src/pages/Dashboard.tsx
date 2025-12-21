import React from 'react';
import { useDashboard } from '../context/DashboardContext';
import { useDetectionData } from '../hooks/useDetectionData';
import { useImageGallery } from '../hooks/useImageGallery';
import { DateRangePicker } from '../components/filters/DateRangePicker';
import { DetectionTimeSeriesChart } from '../components/charts/DetectionTimeSeriesChart';
import { DailyDetectionChart } from '../components/charts/DailyDetectionChart';
import { HourlyDistributionChart } from '../components/charts/HourlyDistributionChart';
import { ImageGallery } from '../components/gallery/ImageGallery';
import { LoadingSpinner } from '../components/shared/LoadingSpinner';
import { formatDate } from '../utils/dateHelpers';
import {
  processDetectionDataByDay,
  processDetectionDataByHour,
} from '../utils/chartHelpers';
import './Dashboard.css';

export function Dashboard() {
  const { dateRange, setDateRange, selectedDate, setSelectedDate, minConfidence } = useDashboard();

  // Fetch detection data
  const { data, isLoading, error } = useDetectionData({
    startDate: formatDate(dateRange.start),
    endDate: formatDate(dateRange.end),
    minConfidence,
  });

  // Fetch images for selected date
  const {
    data: imageData,
    isLoading: imagesLoading,
  } = useImageGallery(
    {
      date: selectedDate ? formatDate(selectedDate) : formatDate(new Date()),
      containsWildboar: true, // Only show wildboar detections
      minConfidence,
      pageSize: 50,
    },
    !!selectedDate // Only fetch when a date is selected
  );

  const handleDayClick = (date: string) => {
    setSelectedDate(new Date(date));
  };

  const handleTimeSeriesClick = (timestamp: string) => {
    const date = new Date(timestamp);
    setSelectedDate(date);
  };

  if (error) {
    return (
      <div className="dashboard-error">
        <h2>Error loading dashboard data</h2>
        <p>{(error as Error).message}</p>
        <button onClick={() => window.location.reload()}>Retry</button>
      </div>
    );
  }

  // Prepare chart data
  const dailyData = data
    ? processDetectionDataByDay(data.detections, new Map())
    : [];

  const hourlyData = data ? processDetectionDataByHour(data.detections) : [];

  return (
    <div className="dashboard">
      <DateRangePicker
        startDate={dateRange.start}
        endDate={dateRange.end}
        onDateRangeChange={setDateRange}
      />

      {isLoading ? (
        <LoadingSpinner size="large" message="Loading detection data..." />
      ) : data ? (
        <>
          <div className="stats-summary">
            <div className="stat-card">
              <h4>Total Images</h4>
              <p className="stat-value">{data.totalImages}</p>
            </div>
            <div className="stat-card">
              <h4>Wildboar Detected</h4>
              <p className="stat-value wildboar">{data.wildboarImages}</p>
            </div>
            <div className="stat-card">
              <h4>Detection Rate</h4>
              <p className="stat-value">
                {data.totalImages > 0
                  ? ((data.wildboarImages / data.totalImages) * 100).toFixed(1)
                  : 0}
                %
              </p>
            </div>
          </div>

          <div className="charts-grid">
            <div className="chart-container">
              <DetectionTimeSeriesChart
                data={data.detections}
                onPointClick={handleTimeSeriesClick}
              />
            </div>

            <div className="chart-container">
              <DailyDetectionChart data={dailyData} onDayClick={handleDayClick} />
            </div>

            <div className="chart-container">
              <HourlyDistributionChart data={hourlyData} />
            </div>
          </div>

          {selectedDate && (
            <div className="gallery-section">
              <div className="gallery-header-info">
                <h2>Images for {formatDate(selectedDate)}</h2>
                <button
                  className="clear-selection-btn"
                  onClick={() => setSelectedDate(null)}
                >
                  Clear Selection
                </button>
              </div>
              <ImageGallery
                images={imageData?.images || []}
                loading={imagesLoading}
              />
            </div>
          )}
        </>
      ) : (
        <div className="no-data">
          <p>No detection data available for the selected date range.</p>
        </div>
      )}
    </div>
  );
}
