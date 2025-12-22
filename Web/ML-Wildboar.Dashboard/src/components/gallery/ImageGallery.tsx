import React, { useState } from 'react';
import { ImageDto } from '../../types/api.types';
import { ImageThumbnail } from './ImageThumbnail';
import { ImageLightbox } from './ImageLightbox';
import './ImageGallery.css';

interface ImageGalleryProps {
  images: ImageDto[];
  loading?: boolean;
}

export function ImageGallery({ images, loading }: ImageGalleryProps) {
  const [selectedIndex, setSelectedIndex] = useState<number>(-1);

  const handleThumbnailClick = (index: number) => {
    setSelectedIndex(index);
  };

  const handleClose = () => {
    setSelectedIndex(-1);
  };

  const handlePrevious = () => {
    setSelectedIndex((prev) => (prev > 0 ? prev - 1 : images.length - 1));
  };

  const handleNext = () => {
    setSelectedIndex((prev) => (prev < images.length - 1 ? prev + 1 : 0));
  };

  if (loading) {
    return (
      <div className="image-gallery-loading">
        <div className="loading-spinner"></div>
        <p>Laddar bilder...</p>
      </div>
    );
  }

  if (images.length === 0) {
    return (
      <div className="image-gallery-empty">
        <p>Inga bilder hittades för det valda datumet och filtren.</p>
        <small>Försök att välja ett annat datum eller justera dina filter.</small>
      </div>
    );
  }

  return (
    <>
      <div className="image-gallery">
        <div className="gallery-header">
          <h3>Bilder ({images.length})</h3>
        </div>
        <div className="gallery-grid">
          {images.map((image, index) => (
            <ImageThumbnail
              key={image.id}
              image={image}
              onClick={() => handleThumbnailClick(index)}
            />
          ))}
        </div>
      </div>

      {selectedIndex >= 0 && (
        <ImageLightbox
          images={images}
          currentIndex={selectedIndex}
          onClose={handleClose}
          onPrevious={handlePrevious}
          onNext={handleNext}
        />
      )}
    </>
  );
}
