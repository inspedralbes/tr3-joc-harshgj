# foundations.md - ArkanoidX Gameplay Slice

## 1. Contexto

**Proyecto:** ArkanoidX  
**Plataforma:** Unity 2D  
**Feature seleccionada para SDD:** bucle principal de juego dentro de una sola partida, centrado en:
- comportamiento de la pelota
- comportamiento de la pala
- destruccion de bloques y finalizacion del nivel
- modo de pala controlada por IA basado en `ArkanoidAgent`

Esta especificacion cubre solo una parte acotada del proyecto, no toda la aplicacion. El objetivo es definir y controlar la implementacion del bucle de partida que el jugador experimenta momento a momento.

## 2. Objetivo

Implementar y validar un bucle de juego estable en el que:
- la pelota se lance y siga moviendose con velocidad constante
- la pala pueda moverse horizontalmente dentro de los limites de pantalla
- la pelota rebote de forma distinta segun donde golpee la pala
- los bloques se destruyan cuando la pelota los golpea
- el nivel termine solo cuando todos los bloques hayan sido destruidos
- el modo IA controle la pala usando la integracion existente con ML-Agent

## 3. Componentes dentro del alcance

Esta feature se basa en los scripts actuales de Unity:
- `Assets/UI/BallBounce.cs`
- `Assets/UI/PaddleMovement.cs`
- `Assets/UI/Block.cs`
- `Assets/UI/GameManager.cs`
- `Assets/UI/AIPaddleController.cs`
- `Assets/UI/ArkanoidAgent.cs`
- `Assets/UI/DeathZone.cs`

## 4. Alcance funcional

### Bucle de juego humano
- Una ronda comienza con la pelota y la pala reiniciadas en sus posiciones de aparicion.
- El jugador mueve la pala a izquierda y derecha.
- La pelota rebota en las paredes y en la pala.
- Golpear un bloque lo elimina del juego activo.
- Perder la pelota reduce vidas y activa un retardo de respawn.
- Destruir todos los bloques termina el nivel.

### Bucle de juego con IA
- En modo IA, la pala controlada es dirigida por `ArkanoidAgent`.
- El agente observa la posicion de la pala, la posicion de la pelota y la velocidad de la pelota.
- El agente selecciona una de tres acciones: quedarse quieto, moverse a la izquierda o moverse a la derecha.
- Se aplican recompensas por sobrevivir, golpear la pelota, destruir bloques y completar el nivel.
- Se aplica una penalizacion cuando se pierde la pelota.

## 5. Restricciones

- Motor: Unity con C#
- Fisica: fisica 2D de Unity (`Rigidbody2D`, `Collider2D`, `PhysicsMaterial2D`)
- El movimiento de la pelota debe mantenerse lo bastante determinista para pruebas de gameplay.
- El movimiento de la pala debe permanecer dentro de los limites de la camara.
- La implementacion debe reutilizar la arquitectura actual de la escena basada en tags como `Ball`, `Paddle` y `Block`.
- El modo IA debe usar la configuracion existente de `Unity.MLAgents` ya presente en el proyecto.
- La feature no debe requerir redisenar login, red, menus ni sistemas de backend.

## 6. Fuera de alcance

- Autenticacion de usuario
- Gestion de perfiles
- Detalles de sincronizacion multijugador
- Matchmaking
- Marcadores persistentes
- Generacion de nuevos niveles
- Pipeline de entrenamiento del modelo fuera de la integracion del agente dentro del juego

## 7. Criterios de exito

1. La pelota siempre se lanza despues del reset y mantiene una velocidad casi constante durante el movimiento.
2. La pala nunca sale del area de juego horizontal.
3. El angulo de rebote en la pala cambia segun el punto de impacto.
4. Un bloque solo se elimina cuando es golpeado por la pelota.
5. El nivel solo se completa cuando quedan cero bloques activos.
6. En modo IA, la pala puede ser controlada por `ArkanoidAgent` sin entrada manual.
7. Las recompensas y penalizaciones de la IA se activan a partir de eventos reales del gameplay.
