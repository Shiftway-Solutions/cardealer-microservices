# Guia Completa del Agente con Gemma 3

## 1. Que construimos y por que

Esta guia explica, de forma detallada y paso a paso, como se construyo el nuevo agente inteligente que usa **Gemma 3 local** para decidir que hacer cuando GitHub Copilot en VS Code se queda estancado, falla, entra en rate limit, termina su turno o necesita cambiar de chat.

La idea central no fue simplemente "poner un modelo de IA" encima del monitor anterior.
La idea real fue construir un sistema confiable con esta separacion:

1. **La observacion del entorno** sigue siendo determinista.
2. **La decision** puede ser inteligente y contextual.
3. **La ejecucion** sigue siendo determinista y reutiliza lo que ya funcionaba.
4. **El aprendizaje** queda persistido para que el sistema mejore con el tiempo.

Eso es importante porque, si usas un modelo para todo, el sistema se vuelve inestable.
Si usas el modelo solo para decidir y mantienes las acciones criticas bajo control determinista, obtienes algo mucho mas robusto.

---

## 2. El problema original

Tu monitor original ya hacia muchas cosas bien:

- observaba el chat de Copilot
- detectaba errores visibles
- detectaba rate limits
- abria nuevo chat
- enviaba `continuar`
- media inactividad
- cambiaba de modelo
- usaba CDP, logs y AppleScript

Pero tenia una limitacion estructural:

- estaba basado principalmente en reglas fijas
- era dificil incorporar criterio contextual fino
- cada nueva situacion implicaba agregar mas `if/else`
- no tenia una memoria real de lo que funciono y lo que no
- no tenia un ciclo formal de aprendizaje

En otras palabras: el sistema era fuerte para ejecutar, pero no tenia una "capa de cerebro" separada.

Lo que hicimos fue evolucionarlo desde:

- **watchdog basado en reglas**

a:

- **agente autonomo con observacion + razonamiento + accion + aprendizaje**

---

## 3. La idea de arquitectura

La arquitectura final se construyo en capas.

### 3.1 Capa 1: Observer

Su trabajo es leer la realidad del sistema y convertirla en una observacion estructurada.

No decide nada.
No actua.
Solo observa.

### 3.2 Capa 2: Brain

Su trabajo es recibir la observacion y decidir la mejor accion.

Aqui entra Gemma 3.

Pero antes de invocar Gemma, se aplican **overrides deterministas** para situaciones obvias o criticas.

### 3.3 Capa 3: Action Executor

Su trabajo es traducir la decision en acciones reales sobre VS Code.

Aqui no reinventamos nada.
Reutilizamos los ejecutores ya probados del monitor v7:

- `vscode_send_continue()`
- `vscode_open_new_chat()`
- `open_new_chat_with_stop()`
- `cycle_model()`
- `vscode_focus()`

### 3.4 Capa 4: Feedback Loop

Despues de ejecutar una accion, el sistema no debe volver a actuar de inmediato.

Primero debe observar si la situacion se normalizo.

Esta capa trackea el resultado de la accion y clasifica el outcome como:

- `resolved`
- `no_effect`
- `escalated`
- `pending`

### 3.5 Capa 5: Memory

Toda experiencia importante queda guardada en SQLite.

Eso permite:

- recordar decisiones recientes
- saber que acciones funcionaron
- extraer lecciones
- reforzar o debilitar reglas aprendidas

---

## 4. Estructura final del paquete

Se creo este paquete nuevo dentro del repo:

```text
.prompts/agent/smart_monitor/
├── __init__.py
├── __main__.py
├── agent.py
├── observer.py
├── brain.py
├── feedback.py
├── memory.py
├── memory.db
└── GUIA-AGENTE-GEMMA3.md
```

### Que hace cada archivo

#### `observer.py`

Recoge senales del entorno y las encapsula en un objeto `Observation`.

#### `brain.py`

Construye el prompt, llama a Ollama con `gemma3:4b`, parsea la respuesta JSON y decide.
Tambien tiene fallback determinista.

