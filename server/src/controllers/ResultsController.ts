import { Router, Request, Response } from 'express';
import { ResultService } from '../services/ResultService';
import { GameService } from '../services/GameService';

interface AuthenticatedRequest extends Request {
  session?: {
    user?: {
      username: string;
      loginAt: string;
    };
  };
}

export class ResultsController {
  private router: Router;
  private resultService: ResultService;
  private gameService: GameService;

  constructor(resultService: ResultService, gameService: GameService) {
    this.router = Router();
    this.resultService = resultService;
    this.gameService = gameService;
    this.setupRoutes();
  }

  private setupRoutes(): void {
    this.router.get('/my-results', this.getMyResults.bind(this));
    this.router.get('/my-stats', this.getMyStats.bind(this));
    this.router.get('/leaderboard', this.getLeaderboard.bind(this));
    this.router.get('/:gameId', this.getResultByGameId.bind(this));
  }

  getRouter(): Router {
    return this.router;
  }

  async getMyResults(req: AuthenticatedRequest, res: Response): Promise<void> {
    try {
      const username = req.session?.user?.username;
      
      if (!username) {
        res.status(401).json({ error: 'Not authenticated' });
        return;
      }

      const limit = parseInt(req.query.limit as string) || 10;
      const results = await this.resultService.getRecentResults(username, limit);
      
      res.json(results);
    } catch (error) {
      console.error('Get my results error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }

  async getMyStats(req: AuthenticatedRequest, res: Response): Promise<void> {
    try {
      const username = req.session?.user?.username;
      
      if (!username) {
        res.status(401).json({ error: 'Not authenticated' });
        return;
      }

      const stats = await this.resultService.getUserStats(username);
      
      res.json(stats);
    } catch (error) {
      console.error('Get my stats error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }

  async getLeaderboard(req: AuthenticatedRequest, res: Response): Promise<void> {
    try {
      const limit = parseInt(req.query.limit as string) || 10;
      const leaderboard = await this.resultService.getLeaderboard(limit);
      
      res.json(leaderboard);
    } catch (error) {
      console.error('Get leaderboard error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }

  async getResultByGameId(req: Request, res: Response): Promise<void> {
    try {
      const gameId = parseInt(req.params.gameId);
      
      if (isNaN(gameId)) {
        res.status(400).json({ error: 'Invalid game ID' });
        return;
      }

      const result = await this.resultService.findByGameId(gameId);
      
      if (!result) {
        res.status(404).json({ error: 'Result not found' });
        return;
      }
      
      res.json(result);
    } catch (error) {
      console.error('Get result by game ID error:', error);
      res.status(500).json({ error: 'Server error' });
    }
  }
}