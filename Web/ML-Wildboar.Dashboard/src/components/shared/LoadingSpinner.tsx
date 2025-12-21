import React from 'react';
import './LoadingSpinner.css';

interface LoadingSpinnerProps {
  size?: 'small' | 'medium' | 'large';
  message?: string;
}

export function LoadingSpinner({ size = 'medium', message }: LoadingSpinnerProps) {
  const sizeMap = {
    small: '24px',
    medium: '48px',
    large: '72px',
  };

  return (
    <div className="loading-spinner-container">
      <div
        className="loading-spinner"
        style={{
          width: sizeMap[size],
          height: sizeMap[size],
          border: `4px solid #f3f3f3`,
          borderTop: `4px solid #007bff`,
        }}
      />
      {message && <p className="loading-message">{message}</p>}
    </div>
  );
}
