import { Router, Request, Response } from 'express';
import bcrypt from 'bcryptjs';
import signature from 'cookie-signature';
import session from 'express-session';
import { UserService } from '../services/UserService';

interface AuthenticatedRequest extends Request {
  session?: {
    user?: {
      username: string;
      loginAt: string;
    };
  };
}

export class AuthController {
  private router: Router;
  private userService: UserService;
  private sessionSecret: string;

  constructor(userService: UserService, sessionSecret: string) {
    this.router = Router();
    this.userService = userService;
    this.sessionSecret = sessionSecret;
    this.setupRoutes();
  }

  private setupRoutes(): void {
    this.router.post('/register', this.register.bind(this));
    this.router.post('/login', this.login.bind(this));
    this.router.post('/logout', this.logout.bind(this));
  }

  getRouter(): Router {
    return this.router;
  }

  async register(req: Request, res: Response): Promise<void> {
    try {
      const { username, password } = req.body;

      if (!username || !password) {
        res.status(400).json({ error: 'Missing fields' });
        return;
      }

      if (username.length < 3) {
        res.status(400).json({ error: 'Username must be at least 3 characters' });
        return;
      }

      if (password.length < 6) {
        res.status(400).json({ error: 'Password must be at least 6 characters' });
        return;
      }

      const passwordHash = await bcrypt.hash(password, 10);
      const result = await this.userService.register(username, passwordHash);

      if (!result.success) {
        res.status(409).json({ error: result.error });
        return;
      }

      res.json({ message: 'User registered' });
    } catch (error) {
      console.error('Register error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }

  async login(req: AuthenticatedRequest, res: Response): Promise<void> {
    try {
      const { username, password } = req.body;

      if (!username || !password) {
        res.status(400).json({ error: 'Missing credentials' });
        return;
      }

      const passwordHash = await bcrypt.hash(password, 10);
      const validation = await this.userService.validatePassword(username, passwordHash);

      if (!validation.valid) {
        res.status(401).json({ error: validation.error });
        return;
      }

      req.session!.user = {
        username: validation.user!.username,
        loginAt: new Date().toISOString(),
      };

      req.session!.save((saveError: Error) => {
        if (saveError) {
          console.error('Session save error:', saveError);
          res.status(500).json({ error: 'Session save failed' });
          return;
        }

        const signedSessionId = `s:${signature.sign(req.sessionID!, this.sessionSecret)}`;
        const sessionCookie = `connect.sid=${encodeURIComponent(signedSessionId)}`;

        res.json({
          message: 'Login successful',
          username: validation.user!.username,
          sessionCookie,
        });
      });
    } catch (error) {
      console.error('Login error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }

  logout(req: Request, res: Response): void {
    req.session?.destroy(() => {
      res.clearCookie('connect.sid');
      res.json({ message: 'Logged out' });
    });
  }
}