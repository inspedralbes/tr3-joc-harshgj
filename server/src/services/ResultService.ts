import { IResult, IResultRepository } from '../repositories/interfaces';

export class ResultService {
  constructor(private resultRepository: IResultRepository) {}

  async findById(id: number): Promise<IResult | null> {
    return this.resultRepository.findById(id);
  }

  async findByGameId(gameId: number): Promise<IResult | null> {
    return this.resultRepository.findByGameId(gameId);
  }

  async createResult(
    gameId: number,
    winnerUsername: string,
    loserUsername: string,
    winnerScore: number,
    loserScore: number,
    durationSeconds: number
  ): Promise<IResult> {
    return this.resultRepository.create({
      game_id: gameId,
      winner_username: winnerUsername,
      loser_username: loserUsername,
      winner_score: winnerScore,
      loser_score: loserScore,
      duration_seconds: durationSeconds,
    });
  }

  async getUserResults(username: string): Promise<IResult[]> {
    return this.resultRepository.findByUsername(username);
  }

  async getRecentResults(username: string, limit: number = 10): Promise<IResult[]> {
    return this.resultRepository.findRecentByUsername(username, limit);
  }

  async getUserStats(username: string): Promise<{
    totalGames: number;
    wins: number;
    losses: number;
    averageScore: number;
  }> {
    return this.resultRepository.getUserStats(username);
  }

  async getLeaderboard(limit: number = 10): Promise<Array<{
    username: string;
    wins: number;
  }>> {
    const allResults = await this.resultRepository.findAll();
    
    const winCounts = new Map<string, number>();
    for (const result of allResults) {
      const current = winCounts.get(result.winner_username) || 0;
      winCounts.set(result.winner_username, current + 1);
    }

    return Array.from(winCounts.entries())
      .map(([username, wins]) => ({ username, wins }))
      .sort((a, b) => b.wins - a.wins)
      .slice(0, limit);
  }

  async getAllResults(): Promise<IResult[]> {
    return this.resultRepository.findAll();
  }
}