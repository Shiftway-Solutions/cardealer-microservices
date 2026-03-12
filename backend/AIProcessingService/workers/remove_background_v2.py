#!/usr/bin/env python3
"""
Script MEJORADO para remover el fondo de imágenes de vehículos.
Usa modelo BiRefNet + Alpha Matting + Post-procesamiento + Sombra artificial.

Input: ./input/
Output: ./output_v2/
"""

import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter
from rembg import new_session, remove

# Configuración de directorios
SCRIPT_DIR = Path(__file__).parent
INPUT_DIR = SCRIPT_DIR / "input"
OUTPUT_DIR = SCRIPT_DIR / "output_v2"

# Extensiones de imagen soportadas
SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tiff"}

# Configuración del modelo - BiRefNet es el más preciso para vehículos
# Opciones: 'birefnet-general', 'isnet-general-use', 'u2net', 'u2netp'
MODEL_NAME = "birefnet-general"

# Sesión global del modelo (se carga una vez)
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


def clean_alpha_channel(img: Image.Image, threshold: int = 240) -> Image.Image:
    """
    Limpiar el canal alpha para eliminar artefactos semi-transparentes.
    Hace los píxeles casi opacos completamente opacos y los casi transparentes completamente transparentes.
    """
    if img.mode != "RGBA":
        return img

    # Convertir a numpy para procesamiento rápido
    arr = np.array(img)
    alpha = arr[:, :, 3]

    # Limpiar alpha: hacer binario con threshold
    # Píxeles con alpha > threshold -> completamente opaco (255)
    # Píxeles con alpha < (255 - threshold) -> completamente transparente (0)
    alpha_clean = np.where(alpha > threshold, 255, alpha)
    alpha_clean = np.where(alpha_clean < (255 - threshold), 0, alpha_clean)

    arr[:, :, 3] = alpha_clean
    return Image.fromarray(arr)


def refine_edges(img: Image.Image, iterations: int = 1) -> Image.Image:
    """
    Refinar los bordes de la máscara alpha para hacerlos más suaves.
    """
    if img.mode != "RGBA":
        return img

    # Separar canales
    r, g, b, a = img.split()

    # Aplicar un ligero blur al canal alpha para suavizar bordes
    for _ in range(iterations):
        a = a.filter(ImageFilter.MedianFilter(size=3))

    # Recombinar
    return Image.merge("RGBA", (r, g, b, a))


def remove_small_artifacts(img: Image.Image, min_size: int = 100) -> Image.Image:
    """
    Eliminar pequeños artefactos (islas de píxeles) que no son parte del vehículo.
    """
    if img.mode != "RGBA":
        return img

    arr = np.array(img)
    alpha = arr[:, :, 3]

    # Encontrar componentes conectados
    from scipy import ndimage

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


def add_contact_shadow(img: Image.Image, shadow_height: int = 30, shadow_opacity: int = 80) -> Image.Image:
    """
    Añadir sombra de contacto debajo del vehículo para mayor realismo.
    """
    if img.mode != "RGBA":
        return img

    width, height = img.size

    # Encontrar la parte inferior del vehículo
    arr = np.array(img)
    alpha = arr[:, :, 3]

    # Encontrar la fila más baja con píxeles opacos
    rows_with_content = np.where(np.any(alpha > 128, axis=1))[0]
    if len(rows_with_content) == 0:
        return img

    bottom_row = rows_with_content[-1]

    # Encontrar los límites horizontales del vehículo en la parte inferior
    bottom_pixels = np.where(alpha[bottom_row] > 128)[0]
    if len(bottom_pixels) == 0:
        return img

    left_x = bottom_pixels[0]
    right_x = bottom_pixels[-1]
    vehicle_width = right_x - left_x

    # Crear nueva imagen con espacio para sombra
    new_height = height + shadow_height
    new_img = Image.new("RGBA", (width, new_height), (255, 255, 255, 0))

    # Crear sombra elíptica
    shadow = Image.new("RGBA", (width, new_height), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)

    # Dibujar elipse de sombra
    shadow_left = left_x + int(vehicle_width * 0.1)
    shadow_right = right_x - int(vehicle_width * 0.1)
    shadow_top = bottom_row + 5
    bottom_row + shadow_height

    # Crear gradiente de sombra
    for i in range(shadow_height):
        opacity = int(shadow_opacity * (1 - i / shadow_height) ** 2)
        y = shadow_top + i
        # Hacer la elipse más estrecha a medida que baja
        shrink = int(vehicle_width * 0.1 * (i / shadow_height))
        shadow_draw.ellipse([shadow_left + shrink, y, shadow_right - shrink, y + 2], fill=(0, 0, 0, opacity))

    # Aplicar blur a la sombra
    shadow = shadow.filter(ImageFilter.GaussianBlur(radius=10))

    # Combinar: primero sombra, luego vehículo
    new_img = Image.alpha_composite(new_img, shadow)
    new_img.paste(img, (0, 0), img)

    return new_img


def enhance_image(img: Image.Image) -> Image.Image:
    """
    Mejorar ligeramente la imagen (contraste, nitidez).
    """
    if img.mode == "RGBA":
        # Separar alpha
        r, g, b, a = img.split()
        rgb = Image.merge("RGB", (r, g, b))

        # Mejorar
        enhancer = ImageEnhance.Contrast(rgb)
        rgb = enhancer.enhance(1.05)

        enhancer = ImageEnhance.Sharpness(rgb)
        rgb = enhancer.enhance(1.1)

        # Recombinar
        r, g, b = rgb.split()
        return Image.merge("RGBA", (r, g, b, a))

    return img


