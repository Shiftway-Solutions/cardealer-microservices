# Manual de Terminología Operativa del Smart Monitor

Este documento traduce frases de trabajo interno del agente a lenguaje operativo.
La idea es que cuando leas mensajes como "actualizo situation_summary" o
"35 lecciones sembradas" entiendas exactamente qué significa, qué archivo toca,
y cuándo debes intervenir según tu objetivo.

---

## 1. Mapa Mental del Sistema

El smart monitor funciona como una cadena de 6 capas:

1. `chat_snapshot.txt`
   Guarda una foto textual reciente de lo que se ve en el chat.

2. `observer.py`
   Lee señales del snapshot, logs, CDP y estado interno.
   Aquí se detecta si hay errores, actividad, estancamiento o contexto lleno.

3. `situation_summary`
   Es un resumen en lenguaje natural de lo que está pasando.
   No ejecuta acciones; describe la situación para que el cerebro del agente la entienda.

4. `brain.py`
   Decide qué hacer con base en la observación y en el resumen.
   Aquí viven el árbol de decisión y la prioridad de acciones.

5. `memory.py` + `memory.db`
   Guardan lo que el agente "sabe" y lo que aprende con el tiempo.
   `memory.py` define la estructura y las lecciones base.
   `memory.db` almacena esas lecciones ya sembradas.

6. `agent.py`
   Orquesta el loop completo: observar, decidir, actuar, verificar, aprender.

Resumen corto:

`snapshot/logs -> observer -> situation_summary -> brain -> action -> memory`

---

## 2. Glosario de la Terminología que Viste

### "Ahora actualizo la lógica de situation_summary para incluir el nuevo catálogo de estados"

Significa que se cambió la forma en que el observador redacta el estado actual del sistema.

No significa que el agente cambió una acción directamente.
Significa que ahora describe mejor lo que está pasando para que la capa de decisión razone mejor.

Ejemplo:

- Antes podía decir algo genérico como: "Sin cambios recientes en el chat".
- Ahora puede decir algo más útil como: "STALL CRITICO -> open_new_chat" o
  "RATE LIMIT detectado -> cycle_model".

Archivo principal:

- `.prompts/agent/smart_monitor/observer.py`

Qué tocas si quieres cambiar esto:

- `_summarize_situation()`
- patrones como `CHAT_ERROR_PATTERNS`, `LOG_PATTERNS`, `ACTIVE_GENERATION_PATTERNS`

---

### "catalogo de estados"

Es la lista de situaciones que el sistema sabe reconocer.

Por ejemplo:

- generacion activa
- actividad normal
- rate limit
- hard error
- tool validation error
- cancelled
- contexto lleno
- loop detenido
- stall suave
- stall critico

Un estado no es una accion.
Es una interpretacion del momento actual.

---

### "Ahora actualizo la memoria del agente con las lecciones iniciales del catalogo de errores"

Significa que se agregaron reglas base para que el agente arranque con conocimiento previo,
sin tener que aprender todo desde cero por prueba y error.

Ejemplo de leccion inicial:

- "Cuando hay rate limit, ciclar modelo inmediatamente es la accion correcta."

Estas lecciones no son observaciones del momento.
Son conocimiento persistente que el agente consulta cuando decide.

Archivo principal:

- `.prompts/agent/smart_monitor/memory.py`

Funcion principal:

- `seed_initial_lessons()`

---

### "lecciones iniciales"

Son reglas sembradas de fabrica.
Sirven para que el agente no empiece vacio.

Hay dos tipos de conocimiento:

1. Conocimiento inicial
   Lo defines en `seed_initial_lessons()`.

2. Conocimiento aprendido
   Se va acumulando en `memory.db` a partir de episodios reales y feedback.

---

### "catalogo de errores"

Es la lista de errores relevantes que el agente sabe reconocer y asociar con una accion.

Ejemplos:

- `rate_limited`
- `hard_error`
- `tool_validation_error`
- `cancelled`
- `loop_stopped`
- `context_full`

El catalogo vive repartido en dos capas:

1. Deteccion
   En `observer.py`, por regex y señales.

2. Respuesta estrategica
   En `brain.py`, dentro del `SYSTEM_PROMPT` y del fallback.

---

### "Ahora borrar la base de datos para que las nuevas lecciones se siembren desde cero"

