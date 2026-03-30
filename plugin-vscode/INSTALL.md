# Instalación — Copilot Model Cycler v3.0

Plugin personalizado de VS Code. No está en el Marketplace. El único método que funciona correctamente es empaquetar como `.vsix` e instalar con el comando oficial de VS Code.

> ⚠️ **Copiar los archivos manualmente a `~/.vscode/extensions/` NO funciona.** VS Code no registra la extensión en su base de datos interna y la ignora por completo.

---

## Requisitos

- VS Code **≥ 1.90**
- GitHub Copilot instalado y activo
- Node.js instalado (`node --version`)
- macOS, Linux o Windows

---

## Instalación (único método que funciona)

### Paso 1 — Instalar `vsce`

```bash
npm install -g @vscode/vsce
```

### Paso 2 — Empaquetar desde la carpeta del plugin

```bash
cd plugin-vscode
vsce package --allow-missing-repository --skip-license
```

Genera `copilot-model-cycler-3.0.0.vsix` en la misma carpeta.

### Paso 3 — Instalar el .vsix con `code`

```bash
code --install-extension copilot-model-cycler-3.0.0.vsix
```

Deberías ver:

```
Installing extensions...
Extension 'copilot-model-cycler-3.0.0.vsix' was successfully installed.
```

### Paso 4 — Recargar VS Code

```
Cmd+Shift+P (Mac) / Ctrl+Shift+P (Windows) → "Developer: Reload Window"
```

Tras el reload aparecerá en la barra inferior: `🤖 [modelo]  💬0/25  [1/5]`

---

## Alternativa: instalar desde la UI de VS Code

1. `Cmd+Shift+P` → `Extensions: Install from VSIX...`
2. Seleccionar el archivo `plugin-vscode/copilot-model-cycler-3.0.0.vsix`
3. Clic en **Install**
4. `Cmd+Shift+P` → **Developer: Reload Window**

---

## Verificar que está instalado

1. Abrir `Cmd+Shift+P` → `Extensions: Show Installed Extensions`
2. Buscar **"Copilot Model Cycler"** — debe aparecer en la lista
3. En la barra inferior derecha de VS Code debe aparecer: `🤖 [modelo]  💬0/25  [1/N]`

---

## Configuración obligatoria (settings.json)

Abrir `Cmd+Shift+P` → `Preferences: Open User Settings (JSON)` y agregar:

```jsonc
{
  // Archivo de prompt que se enviará al abrir un nuevo chat
  "modelCycler.session.promptFilePath": ".prompts/prompt_1.md",

  // Lista de modelos de más poderoso a menos poderoso
  "modelCycler.models": [
    "claude-opus-4-6",
    "claude-opus-4-5",
    "claude-sonnet-4-6",
    "claude-sonnet-4-5",
    "claude-sonnet-4",
  ],

  // Etiquetas legibles para el status bar
  "modelCycler.modelLabels": {
    "claude-opus-4-6": "Opus 4.6 ⚡⚡⚡",
    "claude-opus-4-5": "Opus 4.5 ⚡⚡⚡",
    "claude-sonnet-4-6": "Sonnet 4.6 ⚡⚡",
    "claude-sonnet-4-5": "Sonnet 4.5 ⚡⚡",
    "claude-sonnet-4": "Sonnet 4 ⚡",
  },

  // Límite de mensajes antes de abrir nuevo chat automáticamente
  "modelCycler.session.maxMessages": 25,

  // Avisar cuando quedan N mensajes para el límite
  "modelCycler.session.warningAt": 20,

  // true = abre nuevo chat solo; false = pregunta primero
  "modelCycler.session.autoReset": true,

  // Al resetear: "top" = volver al primer modelo de la lista siempre
  "modelCycler.session.resetModel": "top",

  // Rate limit: "next" = siguiente modelo | "top" = más poderoso disponible
  "modelCycler.session.rateLimitCycleTo": "next",

  // Milisegundos de espera antes de enviar el prompt al nuevo chat
  "modelCycler.session.continuationDelay": 1000,

  // Intervalo de monitoreo automático de logs de Copilot (ms) — 0 = desactivado
  "modelCycler.monitoring.pollInterval": 3000,

  // Mostrar el status bar
  "modelCycler.showStatusBar": true,

  // Notificar al cambiar de modelo
  "modelCycler.notifyOnSwitch": true,
}
```

---

## Atajos de teclado

Solo activos cuando el foco está en el chat de Copilot (`chatInputHasFocus`), excepto donde se indica.

| Acción                                   | Mac     | Windows/Linux  |
| ---------------------------------------- | ------- | -------------- |
| Enviar mensaje + contar                  | `Enter` | `Enter`        |
| Rate limit → ciclar modelo + "Continuar" | `⌘⇧L`   | `Ctrl+Shift+L` |
| Error → nuevo chat + AGENT_LOOP_PROMPT   | `⌘⇧E`   | `Ctrl+Shift+E` |
| Nuevo chat manual + AGENT_LOOP_PROMPT    | `⌘⇧R`   | `Ctrl+Shift+R` |
| Siguiente modelo                         | `⌘⇧.`   | `Ctrl+Shift+.` |
| Modelo anterior                          | `⌘⇧,`   | `Ctrl+Shift+,` |
| Elegir modelo de la lista                | `⌘⇧M`   | `Ctrl+Shift+M` |

---

## Actualizar el plugin

Cuando modifiques `extension.js` o `package.json`, hay que reempaquetar e instalar:

```bash
cd plugin-vscode
vsce package --allow-missing-repository --skip-license
code --install-extension copilot-model-cycler-3.0.0.vsix
```

Luego: `Cmd+Shift+P` → **Developer: Reload Window**

> VS Code sobreescribe la versión anterior automáticamente.

---

## Desinstalar

### Eliminando la carpeta (manual)

```bash
rm -rf ~/.vscode/extensions/gregory-local.copilot-model-cycler-3.0.0
```

### Desde VS Code UI

`Cmd+Shift+P` → `Extensions: Show Installed Extensions` → buscar **Copilot Model Cycler** → `Uninstall`

---

## Troubleshooting

### El status bar no aparece

Verifica que `"modelCycler.showStatusBar": true` esté en tu `settings.json` y recarga VS Code.

### El comando `workbench.action.chat.submit` falla

Asegúrate de tener VS Code **≥ 1.90**. Versiones anteriores no tienen este comando.

### El prompt file no se encuentra

Verifica que el archivo `.prompts/prompt_1.md` exista en la raíz de tu workspace. Puedes cambiar la ruta en `"modelCycler.session.promptFilePath"`.

### Ver logs del plugin

`Cmd+Shift+P` → `Output: Show Output Channels` → seleccionar **Copilot Model Cycler**

### El Enter no funciona para enviar

El shortcut de `Enter` tiene `when: "chatInputHasFocus && !suggestWidgetVisible && !inlineChatFocused && !compositeInputBox.inFirst"`. Si hay algún conflict con otro plugin, puedes desactivar el binding manteniendo `trackAndSend` y usando solo `Cmd+Enter` como alternativa desde `keybindings.json`.
