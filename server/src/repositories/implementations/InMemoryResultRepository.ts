import { IResult, IResultRepository } from '../interfaces/IResultRepository';

export class InMemoryResultRepository implements IResultRepository {
  private results: Map<number, IResult> = new Map();
  private gameIds: Map<number, number> = new Map();
  private nextId: number = 1;

  async findById(id: number): Promise<IResult | null> {
    return this.results.get(id) || null;
  }

  async findByGameId(gameId: number): Promise<IResult | null> {
    const id = this.gameIds.get(gameId);
    return id ? this.results.get(id) || null : null;
  }

  async create(resultData: Omit<IResult, 'id' | 'created_at'>): Promise<IResult> {
    const result: IResult = {
      ...resultData,
      id: this.nextId++,
      created_at: new Date(),
    };
    this.results.set(result.id!, result);
    this.gameIds.set(result.game_id, result.id!);
    return result;
  }

  async update(id: number, resultData: Partial<IResult>): Promise<IResult | null> {
    const existing = this.results.get(id);
    if (!existing) return null;

    const updated: IResult = { ...existing, ...resultData };
    this.results.set(id, updated);
    return updated;
  }

  async delete(id: number): Promise<boolean> {
    const result = this.results.get(id);
    if (result) {
      this.gameIds.delete(result.game_id);
    }
    return this.results.delete(id);
  }

  async findAll(): Promise<IResult[]> {
    return Array.from(this.results.values());
  }

  async findByUsername(username: string): Promise<IResult[]> {
    return Array.from(this.results.values())
      .filter(r => r.winner_username === username || r.loser_username === username)
      .sort((a, b) => (b.created_at?.getTime() || 0) - (a.created_at?.getTime() || 0));
  }

  async findRecentByUsername(username: string, limit: number): Promise<IResult[]> {
    const userResults = await this.findByUsername(username);
    return userResults.slice(0, limit);
  }

  async getUserStats(username: string): Promise<{
    totalGames: number;
    wins: number;
    losses: number;
    averageScore: number;
  }> {
    const userResults = Array.from(this.results.values())
      .filter(r => r.winner_username === username || r.loser_username === username);

    const wins = userResults.filter(r => r.winner_username === username).length;
    const losses = userResults.filter(r => r.loser_username === username).length;
    const totalGames = wins + losses;

    let totalScore = 0;
    for (const r of userResults) {
      totalScore += r.winner_username === username ? r.winner_score : r.loser_score;
    }

    return {
      totalGames,
      wins,
      losses,
      averageScore: totalGames > 0 ? Math.round(totalScore / totalGames) : 0,
    };
  }

  clear(): void {
    this.results.clear();
    this.gameIds.clear();
    this.nextId = 1;
  }
}