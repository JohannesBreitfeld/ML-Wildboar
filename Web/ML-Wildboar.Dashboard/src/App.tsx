import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DashboardProvider } from './context/DashboardContext';
import { ErrorBoundary } from './components/shared/ErrorBoundary';
import { Dashboard } from './pages/Dashboard';
import './App.css';

// Create a QueryClient instance
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <DashboardProvider>
          <div className="App">
            <header className="App-header">
              <h1>Wildboar Monitoring Dashboard</h1>
            </header>
            <main>
              <Dashboard />
            </main>
          </div>
        </DashboardProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}

export default App;
