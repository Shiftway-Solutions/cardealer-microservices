# 🤖 Reemplazo de Spyne AI con Modelos Propios

**Última actualización:** Enero 26, 2026  
**Autor:** Equipo OKLA  
**Estado:** 📋 Análisis y Propuesta

---

## 📋 Resumen Ejecutivo

Este documento analiza las funcionalidades de Spyne AI y propone modelos de IA open-source y comerciales para replicar cada capacidad internamente, eliminando la dependencia de Spyne.

---

## 🎯 Funcionalidades de Spyne AI a Reemplazar

| #   | Funcionalidad                 | Descripción                                  | Complejidad |
| --- | ----------------------------- | -------------------------------------------- | ----------- |
| 1   | **Background Replacement**    | Reemplazar fondo de fotos de vehículos       | 🟡 Media    |
| 2   | **Vehicle Segmentation**      | Detectar y separar vehículo del fondo        | 🟡 Media    |
| 3   | **Image Classification**      | Clasificar: Exterior/Interior/Misc           | 🟢 Baja     |
| 4   | **Angle Detection**           | Detectar ángulo: front/rear/side/quarter     | 🟢 Baja     |
| 5   | **License Plate Masking**     | Enmascarar placas automáticamente            | 🟢 Baja     |
| 6   | **360° Spin Generation**      | Generar viewer 360° desde múltiples imágenes | 🟡 Media    |
| 7   | **Shadow Generation**         | Generar sombras realistas en nuevo fondo     | 🟡 Media    |
| 8   | **Color/Exposure Correction** | Normalizar colores e iluminación             | 🟢 Baja     |
| 9   | **Video Frame Extraction**    | Extraer frames de video para 360°            | 🟢 Baja     |
| 10  | **Feature Video Generation**  | Generar video promocional                    | 🔴 Alta     |

---

## 🧠 Modelos de IA Recomendados por Funcionalidad

### 1️⃣ Background Replacement (Segmentación + Inpainting)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     PIPELINE: BACKGROUND REPLACEMENT                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   📷 Imagen Original                                                        │
│         │                                                                   │
│         ▼                                                                   │
│   ┌─────────────────┐                                                       │
│   │ 1. SEGMENTACIÓN │  ◄── SAM 2 (Segment Anything Model 2)                │
│   │    del Vehículo │      Meta AI - Open Source                           │
│   └────────┬────────┘      https://github.com/facebookresearch/sam2        │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────┐                                                       │
│   │ 2. MÁSCARA      │  Resultado: Máscara binaria del vehículo             │
│   │    Generada     │  (vehicle = 1, background = 0)                       │
│   └────────┬────────┘                                                       │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────┐                                                       │
│   │ 3. COMPOSICIÓN  │  ◄── Simple alpha compositing                        │
│   │    con Nuevo BG │      OpenCV / Pillow                                 │
│   └────────┬────────┘                                                       │
│            │                                                                │
│            ▼                                                                │
│   🎨 Imagen Final con Nuevo Fondo                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Modelos Recomendados:

| Modelo                       | Tipo                  | Licencia   | GPU Requerida | Calidad    |
| ---------------------------- | --------------------- | ---------- | ------------- | ---------- |
| **SAM 2 (Meta)**             | Segmentación          | Apache 2.0 | 8GB VRAM      | ⭐⭐⭐⭐⭐ |
| **SegGPT**                   | Segmentación          | MIT        | 16GB VRAM     | ⭐⭐⭐⭐   |
| **U²-Net**                   | Segmentación (ligero) | Apache 2.0 | 4GB VRAM      | ⭐⭐⭐⭐   |
| **rembg (basado en U²-Net)** | Background Removal    | MIT        | 2GB VRAM      | ⭐⭐⭐⭐   |
| **CarveKit**                 | Background Removal    | MIT        | 4GB VRAM      | ⭐⭐⭐⭐   |

#### Código de Ejemplo (SAM 2):

