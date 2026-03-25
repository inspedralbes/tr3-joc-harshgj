const express = require('express');
const session = require('express-session');
const bcrypt = require('bcryptjs');
const path = require('path');

const app = express();
const PORT = 3000;

// ── Middleware ──────────────────────────────────────────────────────────────
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static(path.join(__dirname, 'client')));

app.use(session({
  secret: process.env.SESSION_SECRET || 'arkanoid-super-secret-key',
  resave: false,
  saveUninitialized: false,
  cookie: {
    secure: false,       // set true if using HTTPS
    httpOnly: true,
    maxAge: 1000 * 60 * 60 * 2  // 2 hours
  }
}));

// ── In-memory user store (replace with DB in production) ───────────────────
const users = {};

// Pre-seed a demo user: admin / password123
(async () => {
  users['admin'] = {
    username: 'admin',
    passwordHash: await bcrypt.hash('password123', 10),
    createdAt: new Date().toISOString()
  };
})();

// ── Auth middleware ─────────────────────────────────────────────────────────
function requireAuth(req, res, next) {
  if (req.session && req.session.user) return next();
  res.status(401).json({ error: 'Not authenticated' });
}

// ── Routes ──────────────────────────────────────────────────────────────────

// Serve login page
app.get('/', (req, res) => {
  if (req.session.user) return res.redirect('/dashboard');
  res.sendFile(path.join(__dirname, 'client', 'login.html'));
});

app.get('/dashboard', (req, res) => {
  if (!req.session.user) return res.redirect('/');
  res.sendFile(path.join(__dirname, 'client', 'dashboard.html'));
});

// ── API: Register ───────────────────────────────────────────────────────────
app.post('/api/register', async (req, res) => {
  const { username, password } = req.body;

  if (!username || !password)
    return res.status(400).json({ error: 'Username and password are required' });
  if (username.length < 3)
    return res.status(400).json({ error: 'Username must be at least 3 characters' });
  if (password.length < 6)
    return res.status(400).json({ error: 'Password must be at least 6 characters' });
  if (users[username])
    return res.status(409).json({ error: 'Username already taken' });

  const passwordHash = await bcrypt.hash(password, 10);
  users[username] = { username, passwordHash, createdAt: new Date().toISOString() };

  console.log(`[Register] New user: ${username}`);
  res.json({ message: 'Account created successfully!' });
});

// ── API: Login ──────────────────────────────────────────────────────────────
app.post('/api/login', async (req, res) => {
  const { username, password } = req.body;

  if (!username || !password)
    return res.status(400).json({ error: 'Username and password are required' });

  const user = users[username];
  if (!user)
    return res.status(401).json({ error: 'Invalid username or password' });

  const valid = await bcrypt.compare(password, user.passwordHash);
  if (!valid)
    return res.status(401).json({ error: 'Invalid username or password' });

  req.session.user = { username: user.username, loginAt: new Date().toISOString() };
  console.log(`[Login] ${username} logged in`);
  res.json({ message: 'Login successful', username: user.username });
});

// ── API: Logout ─────────────────────────────────────────────────────────────
app.post('/api/logout', (req, res) => {
  const username = req.session.user?.username;
  req.session.destroy(err => {
    if (err) return res.status(500).json({ error: 'Logout failed' });
    res.clearCookie('connect.sid');
    console.log(`[Logout] ${username} logged out`);
    res.json({ message: 'Logged out successfully' });
  });
});

// ── API: Session check ──────────────────────────────────────────────────────
app.get('/api/me', requireAuth, (req, res) => {
  res.json({ username: req.session.user.username, loginAt: req.session.user.loginAt });
});

// ── Start ───────────────────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log(`✅ Server running at http://localhost:${PORT}`);
  console.log(`   Demo account → username: admin  password: password123`);
});
