#!/usr/bin/env python3
"""
Script V3 - Remoción de fondo con REFINAMIENTO DE BORDES AVANZADO.

Mejoras sobre V2:
- Operaciones morfológicas para reparar bordes cortados
- Suavizado de contornos (anti-aliasing)
- Relleno de huecos pequeños (inpainting de máscara)
- Feathering de bordes para transiciones suaves

Input: ./input/
Output: ./output_v3/
"""

import time
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter
from rembg import new_session, remove
from scipy import ndimage

# Configuración de directorios
SCRIPT_DIR = Path(__file__).parent
INPUT_DIR = SCRIPT_DIR / "input"
OUTPUT_DIR = SCRIPT_DIR / "output_v3"

# Extensiones de imagen soportadas
SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tiff"}

# Modelo - BiRefNet es el más preciso
MODEL_NAME = "birefnet-general"

# Sesión global del modelo
SESSION = None


def get_session():
    """Obtener o crear sesión del modelo."""
    global SESSION
    if SESSION is None:
        print(f"🤖 Cargando modelo: {MODEL_NAME}...")
        try:
            SESSION = new_session(MODEL_NAME)
            print("   ✅ Modelo cargado correctamente")
        except Exception as e:
            print(f"   ⚠️ Error cargando {MODEL_NAME}, usando u2net: {e}")
            SESSION = new_session("u2net")
    return SESSION


def setup_directories():
    """Crear directorio de salida si no existe."""
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    print(f"📁 Input:  {INPUT_DIR}")
    print(f"📁 Output: {OUTPUT_DIR}")


def get_images():
    """Obtener lista de imágenes del directorio input."""
    images = []
    for file in INPUT_DIR.iterdir():
        if file.suffix.lower() in SUPPORTED_EXTENSIONS:
            images.append(file)
    return sorted(images)


def repair_mask_holes(mask: np.ndarray, max_hole_size: int = 500) -> np.ndarray:
    """
    Rellenar huecos pequeños en la máscara (bordes cortados).

    Args:
        mask: Máscara alpha (0-255)
        max_hole_size: Tamaño máximo de hueco a rellenar en píxeles

    Returns:
        Máscara con huecos rellenados
    """
    # Binarizar máscara
    binary = (mask > 128).astype(np.uint8)

    # Encontrar contornos
    contours, hierarchy = cv2.findContours(binary, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)

    if hierarchy is None:
        return mask

    # Rellenar huecos pequeños (contornos internos)
    for i, (contour, hier) in enumerate(zip(contours, hierarchy[0])):
        # hier[3] >= 0 significa que es un contorno interno (hueco)
        if hier[3] >= 0:
            area = cv2.contourArea(contour)
            if area < max_hole_size:
                cv2.drawContours(binary, [contour], -1, 1, -1)

    # Convertir de nuevo a 0-255
    return (binary * 255).astype(np.uint8)


def morphological_refinement(mask: np.ndarray, iterations: int = 2) -> np.ndarray:
    """
    Aplicar operaciones morfológicas para suavizar y reparar bordes.

    Pipeline:
    1. Closing (dilate + erode) - cierra huecos pequeños
    2. Opening (erode + dilate) - elimina ruido
    3. Dilate ligero - recupera bordes perdidos
    """
    # Kernel para operaciones morfológicas
    kernel_close = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    kernel_open = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    kernel_dilate = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))

    # 1. Closing - cierra huecos pequeños en los bordes
    closed = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel_close, iterations=iterations)

    # 2. Opening - elimina pequeños artefactos de ruido
    opened = cv2.morphologyEx(closed, cv2.MORPH_OPEN, kernel_open, iterations=1)

    # 3. Dilate ligero - recupera bordes que se pudieron perder
    dilated = cv2.dilate(opened, kernel_dilate, iterations=1)

    return dilated