```python
from sam2 import SAM2
import cv2
import numpy as np

# Cargar modelo
sam = SAM2.from_pretrained("facebook/sam2-hiera-large")

# Cargar imagen
image = cv2.imread("vehicle.jpg")

# Detectar vehículo automáticamente (auto-prompt)
masks = sam.generate_masks(image, object_type="vehicle")

# Aplicar máscara
vehicle_mask = masks[0]  # Primera máscara (vehículo principal)

# Cargar fondo de estudio
background = cv2.imread("studio_background.jpg")
background = cv2.resize(background, (image.shape[1], image.shape[0]))

# Composición
result = np.where(vehicle_mask[:,:,None], image, background)

cv2.imwrite("processed.jpg", result)
```

---

### 2️⃣ Vehicle Segmentation (Detección de Vehículo)

#### Modelos Recomendados:

| Modelo                | Especialización        | Licencia   | Performance    |
| --------------------- | ---------------------- | ---------- | -------------- |
| **YOLO v8/v9**        | Detección de objetos   | AGPL-3.0   | Muy rápido     |
| **Detectron2 (Meta)** | Instance Segmentation  | Apache 2.0 | Alta precisión |
| **SAM 2**             | Segmentación universal | Apache 2.0 | Mejor calidad  |
| **Grounding DINO**    | Zero-shot detection    | Apache 2.0 | Flexible       |

#### Pipeline Recomendado:

```python
# Opción 1: YOLO + SAM (más rápido)
from ultralytics import YOLO
from sam2 import SAM2

yolo = YOLO("yolov8x.pt")
sam = SAM2.from_pretrained("facebook/sam2-hiera-base")

# YOLO detecta bounding box del vehículo
results = yolo(image, classes=[2, 5, 7])  # car, bus, truck

# SAM genera máscara precisa usando el bbox como prompt
bbox = results[0].boxes[0].xyxy
mask = sam.segment_with_box(image, bbox)
```

---

### 3️⃣ Image Classification (Exterior/Interior/Misc)

#### Modelos Recomendados:

| Modelo                       | Tipo                     | Fine-tuning Necesario | Facilidad  |
| ---------------------------- | ------------------------ | --------------------- | ---------- |
| **CLIP (OpenAI)**            | Zero-shot classification | No                    | ⭐⭐⭐⭐⭐ |
| **ViT (Vision Transformer)** | Fine-tuned classifier    | Sí (fácil)            | ⭐⭐⭐⭐   |
| **EfficientNet**             | CNN Classifier           | Sí (fácil)            | ⭐⭐⭐⭐   |
| **ResNet-50**                | CNN Classifier           | Sí                    | ⭐⭐⭐     |

#### Código de Ejemplo (CLIP - Zero-shot):

```python
import torch
from transformers import CLIPProcessor, CLIPModel

model = CLIPModel.from_pretrained("openai/clip-vit-large-patch14")
processor = CLIPProcessor.from_pretrained("openai/clip-vit-large-patch14")

# Categorías de clasificación
categories = [
    "exterior view of a car from outside",
    "interior view of a car dashboard and seats",
    "close-up detail of car parts like engine or wheels"
]

# Procesar imagen
inputs = processor(
    text=categories,
    images=image,
    return_tensors="pt",
    padding=True
)

# Clasificar
outputs = model(**inputs)
probs = outputs.logits_per_image.softmax(dim=1)

# Resultado
category_idx = probs.argmax().item()
categories_map = ["Exterior", "Interior", "Misc"]
classification = categories_map[category_idx]
```

---

### 4️⃣ Angle Detection (front/rear/side/quarter)

#### Modelos Recomendados:

| Modelo              | Enfoque        | Dataset Necesario        |
| ------------------- | -------------- | ------------------------ |
| **CLIP**            | Zero-shot      | No                       |
| **Custom CNN**      | Fine-tuned     | ~5K imágenes etiquetadas |
| **Pose Estimation** | 3D orientation | Más complejo             |

#### Código de Ejemplo (CLIP):

```python
# Usando CLIP para detectar ángulo
angles = [
    "front view of a car showing the headlights and grille",
    "rear view of a car showing the taillights and trunk",
    "side view of a car showing the full profile",
    "front quarter view of a car at 45 degree angle",
    "rear quarter view of a car at 45 degree angle from behind"
]

# Clasificar con CLIP
inputs = processor(text=angles, images=image, return_tensors="pt", padding=True)
outputs = model(**inputs)
probs = outputs.logits_per_image.softmax(dim=1)

angle_map = ["front", "rear", "side", "front-quarter", "rear-quarter"]
detected_angle = angle_map[probs.argmax().item()]
```

