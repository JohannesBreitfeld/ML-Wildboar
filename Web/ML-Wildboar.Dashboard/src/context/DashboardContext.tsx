import React, { createContext, useContext, useState, ReactNode } from 'react';

interface DashboardState {
  dateRange: { start: Date; end: Date };
  timeRange: { start: number; end: number };
  minConfidence: number;
  selectedDate: Date | null;
}

interface DashboardContextType extends DashboardState {
  setDateRange: (start: Date, end: Date) => void;
  setTimeRange: (start: number, end: number) => void;
  setMinConfidence: (value: number) => void;
  setSelectedDate: (date: Date | null) => void;
}

const DashboardContext = createContext<DashboardContextType | undefined>(undefined);

interface DashboardProviderProps {
  children: ReactNode;
}

export function DashboardProvider({ children }: DashboardProviderProps) {
  const [dateRange, setDateRangeState] = useState<{ start: Date; end: Date }>({
    start: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // 7 days ago
    end: new Date(),
  });

  const [timeRange, setTimeRangeState] = useState<{ start: number; end: number }>({
    start: 0,
    end: 23,
  });

  const [minConfidence, setMinConfidenceState] = useState<number>(0.5);
  const [selectedDate, setSelectedDateState] = useState<Date | null>(null);

  const setDateRange = (start: Date, end: Date) => {
    setDateRangeState({ start, end });
  };

  const setTimeRange = (start: number, end: number) => {
    setTimeRangeState({ start, end });
  };

  const setMinConfidence = (value: number) => {
    setMinConfidenceState(value);
  };

  const setSelectedDate = (date: Date | null) => {
    setSelectedDateState(date);
  };

  return (
    <DashboardContext.Provider
      value={{
        dateRange,
        timeRange,
        minConfidence,
        selectedDate,
        setDateRange,
        setTimeRange,
        setMinConfidence,
        setSelectedDate,
      }}
    >
      {children}
    </DashboardContext.Provider>
  );
}

export function useDashboard() {
  const context = useContext(DashboardContext);
  if (!context) {
    throw new Error('useDashboard must be used within DashboardProvider');
  }
  return context;
}