def smooth_contours(mask: np.ndarray, smoothing_factor: float = 0.02) -> np.ndarray:
    """
    Suavizar los contornos de la máscara usando aproximación poligonal.
    Esto elimina los bordes dentados y hace curvas más suaves.
    """
    # Binarizar
    binary = (mask > 128).astype(np.uint8)

    # Encontrar contornos
    contours, _ = cv2.findContours(binary, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    if not contours:
        return mask

    # Crear máscara vacía
    smooth_mask = np.zeros_like(binary)

    for contour in contours:
        # Calcular perímetro
        perimeter = cv2.arcLength(contour, True)

        # Aproximar contorno con menos puntos (suavizado)
        epsilon = smoothing_factor * perimeter
        approx = cv2.approxPolyDP(contour, epsilon, True)

        # Dibujar contorno suavizado
        cv2.drawContours(smooth_mask, [approx], -1, 1, -1)

    return (smooth_mask * 255).astype(np.uint8)


def feather_edges(mask: np.ndarray, feather_radius: int = 3) -> np.ndarray:
    """
    Aplicar feathering (suavizado gradual) a los bordes de la máscara.
    Esto crea una transición suave entre el objeto y el fondo.
    """
    # Convertir a float para procesamiento
    mask_float = mask.astype(np.float32) / 255.0

    # Aplicar blur gaussiano
    feathered = cv2.GaussianBlur(mask_float, (0, 0), feather_radius)

    # Combinar: mantener centro sólido, solo suavizar bordes
    # Crear máscara de bordes
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (feather_radius * 2 + 1, feather_radius * 2 + 1))
    eroded = cv2.erode(mask_float, kernel, iterations=1)

    # El centro permanece sólido, los bordes se suavizan
    result = np.where(eroded > 0.5, mask_float, feathered)

    return (result * 255).astype(np.uint8)


def anti_aliasing(mask: np.ndarray) -> np.ndarray:
    """
    Aplicar anti-aliasing a los bordes de la máscara.
    """
    # Detectar bordes
    edges = cv2.Canny(mask, 100, 200)

    # Dilatar bordes ligeramente
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    dilated_edges = cv2.dilate(edges, kernel, iterations=1)

    # Aplicar blur solo en los bordes
    blurred = cv2.GaussianBlur(mask, (5, 5), 0)

    # Combinar: original en interior, suavizado en bordes
    result = mask.copy()
    result[dilated_edges > 0] = blurred[dilated_edges > 0]

    return result


def refine_alpha_channel(img: Image.Image) -> Image.Image:
    """
    Pipeline completo de refinamiento del canal alpha.
    """
    if img.mode != "RGBA":
        return img

    # Separar canales
    r, g, b, a = img.split()
    alpha = np.array(a)

    # Pipeline de refinamiento
    print("      🔧 Reparando huecos en bordes...")
    alpha = repair_mask_holes(alpha, max_hole_size=300)

    print("      🔧 Operaciones morfológicas...")
    alpha = morphological_refinement(alpha, iterations=2)

    print("      🔧 Suavizando contornos...")
    # alpha = smooth_contours(alpha, smoothing_factor=0.01)  # Opcional, puede perder detalle

    print("      🔧 Aplicando feathering...")
    alpha = feather_edges(alpha, feather_radius=2)

    print("      🔧 Anti-aliasing...")
    alpha = anti_aliasing(alpha)

    # Recombinar
    a_refined = Image.fromarray(alpha)
    return Image.merge("RGBA", (r, g, b, a_refined))


def remove_small_artifacts(img: Image.Image, min_size: int = 500) -> Image.Image:
    """
    Eliminar pequeños artefactos que no son parte del vehículo.
    """
    if img.mode != "RGBA":
        return img

    arr = np.array(img)
    alpha = arr[:, :, 3]

    # Crear máscara binaria
    binary_mask = alpha > 128

    # Etiquetar componentes conectados
    labeled, num_features = ndimage.label(binary_mask)

    if num_features <= 1:
        return img

    # Encontrar el componente más grande (el vehículo)
    component_sizes = ndimage.sum(binary_mask, labeled, range(1, num_features + 1))
    largest_component = np.argmax(component_sizes) + 1

    # Mantener solo el componente más grande
    clean_mask = (labeled == largest_component).astype(np.uint8) * 255

    # Aplicar la máscara limpia
    arr[:, :, 3] = np.where(clean_mask > 0, alpha, 0)

    return Image.fromarray(arr)


