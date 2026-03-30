// server.js
const express = require('express');
const session = require('express-session');
const bcrypt = require('bcryptjs');
const path = require('path');
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
    host: process.env.DB_HOST || 'mysql', // <-- use service name in Docker
    port: process.env.DB_PORT || 3306,
    dialect: 'mysql',
    logging: false
  }
);

// User model
const User = sequelize.define('User', {
  username: { type: DataTypes.STRING, unique: true, allowNull: false },
  password_hash: { type: DataTypes.STRING, allowNull: false }
});

// ── Express setup ─────────────────────────
const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static(path.join(__dirname, 'client')));

const sessionParser = session({
  secret: process.env.SESSION_SECRET || 'super-secret-key',
  resave: false,
  saveUninitialized: false,
  cookie: { secure: false, httpOnly: true, maxAge: 1000 * 60 * 60 * 2 }
});
app.use(sessionParser);

// ── Initialize DB ─────────────────────────
(async () => {
  try {
    await sequelize.authenticate();
    console.log('✅ DB connected');
    await sequelize.sync();

    // Create demo admin
    const admin = await User.findOne({ where: { username: 'admin' } });
    if (!admin) {
      const passwordHash = await bcrypt.hash('password123', 10);
      await User.create({ username: 'admin', password_hash: passwordHash });
      console.log('✅ Demo admin created: admin / password123');
    }
  } catch (err) {
    console.error('❌ DB connection failed:', err);
  }
})();

// ── Auth middleware ───────────────────────
function requireAuth(req, res, next) {
  if (req.session?.user) return next();
  res.status(401).json({ error: 'Not authenticated' });
}

// ── Routes ────────────────────────────────
app.get('/', (req, res) => {
  if (req.session.user) return res.redirect('/dashboard');
  res.sendFile(path.join(__dirname, 'client', 'login.html'));
});

app.get('/dashboard', requireAuth, (req, res) => {
  res.sendFile(path.join(__dirname, 'client', 'dashboard.html'));
});

// Register
app.post('/api/register', async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password) return res.status(400).json({ error: 'Username and password required' });

  if (username.length < 3) return res.status(400).json({ error: 'Username must be at least 3 chars' });
  if (password.length < 6) return res.status(400).json({ error: 'Password must be at least 6 chars' });

  try {
    if (await User.findOne({ where: { username } })) return res.status(409).json({ error: 'Username taken' });
    const passwordHash = await bcrypt.hash(password, 10);
    await User.create({ username, password_hash: passwordHash });
    res.json({ message: 'Account created successfully!' });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Server error' });
  }
});

// Login
app.post('/api/login', async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password) return res.status(400).json({ error: 'Username and password required' });

  try {
    const user = await User.findOne({ where: { username } });
    if (!user) return res.status(401).json({ error: 'Invalid username or password' });

    const valid = await bcrypt.compare(password, user.password_hash);
    if (!valid) return res.status(401).json({ error: 'Invalid username or password' });

    req.session.user = { username: user.username, loginAt: new Date().toISOString() };
    res.json({ message: 'Login successful', username: user.username });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Server error' });
  }
});

// Logout
app.post('/api/logout', (req, res) => {
  const username = req.session.user?.username;
  req.session.destroy(err => {
    if (err) return res.status(500).json({ error: 'Logout failed' });
    res.clearCookie('connect.sid');
    res.json({ message: 'Logged out successfully' });
  });
});

// Current user
app.get('/api/me', requireAuth, (req, res) => {
  res.json({ username: req.session.user.username, loginAt: req.session.user.loginAt });
});

// ── WebSocket ────────────────────────────
const server = http.createServer(app);
const wss = new WebSocket.Server({ noServer: true });
const rooms = {};

wss.on('connection', (ws) => {
  ws.on('message', (msg) => {
    const data = JSON.parse(msg);
    if (data.type === 'join') {
      const room = data.room || 'default';
      ws.room = room;
      if (!rooms[room]) rooms[room] = [];
      rooms[room].push(ws);
    }
    if (data.type === 'move' && ws.room) {
      rooms[ws.room].forEach(client => {
        if (client !== ws && client.readyState === WebSocket.OPEN) {
          client.send(JSON.stringify({ type: 'move', payload: data.payload }));
        }
      });
    }
  });

  ws.on('close', () => {
    if (ws.room && rooms[ws.room]) {
      rooms[ws.room] = rooms[ws.room].filter(c => c !== ws);
    }
  });
});

// Upgrade HTTP to WebSocket
server.on('upgrade', (req, socket, head) => {
  sessionParser(req, {}, () => {
    if (!req.session.user) return socket.destroy();
    wss.handleUpgrade(req, socket, head, (ws) => wss.emit('connection', ws, req));
  });
});

// Start server
server.listen(PORT, () => {
  console.log(`✅ Server running at http://localhost:${PORT}`);
});