Significa resetear la memoria persistente para que vuelva a construirse usando las nuevas
lecciones base del codigo actual.

La idea operativa es esta:

- Si cambiaste `seed_initial_lessons()` pero `memory.db` ya existe,
  el agente no volvera a sembrar esas lecciones automaticamente.
- Por eso necesitas reiniciar la base para forzar una nueva siembra.

En este repo, la forma segura no es borrar destructivamente.
La forma correcta es respaldar y recrear.

Ejemplo seguro:

```bash
mv .prompts/agent/smart_monitor/memory.db \
   .prompts/agent/smart_monitor/memory.db.bak_$(date +%Y%m%d_%H%M%S)
```

Luego, al correr el monitor o una rutina de seed, la base nueva se crea sola.

Archivo afectado:

- `.prompts/agent/smart_monitor/memory.db`

---

### "sembrar" o "seed"

Sembrar significa insertar datos iniciales en una base vacia.

En este contexto:

- el codigo define 35 lecciones
- la base nueva se crea
- esas 35 lecciones se insertan una vez

No es entrenamiento de modelo.
Es inicializacion de memoria estructurada.

---

### "35 lecciones sembradas"

Significa que la base quedo poblada correctamente con 35 registros activos en `lessons`.

Eso valida tres cosas:

1. La base existe y abre bien.
2. La funcion de seed corrio.
3. El conocimiento inicial esperado ya esta disponible para decisiones futuras.

No significa que el agente ya aprendio de la realidad.
Solo significa que arranco con el conocimiento base correcto.

---

### "Ahora verificar que el agente parsea correctamente con el nuevo codigo"

Parsear significa leer un texto o una señal y convertirla en una interpretacion estructurada.

Ejemplo:

- texto: `Thinking...`
- parseo esperado: generacion activa

- texto: `429 Too Many Requests`
- parseo esperado: `rate_limited`

- texto: `Compacting conversation...`
- parseo esperado: estado activo normal, no error

Cuando se dice "verificar que parsea correctamente", se esta comprobando que los regex,
patrones y condiciones del observador detectan bien cada caso.

Archivos implicados:

- `.prompts/agent/smart_monitor/observer.py`
- tests manuales o scripts de validacion

---

### "Compacted conversation"

Es una forma truncada o variante de `Compacting conversation...`.

En terminos operativos significa:

- el sistema esta compactando el contexto del chat
- no es un error
- no debes intervenir de inmediato
- se considera una senal de actividad interna

Decision correcta normalmente:

- `wait`

Solo se interviene si se convierte en un bloqueo anormal y pasa demasiado tiempo sin progreso.

---

### "6/6 OK"

Significa que una bateria de 6 pruebas paso completamente.

No prueba todo el sistema.
Prueba solo el bloque que se esta validando en ese momento.

Ejemplo:

- 6 casos de regex
- 6 escenarios de parseo
- 6 matches esperados

Lectura correcta:

- el cambio local parece sano
- aun falta validar el estado total del sistema

---

### "Let me now verify the full system state"

Significa pasar de una validacion puntual a una validacion integral.

Ejemplo de validacion integral:

- imports correctos
- patrones correctos
- base de memoria poblada
- conteo de lecciones correcto
- componentes clave cargando sin excepciones

---

### "35 lecciones co"

Eso no es un termino del sistema.
Es casi seguro una frase cortada por truncamiento de salida o resumen.

Lo correcto es leerlo como algo tipo:

- `35 lecciones confirmadas`
- `35 lecciones cargadas`
- `35 lecciones contabilizadas`

No debes crear una regla nueva basada solo en esa frase truncada.

---

## 3. Que Archivo Controla Cada Cosa

Si quieres cambiar el comportamiento, esta es la regla mas importante del sistema:

### Si quieres cambiar lo que el agente detecta

Tocas:

- `.prompts/agent/smart_monitor/observer.py`

Ejemplos:

- reconocer un error nuevo
- reconocer una frase nueva como actividad activa
- cambiar cuando algo cuenta como stall
- mejorar `situation_summary`

---

### Si quieres cambiar lo que el agente decide hacer

Tocas:

- `.prompts/agent/smart_monitor/brain.py`

Ejemplos:

