import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ErrorBoundary } from './components/shared/ErrorBoundary';
import { Dashboard } from './pages/Dashboard';
import './App.css';

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
        <div className="App">
          <header className="App-header">
            <span className="App-logo">W</span>
            <h1>Wildboar</h1>
          </header>
          <Dashboard />
        </div>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}

export default App;
