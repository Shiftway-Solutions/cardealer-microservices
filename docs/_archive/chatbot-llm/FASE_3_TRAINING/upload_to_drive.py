#!/usr/bin/env python3
"""
============================================================
OKLA — Subir Dataset a Google Drive para Entrenamiento
============================================================

Este script sube los archivos JSONL del dataset de FASE 2
a Google Drive, donde serán accesibles desde el runtime de
Google Colab.

Uso:
    python3 upload_to_drive.py

Pre-requisitos:
    pip install google-api-python-client google-auth-oauthlib

El script:
    1. Autentica con tu cuenta de Google (abre navegador)
    2. Crea la estructura OKLA/dataset/ en Drive si no existe
    3. Sube okla_train.jsonl, okla_eval.jsonl, okla_test.jsonl
    4. Verifica la subida
============================================================
"""
import os
import sys
import json
from pathlib import Path

# ── Paths ──────────────────────────────────────────────────
SCRIPT_DIR = Path(__file__).parent.resolve()
DATASET_DIR = SCRIPT_DIR.parent / "FASE_2_DATASET" / "output"

FILES_TO_UPLOAD = [
    "okla_train.jsonl",
    "okla_eval.jsonl",
    "okla_test.jsonl",
]


def check_dependencies():
    """Verifica que las dependencias están instaladas."""
    try:
        from google.oauth2.credentials import Credentials
        from google_auth_oauthlib.flow import InstalledAppFlow
        from googleapiclient.discovery import build
        return True
    except ImportError:
        return False


def upload_via_google_api():
    """Sube archivos usando la API oficial de Google Drive."""
    from google.oauth2.credentials import Credentials
    from google_auth_oauthlib.flow import InstalledAppFlow
    from google.auth.transport.requests import Request
    from googleapiclient.discovery import build
    from googleapiclient.http import MediaFileUpload
    import pickle

    SCOPES = ['https://www.googleapis.com/auth/drive.file']
    TOKEN_FILE = SCRIPT_DIR / '.gdrive_token.pickle'

    creds = None
    if TOKEN_FILE.exists():
        with open(TOKEN_FILE, 'rb') as f:
            creds = pickle.load(f)

    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            creds.refresh(Request())
        else:
            # Check for credentials file
            creds_file = SCRIPT_DIR / 'credentials.json'
            if not creds_file.exists():
                print("❌ No se encontró credentials.json")
                print()
                print("   Para usar la API de Google Drive necesitas:")
                print("   1. Ir a https://console.cloud.google.com/apis/credentials")
                print("   2. Crear un OAuth 2.0 Client ID (tipo Desktop)")
                print("   3. Descargar el JSON y guardarlo como:")
                print(f"      {creds_file}")
                print()
                return False

            flow = InstalledAppFlow.from_client_secrets_file(str(creds_file), SCOPES)
            creds = flow.run_local_server(port=0)

        with open(TOKEN_FILE, 'wb') as f:
            pickle.dump(creds, f)

    service = build('drive', 'v3', credentials=creds)

    # Crear carpeta OKLA si no existe
    def find_or_create_folder(name, parent_id=None):
        query = f"name='{name}' and mimeType='application/vnd.google-apps.folder' and trashed=false"
        if parent_id:
            query += f" and '{parent_id}' in parents"

        results = service.files().list(q=query, fields="files(id, name)").execute()
        files = results.get('files', [])

        if files:
            return files[0]['id']

        metadata = {
            'name': name,
            'mimeType': 'application/vnd.google-apps.folder',
        }
        if parent_id:
            metadata['parents'] = [parent_id]

        folder = service.files().create(body=metadata, fields='id').execute()
        return folder['id']

    print("📂 Creando estructura en Google Drive...")
    okla_id = find_or_create_folder("OKLA")
    dataset_id = find_or_create_folder("dataset", okla_id)
    print(f"   ✅ OKLA/dataset/ → ID: {dataset_id}")

    # Subir archivos
    for filename in FILES_TO_UPLOAD:
        filepath = DATASET_DIR / filename
        if not filepath.exists():
            print(f"   ❌ {filename} no encontrado en {DATASET_DIR}")
            continue

        size_mb = filepath.stat().st_size / 1024 / 1024
        print(f"   📤 Subiendo {filename} ({size_mb:.1f} MB)...")

        # Check if file already exists
        query = f"name='{filename}' and '{dataset_id}' in parents and trashed=false"
        existing = service.files().list(q=query, fields="files(id)").execute().get('files', [])

        media = MediaFileUpload(str(filepath), mimetype='application/jsonl')

        if existing:
            # Update existing file
            service.files().update(
                fileId=existing[0]['id'],
                media_body=media
            ).execute()
            print(f"   ✅ {filename} actualizado")
        else:
            # Create new file
            metadata = {
                'name': filename,
                'parents': [dataset_id],
            }
            service.files().create(
                body=metadata,
                media_body=media,
                fields='id'
            ).execute()
            print(f"   ✅ {filename} subido")

    return True