---

### 5️⃣ License Plate Masking (Enmascarar Placas)

#### Modelos Recomendados:

| Modelo                  | Especialización         | Licencia   | Regiones |
| ----------------------- | ----------------------- | ---------- | -------- |
| **OpenALPR**            | License plate detection | AGPL-3.0   | Global   |
| **Plate Recognizer**    | API comercial           | Comercial  | Global   |
| **YOLO Custom**         | Fine-tuned for plates   | AGPL-3.0   | Custom   |
| **EasyOCR + Detection** | OCR-based               | Apache 2.0 | Global   |

#### Pipeline Recomendado:

```python
from ultralytics import YOLO
import cv2

# Modelo YOLO entrenado para placas
plate_detector = YOLO("license_plate_detector.pt")

# Detectar placa
results = plate_detector(image)

for box in results[0].boxes:
    x1, y1, x2, y2 = box.xyxy[0].int().tolist()

    # Aplicar blur
    roi = image[y1:y2, x1:x2]
    blurred = cv2.GaussianBlur(roi, (51, 51), 0)
    image[y1:y2, x1:x2] = blurred

    # O aplicar rectángulo blanco
    # cv2.rectangle(image, (x1, y1), (x2, y2), (255, 255, 255), -1)
```

---

### 6️⃣ 360° Spin Generation

Esto NO requiere IA - es un **viewer web interactivo** que muestra imágenes en secuencia.

#### Tecnologías Recomendadas:

| Tecnología           | Tipo               | Licencia | Recomendación |
| -------------------- | ------------------ | -------- | ------------- |
| **360-Image-Viewer** | JavaScript library | MIT      | ⭐⭐⭐⭐⭐    |
| **Three.js**         | 3D library         | MIT      | ⭐⭐⭐⭐      |
| **Pannellum**        | Panorama viewer    | MIT      | ⭐⭐⭐        |
| **Custom React**     | DIY solution       | -        | ⭐⭐⭐⭐      |

#### Implementación (React + TypeScript):

```tsx
// Ya implementado en Media360ViewerPage.tsx
// El viewer rota entre frames basado en:
// 1. currentFrame (0 a totalFrames-1)
// 2. Mouse drag / touch para rotar
// 3. Auto-rotation opcional
// 4. Keyboard controls (arrow keys)

// Los frames vienen pre-procesados del backend:
// - Fondo reemplazado
// - Colores normalizados
// - Mismo tamaño/aspecto
```

---

### 7️⃣ Shadow Generation (Sombras Realistas)

#### Modelos Recomendados:

| Modelo                          | Enfoque               | Complejidad |
| ------------------------------- | --------------------- | ----------- |
| **Stable Diffusion Inpainting** | Generative AI         | Alta        |
| **ControlNet (depth/normal)**   | Controlled generation | Alta        |
| **OpenCV Shadow Simulation**    | Algorithmic           | Media       |
| **Photoshop-style Dropshadow**  | CSS/Canvas            | Baja        |

#### Enfoque Práctico (Sin IA):

```python
import cv2
import numpy as np

def add_car_shadow(vehicle_mask, background, opacity=0.4, blur=50, offset=(0, 20)):
    """Agregar sombra simple basada en la máscara del vehículo"""

    # Crear sombra desde la máscara
    shadow = np.zeros_like(background)

    # Desplazar máscara hacia abajo (sombra debajo del vehículo)
    shadow_mask = np.roll(vehicle_mask, offset[1], axis=0)
    shadow_mask = np.roll(shadow_mask, offset[0], axis=1)

    # Aplicar blur
    shadow_mask = cv2.GaussianBlur(shadow_mask.astype(float), (blur, blur), 0)

    # Componer sombra
    shadow_layer = (shadow_mask[:,:,None] * opacity * 255).astype(np.uint8)

    # Mezclar con background
    result = cv2.addWeighted(background, 1.0, shadow_layer, -1, 0)

    return result
```

#### Enfoque con IA (Mejor Calidad):

