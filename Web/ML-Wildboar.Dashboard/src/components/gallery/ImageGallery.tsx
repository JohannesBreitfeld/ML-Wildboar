import React, { useState } from 'react';
import { ImageDto } from '../../types/api.types';
import { ImageCard } from './ImageCard';
import { ImageLightbox } from './ImageLightbox';

interface Props {
  images: ImageDto[];
  minThumbnailWidth?: number;
  maxItems?: number;
}

export function ImageGallery({ images, minThumbnailWidth = 200, maxItems }: Props) {
  const [lightboxIdx, setLightboxIdx] = useState<number | null>(null);
  const displayed = maxItems != null ? images.slice(0, maxItems) : images;

  return (
    <>
      <div style={{
        display: 'grid',
        gridTemplateColumns: `repeat(auto-fill, minmax(${minThumbnailWidth}px, 1fr))`,
        gap: 14,
      }}>
        {displayed.map((img, idx) => (
          <ImageCard key={img.id} image={img} onClick={() => setLightboxIdx(idx)} />
        ))}
      </div>
      {lightboxIdx != null && (
        <ImageLightbox
          images={displayed}
          index={lightboxIdx}
          onClose={() => setLightboxIdx(null)}
          onPrev={() => setLightboxIdx(i => Math.max(0, (i ?? 0) - 1))}
          onNext={() => setLightboxIdx(i => Math.min(displayed.length - 1, (i ?? 0) + 1))}
        />
      )}
    </>
  );
}
