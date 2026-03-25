# 🔐 Auth Server — Node.js + Docker

Login/logout server running on **localhost:3000**

## Files
```
auth-server/
├── server.js          ← Express server (login, logout, register, session)
├── package.json
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
└── client/
    ├── login.html     ← Login + Register page
    └── dashboard.html ← Protected page (after login)
```

---

## 🚀 Run with Docker (recommended)

### Option A — docker-compose (easiest)
```bash
docker-compose up --build
```
Open → http://localhost:3000

### Option B — docker build + run
```bash
docker build -t auth-server .
docker run -p 3000:3000 auth-server
```

### Stop
```bash
docker-compose down
```

---

## 🖥️ Run without Docker (Node.js)
```bash
npm install
node server.js
```

---

## 🔑 Demo Account
| Field    | Value        |
|----------|-------------|
| Username | `admin`      |
| Password | `password123`|

---

## 📡 API Endpoints
| Method | Route           | Description         |
|--------|----------------|---------------------|
| GET    | `/`             | Login page          |
| GET    | `/dashboard`    | Protected dashboard |
| POST   | `/api/register` | Create account      |
| POST   | `/api/login`    | Login               |
| POST   | `/api/logout`   | Logout              |
| GET    | `/api/me`       | Current session     |