```python
from diffusers import StableDiffusionInpaintPipeline
import torch

# Usar Stable Diffusion para generar sombras realistas
pipe = StableDiffusionInpaintPipeline.from_pretrained(
    "stabilityai/stable-diffusion-2-inpainting",
    torch_dtype=torch.float16
)

# Generar área de sombra
shadow_area_mask = create_shadow_mask(vehicle_mask)

# Inpaint la sombra
result = pipe(
    prompt="realistic car shadow on studio floor, soft shadow",
    image=composite_image,
    mask_image=shadow_area_mask,
    num_inference_steps=30
).images[0]
```

---

### 8️⃣ Color/Exposure Correction

#### Herramientas Recomendadas:

| Herramienta          | Tipo        | Enfoque                       |
| -------------------- | ----------- | ----------------------------- |
| **OpenCV**           | Algorítmico | CLAHE, histogram equalization |
| **Pillow/PIL**       | Algorítmico | Auto contrast, brightness     |
| **scikit-image**     | Algorítmico | Exposure correction           |
| **Deep Image Prior** | IA          | Neural enhancement            |
| **Real-ESRGAN**      | IA          | Super resolution + enhance    |

#### Código de Ejemplo:

```python
import cv2
import numpy as np
from PIL import Image, ImageEnhance

def auto_enhance_image(image_path):
    """Mejora automática de color y exposición"""

    # Leer imagen
    img = cv2.imread(image_path)

    # 1. Convertir a LAB color space
    lab = cv2.cvtColor(img, cv2.COLOR_BGR2LAB)
    l, a, b = cv2.split(lab)

    # 2. Aplicar CLAHE al canal L (luminosidad)
    clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
    l = clahe.apply(l)

    # 3. Recombinar
    lab = cv2.merge([l, a, b])
    result = cv2.cvtColor(lab, cv2.COLOR_LAB2BGR)

    # 4. Ajuste de saturación con PIL
    pil_img = Image.fromarray(cv2.cvtColor(result, cv2.COLOR_BGR2RGB))
    enhancer = ImageEnhance.Color(pil_img)
    pil_img = enhancer.enhance(1.1)  # Slight saturation boost

    return np.array(pil_img)
```

---

### 9️⃣ Video Frame Extraction

Esto es trivial con **FFmpeg** u **OpenCV**:

```python
import cv2
import os

def extract_frames(video_path, output_dir, frame_count=36):
    """Extraer N frames equidistantes de un video"""

    cap = cv2.VideoCapture(video_path)
    total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))

    # Calcular intervalo
    interval = total_frames // frame_count

    os.makedirs(output_dir, exist_ok=True)

    frames = []
    for i in range(frame_count):
        frame_num = i * interval
        cap.set(cv2.CAP_PROP_POS_FRAMES, frame_num)
        ret, frame = cap.read()

        if ret:
            frame_path = os.path.join(output_dir, f"frame_{i:03d}.jpg")
            cv2.imwrite(frame_path, frame)
            frames.append(frame_path)

    cap.release()
    return frames
```

O con **FFmpeg** (más rápido):

```bash
# Extraer 36 frames equidistantes
ffmpeg -i video.mp4 -vf "select='not(mod(n\,$(echo "scale=0; $(ffprobe -v error -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of csv=p=0 video.mp4) / 36" | bc)")'" -vsync vfr frame_%03d.jpg
```

---

### 🔟 Feature Video Generation (Video Promocional)

Esta es la funcionalidad más compleja. Requiere:

#### Modelos Recomendados:

| Modelo                     | Tipo                    | Uso           | Complejidad |
| -------------------------- | ----------------------- | ------------- | ----------- |
| **Runway Gen-3**           | Video generation        | Comercial API | 🔴          |
| **Pika Labs**              | Video generation        | Comercial API | 🔴          |
| **Stable Video Diffusion** | Open-source video       | Self-hosted   | 🔴          |
| **Luma Dream Machine**     | Video generation        | Comercial API | 🔴          |
| **FFmpeg Templates**       | Slideshow + transitions | Self-hosted   | 🟡          |

#### Enfoque Práctico (Sin IA Generativa):