#### `memory.py`

Implementa SQLite para guardar episodios, lecciones y patrones.

#### `feedback.py`

Verifica si una accion funciono y lanza reflexion periodica con Gemma para extraer aprendizaje.

#### `agent.py`

Es el punto de entrada principal.
Orquesta todo el ciclo:

- observar
- pensar
- actuar
- aprender

#### `__main__.py`

Permite ejecutar el paquete con:

```bash
PYTHONPATH=.prompts/agent python3 -m smart_monitor
```

#### `memory.db`

Base de datos SQLite donde el agente guarda su experiencia.

---

## 5. Por que Gemma 3 local y no otro modelo

Se eligio **Gemma 3 local** por varias razones practicas:

1. **Corre en tu Mac** sin depender de internet para cada decision.
2. **Reduce latencia operativa** en comparacion con depender de una API remota para cada ciclo.
3. **Da privacidad** porque el estado del agente no sale de tu maquina.
4. **Permite aprendizaje local** sin tener que depender de infraestructura externa.
5. **Es suficiente para toma de decisiones estructuradas** si el problema se modela bien.

El objetivo aqui no es usar un LLM gigante para escribir codigo.
El objetivo es usar un modelo local para clasificar situaciones y decidir entre un conjunto pequeno de acciones de alto impacto.

Eso encaja muy bien con Gemma 3.

---

## 6. Por que no reemplazamos el monitor v7

Esto es una decision de ingenieria importante.

No borramos ni reemplazamos completamente tu monitor anterior.
En lugar de eso, lo reutilizamos como base de ejecucion.

### Razon tecnica

Tu monitor v7 ya resolvia cosas complejas como:

- AppleScript hacia VS Code
- CDP
- foco del chat
- apertura de nuevo chat
- stop de respuesta
- cambio de modelo
- envio de prompt
- manejo de cooldowns operativos

Reescribir todo eso desde cero habria aumentado mucho el riesgo de romper algo.

### Entonces, que hicimos

El smart monitor usa el v7 como **motor de accion**.
Gemma decide.
El v7 ejecuta.

Eso da una combinacion muy fuerte:

- inteligencia nueva
- ejecucion probada
- menor riesgo

---

## 7. El ciclo mental completo del agente

Cada ciclo del agente sigue esta secuencia:

```text
1. Observer mira el entorno
2. Brain decide que hacer
3. Executor hace la accion
4. Feedback Loop verifica si funciono
5. Memory guarda la experiencia
6. El siguiente ciclo usa esa experiencia como contexto
```

Esto convierte al sistema en algo mas parecido a un agente real y no solo a un script.

---

## 8. Detalle de `observer.py`

`observer.py` fue disenado para transformar senales sueltas en una observacion util para el cerebro.

### 8.1 Senales que observa

#### a. `chat_snapshot.txt`

Extrae:

- si existe o no
- `mtime`
- edad del snapshot
- hash del cuerpo
- si el body cambio
- tamano en caracteres
- errores visibles en el tail
- si hay porcentaje visible de generacion activa
- si el footer del modelo aparece

#### b. Log de Copilot Chat

Extrae:

- archivo activo del log
- cuantas lineas nuevas aparecieron
- eventos detectados
- evento dominante por prioridad

#### c. CDP

Extrae:

- si CDP esta disponible
- longitud estimada del chat
- si el contexto esta lleno

#### d. VS Code

Extrae:

- si VS Code esta corriendo
- si tiene foco

#### e. Estado persistente

Extrae:

- segundos desde la ultima actividad
- segundos desde la ultima accion
- segundos desde el ultimo `continue`
- segundos desde el ultimo `new_chat`
- contadores de retries
- estado de verificacion post-accion

### 8.2 Objeto `Observation`

La clase `Observation` es la pieza central de esta capa.

Su ventaja es que Gemma no razona sobre logs crudos ni sobre muchos archivos sueltos.
Razonara sobre un resumen estructurado y consistente.

