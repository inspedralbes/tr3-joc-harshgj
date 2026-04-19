import { Sequelize, Model, DataTypes } from 'sequelize';
import { IGame, IGameRepository, GameStatus } from '../interfaces/IGameRepository';

export class MySqlGameModel extends Model<IGame> implements IGame {
  declare id?: number;
  declare room_code: string;
  declare host_username: string;
  declare guest_username?: string;
  declare status: GameStatus;
  declare host_score: number;
  declare guest_score: number;
  declare max_score: number;
  declare created_at?: Date;
  declare started_at?: Date;
  declare finished_at?: Date;
}

export class MySqlGameRepository implements IGameRepository {
  private sequelize: Sequelize;

  constructor(sequelize: Sequelize) {
    this.sequelize = sequelize;
    this.initModel();
  }

  private initModel(): void {
    MySqlGameModel.init(
      {
        id: {
          type: DataTypes.INTEGER,
          autoIncrement: true,
          primaryKey: true,
        },
        room_code: {
          type: DataTypes.STRING(6),
          unique: true,
          allowNull: false,
        },
        host_username: {
          type: DataTypes.STRING,
          allowNull: false,
        },
        guest_username: {
          type: DataTypes.STRING,
          allowNull: true,
        },
        status: {
          type: DataTypes.ENUM('created', 'in_progress', 'finished'),
          defaultValue: 'created',
        },
        host_score: {
          type: DataTypes.INTEGER,
          defaultValue: 0,
        },
        guest_score: {
          type: DataTypes.INTEGER,
          defaultValue: 0,
        },
        max_score: {
          type: DataTypes.INTEGER,
          defaultValue: 5,
        },
        created_at: {
          type: DataTypes.DATE,
          defaultValue: DataTypes.NOW,
        },
        started_at: {
          type: DataTypes.DATE,
          allowNull: true,
        },
        finished_at: {
          type: DataTypes.DATE,
          allowNull: true,
        },
      },
      {
        sequelize: this.sequelize,
        tableName: 'games',
        timestamps: false,
      }
    );
  }

  async findById(id: number): Promise<IGame | null> {
    const game = await MySqlGameModel.findByPk(id);
    return game ? this.toIGame(game) : null;
  }

  async findByRoomCode(roomCode: string): Promise<IGame | null> {
    const game = await MySqlGameModel.findOne({ where: { room_code: roomCode } });
    return game ? this.toIGame(game) : null;
  }

  async create(gameData: Omit<IGame, 'id' | 'created_at' | 'started_at' | 'finished_at'>): Promise<IGame> {
    const game = await MySqlGameModel.create(gameData);
    return this.toIGame(game);
  }

  async update(id: number, gameData: Partial<IGame>): Promise<IGame | null> {
    const game = await MySqlGameModel.findByPk(id);
    if (!game) return null;

    await game.update(gameData);
    return this.toIGame(game);
  }

  async delete(id: number): Promise<boolean> {
    const deleted = await MySqlGameModel.destroy({ where: { id } });
    return deleted > 0;
  }

  async findAll(): Promise<IGame[]> {
    const games = await MySqlGameModel.findAll();
    return games.map(this.toIGame);
  }

  async findByStatus(status: GameStatus): Promise<IGame[]> {
    const games = await MySqlGameModel.findAll({ where: { status } });
    return games.map(this.toIGame);
  }

  async findByHostUsername(username: string): Promise<IGame[]> {
    const games = await MySqlGameModel.findAll({ where: { host_username: username } });
    return games.map(this.toIGame);
  }

  async findByGuestUsername(username: string): Promise<IGame[]> {
    const games = await MySqlGameModel.findAll({ where: { guest_username: username } });
    return games.map(this.toIGame);
  }

  private toIGame(model: MySqlGameModel): IGame {
    return {
      id: model.id,
      room_code: model.room_code,
      host_username: model.host_username,
      guest_username: model.guest_username,
      status: model.status,
      host_score: model.host_score,
      guest_score: model.guest_score,
      max_score: model.max_score,
      created_at: model.created_at,
      started_at: model.started_at,
      finished_at: model.finished_at,
    };
  }
}