- decidir que `rate_limit` haga `cycle_model` en vez de `wait`
- decidir que `stall > 10 min` haga `send_continue`
- cambiar la prioridad entre `open_new_chat` y `send_continue`

---

### Si quieres cambiar lo que el agente sabe de fabrica

Tocas:

- `.prompts/agent/smart_monitor/memory.py`

Ejemplos:

- agregar una nueva leccion base
- cambiar la confianza inicial de una leccion
- ajustar categorias como `rate_limit`, `stall`, `models`

Y si la base ya existe, debes resetear la DB para que esa siembra vuelva a correr.

---

### Si quieres resetear el conocimiento persistente

Tocas:

- `.prompts/agent/smart_monitor/memory.db`

Pero no se edita a mano normalmente.
Se respalda y se deja recrear.

---

### Si quieres cambiar el loop real de ejecucion

Tocas:

- `.prompts/agent/smart_monitor/agent.py`

Ejemplos:

- cada cuanto observa
- como ejecuta acciones
- cuando marca una post-accion
- como persiste offsets y contadores

---

## 4. Logica de Trabajo Segun lo que Tu Quieras

Esta es la parte practica: primero defines tu objetivo y luego sabes que capa tocar.

### Quiero que el agente entienda un error nuevo del chat

Haz esto:

1. Agrega el patron en `observer.py`.
2. Haz que ese patron se refleje en `snapshot_errors` o `log_dominant_event`.
3. Ajusta `_summarize_situation()` para que lo describa claramente.
4. Ajusta `brain.py` para decidir la accion correcta.
5. Si quieres que lo recuerde como regla base, agrega una leccion en `memory.py`.
6. Valida con casos de prueba de parseo.

Usa este camino cuando el sistema "no ve" algo que tu si ves.

---

### Quiero que el agente reaccione distinto ante un error que ya detecta

Haz esto:

1. No empieces por `observer.py` si la deteccion ya funciona.
2. Cambia la politica en `brain.py`.
3. Si hace falta, ajusta el texto de `situation_summary` para que la razon sea mas clara.
4. Agrega o corrige lecciones en `memory.py` si quieres reforzar la nueva conducta.

Usa este camino cuando el problema no es ver la senal sino decidir mal.

---

### Quiero que arranque con conocimiento distinto desde cero

Haz esto:

1. Edita `seed_initial_lessons()` en `memory.py`.
2. Respalda `memory.db`.
3. Deja que el sistema vuelva a sembrar la base.
4. Verifica el conteo de lecciones y sus categorias.

Usa este camino cuando cambias reglas base y quieres que la memoria persistente quede alineada.

---

### Quiero resetear la memoria porque quedo contaminada o vieja

Haz esto:

1. Respalda `memory.db`.
2. Recreate la DB desde el codigo actual.
3. Verifica que la nueva base tenga las lecciones esperadas.
4. Prueba una corrida del agente.

Usa este camino cuando el conocimiento persistido ya no coincide con la estrategia actual.

---

### Quiero que el agente sea mas conservador

Haz esto:

1. Amplia los casos de `wait` en `brain.py`.
2. Trata mas estados como "actividad normal" en `observer.py`.
3. Sube thresholds de stall.
4. Refuerza lecciones como "no intervenir durante generacion activa".

Usa esto si el agente esta interrumpiendo demasiado.

---

### Quiero que el agente sea mas agresivo

Haz esto:

1. Reduce thresholds de stall.
2. Haz que ciertos footers o estados disparen antes `send_continue` u `open_new_chat`.
3. Ajusta el `SYSTEM_PROMPT` para escalar mas rapido.
4. Valida muy bien que no interrumpa generaciones legitimas.

Usa esto si el agente deja morir chats que aun podrian rescatarse.

---

### Quiero cambiar la estrategia de modelos

Haz esto:

1. Cambia la prioridad o el pool en la logica correspondiente.
2. Ajusta lecciones de categoria `models`.
3. Mantén coherencia entre decision, memoria y documentacion.

Usa esto cuando quieras priorizar GPT-5.4, Claude u otro fallback.

---

### Quiero saber por que el agente no hizo nada

Revisa en este orden:

1. `chat_snapshot.txt`
2. `situation_summary`
3. `snapshot_errors`
4. `log_dominant_event`
5. `post_action_active`
6. thresholds de tiempo
7. decision final en `brain.py`

