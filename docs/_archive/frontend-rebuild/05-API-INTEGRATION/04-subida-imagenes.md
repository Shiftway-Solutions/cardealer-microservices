# 📤 Integración API - Media Service

**Fecha:** Enero 30, 2026 (actualizado)  
**Propósito:** Documentación completa del MediaService para gestión de archivos y upload  
**Endpoints Documentados:** 5 endpoints

---

## 📋 ÍNDICE

1. [Cliente HTTP Base](#1-cliente-http-base)
2. [Tipos TypeScript](#2-tipos-typescript)
3. [Media Service Completo](#3-media-service-completo)
4. [Hooks de React Query](#4-hooks-de-react-query)
5. [Componente ImageUploader](#5-componente-imageuploader)
6. [Componente MultipleImageUploader](#6-componente-multipleimageuploader)
7. [Ejemplos de Uso](#7-ejemplos-de-uso)

---

## 1. Cliente HTTP Base

```typescript
// filepath: src/services/api/apiClient.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: process.env.REACT_APP_API_URL || 'https://api.okla.com.do',
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = \`Bearer \${token}\`;
  }
  return config;
});

export default apiClient;
```

---

## 2. Tipos TypeScript

```typescript
// filepath: src/types/media.ts

export enum MediaType {
  Image = "Image",
  Video = "Video",
  Document = "Document",
  Audio = "Audio",
}

export interface UploadedMedia {
  id: string;
  url: string;
  thumbnailUrl?: string;
  name: string;
  size: number;
  mimeType: string;
  mediaType: MediaType;
  uploadedAt: string;
  userId: string;
}

export interface UploadProgress {
  file: File;
  progress: number;
  status: "pending" | "uploading" | "success" | "error";
  error?: string;
  result?: UploadedMedia;
}

export interface ChunkedUploadInitResponse {
  uploadId: string;
  mediaId: string;
  chunkSize: number;
  totalChunks: number;
}
```

---

## 3. Media Service Completo

```typescript
// filepath: src/services/api/mediaService.ts
import apiClient from './apiClient';
import type { UploadedMedia, ChunkedUploadInitResponse } from '@/types/media';

export const mediaService = {
  /**
   * POST /api/media/upload
   * Subir archivo genérico (cualquier tipo)
   * Max 100MB, para archivos grandes
   */
  async upload(
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<UploadedMedia> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<UploadedMedia>(
      '/media/upload',
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (e) => {
          if (e.total && onProgress) {
            const percent = Math.round((e.loaded * 100) / e.total);
            onProgress(percent);
          }
        },
      }
    );

    return response.data;
  },

  /**
   * POST /api/media/upload/image
   * Subir imagen optimizada
   * Genera automáticamente thumbnail y comprime
   * Max 10MB, recomendado para imágenes de vehículos
   */
  async uploadImage(
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<UploadedMedia> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<UploadedMedia>(
      '/media/upload/image',
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (e) => {
          if (e.total && onProgress) {
            onProgress(Math.round((e.loaded / e.total) * 100));
          }
        },
      }
    );

    return response.data;
  },

  /**
   * POST /api/media/upload/init
   * Iniciar upload por chunks (archivos muy grandes)
   * Para archivos > 100MB (videos, documentos grandes)
   */
  async initChunkedUpload(
    fileName: string,
    fileSize: number,
    mimeType: string
  ): Promise<ChunkedUploadInitResponse> {
    const response = await apiClient.post<ChunkedUploadInitResponse>(
      '/media/upload/init',
      {
        fileName,
        fileSize,
        mimeType,
      }
    );

    return response.data;
  },

  /**
   * POST /api/media/upload/finalize/{mediaId}
   * Finalizar upload por chunks
   */
  async finalizeChunkedUpload(
    mediaId: string,
    uploadId: string,
    etags: string[]
  ): Promise<UploadedMedia> {
    const response = await apiClient.post<UploadedMedia>(
      \`/media/upload/finalize/\${mediaId}\`,
      {
        uploadId,
        etags,
      }
    );

    return response.data;
  },

  /**
   * GET /api/media/{mediaId}
   * Obtener información de un archivo subido
   */
  async getMediaById(mediaId: string): Promise<UploadedMedia> {
    const response = await apiClient.get<UploadedMedia>(\`/media/\${mediaId}\`);
    return response.data;
  },

  /**
   * DELETE /api/media/{mediaId}
   * Eliminar archivo de S3
   * Requiere autenticación: Usuario propietario o Admin
   */
  async deleteMedia(mediaId: string): Promise<void> {
    await apiClient.delete(\`/media/\${mediaId}\`);
  },

  /**
   * Subir múltiples imágenes en secuencia
   * Útil para galerías de vehículos
   */
  async uploadMultiple(
    files: File[],
    onFileProgress?: (index: number, progress: number) => void
  ): Promise<UploadedMedia[]> {
    const results: UploadedMedia[] = [];

    for (let i = 0; i < files.length; i++) {
      const result = await this.uploadImage(files[i], (p) => {
        onFileProgress?.(i, p);
      });
      results.push(result);
    }

    return results;
  },
};
```

---

## 4. Hooks de React Query

```typescript
// filepath: src/hooks/useMedia.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { mediaService } from "@/services/api/mediaService";

export const useUploadImage = () => {
  return useMutation({
    mutationFn: ({
      file,
      onProgress,
    }: {
      file: File;
      onProgress?: (p: number) => void;
    }) => mediaService.uploadImage(file, onProgress),
  });
};

export const useUploadMultiple = () => {
  return useMutation({
    mutationFn: ({
      files,
      onFileProgress,
    }: {
      files: File[];
      onFileProgress?: (index: number, progress: number) => void;
    }) => mediaService.uploadMultiple(files, onFileProgress),
  });
};

export const useMedia = (mediaId: string) => {
  return useQuery({
    queryKey: ["media", mediaId],
    queryFn: () => mediaService.getMediaById(mediaId),
    enabled: !!mediaId,
  });
};

export const useDeleteMedia = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (mediaId: string) => mediaService.deleteMedia(mediaId),
    onSuccess: () => {
      queryClient.invalidateQueries(["media"]);
    },
  });
};
```

---

## 5. Componente ImageUploader

```typescript
// filepath: src/components/forms/ImageUploader.tsx
import React, { useState, useRef } from 'react';
import { useUploadImage } from '@/hooks/useMedia';

interface ImageUploaderProps {
  onUploadComplete: (media: UploadedMedia) => void;
  maxSizeMB?: number;
  accept?: string;
}

export const ImageUploader: React.FC<ImageUploaderProps> = ({
  onUploadComplete,
  maxSizeMB = 10,
  accept = 'image/jpeg,image/png,image/webp',
}) => {
  const [preview, setPreview] = useState<string | null>(null);
  const [progress, setProgress] = useState(0);
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const uploadImage = useUploadImage();

  const handleFileSelect = async (file: File) => {
    // Validar tamaño
    if (file.size > maxSizeMB * 1024 * 1024) {
      alert(\`El archivo excede el tamaño máximo de \${maxSizeMB}MB\`);
      return;
    }

    // Validar tipo
    if (!file.type.startsWith('image/')) {
      alert('Solo se permiten imágenes');
      return;
    }

    // Generar preview
    const reader = new FileReader();
    reader.onload = (e) => setPreview(e.target?.result as string);
    reader.readAsDataURL(file);

    // Subir
    try {
      const result = await uploadImage.mutateAsync({
        file,
        onProgress: setProgress,
      });
      onUploadComplete(result);
    } catch (error) {
      console.error('Error al subir imagen:', error);
      alert('Error al subir la imagen');
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFileSelect(file);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  return (
    <div>
      <div
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={() => setIsDragOver(false)}
        onClick={() => fileInputRef.current?.click()}
        className={\`border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition \${
          isDragOver ? 'border-blue-500 bg-blue-50' : 'border-gray-300'
        }\`}
      >
        {preview ? (
          <div className="relative">
            <img src={preview} alt="Preview" className="max-w-full h-48 mx-auto rounded" />
            {uploadImage.isLoading && (
              <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                <div className="text-white">
                  <div className="text-2xl mb-2">{progress}%</div>
                  <div className="w-48 h-2 bg-gray-300 rounded">
                    <div
                      className="h-full bg-blue-500 rounded"
                      style={{ width: \`\${progress}%\` }}
                    />
                  </div>
                </div>
              </div>
            )}
          </div>
        ) : (
          <div>
            <div className="text-4xl mb-4">📤</div>
            <p className="text-lg font-medium">Arrastra una imagen aquí</p>
            <p className="text-sm text-gray-500 mt-2">o haz click para seleccionar</p>
            <p className="text-xs text-gray-400 mt-1">
              Máx. {maxSizeMB}MB - JPG, PNG, WEBP
            </p>
          </div>
        )}
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept={accept}
        onChange={(e) => e.target.files && handleFileSelect(e.target.files[0])}
        className="hidden"
      />
    </div>
  );
};
```

---

## 6. Componente MultipleImageUploader

```typescript
// filepath: src/components/forms/MultipleImageUploader.tsx
import React, { useState } from 'react';
import { useUploadMultiple } from '@/hooks/useMedia';
import type { UploadProgress } from '@/types/media';

interface MultipleImageUploaderProps {
  onUploadComplete: (media: UploadedMedia[]) => void;
  maxFiles?: number;
  maxSizeMB?: number;
}

export const MultipleImageUploader: React.FC<MultipleImageUploaderProps> = ({
  onUploadComplete,
  maxFiles = 10,
  maxSizeMB = 10,
}) => {
  const [files, setFiles] = useState<File[]>([]);
  const [previews, setPreviews] = useState<string[]>([]);
  const [uploadProgress, setUploadProgress] = useState<Record<number, number>>({});

  const uploadMultiple = useUploadMultiple();

  const handleFilesSelect = (selectedFiles: FileList) => {
    const filesArray = Array.from(selectedFiles);

    // Validar cantidad
    if (filesArray.length > maxFiles) {
      alert(\`Máximo \${maxFiles} archivos permitidos\`);
      return;
    }

    // Validar tamaño
    const invalidFiles = filesArray.filter((f) => f.size > maxSizeMB * 1024 * 1024);
    if (invalidFiles.length > 0) {
      alert(\`Algunos archivos exceden \${maxSizeMB}MB\`);
      return;
    }

    setFiles(filesArray);

    // Generar previews
    const previewUrls: string[] = [];
    filesArray.forEach((file) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        previewUrls.push(e.target?.result as string);
        if (previewUrls.length === filesArray.length) {
          setPreviews(previewUrls);
        }
      };
      reader.readAsDataURL(file);
    });
  };

  const handleUpload = async () => {
    try {
      const results = await uploadMultiple.mutateAsync({
        files,
        onFileProgress: (index, progress) => {
          setUploadProgress((prev) => ({ ...prev, [index]: progress }));
        },
      });
      onUploadComplete(results);
    } catch (error) {
      console.error('Error:', error);
    }
  };

  return (
    <div>
      <input
        type="file"
        multiple
        accept="image/*"
        onChange={(e) => e.target.files && handleFilesSelect(e.target.files)}
        className="mb-4"
      />

      <div className="grid grid-cols-3 gap-4 mb-4">
        {previews.map((preview, index) => (
          <div key={index} className="relative">
            <img src={preview} alt={\`Preview \${index + 1}\`} className="w-full h-32 object-cover rounded" />
            <button
              onClick={() => {
                setFiles(files.filter((_, i) => i !== index));
                setPreviews(previews.filter((_, i) => i !== index));
              }}
              className="absolute top-1 right-1 bg-red-600 text-white rounded-full w-6 h-6"
            >
              ×
            </button>
            {uploadProgress[index] !== undefined && (
              <div className="absolute bottom-0 left-0 right-0 bg-black/50 text-white text-xs p-1">
                {uploadProgress[index]}%
              </div>
            )}
          </div>
        ))}
      </div>

      {files.length > 0 && (
        <button
          onClick={handleUpload}
          disabled={uploadMultiple.isLoading}
          className="px-6 py-2 bg-blue-600 text-white rounded disabled:opacity-50"
        >
          {uploadMultiple.isLoading ? 'Subiendo...' : \`Subir \${files.length} imágenes\`}
        </button>
      )}
    </div>
  );
};
```

---

## 7. Ejemplos de Uso

### Caso 1: Avatar de Usuario

```typescript
// filepath: src/pages/EditProfilePage.tsx
import { ImageUploader } from '@/components/forms/ImageUploader';

export const EditProfilePage = () => {
  const handleAvatarUpload = (media: UploadedMedia) => {
    // Actualizar perfil con nueva URL
    updateUser({ avatarUrl: media.url });
  };

  return (
    <div>
      <h2>Foto de Perfil</h2>
      <ImageUploader onUploadComplete={handleAvatarUpload} maxSizeMB={5} />
    </div>
  );
};
```

### Caso 2: Galería de Vehículo

```typescript
// filepath: src/pages/CreateVehiclePage.tsx
import { MultipleImageUploader } from '@/components/forms/MultipleImageUploader';

export const CreateVehiclePage = () => {
  const [vehicleImages, setVehicleImages] = useState<UploadedMedia[]>([]);

  const handleImagesUpload = (media: UploadedMedia[]) => {
    setVehicleImages(media);
    // Asociar imágenes al vehículo
    const imageIds = media.map((m) => m.id);
    createVehicle({ ...formData, imageIds });
  };

  return (
    <div>
      <h2>Fotos del Vehículo</h2>
      <MultipleImageUploader
        onUploadComplete={handleImagesUpload}
        maxFiles={20}
      />
    </div>
  );
};
```

---

## 🎯 Resumen de Endpoints Documentados

| Método | Endpoint                            | Autenticación | Descripción                     |
| ------ | ----------------------------------- | ------------- | ------------------------------- |
| POST   | \`/api/media/upload\`               | ✅            | Upload genérico (max 100MB)     |
| POST   | \`/api/media/upload/image\`         | ✅            | Upload imagen optimizada (10MB) |
| POST   | \`/api/media/upload/init\`          | ✅            | Iniciar upload por chunks       |
| POST   | \`/api/media/upload/finalize/{id}\` | ✅            | Finalizar upload por chunks     |
| GET    | \`/api/media/{mediaId}\`            | No            | Obtener info de archivo         |

**Total: 5 endpoints documentados** ✅

---

## 🔒 Notas de Seguridad

- **Tamaños máximos:**
  - Imágenes: 10MB
  - Archivos genéricos: 100MB
  - Chunked upload: Sin límite (videos grandes)

- **Tipos permitidos:**
  - Imágenes: JPG, PNG, WEBP, GIF
  - Videos: MP4, MOV, AVI
  - Documentos: PDF, DOC, DOCX

- **Almacenamiento:** DigitalOcean Spaces (S3-compatible)
- **CDN:** Todos los archivos servidos desde CDN para velocidad
- **Eliminación:** Soft delete mantiene archivos 30 días para recuperación

---

_Generado: Enero 30, 2026_  
_Actualizado por: Sistema de Auditoría Automatizado_