```python
import moviepy.editor as mpe

def create_promo_video(images, music_path, output_path):
    """Crear video promocional con transiciones"""

    clips = []
    duration_per_image = 3  # segundos

    for img_path in images:
        clip = mpe.ImageClip(img_path).set_duration(duration_per_image)

        # Agregar efecto Ken Burns (zoom lento)
        clip = clip.resize(lambda t: 1 + 0.1 * t / duration_per_image)

        clips.append(clip)

    # Concatenar con crossfade
    video = mpe.concatenate_videoclips(clips, method="compose")

    # Agregar música
    audio = mpe.AudioFileClip(music_path)
    audio = audio.set_duration(video.duration)
    video = video.set_audio(audio)

    # Agregar texto overlay
    txt = mpe.TextClip("¡Tu próximo vehículo te espera!",
                       fontsize=50, color='white')
    txt = txt.set_position('center').set_duration(3)

    final = mpe.CompositeVideoClip([video, txt])

    final.write_videofile(output_path, fps=30)
```

---

## 🏗️ Arquitectura Propuesta: AIProcessingService

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      AI PROCESSING SERVICE (NUEVO)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                         API LAYER (.NET 8)                           │  │
│   │  POST /api/ai-process/background-replace                            │  │
│   │  POST /api/ai-process/segment-vehicle                               │  │
│   │  POST /api/ai-process/classify-image                                │  │
│   │  POST /api/ai-process/mask-license-plate                            │  │
│   │  POST /api/ai-process/generate-360                                  │  │
│   │  GET  /api/ai-process/status/{jobId}                                │  │
│   └──────────────────────────────┬──────────────────────────────────────┘  │
│                                  │                                          │
│                                  ▼                                          │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                       MESSAGE QUEUE (RabbitMQ)                       │  │
│   │  ai.background-replace  │  ai.segment  │  ai.classify  │  ai.360   │  │
│   └──────────────────────────────┬──────────────────────────────────────┘  │
│                                  │                                          │
│                                  ▼                                          │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                    PYTHON AI WORKERS (GPU)                           │  │
│   │                                                                      │  │
│   │   ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐    │  │
│   │   │  Worker: SAM2   │  │  Worker: CLIP   │  │  Worker: YOLO   │    │  │
│   │   │  Segmentation   │  │  Classification │  │  Detection      │    │  │
│   │   └─────────────────┘  └─────────────────┘  └─────────────────┘    │  │
│   │                                                                      │  │
│   │   ┌─────────────────┐  ┌─────────────────┐                          │  │
│   │   │  Worker: Plate  │  │  Worker: 360    │                          │  │
│   │   │  Masking        │  │  Viewer Gen     │                          │  │
│   │   └─────────────────┘  └─────────────────┘                          │  │
│   │                                                                      │  │
│   └──────────────────────────────┬──────────────────────────────────────┘  │
│                                  │                                          │
│                                  ▼                                          │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                         STORAGE (S3/Spaces)                          │  │
│   │  /processed/{vehicleId}/exterior_001_processed.jpg                  │  │
│   │  /processed/{vehicleId}/360-viewer/frame_001.jpg                    │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 💰 Comparación de Costos

### Spyne AI (Actual)

| Plan       | Costo    | Imágenes/mes | Costo por imagen |
| ---------- | -------- | ------------ | ---------------- |
| Starter    | $99/mes  | 500          | $0.20            |
| Pro        | $299/mes | 2,000        | $0.15            |
| Enterprise | Custom   | Unlimited    | ~$0.05-0.10      |

### Solución Propia (GPU Cloud)

| Proveedor          | GPU         | Costo/hora | Imágenes/hora\* | Costo/imagen |
| ------------------ | ----------- | ---------- | --------------- | ------------ |
| **RunPod**         | A100        | $1.89/hr   | ~500            | $0.004       |
| **Lambda Labs**    | A100        | $1.10/hr   | ~500            | $0.002       |
| **AWS g5.xlarge**  | A10G        | $1.01/hr   | ~300            | $0.003       |
| **DO GPU Droplet** | A100 (80GB) | $4.14/hr   | ~500            | $0.008       |

\*Estimado con SAM2 + composición

### ROI Proyectado

