require('dotenv').config();

const bcrypt = require('bcryptjs');
const signature = require('cookie-signature');
const cors = require('cors');
const express = require('express');
const http = require('http');
const session = require('express-session');
const WebSocket = require('ws');
const { Sequelize, DataTypes } = require('sequelize');

const sequelize = new Sequelize(
  process.env.DB_NAME || 'authdb',
  process.env.DB_USER || 'root',
  process.env.DB_PASS || 'password',
  {
    host: process.env.DB_HOST || 'mysql',
    port: parseInt(process.env.DB_PORT || '3306', 10),
    dialect: 'mysql',
    logging: false,
  }
);

const User = sequelize.define(
  'User',
  {
    username: { type: DataTypes.STRING, unique: true, allowNull: false },
    password_hash: { type: DataTypes.STRING, allowNull: false },
    created_at: { type: DataTypes.DATE, defaultValue: DataTypes.NOW },
  },
  { tableName: 'users', timestamps: false }
);

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors({ origin: true, credentials: true }));
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const sessionParser = session({
  secret: process.env.SESSION_SECRET || 'super-secret-key',
  resave: false,
  saveUninitialized: false,
  cookie: { secure: false, httpOnly: true, maxAge: 1000 * 60 * 60 * 2 },
});

const sessionSecret = process.env.SESSION_SECRET || 'super-secret-key';

app.use(sessionParser);

const rooms = new Map();

async function connectWithRetry() {
  let retries = 10;

  while (retries > 0) {
    try {
      await sequelize.authenticate();
      await sequelize.sync();
      console.log('Database connected');

      const admin = await User.findOne({ where: { username: 'admin' } });
      if (!admin) {
        const hash = await bcrypt.hash('password123', 10);
        await User.create({ username: 'admin', password_hash: hash });
        console.log('Default admin created: admin / password123');
      }

      return;
    } catch (error) {
      retries -= 1;
      console.log('Waiting for MySQL to be ready...');
      await new Promise((resolve) => setTimeout(resolve, 3000));
    }
  }

  console.error('Could not connect to DB after retries');
  process.exit(1);
}

connectWithRetry();

function requireAuth(req, res, next) {
  if (req.session && req.session.user)
    return next();

  return res.status(401).json({ error: 'Not authenticated' });
}

function generateRoomCode() {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let roomCode = '';

  do {
    roomCode = '';
    for (let index = 0; index < 6; index += 1)
      roomCode += alphabet[Math.floor(Math.random() * alphabet.length)];
  } while (rooms.has(roomCode));

  return roomCode;
}

function getRoomSummary(room, username) {
  let role = 'spectator';
  if (room.hostUsername === username)
    role = 'host';
  else if (room.guestUsername === username)
    role = 'guest';

  return {
    roomCode: room.code,
    role,
    hostUsername: room.hostUsername,
    guestUsername: room.guestUsername,
    hostConnected: !!room.hostSocket && room.hostSocket.readyState === WebSocket.OPEN,
    guestConnected: !!room.guestSocket && room.guestSocket.readyState === WebSocket.OPEN,
    started: room.started,
  };
}

function sendJson(target, payload) {
  if (target && target.readyState === WebSocket.OPEN)
    target.send(JSON.stringify(payload));
}

function broadcastRoomState(room) {
  const recipients = [
    [room.hostSocket, room.hostUsername],
    [room.guestSocket, room.guestUsername],
  ];

  recipients.forEach(([socket, username]) => {
    if (!socket || !username)
      return;

    sendJson(socket, {
      type: 'room_state',
      ...getRoomSummary(room, username),
    });
  });
}

function maybeStartRoom(room) {
  const hostConnected = room.hostSocket && room.hostSocket.readyState === WebSocket.OPEN;
  const guestConnected = room.guestSocket && room.guestSocket.readyState === WebSocket.OPEN;

  if (room.started || !hostConnected || !guestConnected)
    return;

  room.started = true;
  broadcastRoomState(room);
}

function detachSocket(ws) {
  if (!ws.roomCode)
    return;

  const room = rooms.get(ws.roomCode);
  if (!room)
    return;

  const wasHostSocket = room.hostSocket === ws;
  const wasGuestSocket = room.guestSocket === ws;

  if (room.hostSocket === ws)
    room.hostSocket = null;

  if (room.guestSocket === ws)
    room.guestSocket = null;

  room.started = false;

  if (wasGuestSocket && !room.hostSocket)
    room.guestUsername = null;

  if (wasHostSocket && !room.guestSocket && !room.guestUsername)
    rooms.delete(room.code);

  if (!rooms.has(room.code))
    return;

  broadcastRoomState(room);

  if (!room.hostSocket && !room.guestSocket && !room.guestUsername)
    rooms.delete(room.code);
}

