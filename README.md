# Arkanoid Multiplayer - Projecte DAM

## Grup
**Grup1** (Projecte Individual)

## Integrants
- Nom: [El teu nom]
- Rol: Desenvolupador complet

## Objectiu
Joc 2D tipus Arkanoid multijugador desenvolupat amb Unity connectat a un servidor Node.js. Els jugadors poden crear partides, unió-se-hi i jugar en temps real mitjançant WebSockets.

## Estat
**Projecte completat** - Totes les funcionalitats implementades.

## Característiques
- Joc 2D Arkanoid amb pala i pilota
- Registre i login d'usuaris
- Crear i unió-se a partides multijugador
- Comunicació en temps real via WebSockets
- Agent ML per a mode training
- Fons animat amb estels i nebulosa

## Adreça Documentació
`https://dam.inspedralbes.cat`

## Adreça Projecte Desplegat
`http://204.168.192.187:3000`

## Estructura del Projecte

```
tr3-joc-harshgj/
├── client/                  # Aplicació Unity
│   └── Arkanoid1/
│       ├── Assets/
│       │   ├── Scenes/      # Escenes del joc
│       │   ├── Scripts/     # Scripts Unity
│       │   └── UI/          # Interfície i controladors
│       └── Arkanoid1.sln
├── server/                   # Backend Node.js
│   ├── src/
│   │   ├── controllers/    # Controladors API
│   │   ├── repositories/  # Patró Repository
│   │   │   ├── interfaces/
│   │   │   └── implementations/
│   │   └── services/      # Serveis de negoci
│   ├── tests/             # Tests unitaris
│   ├── server.js          # Servidor principal
│   └── package.json
├── database/               # Scripts i esquema DB
└── README.md
```

## Enllaç Prototipatge
[Enllaç a Figma/Penpot]

## Documentació Tècnica

### Arquitectura Client-Servidor
- **Frontend**: Unity 2D amb C#
- **Backend**: Node.js + Express
- **Base de dades**: MySQL (Docker)
- **Comunicació**: HTTP (UnityWebRequest) + WebSockets

### Repository Pattern
El projecte implementa el patró Repository amb:
- **Interfícies**: IUserRepository, IGameRepository, IResultRepository
- **Implementació MySQL**: Per a producció
- **Implementació InMemory**: Per a testing
- **Serveis**: UserService, GameService, ResultService
- **Controladors**: AuthController, RoomsController, ResultsController

### API Endpoints
- `POST /api/register` - Registre usuari
- `POST /api/login` - Login usuari
- `POST /api/rooms` - Crear sala
- `POST /api/rooms/join` - Unir-se a sala
- `GET /api/rooms/:roomCode` - Consultar estat sala

### WebSockets
- `join_room` - Unir-se a sala
- `paddle_move` - Sincronitzar moviment pala
- `room_state` - Actualitzar estat de la partida

## Com Executar

### Backend (Docker)
```bash
cd server
docker-compose up -d
```

### Frontend (Unity)
1. Obrir projecte amb Unity
2. Build & Run

## Technologies
- Unity 2022+
- Node.js 18+
- MySQL 8.0
- Express.js
- WebSockets (ws)
- Sequelize ORM
- ML-Agents

## Llicència
MIT