def remove_background_advanced(input_path: Path, output_path: Path, add_shadow: bool = True) -> bool:
    """
    Remover fondo de forma avanzada con múltiples técnicas de post-procesamiento.
    """
    try:
        # Cargar imagen
        input_image = Image.open(input_path)
        input_image.size

        # Convertir a RGB si es necesario
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

        # Remover fondo con alpha matting habilitado
        output_image = remove(
            input_image,
            session=session,
            alpha_matting=True,
            alpha_matting_foreground_threshold=240,
            alpha_matting_background_threshold=20,
            alpha_matting_erode_size=10,
        )

        # Post-procesamiento
        # 1. Limpiar canal alpha
        output_image = clean_alpha_channel(output_image, threshold=230)

        # 2. Refinar bordes
        output_image = refine_edges(output_image, iterations=1)

        # 3. Eliminar artefactos pequeños
        try:
            output_image = remove_small_artifacts(output_image, min_size=500)
        except ImportError:
            pass  # scipy no disponible

        # 4. Mejorar imagen
        output_image = enhance_image(output_image)

        # 5. Añadir sombra de contacto (opcional)
        if add_shadow:
            output_image = add_contact_shadow(output_image, shadow_height=25, shadow_opacity=60)

        # Guardar como PNG
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

        # Crear fondo blanco
        white_bg = Image.new("RGB", img.size, (255, 255, 255))

        # Pegar imagen sobre fondo blanco
        if img.mode == "RGBA":
            white_bg.paste(img, mask=img.split()[3])
        else:
            white_bg.paste(img)

        # Guardar
        white_path = png_path.with_name(png_path.stem + "_white.jpg")
        white_bg.save(white_path, "JPEG", quality=95)

    except Exception as e:
        print(f"   ⚠️ No se pudo crear versión con fondo blanco: {e}")


def create_gray_background_version(png_path: Path):
    """Crear versión con fondo gris neutro (estilo showroom)."""
    try:
        img = Image.open(png_path)

        # Crear fondo gris gradiente
        width, height = img.size
        gray_bg = Image.new("RGB", (width, height), (245, 245, 245))

        # Crear gradiente sutil
        draw = ImageDraw.Draw(gray_bg)
        for y in range(height):
            # Gradiente de arriba (más claro) a abajo (más oscuro)
            gray_value = int(245 - (y / height) * 20)
            draw.line([(0, y), (width, y)], fill=(gray_value, gray_value, gray_value))

        # Pegar imagen sobre fondo gris
        if img.mode == "RGBA":
            gray_bg.paste(img, mask=img.split()[3])
        else:
            gray_bg.paste(img)

        # Guardar
        gray_path = png_path.with_name(png_path.stem + "_showroom.jpg")
        gray_bg.save(gray_path, "JPEG", quality=95)

    except Exception as e:
        print(f"   ⚠️ No se pudo crear versión showroom: {e}")


def main():
    """Procesar todas las imágenes."""
    print("=" * 70)
    print("🚗 REMBG PRO - Removedor de Fondo AVANZADO para Vehículos")
    print("=" * 70)
    print(f"🔧 Modelo: {MODEL_NAME}")
    print("🔧 Alpha Matting: Habilitado")
    print("🔧 Sombra de contacto: Habilitado")
    print("🔧 Post-procesamiento: Habilitado")
    print("=" * 70)

    # Preparar directorios
    setup_directories()

    # Obtener imágenes
    images = get_images()

    if not images:
        print("\n⚠️ No se encontraron imágenes en el directorio input/")
        print(f"   Extensiones soportadas: {', '.join(SUPPORTED_EXTENSIONS)}")
        return

    print(f"\n📷 Imágenes encontradas: {len(images)}")
    print("-" * 70)

    # Procesar cada imagen
    successful = 0
    failed = 0
    start_time = time.time()

    for i, image_path in enumerate(images, 1):
        # Nombre de salida
        output_name = image_path.stem + ".png"
        output_path = OUTPUT_DIR / output_name

        print(f"\n[{i}/{len(images)}] Procesando: {image_path.name}")

        img_start = time.time()

        if remove_background_advanced(image_path, output_path, add_shadow=True):
            img_time = time.time() - img_start
            print(f"   ✅ Guardado: {output_name} ({img_time:.1f}s)")

            # Crear versiones adicionales
            create_white_background_version(output_path)
            print("   ✅ Versión fondo blanco creada")

            create_gray_background_version(output_path)
            print("   ✅ Versión showroom creada")

            successful += 1
        else:
            failed += 1

    # Resumen
    total_time = time.time() - start_time
    avg_time = total_time / len(images) if images else 0

    print("\n" + "=" * 70)
    print("📊 RESUMEN")
    print("=" * 70)
    print(f"   ✅ Exitosas:     {successful}")
    print(f"   ❌ Fallidas:     {failed}")
    print(f"   ⏱️  Tiempo total: {total_time:.1f} segundos")
    print(f"   ⏱️  Promedio:     {avg_time:.1f} segundos/imagen")
    print(f"   📁 Output:       {OUTPUT_DIR}")
    print("=" * 70)
    print("\n📦 Archivos generados por imagen:")
    print("   • .png         → Fondo transparente con sombra")
    print("   • _white.jpg   → Fondo blanco")
    print("   • _showroom.jpg → Fondo gris gradiente (estilo showroom)")
    print("=" * 70)


if __name__ == "__main__":
    main()
