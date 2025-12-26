import React from 'react';
import { useDashboard } from '../context/DashboardContext';
import { useDetectionData } from '../hooks/useDetectionData';
import { useImageGallery } from '../hooks/useImageGallery';
import { DateRangePicker } from '../components/filters/DateRangePicker';
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
  const [showWildboarImages, setShowWildboarImages] = React.useState<boolean | undefined>(true);

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
      containsWildboar: showWildboarImages,
      minConfidence,
      pageSize: 50,
    },
    !!selectedDate // Only fetch when a date is selected
  );

  const handleDayClick = (date: string, containsWildboar?: boolean) => {
    setSelectedDate(new Date(date));
    setShowWildboarImages(containsWildboar);
  };

  if (error) {
    return (
      <div className="dashboard-error">
        <h2>Fel vid inläsning av dashboard-data</h2>
        <p>{(error as Error).message}</p>
        <button onClick={() => window.location.reload()}>Försök igen</button>
      </div>
    );
  }

  // Prepare chart data
  const dailyData = data
    ? processDetectionDataByDay(
        data.detections,
        new Map(Object.entries(data.totalImagesByDay))
      )
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
        <LoadingSpinner size="large" message="Hämtar vald data..." />
      ) : data ? (
        <>
          <div className="stats-summary">
            <div className="stat-card">
              <h4>Totala bilder</h4>
              <p className="stat-value">{data.totalImages}</p>
            </div>
            <div className="stat-card">
              <h4>Vildsvin upptäckta</h4>
              <p className="stat-value wildboar">{data.wildboarImages}</p>
            </div>
            <div className="stat-card">
              <h4>Detektionsfrekvens</h4>
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
              <DailyDetectionChart data={dailyData} onDayClick={handleDayClick} />
            </div>

            <div className="chart-container">
              <HourlyDistributionChart data={hourlyData} />
            </div>
          </div>

          {selectedDate && (
            <div className="gallery-section">
              <div className="gallery-header-info">
                <h2>
                  {showWildboarImages === true && 'Vildsvin Bilder för '}
                  {showWildboarImages === false && 'Övriga bilder för '}
                  {showWildboarImages === undefined && 'Alla bilder för '}
                  {formatDate(selectedDate)}
                </h2>
                <button
                  className="clear-selection-btn"
                  onClick={() => setSelectedDate(null)}
                >
                  Rensa val för bildvisning
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
          <p>Ingen detektionsdata tillgänglig för det valda datumintervallet.</p>
        </div>
      )}
    </div>
  );
}
