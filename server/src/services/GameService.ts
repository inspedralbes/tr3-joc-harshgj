import { IGame, IGameRepository, GameStatus } from '../repositories/interfaces';

export class GameService {
  constructor(private gameRepository: IGameRepository) {}

  async findById(id: number): Promise<IGame | null> {
    return this.gameRepository.findById(id);
  }

  async findByRoomCode(roomCode: string): Promise<IGame | null> {
    return this.gameRepository.findByRoomCode(roomCode);
  }

  async createGame(hostUsername: string, roomCode: string, maxScore: number = 5): Promise<IGame> {
    return this.gameRepository.create({
      room_code: roomCode,
      host_username: hostUsername,
      status: 'created',
      host_score: 0,
      guest_score: 0,
      max_score: maxScore,
    });
  }

  async joinGame(roomCode: string, guestUsername: string): Promise<{ success: boolean; game?: IGame; error?: string }> {
    const game = await this.gameRepository.findByRoomCode(roomCode);
    
    if (!game) {
      return { success: false, error: 'Room not found' };
    }

    if (game.guest_username) {
      return { success: false, error: 'Room is full' };
    }

    if (game.host_username === guestUsername) {
      return { success: false, error: 'Cannot join own room' };
    }

    const updated = await this.gameRepository.update(game.id!, {
      guest_username: guestUsername,
    });

    return { success: true, game: updated! };
  }

  async startGame(roomCode: string): Promise<{ success: boolean; error?: string }> {
    const game = await this.gameRepository.findByRoomCode(roomCode);
    
    if (!game) {
      return { success: false, error: 'Room not found' };
    }

    await this.gameRepository.update(game.id!, {
      status: 'in_progress',
      started_at: new Date(),
    });

    return { success: true };
  }

  async updateScore(
    roomCode: string,
    isHost: boolean,
    newScore: number
  ): Promise<{ success: boolean; gameFinished?: boolean; winner?: string; error?: string }> {
    const game = await this.gameRepository.findByRoomCode(roomCode);
    
    if (!game) {
      return { success: false, error: 'Room not found' };
    }

    const updateData: Partial<IGame> = isHost
      ? { host_score: newScore }
      : { guest_score: newScore };

    await this.gameRepository.update(game.id!, updateData);

    if (newScore >= game.max_score) {
      const updatedGame = await this.gameRepository.findByRoomCode(roomCode);
      const winner = isHost ? updatedGame!.host_username : updatedGame!.guest_username;
      
      await this.gameRepository.update(game.id!, {
        status: 'finished',
        finished_at: new Date(),
      });

      return { success: true, gameFinished: true, winner };
    }

    return { success: true };
  }

  async finishGame(roomCode: string): Promise<{ success: boolean; error?: string }> {
    const game = await this.gameRepository.findByRoomCode(roomCode);
    
    if (!game) {
      return { success: false, error: 'Room not found' };
    }

    await this.gameRepository.update(game.id!, {
      status: 'finished',
      finished_at: new Date(),
    });

    return { success: true };
  }

  async getGamesByStatus(status: GameStatus): Promise<IGame[]> {
    return this.gameRepository.findByStatus(status);
  }

  async getAllGames(): Promise<IGame[]> {
    return this.gameRepository.findAll();
  }

  async getUserGames(username: string): Promise<IGame[]> {
    const asHost = await this.gameRepository.findByHostUsername(username);
    const asGuest = await this.gameRepository.findByGuestUsername(username);
    return [...asHost, ...asGuest];
  }
}