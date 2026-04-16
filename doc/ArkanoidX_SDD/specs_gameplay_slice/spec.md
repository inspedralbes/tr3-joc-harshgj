# spec.md - Comportamiento esperado del gameplay principal y del modo de pala con IA

## 1. Comportamiento de la pelota

### 1.1 Inicializacion
- La pelota debe contener un `Rigidbody2D`.
- Al iniciarse, la pelota configura la fisica con:
  - `gravityScale = 0`
  - damping a cero
  - rotacion bloqueada
  - deteccion continua de colisiones
- Se aplica al collider de la pelota un `PhysicsMaterial2D` con rebote y sin friccion.

### 1.2 Lanzamiento
- Al comenzar una ronda, la pelota se reinicia en su punto de aparicion.
- La pelota se lanza automaticamente con una direccion ascendente y una variacion horizontal aleatoria.
- Si la velocidad de la pelota se vuelve casi cero, debe relanzarse automaticamente.

### 1.3 Movimiento continuo
- Durante las actualizaciones de fisica, la magnitud de la velocidad de la pelota debe normalizarse de nuevo a la velocidad configurada.
- La pelota no debe frenarse de forma gradual durante el juego normal.

### 1.4 Rebote en la pala
- Cuando la pelota colisiona con un objeto con tag `Paddle`, la direccion del rebote depende del lugar de impacto.
- El componente horizontal se calcula a partir del desplazamiento entre el centro de la pelota y el centro de la pala.
- Los impactos cerca de los extremos de la pala generan una desviacion horizontal mayor.
- El componente vertical debe mantenerse ascendente despues del contacto con la pala.

## 2. Comportamiento de la pala

### 2.1 Pala controlada por el jugador
- La pala se mueve solo en el eje X.
- Entradas soportadas:
  - izquierda por teclado: `LeftArrow` o `A`
  - derecha por teclado: `RightArrow` o `D`
  - seguimiento por touch y raton en plataformas compatibles
- El movimiento de la pala debe quedar limitado al area horizontal visible de juego.

### 2.2 Controlador auxiliar de pala IA
- `AIPaddleController` puede seguir la pelota moviendose hacia la coordenada X de la pelota.
- Debe mantener una posicion Y fija.
- No debe moverse cuando la pelota esta dentro de una dead zone configurable.
- Debe permanecer dentro de los limites horizontales.

## 3. Comportamiento de los bloques

### 3.1 Colision
- Un bloque reacciona solo a colisiones con la pelota.
- Un bloque debe ignorar impactos repetidos despues de haber sido marcado como destruido.

### 3.2 Destruccion
- En una colision valida:
  - el bloque se marca como destruido
  - el bloque sale del juego activo
  - se notifica al `GameManager`
  - en modo IA tambien se notifica al agente de entrenamiento que un bloque ha sido roto

## 4. Flujo de partida gestionado por GameManager

### 4.1 Inicio de ronda
- `GameManager` resuelve referencias de escena para pelota, pala, UI y death zone.
- Guarda las posiciones de aparicion en cache.
- Lee el modo de juego seleccionado.
- Configura las palas segun el modo.
- Inicializa las vidas.
- Cuenta los bloques activos actuales.
- Inicia la ronda reseteando pala y pelota.

### 4.2 Perdida de pelota
- Si la pelota alcanza la death zone, la perdida debe gestionarse una sola vez.
- En modo normal:
  - se pierde una vida
  - se actualiza la UI
  - si no quedan vidas, se muestra game over
  - en caso contrario, la ronda reaparece tras un retraso
- En modo IA:
  - el agente recibe una penalizacion por perdida de pelota
  - el episodio de entrenamiento puede reiniciarse

### 4.3 Conteo de bloques
- Los bloques restantes deben derivarse del estado real de los bloques activos en la escena.
- El juego no debe completar el nivel mientras siga existiendo cualquier bloque activo.

### 4.4 Finalizacion del nivel
- Cuando los bloques activos restantes llegan a cero:
  - la pelota se congela
  - se entra en estado de finalizacion del nivel
  - el agente de IA recibe la recompensa de nivel completado en modo IA

## 5. Comportamiento del modo IA

### 5.1 Activacion
- En modo IA, la pala principal se configura como agente de entrenamiento.
- Los scripts de control manual deben desactivarse para la pala controlada por entrenamiento.
- El agente debe recibir referencias a:
  - `GameManager`
  - transform de la pelota
  - rigidbody de la pelota

### 5.2 Observaciones
- El agente observa:
  - X normalizada de la pala
  - X normalizada de la pelota
  - Y normalizada de la pelota
  - velocidad X normalizada de la pelota
  - velocidad Y normalizada de la pelota

### 5.3 Acciones
- El espacio de acciones discretas tiene tamano 3:
  - `0`: quedarse quieto
  - `1`: mover a la izquierda
  - `2`: mover a la derecha

### 5.4 Recompensas
- Recompensa por paso de supervivencia
- Recompensa por golpear la pelota con la pala
- Recompensa por destruir un bloque
- Penalizacion por perder la pelota
- Recompensa por completar el nivel

### 5.5 Gestion del episodio
- Al comienzo de un nuevo episodio de IA, el estado del gameplay se reinicia.
- El reinicio puede implementarse mediante recarga de escena o reset explicito de la ronda, siempre que el estado resultante cumpla el spec.

## 6. Criterios de aceptacion

1. El modo humano permite al jugador destruir bloques usando la pala.
2. La pelota rebota hacia arriba despues del contacto con la pala y no se queda pegada a ella.
3. Perder la pelota reduce vidas exactamente una vez por evento de perdida.
4. El juego ya no termina mientras siga habiendo un bloque visible.
5. El modo IA puede controlar la pala sin teclado ni touch.
6. Las callbacks de recompensa de la IA corresponden a eventos reales del gameplay.