app.post('/api/register', async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password)
    return res.status(400).json({ error: 'Missing fields' });

  if (username.length < 3)
    return res.status(400).json({ error: 'Username too short' });

  if (password.length < 6)
    return res.status(400).json({ error: 'Password too short' });

  try {
    const existingUser = await User.findOne({ where: { username } });
    if (existingUser)
      return res.status(409).json({ error: 'User already exists' });

    const hash = await bcrypt.hash(password, 10);
    await User.create({ username, password_hash: hash });
    return res.json({ message: 'User registered' });
  } catch (error) {
    console.error(error);
    return res.status(500).json({ error: 'Server error' });
  }
});

app.post('/api/login', async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password)
    return res.status(400).json({ error: 'Missing credentials' });

  try {
    const user = await User.findOne({ where: { username } });
    if (!user)
      return res.status(401).json({ error: 'Invalid credentials' });

    const valid = await bcrypt.compare(password, user.password_hash);
    if (!valid)
      return res.status(401).json({ error: 'Invalid credentials' });

    req.session.user = {
      username: user.username,
      loginAt: new Date().toISOString(),
    };

    return req.session.save((saveError) => {
      if (saveError) {
        console.error(saveError);
        return res.status(500).json({ error: 'Session save failed' });
      }

      const signedSessionId = `s:${signature.sign(req.sessionID, sessionSecret)}`;
      const sessionCookie = `connect.sid=${encodeURIComponent(signedSessionId)}`;

      return res.json({
        message: 'Login successful',
        username: user.username,
        sessionCookie,
      });
    });
  } catch (error) {
    console.error(error);
    return res.status(500).json({ error: 'Server error' });
  }
});

app.post('/api/logout', (req, res) => {
  req.session.destroy(() => {
    res.clearCookie('connect.sid');
    res.json({ message: 'Logged out' });
  });
});

app.get('/api/me', requireAuth, (req, res) => {
  res.json(req.session.user);
});

app.get('/health', (_req, res) => {
  res.json({ ok: true });
});

app.post('/api/rooms', requireAuth, (req, res) => {
  const username = req.session.user.username;
  const roomCode = generateRoomCode();

  rooms.set(roomCode, {
    code: roomCode,
    hostUsername: username,
    guestUsername: null,
    hostSocket: null,
    guestSocket: null,
    started: false,
  });

  res.json({ roomCode });
});

app.post('/api/rooms/join', requireAuth, (req, res) => {
  const username = req.session.user.username;
  const roomCode = String(req.body.roomCode || '').trim().toUpperCase();

  if (!roomCode)
    return res.status(400).json({ error: 'Room code is required' });

  const room = rooms.get(roomCode);
  if (!room)
    return res.status(404).json({ error: 'Room not found' });

  if (room.hostUsername === username)
    return res.status(400).json({ error: 'Host cannot join own room as guest' });

  if (room.guestUsername && room.guestUsername !== username)
    return res.status(409).json({ error: 'Room is full' });

  room.guestUsername = username;
  room.started = false;

  broadcastRoomState(room);
  return res.json({ roomCode });
});

app.get('/api/rooms/:roomCode', requireAuth, (req, res) => {
  const roomCode = String(req.params.roomCode || '').trim().toUpperCase();
  const room = rooms.get(roomCode);

  if (!room)
    return res.status(404).json({ error: 'Room not found' });

  return res.json(getRoomSummary(room, req.session.user.username));
});

const server = http.createServer(app);
const wss = new WebSocket.Server({ noServer: true });

wss.on('connection', (ws, req) => {
  const username = req.session.user.username;

  ws.on('message', (rawMessage) => {
    let message;
    try {
      message = JSON.parse(rawMessage.toString());
    } catch (error) {
      sendJson(ws, { type: 'error', error: 'Invalid JSON payload' });
      return;
    }

    if (message.type === 'join_room') {
      const roomCode = String(message.roomCode || '').trim().toUpperCase();
      const room = rooms.get(roomCode);

      if (!room) {
        sendJson(ws, { type: 'error', error: 'Room not found' });
        return;
      }

      detachSocket(ws);

      ws.roomCode = roomCode;

      if (room.hostUsername === username)
        room.hostSocket = ws;
      else if (room.guestUsername === username)
        room.guestSocket = ws;
      else {
        sendJson(ws, { type: 'error', error: 'You are not part of this room' });
        return;
      }

      broadcastRoomState(room);
      maybeStartRoom(room);
      return;
    }

    if (message.type === 'paddle_move' && ws.roomCode) {
      const room = rooms.get(ws.roomCode);
      if (!room)
        return;

      const otherSocket = room.hostSocket === ws ? room.guestSocket : room.hostSocket;
      sendJson(otherSocket, {
        type: 'paddle_move',
        normalizedX: Number(message.normalizedX) || 0,
      });
    }
  });

  ws.on('close', () => {
    detachSocket(ws);
  });
});

server.on('upgrade', (req, socket, head) => {
  sessionParser(req, {}, () => {
    if (!req.session || !req.session.user) {
      socket.destroy();
      return;
    }

    wss.handleUpgrade(req, socket, head, (ws) => {
      wss.emit('connection', ws, req);
    });
  });
});

server.listen(PORT, () => {
  console.log(`Server running on http://localhost:${PORT}`);
});