def add_contact_shadow(img: Image.Image, shadow_height: int = 30, shadow_opacity: int = 60) -> Image.Image:
    """
    Añadir sombra de contacto debajo del vehículo.
    """
    if img.mode != "RGBA":
        return img

    width, height = img.size
    arr = np.array(img)
    alpha = arr[:, :, 3]

    # Encontrar la parte inferior del vehículo
    rows_with_content = np.where(np.any(alpha > 128, axis=1))[0]
    if len(rows_with_content) == 0:
        return img

    bottom_row = rows_with_content[-1]
    bottom_pixels = np.where(alpha[bottom_row] > 128)[0]
    if len(bottom_pixels) == 0:
        return img

    left_x = bottom_pixels[0]
    right_x = bottom_pixels[-1]
    vehicle_width = right_x - left_x

    # Nueva imagen con espacio para sombra
    new_height = height + shadow_height
    new_img = Image.new("RGBA", (width, new_height), (255, 255, 255, 0))

    # Crear sombra
    shadow = Image.new("RGBA", (width, new_height), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)

    shadow_left = left_x + int(vehicle_width * 0.1)
    shadow_right = right_x - int(vehicle_width * 0.1)
    shadow_top = bottom_row + 5

    for i in range(shadow_height):
        opacity = int(shadow_opacity * (1 - i / shadow_height) ** 2)
        y = shadow_top + i
        shrink = int(vehicle_width * 0.1 * (i / shadow_height))
        shadow_draw.ellipse([shadow_left + shrink, y, shadow_right - shrink, y + 2], fill=(0, 0, 0, opacity))

    shadow = shadow.filter(ImageFilter.GaussianBlur(radius=10))

    new_img = Image.alpha_composite(new_img, shadow)
    new_img.paste(img, (0, 0), img)

    return new_img


def enhance_image(img: Image.Image) -> Image.Image:
    """
    Mejorar ligeramente la imagen.
    """
    if img.mode == "RGBA":
        r, g, b, a = img.split()
        rgb = Image.merge("RGB", (r, g, b))

        enhancer = ImageEnhance.Contrast(rgb)
        rgb = enhancer.enhance(1.03)

        enhancer = ImageEnhance.Sharpness(rgb)
        rgb = enhancer.enhance(1.05)

        r, g, b = rgb.split()
        return Image.merge("RGBA", (r, g, b, a))

    return img


def remove_background_v3(input_path: Path, output_path: Path, add_shadow: bool = True) -> bool:
    """
    Remover fondo con refinamiento avanzado de bordes.
    """
    try:
        # Cargar imagen
        input_image = Image.open(input_path)

        # Convertir a RGB
        if input_image.mode in ("RGBA", "LA", "P"):
            background = Image.new("RGB", input_image.size, (255, 255, 255))
            if input_image.mode == "P":
                input_image = input_image.convert("RGBA")
            if input_image.mode == "RGBA":
                background.paste(input_image, mask=input_image.split()[3])
            else:
                background.paste(input_image)
            input_image = background
        elif input_image.mode != "RGB":
            input_image = input_image.convert("RGB")

        # Obtener sesión del modelo
        session = get_session()

        print("   📸 Removiendo fondo con BiRefNet...")
        # Remover fondo con alpha matting
        output_image = remove(
            input_image,
            session=session,
            alpha_matting=True,
            alpha_matting_foreground_threshold=240,
            alpha_matting_background_threshold=20,
            alpha_matting_erode_size=10,
        )

        # === REFINAMIENTO DE BORDES ===
        print("   🔧 Refinando bordes...")
        output_image = refine_alpha_channel(output_image)

        # Eliminar artefactos pequeños
        print("   🧹 Eliminando artefactos...")
        output_image = remove_small_artifacts(output_image, min_size=500)

        # Mejorar imagen
        output_image = enhance_image(output_image)

        # Añadir sombra de contacto
        if add_shadow:
            print("   🌑 Añadiendo sombra...")
            output_image = add_contact_shadow(output_image, shadow_height=25, shadow_opacity=50)

        # Guardar
        output_image.save(output_path, "PNG", optimize=True)

        return True

    except Exception as e:
        print(f"   ❌ Error: {e}")
        import traceback

        traceback.print_exc()
        return False