La mayoria de los "no hizo nada" caen en una de estas razones:

- detecto generacion activa y eligio `wait`
- estaba en post-accion y eligio `wait`
- no detecto bien el estado
- el prompt del cerebro priorizo otra accion

---

## 5. Flujo Correcto de Cambio

Cuando quieras tocar este sistema, usa esta secuencia mental:

1. Define el problema real.
   Ejemplo: "No detecta hard_error" o "detecta bien pero abre chat demasiado pronto".

2. Decide que capa es la responsable.
   Deteccion: `observer.py`
   Decision: `brain.py`
   Conocimiento base: `memory.py`
   Persistencia: `memory.db`

3. Cambia solo esa capa primero.
   No mezcles deteccion, estrategia y memoria si no hace falta.

4. Valida localmente.
   Primero parseo.
   Luego estado completo.

5. Si cambias seeds, resetea la DB.

6. Vuelve a probar con casos reales o snapshots reales.

---

## 6. Senales Normales vs Senales de Intervencion

### Senales normales

Estas normalmente significan `wait`:

- `Thinking...`
- `Preparing...`
- `Running <tool>`
- `Reading`
- `Writing`
- `Editing`
- `Searching`
- `Compacting conversation...`
- porcentaje visible como `23%`
- texto del chat cambiando sin errores

---

### Senales de intervencion

Estas normalmente significan actuar:

- `rate limit`, `429`, `quota exhausted` -> `cycle_model`
- `500`, `502`, `503`, `overloaded` -> `send_continue`, luego `open_new_chat` si persiste
- `cdp_context_full = true` -> `stop_and_new_chat`
- `stall > 15 min` -> `send_continue`
- `stall > 20 min` -> `open_new_chat`
- `loop_stopped` sin actividad -> revisar y posiblemente `open_new_chat`

---

## 7. Errores de Interpretacion que Debes Evitar

### Error 1: pensar que `situation_summary` ejecuta acciones

No.
Solo describe.
Quien decide es `brain.py`.

### Error 2: pensar que cambiar `memory.py` actualiza automaticamente `memory.db`

No.
Si la base ya existe, las lecciones nuevas no aparecen solas.
Hay que resetear o resembrar.

### Error 3: pensar que "Compacted conversation" es un error

No normalmente.
Es una fase de trabajo interno.

### Error 4: pensar que `6/6 OK` significa sistema completo sano

No.
Solo valida el bloque probado.

### Error 5: cambiar varias capas a la vez sin saber cual fallaba

Eso vuelve el sistema dificil de depurar.

---

## 8. Procedimientos Rapidos

### Procedimiento: agregar una leccion nueva

1. Edita `seed_initial_lessons()`.
2. Agrega texto, categoria y confidence.
3. Respalda la DB si quieres resembrar desde cero.
4. Recreate la base.
5. Verifica conteo y categorias.

---

### Procedimiento: ensenar un error nuevo

1. Agrega regex en `observer.py`.
2. Dale nombre de evento consistente.
3. Haz que aparezca en `situation_summary`.
4. Agrega regla en `brain.py`.
5. Agrega leccion base en `memory.py` si aplica.
6. Prueba parseo y decision.

---

### Procedimiento: resetear memoria sin perder historial anterior

1. Renombra `memory.db` con sufijo `.bak_YYYYMMDD_HHMMSS`.
2. Ejecuta el agente o la rutina que crea la base.
3. Verifica que la base nueva tenga las lecciones esperadas.

---

### Procedimiento: validar un cambio serio

1. Verifica imports y carga del modulo.
2. Verifica parseo de patrones.
3. Verifica conteo de lecciones.
4. Verifica el estado completo del sistema.
5. Corre una observacion real contra snapshot/logs reales.

---

## 9. Regla Practica Final

Si tu pregunta empieza con:

- "por que no detecta..." -> mira `observer.py`
- "por que decidio..." -> mira `brain.py`
- "por que no recuerda..." -> mira `memory.db` y `memory.py`
- "por que despues del cambio no aparecio..." -> probablemente falta resetear `memory.db`
- "por que dijo wait..." -> revisa `situation_summary`, actividad activa, post-accion y thresholds

En una frase:

`observer.py` ve, `brain.py` decide, `memory.py` ensena, `memory.db` recuerda, `agent.py` ejecuta.