| Escenario       | Spyne (Pro) | Solución Propia | Ahorro           |
| --------------- | ----------- | --------------- | ---------------- |
| 2,000 imgs/mes  | $299        | ~$30 (compute)  | **$269 (90%)**   |
| 10,000 imgs/mes | $1,000+     | ~$100           | **$900 (90%)**   |
| 50,000 imgs/mes | $2,500+     | ~$400           | **$2,100 (84%)** |

---

## 📋 Plan de Implementación

### Fase 1: MVP (2-3 semanas)

| Semana | Tarea                              | Modelo       |
| ------ | ---------------------------------- | ------------ |
| 1      | Setup Python workers con Docker    | -            |
| 1      | Implementar Background Replacement | SAM2 + rembg |
| 2      | Implementar Image Classification   | CLIP         |
| 2      | Implementar License Plate Masking  | YOLO custom  |
| 3      | Integrar con .NET API              | RabbitMQ     |
| 3      | Testing y ajustes                  | -            |

### Fase 2: 360° Viewer (1 semana)

| Tarea               | Tecnología               |
| ------------------- | ------------------------ |
| Frame extraction    | FFmpeg + OpenCV          |
| React 360° viewer   | Custom (ya implementado) |
| Backend integration | .NET + Workers           |

### Fase 3: Mejoras (2 semanas)

| Tarea             | Tecnología           |
| ----------------- | -------------------- |
| Shadow generation | OpenCV + algorithmic |
| Color correction  | CLAHE + auto-enhance |
| Quality scoring   | Custom CNN           |
| Hotspot detection | Object detection     |

---

## 🔧 Tecnologías Necesarias

### Backend (.NET 8)

```xml
<!-- Ya existentes -->
<PackageReference Include="MassTransit.RabbitMQ" />
<PackageReference Include="AWSSDK.S3" />

<!-- Nuevos para AI -->
<PackageReference Include="Microsoft.ML" />
<PackageReference Include="Microsoft.ML.OnnxRuntime" />
```

### Python Workers

```txt
# requirements.txt
torch>=2.0.0
torchvision>=0.15.0
transformers>=4.35.0
segment-anything-2>=1.0.0
ultralytics>=8.0.0
opencv-python>=4.8.0
Pillow>=10.0.0
rembg>=2.0.0
pika>=1.3.0  # RabbitMQ
boto3>=1.28.0  # S3
```

### Infraestructura

```yaml
# docker-compose.ai-workers.yaml
services:
  ai-worker-sam:
    image: ghcr.io/okla/ai-worker-sam:latest
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    environment:
      - RABBITMQ_URL=amqp://rabbitmq:5672
      - S3_BUCKET=okla-media

  ai-worker-clip:
    image: ghcr.io/okla/ai-worker-clip:latest
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
```

---

## ✅ Conclusión

Reemplazar Spyne AI es **100% factible** con modelos open-source, con estas ventajas:

| Aspecto             | Spyne               | Solución Propia |
| ------------------- | ------------------- | --------------- |
| **Costo mensual**   | $299-2,500+         | $30-400         |
| **Control**         | Dependencia externa | 100% interno    |
| **Personalización** | Limitada            | Ilimitada       |
| **Latencia**        | ~60-120s            | ~5-30s          |
| **Escalabilidad**   | Limitada por plan   | Infinita (GPU)  |
| **Privacy**         | Datos en Spyne      | Datos propios   |

### Modelos Recomendados (Resumen)

| Funcionalidad | Modelo Primario  | Alternativa    |
| ------------- | ---------------- | -------------- |
| Segmentación  | **SAM 2**        | rembg/U²-Net   |
| Clasificación | **CLIP**         | ViT fine-tuned |
| Detección     | **YOLO v8**      | Detectron2     |
| Placas        | **YOLO custom**  | OpenALPR       |
| 360° Viewer   | **React custom** | Three.js       |
| Sombras       | **OpenCV**       | SD Inpainting  |
| Video         | **FFmpeg**       | MoviePy        |

---

**Próximo Paso:** ¿Quieres que implemente el `AIProcessingService` con los workers de Python?

---

_Documento creado: Enero 26, 2026_  
_Autor: Equipo OKLA_
