import React, { useEffect } from 'react';
import Lightbox from 'yet-another-react-lightbox';
import 'yet-another-react-lightbox/styles.css';
import { ImageDto } from '../../types/api.types';
import { formatDateTime } from '../../utils/dateHelpers';
import './ImageLightbox.css';

interface ImageLightboxProps {
  images: ImageDto[];
  currentIndex: number;
  onClose: () => void;
  onPrevious: () => void;
  onNext: () => void;
}

export function ImageLightbox({
  images,
  currentIndex,
  onClose,
  onPrevious,
  onNext,
}: ImageLightboxProps) {
  const currentImage = images[currentIndex];

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      } else if (e.key === 'ArrowLeft') {
        onPrevious();
      } else if (e.key === 'ArrowRight') {
        onNext();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose, onPrevious, onNext]);

  // Prepare slides for lightbox
  const slides = images.map((image) => ({
    src: image.imageUrl,
    alt: `Captured at ${formatDateTime(new Date(image.capturedAt))}`,
  }));

  return (
    <Lightbox
      open={true}
      close={onClose}
      index={currentIndex}
      slides={slides}
      on={{
        view: ({ index }) => {
          // Update current index when user navigates
          if (index !== currentIndex) {
            if (index > currentIndex) {
              onNext();
            } else {
              onPrevious();
            }
          }
        },
      }}
      render={{
        // Custom slide footer with metadata
        slideFooter: () => {
          if (!currentImage) return null;

          return (
            <div className="lightbox-footer">
              <div className="lightbox-metadata">
                <div className="metadata-row">
                  <span className="metadata-label">Captured:</span>
                  <span className="metadata-value">
                    {formatDateTime(new Date(currentImage.capturedAt))}
                  </span>
                </div>
                {currentImage.containsWildboar && (
                  <>
                    <div className="metadata-row">
                      <span className="metadata-label">Detection:</span>
                      <span className="metadata-value wildboar-detected">
                        🐗 Wildboar Detected
                      </span>
                    </div>
                    <div className="metadata-row">
                      <span className="metadata-label">Confidence:</span>
                      <span className="metadata-value">
                        {(currentImage.confidenceScore * 100).toFixed(1)}%
                      </span>
                    </div>
                  </>
                )}
              </div>
              <div className="lightbox-counter">
                {currentIndex + 1} / {images.length}
              </div>
            </div>
          );
        },
      }}
      carousel={{
        finite: false,
      }}
      animation={{
        fade: 300,
        swipe: 300,
      }}
    />
  );
}