def create_white_background_version(png_path: Path):
    """Crear versión con fondo blanco."""
    try:
        img = Image.open(png_path)
        white_bg = Image.new("RGB", img.size, (255, 255, 255))
        if img.mode == "RGBA":
            white_bg.paste(img, mask=img.split()[3])
        else:
            white_bg.paste(img)
        white_path = png_path.with_name(png_path.stem + "_white.jpg")
        white_bg.save(white_path, "JPEG", quality=95)
    except Exception as e:
        print(f"   ⚠️ Error creando versión blanca: {e}")


def create_showroom_background_version(png_path: Path):
    """Crear versión con fondo gris gradiente."""
    try:
        img = Image.open(png_path)
        width, height = img.size
        gray_bg = Image.new("RGB", (width, height), (245, 245, 245))

        draw = ImageDraw.Draw(gray_bg)
        for y in range(height):
            gray_value = int(245 - (y / height) * 20)
            draw.line([(0, y), (width, y)], fill=(gray_value, gray_value, gray_value))

        if img.mode == "RGBA":
            gray_bg.paste(img, mask=img.split()[3])
        else:
            gray_bg.paste(img)

        gray_path = png_path.with_name(png_path.stem + "_showroom.jpg")
        gray_bg.save(gray_path, "JPEG", quality=95)
    except Exception as e:
        print(f"   ⚠️ Error creando versión showroom: {e}")


def main():
    """Procesar todas las imágenes."""
    print("=" * 75)
    print("🚗 REMBG V3 - Remoción de Fondo con REFINAMIENTO DE BORDES")
    print("=" * 75)
    print(f"🔧 Modelo: {MODEL_NAME}")
    print("🔧 Alpha Matting: Habilitado")
    print("🔧 Reparación de huecos: Habilitado")
    print("🔧 Operaciones morfológicas: Closing + Opening + Dilate")
    print("🔧 Feathering de bordes: Habilitado")
    print("🔧 Anti-aliasing: Habilitado")
    print("🔧 Sombra de contacto: Habilitado")
    print("=" * 75)

    setup_directories()
    images = get_images()

    if not images:
        print("\n⚠️ No se encontraron imágenes en el directorio input/")
        return

    print(f"\n📷 Imágenes encontradas: {len(images)}")
    print("-" * 75)

    successful = 0
    failed = 0
    start_time = time.time()

    for i, image_path in enumerate(images, 1):
        output_name = image_path.stem + ".png"
        output_path = OUTPUT_DIR / output_name

        print(f"\n[{i}/{len(images)}] Procesando: {image_path.name}")

        img_start = time.time()

        if remove_background_v3(image_path, output_path, add_shadow=True):
            img_time = time.time() - img_start
            print(f"   ✅ Guardado: {output_name} ({img_time:.1f}s)")

            create_white_background_version(output_path)
            print("   ✅ Versión fondo blanco")

            create_showroom_background_version(output_path)
            print("   ✅ Versión showroom")

            successful += 1
        else:
            failed += 1

    total_time = time.time() - start_time
    avg_time = total_time / len(images) if images else 0

    print("\n" + "=" * 75)
    print("📊 RESUMEN")
    print("=" * 75)
    print(f"   ✅ Exitosas:     {successful}")
    print(f"   ❌ Fallidas:     {failed}")
    print(f"   ⏱️  Tiempo total: {total_time:.1f} segundos")
    print(f"   ⏱️  Promedio:     {avg_time:.1f} segundos/imagen")
    print(f"   📁 Output:       {OUTPUT_DIR}")
    print("=" * 75)
    print("\n📦 Archivos generados por imagen:")
    print("   • .png          → Fondo transparente + sombra + bordes refinados")
    print("   • _white.jpg    → Fondo blanco")
    print("   • _showroom.jpg → Fondo gris gradiente")
    print("=" * 75)


if __name__ == "__main__":
    main()
