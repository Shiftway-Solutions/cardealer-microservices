#!/usr/bin/env python3
"""
Script para remover el fondo de imágenes de vehículos usando rembg.
Input: ./input/
Output: ./output_v1/
"""

import time
from pathlib import Path

from PIL import Image
from rembg import remove

# Configuración de directorios
SCRIPT_DIR = Path(__file__).parent
INPUT_DIR = SCRIPT_DIR / "input"
OUTPUT_DIR = SCRIPT_DIR / "output_v1"

# Extensiones de imagen soportadas
SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tiff"}


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


def remove_background(input_path: Path, output_path: Path) -> bool:
    """
    Remover fondo de una imagen usando rembg.

    Args:
        input_path: Ruta de la imagen original
        output_path: Ruta donde guardar la imagen sin fondo

    Returns:
        True si fue exitoso, False si hubo error
    """
    try:
        # Cargar imagen
        input_image = Image.open(input_path)

        # Convertir a RGB si es necesario (para webp con alpha)
        if input_image.mode in ("RGBA", "LA", "P"):
            # Crear fondo blanco para imágenes con transparencia
            background = Image.new("RGB", input_image.size, (255, 255, 255))
            if input_image.mode == "P":
                input_image = input_image.convert("RGBA")
            background.paste(input_image, mask=input_image.split()[-1] if len(input_image.split()) == 4 else None)
            input_image = background
        elif input_image.mode != "RGB":
            input_image = input_image.convert("RGB")

        # Remover fondo
        output_image = remove(input_image)

        # Guardar como PNG para preservar transparencia
        output_image.save(output_path, "PNG", quality=95)

        return True

    except Exception as e:
        print(f"   ❌ Error: {e}")
        return False


def create_white_background_version(png_path: Path):
    """Crear versión con fondo blanco además de la transparente."""
    try:
        img = Image.open(png_path)

        # Crear fondo blanco
        white_bg = Image.new("RGB", img.size, (255, 255, 255))

        # Pegar imagen sobre fondo blanco
        if img.mode == "RGBA":
            white_bg.paste(img, mask=img.split()[3])
        else:
            white_bg.paste(img)

        # Guardar versión con fondo blanco
        white_path = png_path.with_name(png_path.stem + "_white.jpg")
        white_bg.save(white_path, "JPEG", quality=95)

    except Exception as e:
        print(f"   ⚠️ No se pudo crear versión con fondo blanco: {e}")


def main():
    """Procesar todas las imágenes."""
    print("=" * 60)
    print("🚗 REMBG - Removedor de Fondo para Vehículos")
    print("=" * 60)

    # Preparar directorios
    setup_directories()

    # Obtener imágenes
    images = get_images()

    if not images:
        print("\n⚠️ No se encontraron imágenes en el directorio input/")
        print(f"   Extensiones soportadas: {', '.join(SUPPORTED_EXTENSIONS)}")
        return

    print(f"\n📷 Imágenes encontradas: {len(images)}")
    print("-" * 60)

    # Procesar cada imagen
    successful = 0
    failed = 0
    start_time = time.time()

    for i, image_path in enumerate(images, 1):
        # Nombre de salida (cambiar extensión a .png)
        output_name = image_path.stem + ".png"
        output_path = OUTPUT_DIR / output_name

        print(f"\n[{i}/{len(images)}] Procesando: {image_path.name}")

        img_start = time.time()

        if remove_background(image_path, output_path):
            img_time = time.time() - img_start
            print(f"   ✅ Guardado: {output_name} ({img_time:.1f}s)")

            # También crear versión con fondo blanco
            create_white_background_version(output_path)
            print("   ✅ Versión fondo blanco creada")

            successful += 1
        else:
            failed += 1

    # Resumen
    total_time = time.time() - start_time
    print("\n" + "=" * 60)
    print("📊 RESUMEN")
    print("=" * 60)
    print(f"   ✅ Exitosas:  {successful}")
    print(f"   ❌ Fallidas:  {failed}")
    print(f"   ⏱️  Tiempo:   {total_time:.1f} segundos")
    print(f"   📁 Output:   {OUTPUT_DIR}")
    print("=" * 60)


if __name__ == "__main__":
    main()
