import { IGame, IGameRepository, GameStatus } from '../interfaces/IGameRepository';

export class InMemoryGameRepository implements IGameRepository {
  private games: Map<number, IGame> = new Map();
  private roomCodes: Map<string, number> = new Map();
  private nextId: number = 1;

  async findById(id: number): Promise<IGame | null> {
    return this.games.get(id) || null;
  }

  async findByRoomCode(roomCode: string): Promise<IGame | null> {
    const id = this.roomCodes.get(roomCode);
    return id ? this.games.get(id) || null : null;
  }

  async create(gameData: Omit<IGame, 'id' | 'created_at' | 'started_at' | 'finished_at'>): Promise<IGame> {
    const game: IGame = {
      ...gameData,
      id: this.nextId++,
      created_at: new Date(),
    };
    this.games.set(game.id!, game);
    this.roomCodes.set(game.room_code, game.id!);
    return game;
  }

  async update(id: number, gameData: Partial<IGame>): Promise<IGame | null> {
    const existing = this.games.get(id);
    if (!existing) return null;

    const updated: IGame = { ...existing, ...gameData };
    this.games.set(id, updated);
    
    if (gameData.room_code && gameData.room_code !== existing.room_code) {
      this.roomCodes.delete(existing.room_code);
      this.roomCodes.set(gameData.room_code, id);
    }
    
    return updated;
  }

  async delete(id: number): Promise<boolean> {
    const game = this.games.get(id);
    if (game) {
      this.roomCodes.delete(game.room_code);
    }
    return this.games.delete(id);
  }

  async findAll(): Promise<IGame[]> {
    return Array.from(this.games.values());
  }

  async findByStatus(status: GameStatus): Promise<IGame[]> {
    return Array.from(this.games.values()).filter(g => g.status === status);
  }

  async findByHostUsername(username: string): Promise<IGame[]> {
    return Array.from(this.games.values()).filter(g => g.host_username === username);
  }

  async findByGuestUsername(username: string): Promise<IGame[]> {
    return Array.from(this.games.values()).filter(g => g.guest_username === username);
  }

  clear(): void {
    this.games.clear();
    this.roomCodes.clear();
    this.nextId = 1;
  }
}