### 8.3 `to_prompt_context()`

Este metodo convierte la observacion en texto para el modelo.

La logica aqui es importante:

- no se le manda ruido innecesario al LLM
- se resume el estado real del sistema
- se normaliza la informacion
- se evita que el modelo tenga que inferir cosas basicas desde datos poco claros

### 8.4 `situation_summary`

Tambien se genera un resumen narrativo para:

- usarlo en memoria
- buscar lecciones relevantes
- registrar episodios de manera compacta

Esto ayuda a que el sistema aprenda mejor.

---

## 9. Detalle de `brain.py`

`brain.py` es donde Gemma 3 se convierte en un "cerebro operativo".

### 9.1 Endpoints usados

Se usa Ollama en local con:

- `http://localhost:11434/api/chat`
- modelo: `gemma3:4b`

### 9.2 Prompt de sistema

El prompt de sistema define reglas muy claras.

Ejemplos de reglas:

- si el agente esta trabajando normalmente, no intervenir
- si el footer del modelo aparece y no hay generacion activa, abrir nuevo chat
- si hay rate limit, primero ciclar modelo
- si hay hard error, esperar y luego intentar continuar
- si el chat lleva mucho tiempo sin actividad, intentar `continuar`
- si el contexto esta lleno, abrir nuevo chat inmediatamente
- si VS Code no esta corriendo, no hacer nada
- durante la verificacion post-accion, no actuar de nuevo sin necesidad

### 9.3 Acciones validas

Gemma solo puede devolver una de estas acciones:

- `wait`
- `send_continue`
- `open_new_chat`
- `stop_and_new_chat`
- `cycle_model`
- `focus_vscode`

Esto es clave.

No dejamos al modelo inventar comandos libres.
El espacio de accion esta acotado.
Eso vuelve al sistema mucho mas seguro.

### 9.4 Formato JSON estricto

La respuesta esperada es JSON con este contrato:

```json
{
  "decision": "send_continue",
  "confidence": 0.84,
  "reasoning": "Hay hard error pero aun vale la pena reintentar en el mismo chat.",
  "wait_before_action_secs": 60
}
```

### 9.5 Overrides

Antes de llamar al modelo, `brain.py` ejecuta reglas instantaneas.

Eso evita gastar inferencia en situaciones donde la respuesta es obvia.

Ejemplos:

- contexto lleno -> `stop_and_new_chat`
- VS Code no corre -> `wait`
- generacion activa sin errores -> `wait`
- actividad normal reciente -> `wait`
- VS Code sin foco -> `focus_vscode`

### 9.6 Fallback determinista

Si Gemma falla, el sistema no puede quedarse ciego.

Por eso se implemento `_fallback_decision()`.

Ese metodo reintroduce una logica tipo watchdog clasico para casos como:

- rate limit
- footer de agente terminado
- hard error
- stall largo

Con esto se garantiza algo muy importante:

> el sistema puede seguir funcionando aunque Gemma 3 este caido, lento o no responda bien.

---

## 10. Detalle de `memory.py`

Aqui se implemento la memoria persistente del agente.

Se uso **SQLite** porque:

- viene con Python
- no requiere servidor
- es rapido para este caso
- es facil de inspeccionar
- persiste entre reinicios

### 10.1 Tabla `episodes`

Cada episodio representa un ciclo importante del agente.

Guarda:

- timestamp
- resumen de la situacion
- errores vistos
- tiempo desde ultima actividad
- evento dominante del log
- si el contexto estaba lleno
- si habia post-action activo
- accion decidida
- confianza
- razonamiento
- fuente de decision
- outcome posterior

Esto permite responder preguntas como:

- cuantas veces `send_continue` funciono realmente
- cuantas veces `open_new_chat` fue necesario
- en que situaciones Gemma toma malas decisiones

### 10.2 Tabla `lessons`

Guarda reglas aprendidas como texto.

Ejemplo conceptual:

