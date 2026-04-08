// server.js
require('dotenv').config();

const express = require('express');
const session = require('express-session');
const bcrypt = require('bcryptjs');
const cors = require('cors');
const http = require('http');
const WebSocket = require('ws');

const { Sequelize, DataTypes } = require('sequelize');

// ── Database setup ─────────────────────────
// Use environment variables
const sequelize = new Sequelize(
  process.env.DB_NAME || 'authdb',
  process.env.DB_USER || 'root',
  process.env.DB_PASS || 'password',
  {
    host: process.env.DB_HOST || 'mysql', // use 'mysql' for Docker
    port: parseInt(process.env.DB_PORT) || 3306,
    dialect: 'mysql',
    logging: false,
  }
);

// User model
const User = sequelize.define(
  'User',
  {
    username: { type: DataTypes.STRING, unique: true, allowNull: false },
    password_hash: { type: DataTypes.STRING, allowNull: false },
    created_at: { type: DataTypes.DATE, defaultValue: DataTypes.NOW },
  },
  { tableName: 'users', timestamps: false }
);

// ── Express setup ─────────────────────────
const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors({ origin: '*', credentials: true }));
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const sessionParser = session({
  secret: process.env.SESSION_SECRET || 'super-secret-key',
  resave: false,
  saveUninitialized: false,
  cookie: { secure: false, httpOnly: true, maxAge: 1000 * 60 * 60 * 2 },
});

app.use(sessionParser);

// ── Database init with retry ──────────────
async function connectWithRetry() {
  let retries = 10;
  while (retries) {
    try {
      await sequelize.authenticate();
      console.log('✅ Database connected');
      await sequelize.sync();
      // Create default admin if missing
      const admin = await User.findOne({ where: { username: 'admin' } });
      if (!admin) {
        const hash = await bcrypt.hash('password123', 10);
        await User.create({ username: 'admin', password_hash: hash });
        console.log('✅ Default admin: admin / password123');
      }
      return;
    } catch (err) {
      console.log('⏳ Waiting for MySQL to be ready...');
      retries--;
      await new Promise((res) => setTimeout(res, 3000));
    }
  }
  console.error('❌ Could not connect to DB after retries');
  process.exit(1);
}

connectWithRetry();

// ── Auth middleware ───────────────────────
function requireAuth(req, res, next) {
  if (req.session?.user) return next();
  return res.status(401).json({ error: 'Not authenticated' });
}

// ── Routes ────────────────────────────────

// Register
app.post('/api/register', async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password)
    return res.status(400).json({ error: 'Missing fields' });
  if (username.length < 3)
    return res.status(400).json({ error: 'Username too short' });
  if (password.length < 6)
    return res.status(400).json({ error: 'Password too short' });

  try {
    const exists = await User.findOne({ where: { username } });
    if (exists) return res.status(409).json({ error: 'User already exists' });

    const hash = await bcrypt.hash(password, 10);
    await User.create({ username, password_hash: hash });

    res.json({ message: 'User registered' });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Server error' });
  }
});

// Login
app.post('/api/login', async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password)
    return res.status(400).json({ error: 'Missing credentials' });

  try {
    const user = await User.findOne({ where: { username } });
    if (!user) return res.status(401).json({ error: 'Invalid credentials' });

    const valid = await bcrypt.compare(password, user.password_hash);
    if (!valid) return res.status(401).json({ error: 'Invalid credentials' });

    req.session.user = { username: user.username, loginAt: new Date().toISOString() };
    res.json({ message: 'Login successful', username: user.username });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Server error' });
  }
});

// Logout
app.post('/api/logout', (req, res) => {
  req.session.destroy(() => {
    res.clearCookie('connect.sid');
    res.json({ message: 'Logged out' });
  });
});

// Current user
app.get('/api/me', requireAuth, (req, res) => {
  res.json(req.session.user);
});

// ── WebSocket ────────────────────────────
const server = http.createServer(app);
const wss = new WebSocket.Server({ noServer: true });
const rooms = {};

wss.on('connection', (ws, req) => {
  const user = req.session.user;

  ws.on('message', (msg) => {
    const data = JSON.parse(msg);

    if (data.type === 'join') {
      const room = data.room || 'default';
      ws.room = room;
      if (!rooms[room]) rooms[room] = [];
      rooms[room].push(ws);
      console.log(`👤 ${user.username} joined ${room}`);
    }

    if (data.type === 'move' && ws.room) {
      rooms[ws.room].forEach((client) => {
        if (client !== ws && client.readyState === WebSocket.OPEN) {
          client.send(JSON.stringify({ type: 'move', payload: data.payload }));
        }
      });
    }
  });

  ws.on('close', () => {
    if (ws.room && rooms[ws.room]) {
      rooms[ws.room] = rooms[ws.room].filter((c) => c !== ws);
    }
  });
});

// Session auth for WebSocket
server.on('upgrade', (req, socket, head) => {
  sessionParser(req, {}, () => {
    if (!req.session.user) return socket.destroy();
    wss.handleUpgrade(req, socket, head, (ws) => wss.emit('connection', ws, req));
  });
});

// ── Start Server ─────────────────────────
server.listen(PORT, () => {
  console.log(`🚀 Server running on http://localhost:${PORT}`);
});