import React, { useState } from 'react';
import { ImageDto } from '../../types/api.types';
import { formatDateTime } from '../../utils/dateHelpers';
import './ImageThumbnail.css';

interface ImageThumbnailProps {
  image: ImageDto;
  onClick: () => void;
}

export function ImageThumbnail({ image, onClick }: ImageThumbnailProps) {
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);

  const handleImageLoad = () => {
    setImageLoaded(true);
  };

  const handleImageError = () => {
    setImageError(true);
  };

  return (
    <div className="image-thumbnail" onClick={onClick}>
      <div className="thumbnail-image-container">
        {!imageLoaded && !imageError && (
          <div className="thumbnail-loading">Loading...</div>
        )}
        {imageError ? (
          <div className="thumbnail-error">
            <span>Failed to load</span>
          </div>
        ) : (
          <img
            src={image.imageUrl}
            alt={`Captured at ${formatDateTime(new Date(image.capturedAt))}`}
            onLoad={handleImageLoad}
            onError={handleImageError}
            style={{ display: imageLoaded ? 'block' : 'none' }}
          />
        )}
        {image.containsWildboar && (
          <div className="wildboar-badge">
            <span>🐗 Wildboar</span>
          </div>
        )}
      </div>
      <div className="thumbnail-info">
        <div className="thumbnail-time">
          {formatDateTime(new Date(image.capturedAt))}
        </div>
        {image.containsWildboar && (
          <div className="thumbnail-confidence">
            Confidence: {(image.confidenceScore * 100).toFixed(1)}%
          </div>
        )}
      </div>
    </div>
  );
}
