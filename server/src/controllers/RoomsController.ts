import { Router, Request, Response } from 'express';
import { GameService } from '../services/GameService';

interface AuthenticatedRequest extends Request {
  session?: {
    user?: {
      username: string;
      loginAt: string;
    };
  };
}

export class RoomsController {
  private router: Router;
  private gameService: GameService;

  constructor(gameService: GameService) {
    this.router = Router();
    this.gameService = gameService;
    this.setupRoutes();
  }

  private setupRoutes(): void {
    this.router.post('/', this.createRoom.bind(this));
    this.router.post('/join', this.joinRoom.bind(this));
    this.router.get('/:roomCode', this.getRoom.bind(this));
  }

  getRouter(): Router {
    return this.router;
  }

  private generateRoomCode(): string {
    const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    let roomCode = '';
    
    for (let index = 0; index < 6; index += 1) {
      roomCode += alphabet[Math.floor(Math.random() * alphabet.length)];
    }
    
    return roomCode;
  }

  async createRoom(req: AuthenticatedRequest, res: Response): Promise<void> {
    try {
      const username = req.session?.user?.username;
      
      if (!username) {
        res.status(401).json({ error: 'Not authenticated' });
        return;
      }

      const roomCode = this.generateRoomCode();
      const maxScore = req.body.maxScore || 5;
      
      await this.gameService.createGame(username, roomCode, maxScore);
      
      res.json({ roomCode });
    } catch (error) {
      console.error('Create room error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }

  async joinRoom(req: AuthenticatedRequest, res: Response): Promise<void> {
    try {
      const username = req.session?.user?.username;
      const roomCode = String(req.body.roomCode || '').trim().toUpperCase();
      
      if (!username) {
        res.status(401).json({ error: 'Not authenticated' });
        return;
      }

      if (!roomCode) {
        res.status(400).json({ error: 'Room code is required' });
        return;
      }

      const result = await this.gameService.joinGame(roomCode, username);
      
      if (!result.success) {
        const statusCode = result.error === 'Room not found' ? 404 : 409;
        res.status(statusCode).json({ error: result.error });
        return;
      }

      res.json({ roomCode });
    } catch (error) {
      console.error('Join room error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }

  async getRoom(req: AuthenticatedRequest, res: Response): Promise<void> {
    try {
      const username = req.session?.user?.username;
      const roomCode = String(req.params.roomCode || '').trim().toUpperCase();
      
      if (!username) {
        res.status(401).json({ error: 'Not authenticated' });
        return;
      }

      const game = await this.gameService.findByRoomCode(roomCode);
      
      if (!game) {
        res.status(404).json({ error: 'Room not found' });
        return;
      }

      const role = game.host_username === username ? 'host' : 'guest';
      
      res.json({
        roomCode: game.room_code,
        role,
        hostUsername: game.host_username,
        guestUsername: game.guest_username,
        status: game.status,
        hostScore: game.host_score,
        guestScore: game.guest_score,
        maxScore: game.max_score,
      });
    } catch (error) {
      console.error('Get room error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }
}