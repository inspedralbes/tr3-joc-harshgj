# plan.md - Estrategia de implementacion para la feature de gameplay

## 1. Objetivo de implementacion

Implementar una feature acotada de ArkanoidX centrada en la interaccion en tiempo de ejecucion entre pelota, pala, bloques y agente de pala con IA.

## 2. Archivos implicados

- `Assets/UI/BallBounce.cs`
- `Assets/UI/PaddleMovement.cs`
- `Assets/UI/Block.cs`
- `Assets/UI/GameManager.cs`
- `Assets/UI/AIPaddleController.cs`
- `Assets/UI/ArkanoidAgent.cs`
- `Assets/UI/DeathZone.cs`

## 3. Fases de implementacion

### Fase 1 - Base de pelota y pala
Tareas:
- Verificar la configuracion fisica de la pelota y su flujo de lanzamiento.
- Verificar el manejo de entrada de la pala y sus limites de movimiento.
- Asegurar que la logica de rebote en la pala usa el offset del impacto para calcular la reflexion.

Aceptacion:
- La pelota se lanza correctamente.
- La pala permanece dentro de los limites de pantalla.
- El rebote en la pala se siente direccional y estable.

### Fase 2 - Ciclo de vida de bloques y progresion de ronda
Tareas:
- Validar el manejo de colisiones de bloques.
- Asegurar que los bloques destruidos salen del juego activo inmediatamente.
- Contar los bloques restantes usando el estado real activo de la escena.
- Activar la finalizacion del nivel solo cuando no quede ningun bloque.

Aceptacion:
- Cada impacto destruye exactamente un bloque.
- No se produce una finalizacion prematura del nivel.

### Fase 3 - Perdida, respawn y flujo de UI
Tareas:
- Detectar la perdida de pelota a traves de la death zone.
- Aplicar la reduccion de vida solo una vez por evento de perdida.
- Reiniciar posiciones de pala y pelota despues del retardo de respawn.
- Congelar la pelota y mostrar el estado final en derrota o al completar el nivel.

Aceptacion:
- Las vidas disminuyen correctamente.
- El respawn funciona sin duplicar eventos.

### Fase 4 - Modo de pala con IA
Tareas:
- Configurar la pala para control mediante ML-Agent en modo IA.
- Desactivar los scripts de movimiento humano cuando la IA controla la pala.
- Confirmar que el vector de observaciones y el espacio de acciones coinciden con el spec.
- Conectar los eventos del gameplay con las callbacks de recompensa.

Aceptacion:
- El agente puede mover la pala.
- Las recompensas se aplican en impactos de pala, destruccion de bloques, nivel completado y perdida de pelota.

## 4. Guia para opsx:propose

La propuesta OpenSpec debe centrarse en una sola feature:
`Bucle principal de gameplay con modo de pala IA`

Debe evitar ampliar el alcance a:
- autenticacion
- menus mas alla de la seleccion de modo
- detalles internos de red multijugador
- backend o base de datos

## 5. Guia para opsx:apply

Prompt sugerido de implementacion:

```text
Implementa la feature de gameplay exactamente como se define en los archivos OpenSpec adjuntos.
El alcance se limita al bucle principal de Arkanoid: movimiento de pelota, movimiento de pala, destruccion de bloques, transiciones de estado de partida y control de pala con IA.
No anadas sistemas no relacionados.
Conserva la estructura actual del proyecto Unity y modifica solo los scripts necesarios segun el spec.
Antes de cambiar comportamiento, compara el codigo actual con el spec y corrige solo las desviaciones.
```

## 6. Reglas de iteracion controlada

Para cada iteracion del agente:
1. Identificar una desviacion entre implementacion y spec.
2. Lanzar un prompt que cite la seccion relevante de `spec.md`.
3. Corregir solo el archivo o comportamiento afectado.
4. Registrar en `docs/prompts-log.md`:
   - prompt usado
   - problema observado
   - correccion aplicada
   - resultado tras la correccion

## 7. Riesgos

| Riesgo | Impacto | Mitigacion |
|---|---|---|
| La fisica de la pelota se vuelve inconsistente tras el reset | Medio | Normalizar velocidad en cada tick de fisica y resetear explicitamente el rigidbody |
| La pala sale de limites en distintas pantallas | Medio | Basar los limites en ancho de camara y ancho del collider |
| El conteo de bloques se desajusta respecto al estado real | Alto | Recalcular bloques restantes desde los bloques activos reales |
| El control IA y el control manual interfieren en la misma pala | Alto | Desactivar `PaddleMovement` y otros scripts de control en modo IA |
| Las recompensas del agente no corresponden a eventos reales | Medio | Conectar recompensas solo desde notificaciones reales de gameplay en `GameManager` y colisiones de la pelota |
