import React from 'react';
import { ImageDto } from '../../types/api.types';

interface ImageThumbnailProps {
  image: ImageDto;
  onClick: () => void;
}

export function ImageThumbnail({ image, onClick }: ImageThumbnailProps) {
  return (
    <button onClick={onClick} style={{ display: 'block', width: '100%', border: 'none', padding: 0, cursor: 'pointer' }}>
      <img
        src={image.blobUrl}
        alt={image.description || 'Viltkamerabild'}
        style={{ width: '100%', aspectRatio: '4/3', objectFit: 'cover', display: 'block' }}
        loading="lazy"
      />
    </button>
  );
}
