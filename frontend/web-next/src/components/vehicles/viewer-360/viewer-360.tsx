'use client';

import {
  useRef,
  useState,
  useCallback,
  useEffect,
  type MouseEvent as ReactMouseEvent,
  type TouchEvent as ReactTouchEvent,
} from 'react';
import {
  RotateCcw,
  ZoomIn,
  ZoomOut,
  Play,
  Pause,
  Maximize2,
  Loader2,
} from 'lucide-react';

// ============================================================
// TYPES
// ============================================================

export interface ViewerFrame {
  index: number;
  url: string;
}

export interface Viewer360Props {
  /** Array of frame URLs in order (36-72 frames ideal) */
  frames: ViewerFrame[];
  /** Initial frame index */
  initialFrame?: number;
  /** Auto-rotate on mount */
  autoRotate?: boolean;
  /** Auto-rotate speed in ms per frame */
  autoRotateSpeed?: number;
  /** Enable zoom via pinch/scroll */
  enableZoom?: boolean;
  /** Max zoom level */
  maxZoom?: number;
  /** Show controls overlay */
  showControls?: boolean;
  /** Loading state */
  isLoading?: boolean;
  /** Error state */
  error?: string;
  className?: string;
}

// ============================================================
// COMPONENT
// ============================================================

export function Viewer360({
  frames,
  initialFrame = 0,
  autoRotate = false,
  autoRotateSpeed = 100,
  enableZoom = true,
  maxZoom = 3,
  showControls = true,
  isLoading = false,
  error,
  className = '',
}: Viewer360Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const imagesRef = useRef<(HTMLImageElement | null)[]>([]);
  const animFrameRef = useRef<number>(0);
  const autoRotateRef = useRef<ReturnType<typeof setInterval>>(undefined);

  const [currentFrame, setCurrentFrame] = useState(initialFrame);
  const [isDragging, setIsDragging] = useState(false);
  const [isAutoRotating, setIsAutoRotating] = useState(autoRotate);
  const [zoom, setZoom] = useState(1);
  const [panOffset, setPanOffset] = useState({ x: 0, y: 0 });
  const [loadedCount, setLoadedCount] = useState(0);
  const [, setIsFullscreen] = useState(false);

  const dragStartRef = useRef({ x: 0, frame: 0 });
  const panStartRef = useRef({ x: 0, y: 0, offsetX: 0, offsetY: 0 });

  const totalFrames = frames.length;
  const loadProgress = totalFrames > 0 ? Math.round((loadedCount / totalFrames) * 100) : 0;
  const allLoaded = loadedCount >= totalFrames;

  // ─── Preload Images ────────────────────────────────────────
  useEffect(() => {
    if (frames.length === 0) return;

    imagesRef.current = new Array(frames.length).fill(null);
    let loaded = 0;

    frames.forEach((frame, index) => {
      const img = new Image();
      img.crossOrigin = 'anonymous';
      img.onload = () => {
        imagesRef.current[index] = img;
        loaded++;
        setLoadedCount(loaded);
      };
      img.onerror = () => {
        loaded++;
        setLoadedCount(loaded);
      };
      img.src = frame.url;
    });

    return () => {
      imagesRef.current = [];
    };
  }, [frames]);

  // ─── Draw Frame ────────────────────────────────────────────
  const drawFrame = useCallback(
    (frameIndex: number) => {
      const canvas = canvasRef.current;
      const ctx = canvas?.getContext('2d');
      const img = imagesRef.current[frameIndex];
      if (!canvas || !ctx || !img) return;

      const container = containerRef.current;
      if (container) {
        canvas.width = container.clientWidth * window.devicePixelRatio;
        canvas.height = container.clientHeight * window.devicePixelRatio;
        canvas.style.width = `${container.clientWidth}px`;
        canvas.style.height = `${container.clientHeight}px`;
      }

      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.save();

      // Apply zoom and pan
      const scale = zoom * window.devicePixelRatio;
      const cx = canvas.width / 2 + panOffset.x * window.devicePixelRatio;
      const cy = canvas.height / 2 + panOffset.y * window.devicePixelRatio;

      ctx.translate(cx, cy);
      ctx.scale(scale, scale);
      ctx.translate(-cx, -cy);

      // Draw image fitted to canvas
      const imgRatio = img.naturalWidth / img.naturalHeight;
      const canvasRatio = canvas.width / canvas.height;

      let drawWidth: number, drawHeight: number;

      if (imgRatio > canvasRatio) {
        drawWidth = canvas.width;
        drawHeight = canvas.width / imgRatio;
      } else {
        drawHeight = canvas.height;
        drawWidth = canvas.height * imgRatio;
      }

      const drawX = (canvas.width - drawWidth) / 2;
      const drawY = (canvas.height - drawHeight) / 2;

      ctx.drawImage(img, drawX, drawY, drawWidth, drawHeight);
      ctx.restore();
    },
    [zoom, panOffset]
  );

  // ─── Animation loop ───────────────────────────────────────
  useEffect(() => {
    if (!allLoaded) return;

    const animate = () => {
      drawFrame(currentFrame);
      animFrameRef.current = requestAnimationFrame(animate);
    };
    animFrameRef.current = requestAnimationFrame(animate);

    return () => {
      cancelAnimationFrame(animFrameRef.current);
    };
  }, [currentFrame, drawFrame, allLoaded]);

  // ─── Auto-rotate ──────────────────────────────────────────
  useEffect(() => {
    if (!isAutoRotating || !allLoaded || isDragging) {
      if (autoRotateRef.current) clearInterval(autoRotateRef.current);
      return;
    }

    autoRotateRef.current = setInterval(() => {
      setCurrentFrame(prev => (prev + 1) % totalFrames);
    }, autoRotateSpeed);

    return () => {
      if (autoRotateRef.current) clearInterval(autoRotateRef.current);
    };
  }, [isAutoRotating, allLoaded, isDragging, totalFrames, autoRotateSpeed]);

  // ─── Drag handlers ────────────────────────────────────────
  const handlePointerDown = useCallback(
    (e: ReactMouseEvent | ReactTouchEvent) => {
      if (!allLoaded) return;

      const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;

      if (zoom > 1) {
        const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;
        panStartRef.current = {
          x: clientX,
          y: clientY,
          offsetX: panOffset.x,
          offsetY: panOffset.y,
        };
      }

      dragStartRef.current = { x: clientX, frame: currentFrame };
      setIsDragging(true);
    },
    [allLoaded, currentFrame, zoom, panOffset]
  );

  const handlePointerMove = useCallback(
    (e: ReactMouseEvent | ReactTouchEvent) => {
      if (!isDragging) return;

      const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
      const delta = clientX - dragStartRef.current.x;

      if (zoom > 1) {
        const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;
        setPanOffset({
          x: panStartRef.current.offsetX + (clientX - panStartRef.current.x),
          y: panStartRef.current.offsetY + (clientY - panStartRef.current.y),
        });
        return;
      }

      // Sensitivity: pixels per frame
      const sensitivity = Math.max(2, Math.round(300 / totalFrames));
      const frameDelta = Math.round(delta / sensitivity);

      let newFrame = (dragStartRef.current.frame - frameDelta) % totalFrames;
      if (newFrame < 0) newFrame += totalFrames;

      setCurrentFrame(newFrame);
    },
    [isDragging, totalFrames, zoom]
  );

  const handlePointerUp = useCallback(() => {
    setIsDragging(false);
  }, []);

  // ─── Zoom handlers ────────────────────────────────────────
  const handleWheel = useCallback(
    (e: React.WheelEvent) => {
      if (!enableZoom) return;
      e.preventDefault();

      setZoom(prev => {
        const newZoom = prev - e.deltaY * 0.001;
        return Math.max(1, Math.min(maxZoom, newZoom));
      });
    },
    [enableZoom, maxZoom]
  );

  const handleZoomIn = useCallback(() => {
    setZoom(prev => Math.min(maxZoom, prev + 0.5));
  }, [maxZoom]);

  const handleZoomOut = useCallback(() => {
    setZoom(prev => {
      const newZoom = Math.max(1, prev - 0.5);
      if (newZoom === 1) setPanOffset({ x: 0, y: 0 });
      return newZoom;
    });
  }, []);

  const toggleFullscreen = useCallback(() => {
    if (!containerRef.current) return;
    if (!document.fullscreenElement) {
      containerRef.current.requestFullscreen();
      setIsFullscreen(true);
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  }, []);

  // ─── Error state ──────────────────────────────────────────
  if (error) {
    return (
      <div className={`flex items-center justify-center rounded-xl bg-gray-100 p-8 ${className}`}>
        <div className="text-center">
          <RotateCcw className="mx-auto h-10 w-10 text-gray-300" />
          <p className="mt-2 text-sm font-medium text-gray-600">Error al cargar vista 360°</p>
          <p className="text-xs text-gray-400">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div
      ref={containerRef}
      className={`relative overflow-hidden rounded-xl bg-gray-900 ${className}`}
      onWheel={handleWheel}
    >
      {/* Canvas */}
      <canvas
        ref={canvasRef}
        role="img"
        aria-label="Vista 360° del vehículo — arrastra para rotar"
        className={`h-full w-full ${isDragging ? 'cursor-grabbing' : 'cursor-grab'}`}
        onMouseDown={handlePointerDown}
        onMouseMove={handlePointerMove}
        onMouseUp={handlePointerUp}
        onMouseLeave={handlePointerUp}
        onTouchStart={handlePointerDown}
        onTouchMove={handlePointerMove}
        onTouchEnd={handlePointerUp}
      />

      {/* Loading overlay */}
      {(isLoading || !allLoaded) && (
        <div className="absolute inset-0 flex flex-col items-center justify-center bg-gray-900/80">
          <Loader2 className="h-8 w-8 animate-spin text-white" />
          <p className="mt-2 text-sm text-white">
            Cargando vista 360°... {loadProgress}%
          </p>
          <div className="mt-2 h-1.5 w-48 overflow-hidden rounded-full bg-white/20">
            <div
              className="h-full rounded-full bg-emerald-500 transition-all"
              style={{ width: `${loadProgress}%` }}
            />
          </div>
        </div>
      )}

      {/* Controls */}
      {showControls && allLoaded && (
        <>
          {/* Top-right controls */}
          <div className="absolute right-3 top-3 flex flex-col gap-1">
            <button
              type="button"
              onClick={toggleFullscreen}
              className="rounded-lg bg-black/50 p-2 text-white backdrop-blur-sm transition-colors hover:bg-black/70"
              title="Pantalla completa"
            >
              <Maximize2 className="h-4 w-4" />
            </button>
          </div>

          {/* Bottom controls */}
          <div className="absolute bottom-3 left-1/2 flex -translate-x-1/2 items-center gap-1 rounded-xl bg-black/50 p-1 backdrop-blur-sm">
            <button
              type="button"
              onClick={() => setIsAutoRotating(r => !r)}
              className="rounded-lg p-2 text-white transition-colors hover:bg-white/20"
              title={isAutoRotating ? 'Pausar rotación' : 'Auto-rotar'}
            >
              {isAutoRotating ? (
                <Pause className="h-4 w-4" />
              ) : (
                <Play className="h-4 w-4" />
              )}
            </button>

            {enableZoom && (
              <>
                <div className="mx-1 h-5 w-px bg-white/30" />
                <button
                  type="button"
                  onClick={handleZoomOut}
                  disabled={zoom <= 1}
                  className="rounded-lg p-2 text-white transition-colors hover:bg-white/20 disabled:opacity-30"
                  title="Alejar"
                >
                  <ZoomOut className="h-4 w-4" />
                </button>
                <span className="min-w-[40px] text-center text-xs font-medium text-white">
                  {Math.round(zoom * 100)}%
                </span>
                <button
                  type="button"
                  onClick={handleZoomIn}
                  disabled={zoom >= maxZoom}
                  className="rounded-lg p-2 text-white transition-colors hover:bg-white/20 disabled:opacity-30"
                  title="Acercar"
                >
                  <ZoomIn className="h-4 w-4" />
                </button>
              </>
            )}
          </div>

          {/* Frame indicator */}
          <div className="absolute bottom-3 right-3 rounded-lg bg-black/50 px-2 py-1 text-xs text-white backdrop-blur-sm">
            {currentFrame + 1}/{totalFrames}
          </div>

          {/* Drag hint (show briefly) */}
          {!isDragging && !isAutoRotating && (
            <div className="absolute left-3 top-3 rounded-lg bg-black/50 px-2 py-1 text-xs text-white/70 backdrop-blur-sm">
              ↔ Arrastra para rotar
            </div>
          )}
        </>
      )}
    </div>
  );
}
