import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import reportWebVitals from './reportWebVitals';
import * as serviceWorkerRegistration from './serviceWorkerRegistration';

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// Register service worker for PWA functionality with automatic updates
serviceWorkerRegistration.register({
  onSuccess: () => {
    console.log('Service worker registered. App is ready for offline use.');
  },
  onUpdate: (registration) => {
    console.log('New version detected! Installing update...');
    // Automatically skip waiting and activate the new service worker
    registration.waiting?.postMessage({ type: 'SKIP_WAITING' });

    // Listen for the controlling service worker change
    navigator.serviceWorker.addEventListener('controllerchange', () => {
      // Reload the page to get the new version
      window.location.reload();
    });
  },
});

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
