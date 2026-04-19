export type GameStatus = 'created' | 'in_progress' | 'finished';

export interface IGame {
  id?: number;
  room_code: string;
  host_username: string;
  guest_username?: string;
  status: GameStatus;
  host_score: number;
  guest_score: number;
  max_score: number;
  created_at?: Date;
  started_at?: Date;
  finished_at?: Date;
}

export interface IGameRepository {
  findById(id: number): Promise<IGame | null>;
  findByRoomCode(roomCode: string): Promise<IGame | null>;
  create(game: Omit<IGame, 'id' | 'created_at' | 'started_at' | 'finished_at'>): Promise<IGame>;
  update(id: number, game: Partial<IGame>): Promise<IGame | null>;
  delete(id: number): Promise<boolean>;
  findAll(): Promise<IGame[]>;
  findByStatus(status: GameStatus): Promise<IGame[]>;
  findByHostUsername(username: string): Promise<IGame[]>;
  findByGuestUsername(username: string): Promise<IGame[]>;
}