- Cuando hay rate limit, ciclar modelo y luego continuar suele funcionar.
- Nunca enviar continuar si hay generacion activa.

Cada leccion tiene:

- categoria
- confianza
- success_count
- failure_count

### 10.3 Tabla `patterns`

Se dejo espacio para registrar patrones mas estadisticos.

Ejemplo:

- rate limits mas frecuentes en cierto horario
- ciertas secuencias de eventos que suelen terminar en escalacion

### 10.4 Lecciones semilla

Se agrego `seed_initial_lessons(memory)`.

Eso hace que el sistema no empiece desde cero.

Se sembraron 12 lecciones iniciales derivadas de tu experiencia con el monitor v7.

Esto es importante porque el aprendizaje real funciona mejor si no arranca desde vacio absoluto.

---

## 11. Detalle de `feedback.py`

`feedback.py` es la capa que evita que el agente se vuelva impulsivo.

Su trabajo es responder esta pregunta:

> Despues de haber actuado, la situacion realmente mejoro o no?

### 11.1 Tracking de acciones

Cuando el agente ejecuta una accion real, se llama:

- `start_tracking(episode_id, action)`

Eso deja el episodio en seguimiento.

### 11.2 `check_outcome()`

En cada ciclo posterior, si hay una accion en seguimiento, se verifica si ya aparecieron senales de normalizacion.

### 11.3 Senales de normalizacion

Se consideran senales como:

- el chat volvio a cambiar
- reaparecio generacion activa
- desaparecio la condicion que origino la accion
- el snapshot es nuevo y joven tras abrir nuevo chat

### 11.4 Clasificacion del resultado

El resultado de una accion puede terminar como:

- `resolved`
- `no_effect`
- `escalated`

### 11.5 Reflexion periodica

Cada cierto numero de episodios con outcome, el sistema ejecuta `_reflect()`.

Eso hace que Gemma revise los episodios recientes y genere:

- nuevas lecciones
- refuerzos o penalizaciones a lecciones existentes
- observaciones sobre el rendimiento del agente

Esta es la parte mas cercana a "aprender de la experiencia".

---

## 12. Detalle de `agent.py`

Este archivo es el orquestador.

### 12.1 Que resuelve

Sin `agent.py`, tendrias modulos aislados.
Con `agent.py`, tienes un sistema completo ejecutable.

### 12.2 `ActionExecutor`

Esta clase traduce decisiones abstractas en acciones reales.

Ejemplos:

- `send_continue` -> `v7.vscode_send_continue()`
- `open_new_chat` -> `v7.vscode_open_new_chat()` y luego `send_loop_prompt()`
- `stop_and_new_chat` -> `v7.open_new_chat_with_stop()`
- `cycle_model` -> `v7.cycle_model(...)` y luego `v7.vscode_send_continue()`
- `focus_vscode` -> `v7.vscode_focus()`

### 12.3 Cooldowns

Se agregaron cooldowns por accion para evitar spam:

- `send_continue`: 60s
- `open_new_chat`: 90s
- `stop_and_new_chat`: 90s
- `cycle_model`: 120s
- `focus_vscode`: 30s

Esto es importante porque un agente inteligente sin limitadores temporales puede convertirse en un agente destructivo.

### 12.4 Verificacion post-accion

Despues de enviar una accion, el sistema entra en modo de verificacion por 2 minutos.

Logica:

1. si se normaliza, se limpia el estado
2. si no se normaliza en 2 minutos, se intenta una vez mas
3. si el reintento tampoco funciona, se escala

Esto alinea el `agent.py` con la logica que tambien reforzaste en el monitor v7.

### 12.5 `run_cycle()`

Este metodo implementa el ciclo real:

1. observar
2. actualizar offsets y actividad
3. verificar post-accion
4. decidir
5. guardar episodio
6. ejecutar accion
7. arrancar tracking del feedback

### 12.6 `cmd_status()`

Da un dashboard operativo rapido con:

- ciclos totales
- decisiones de Gemma
- decisiones fallback
- overrides
- acciones ejecutadas
- estadisticas de memoria
- lecciones activas
- modelos Ollama disponibles

### 12.7 `main()`

Hace:

- parse de argumentos CLI
- inicializacion de memoria
- carga de estado
- siembra de lecciones iniciales
- reset limpio de timers al arranque
- sincronizacion de snapshot
- loop principal
- singleton con PID file
- cierre elegante

---

## 13. Que archivos persistentes usa el sistema

El sistema usa varios archivos persistentes con responsabilidades distintas.

### `.agent_state.json`

Guarda estado operativo del smart monitor.

Ejemplos:

- ultimo action timestamp
- contadores
- post-action active
- offsets del log
- hashes del snapshot

### `.agent_pid`

Evita arrancar varias instancias del mismo agente.

### `memory.db`

Guarda la memoria real del sistema.

### `smart-monitor.log`

Guarda logs operativos del nuevo agente.

### `chat_snapshot.txt`

Sigue siendo una de las fuentes primarias de senales del sistema.

---

## 14. Diferencia entre decision de Gemma, override y fallback

Esto es uno de los conceptos mas importantes para entender el sistema.

### `override`

Decision inmediata y determinista.
No se llama a Gemma.

Se usa cuando la respuesta es obvia o critica.

### `gemma3`

Decision tomada por el modelo local.

Se usa cuando la situacion necesita mas contexto o criterio.

### `fallback`

Decision determinista cuando Gemma no pudo responder bien.

Esto garantiza continuidad operativa.

En otras palabras:

- override = seguridad
- gemma3 = inteligencia
- fallback = resiliencia

---

## 15. Como aprende realmente el sistema

Cuando dices que quieres que el agente "aprenda", hay que aterrizar eso de forma ingenieril.

Aqui el aprendizaje no significa que Gemma se reentrena.
No estamos fine-tuneando el modelo.

Lo que si estamos haciendo es esto:

### 15.1 Aprendizaje por memoria externa

El sistema guarda experiencia estructurada en SQLite.

### 15.2 Recuperacion de lecciones

Antes de decidir, el Brain consulta la memoria y extrae lecciones relevantes.

Eso cambia el contexto del modelo y mejora decisiones futuras.

### 15.3 Reflexion periodica

Gemma analiza episodios pasados y produce nuevas reglas.

### 15.4 Refuerzo de confianza

Las lecciones pueden fortalecerse o debilitarse dependiendo del resultado.

Esto es una forma de aprendizaje practico basada en experiencia operacional.

No modifica los pesos del modelo, pero si modifica el comportamiento del sistema.

Y para un agente operativo, eso suele ser mas util que intentar entrenar el modelo desde cero.

---

## 16. Comandos para usarlo

### Ejecutar un ciclo

```bash
PYTHONPATH=.prompts/agent python3 -m smart_monitor --once --debug
```

### Ver estado

```bash
PYTHONPATH=.prompts/agent python3 -m smart_monitor --status
```

### Loop continuo

```bash
PYTHONPATH=.prompts/agent python3 -m smart_monitor --interval 20
```

### Dry run

```bash
PYTHONPATH=.prompts/agent python3 -m smart_monitor --dry-run
```

---

## 17. Que pruebas se hicieron

Se verifico lo siguiente:

### 17.1 Compilacion Python

Se ejecuto `py_compile` sobre `agent.py` y paso sin errores.

### 17.2 Integracion de modulos

Se verifico que estos modulos importaran correctamente:

- observer
- brain
- memory
- feedback
- agent

### 17.3 Ejecucion real de un ciclo

Se ejecuto:

```bash
PYTHONPATH=.prompts/agent python3 -m smart_monitor.agent --once --debug
```

El sistema arranco correctamente y produjo una decision valida.

### 17.4 Dashboard de estado

Se ejecuto:

```bash
PYTHONPATH=.prompts/agent python3 -m smart_monitor --status
```

Y el sistema mostro:

- ciclos
- decisiones
- episodios
- lecciones
- modelos detectados en Ollama

### 17.5 Deteccion de Gemma

El agente detecto correctamente `gemma3:4b` en Ollama.

---

## 18. Que quedo especialmente bien en esta arquitectura

Hay varias decisiones de diseno que valen mucho la pena.

### 18.1 Separacion de responsabilidades

Cada modulo tiene un rol claro.

### 18.2 Reutilizacion del motor operativo del v7

No se tiro por la borda lo que ya servia.

### 18.3 Gemma no ejecuta acciones directas

Gemma decide, pero no manda directamente comandos arbitrarios.

### 18.4 El sistema siempre tiene red de seguridad

Si Gemma falla, existe fallback.

### 18.5 Aprendizaje persistente

La experiencia no se pierde al reiniciar.

---

## 19. Limitaciones actuales

Tambien es importante que entiendas que este sistema no es magia y todavia tiene limites.

### 19.1 No reentrena Gemma

Aprende por memoria externa, no por fine-tuning.

### 19.2 CDP en el smart monitor es mas liviano que en el v7

La observacion del contexto usa datos del estado y no reimplementa toda la lectura DOM profunda.

### 19.3 La reflexion necesita episodios

Hasta que no haya suficientes outcomes reales, las lecciones nuevas seran pocas.

### 19.4 La calidad depende del prompt y de la calidad de observacion

Si la observacion esta mal resumida, Gemma decide peor.

### 19.5 Algunas acciones siguen dependiendo del entorno de VS Code y macOS

AppleScript, foco y CDP pueden variar si cambian los selectores o la UI.

---

## 20. Como lo mejoraria aun mas en una siguiente iteracion

Si quieres llevar esto mas lejos, las mejoras mas valiosas serian estas:

1. agregar una capa de scoring estadistico por accion y contexto
2. almacenar secuencias de eventos, no solo episodios aislados
3. usar embeddings locales para recuperar lecciones mas relevantes
4. agregar simulaciones o replay de episodios para probar decisiones
5. conectar el agente a tests automaticos del propio monitor
6. medir precision real por tipo de problema
7. separar mejor decisiones de corto plazo vs estrategias de recuperacion largas
8. agregar un modo supervisor que compare Gemma contra el fallback y detecte desalineaciones

---

## 21. La leccion conceptual mas importante

Si hay una sola idea que quiero que te lleves de todo esto es esta:

> Un buen agente autonomo no es solo un LLM.
> Es un sistema compuesto por observacion confiable, decisiones acotadas, ejecucion segura, feedback y memoria.

Eso fue exactamente lo que se construyo aqui.

No hicimos un chatbot suelto.
Hicimos un **agente operacional**.

---

## 22. Resumen final en una frase

El nuevo smart monitor con Gemma 3 convierte tu monitor anterior en un sistema autonomo capaz de:

- entender el estado del chat
- elegir la mejor accion disponible
- ejecutarla de forma segura
- verificar si funciono
- guardar la experiencia
- aprender reglas nuevas con el tiempo

---

## 23. Archivos principales para estudiar primero

Si quieres aprender esto de forma ordenada, te recomiendo leer en este orden:

1. `agent.py`
2. `observer.py`
3. `brain.py`
4. `feedback.py`
5. `memory.py`
6. `vscode_copilot_monitor.py`

Ese orden te ayuda porque primero entiendes el flujo general, luego la observacion, luego la toma de decision, luego el aprendizaje, y al final el motor operativo heredado.

---

## 24. Conclusiones practicas

Lo que ya tienes ahora no es un prototipo vacio.
Ya tienes una base real sobre la que puedes iterar.

La parte mas importante ya esta conseguida:

- hay cerebro local
- hay memoria persistente
- hay aprendizaje acumulativo
- hay fallback
- hay compatibilidad con el monitor existente
- hay comandos operativos
- hay pruebas base de funcionamiento

Eso te deja en una posicion muy buena para seguir endureciendo el sistema con datos reales de uso.