def print_manual_instructions():
    """Muestra instrucciones para subir manualmente."""
    print()
    print("=" * 60)
    print("📋 INSTRUCCIONES — Subir Dataset a Google Drive")
    print("=" * 60)
    print()
    print("Los archivos JSONL están en:")
    print(f"   {DATASET_DIR}")
    print()
    print("Archivos a subir:")
    for f in FILES_TO_UPLOAD:
        fp = DATASET_DIR / f
        if fp.exists():
            size = fp.stat().st_size / 1024 / 1024
            lines = sum(1 for _ in open(fp))
            print(f"   ✅ {f} ({size:.1f} MB, {lines} conversaciones)")
        else:
            print(f"   ❌ {f} — NO ENCONTRADO")
    print()
    print("📁 Estructura esperada en Google Drive:")
    print("   Google Drive/")
    print("   └── OKLA/")
    print("       └── dataset/")
    print("           ├── okla_train.jsonl")
    print("           ├── okla_eval.jsonl")
    print("           └── okla_test.jsonl")
    print()
    print("🔧 Opciones para subir:")
    print()
    print("   OPCIÓN A — Drag & Drop (más fácil):")
    print("   1. Abre drive.google.com en el navegador")
    print("   2. Crea carpeta: OKLA > dataset")
    print("   3. Arrastra los 3 archivos .jsonl a la carpeta")
    print()
    print("   OPCIÓN B — Google Drive Desktop App:")
    print("   1. Si tienes Google Drive para Desktop instalado")
    print("   2. Copia los archivos a ~/Google Drive/OKLA/dataset/")
    print()
    print("   OPCIÓN C — gdrive CLI:")
    print("   brew install gdrive")
    print("   gdrive mkdir OKLA")
    print("   gdrive mkdir dataset --parent <OKLA_ID>")
    print(f"   gdrive upload {DATASET_DIR}/okla_train.jsonl --parent <dataset_ID>")
    print(f"   gdrive upload {DATASET_DIR}/okla_eval.jsonl --parent <dataset_ID>")
    print(f"   gdrive upload {DATASET_DIR}/okla_test.jsonl --parent <dataset_ID>")
    print()
    print("=" * 60)


def main():
    print("=" * 60)
    print("🤖 OKLA — Upload Dataset a Google Drive")
    print("=" * 60)

    # Verificar que el dataset existe
    print()
    print("📊 Verificando dataset local...")
    all_exist = True
    total_lines = 0
    total_size = 0

    for f in FILES_TO_UPLOAD:
        fp = DATASET_DIR / f
        if fp.exists():
            lines = sum(1 for _ in open(fp))
            size = fp.stat().st_size
            total_lines += lines
            total_size += size
            print(f"   ✅ {f}: {lines} conversaciones ({size/1024/1024:.1f} MB)")
        else:
            print(f"   ❌ {f}: NO ENCONTRADO")
            all_exist = False

    if not all_exist:
        print()
        print("❌ Faltan archivos del dataset.")
        print("   Ejecuta primero el pipeline de FASE 2:")
        print("   cd ../FASE_2_DATASET && python generate_dataset.py")
        sys.exit(1)

    print(f"\n   Total: {total_lines} conversaciones ({total_size/1024/1024:.1f} MB)")

    # Intentar subir via API
    if check_dependencies():
        print()
        print("🔑 Intentando subir via Google Drive API...")
        if upload_via_google_api():
            print()
            print("✅ Dataset subido exitosamente a Google Drive!")
            print("   Ubicación: Google Drive > OKLA > dataset/")
            print()
            print("🔜 Siguiente paso:")
            print("   1. Abre okla_finetune_llama3.ipynb en VS Code")
            print("   2. Click 'Select Kernel' → 'Colab' → 'New Colab Server'")
            print("   3. Selecciona GPU T4 (gratis) o A100 (Pro)")
            print("   4. Cmd+Shift+P → 'Colab: Mount Google Drive to Server'")
            print("   5. Ejecuta las celdas secuencialmente")
            return
    
    # Fallback: instrucciones manuales
    print_manual_instructions()


if __name__ == "__main__":
    main()
