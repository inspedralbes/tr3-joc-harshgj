# ── Stage 1: deps ──────────────────────────────────────────────────────────
FROM node:18-alpine AS deps
WORKDIR /app
COPY package.json ./
RUN npm install --omit=dev

# ── Stage 2: runtime ────────────────────────────────────────────────────────
FROM node:18-alpine
WORKDIR /app

# Copy deps from stage 1
COPY --from=deps /app/node_modules ./node_modules

# Copy source
COPY server.js ./
COPY client ./client

# Non-root user for security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

EXPOSE 3000

ENV NODE_ENV=production
ENV SESSION_SECRET=change-this-in-production

CMD ["node", "server.js"]
