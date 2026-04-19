export interface IResult {
  id?: number;
  game_id: number;
  winner_username: string;
  loser_username: string;
  winner_score: number;
  loser_score: number;
  duration_seconds: number;
  created_at?: Date;
}

export interface IResultRepository {
  findById(id: number): Promise<IResult | null>;
  findByGameId(gameId: number): Promise<IResult | null>;
  create(result: Omit<IResult, 'id' | 'created_at'>): Promise<IResult>;
  update(id: number, result: Partial<IResult>): Promise<IResult | null>;
  delete(id: number): Promise<boolean>;
  findAll(): Promise<IResult[]>;
  findByUsername(username: string): Promise<IResult[]>;
  findRecentByUsername(username: string, limit: number): Promise<IResult[]>;
  getUserStats(username: string): Promise<{
    totalGames: number;
    wins: number;
    losses: number;
    averageScore: number;
  }>;
}
