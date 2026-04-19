import { Sequelize, Model, DataTypes } from 'sequelize';
import { IResult, IResultRepository } from '../interfaces/IResultRepository';

export class MySqlResultModel extends Model<IResult> implements IResult {
  declare id?: number;
  declare game_id: number;
  declare winner_username: string;
  declare loser_username: string;
  declare winner_score: number;
  declare loser_score: number;
  declare duration_seconds: number;
  declare created_at?: Date;
}

export class MySqlResultRepository implements IResultRepository {
  private sequelize: Sequelize;

  constructor(sequelize: Sequelize) {
    this.sequelize = sequelize;
    this.initModel();
  }

  private initModel(): void {
    MySqlResultModel.init(
      {
        id: {
          type: DataTypes.INTEGER,
          autoIncrement: true,
          primaryKey: true,
        },
        game_id: {
          type: DataTypes.INTEGER,
          allowNull: false,
        },
        winner_username: {
          type: DataTypes.STRING,
          allowNull: false,
        },
        loser_username: {
          type: DataTypes.STRING,
          allowNull: false,
        },
        winner_score: {
          type: DataTypes.INTEGER,
          allowNull: false,
        },
        loser_score: {
          type: DataTypes.INTEGER,
          allowNull: false,
        },
        duration_seconds: {
          type: DataTypes.INTEGER,
          allowNull: false,
        },
        created_at: {
          type: DataTypes.DATE,
          defaultValue: DataTypes.NOW,
        },
      },
      {
        sequelize: this.sequelize,
        tableName: 'results',
        timestamps: false,
      }
    );
  }

  async findById(id: number): Promise<IResult | null> {
    const result = await MySqlResultModel.findByPk(id);
    return result ? this.toIResult(result) : null;
  }

  async findByGameId(gameId: number): Promise<IResult | null> {
    const result = await MySqlResultModel.findOne({ where: { game_id: gameId } });
    return result ? this.toIResult(result) : null;
  }

  async create(resultData: Omit<IResult, 'id' | 'created_at'>): Promise<IResult> {
    const result = await MySqlResultModel.create(resultData);
    return this.toIResult(result);
  }

  async update(id: number, resultData: Partial<IResult>): Promise<IResult | null> {
    const result = await MySqlResultModel.findByPk(id);
    if (!result) return null;

    await result.update(resultData);
    return this.toIResult(result);
  }

  async delete(id: number): Promise<boolean> {
    const deleted = await MySqlResultModel.destroy({ where: { id } });
    return deleted > 0;
  }

  async findAll(): Promise<IResult[]> {
    const results = await MySqlResultModel.findAll();
    return results.map(this.toIResult);
  }

  async findByUsername(username: string): Promise<IResult[]> {
    const results = await MySqlResultModel.findAll({
      where: {
        [Sequelize.Op.or]: [
          { winner_username: username },
          { loser_username: username },
        ],
      },
      order: [['created_at', 'DESC']],
    });
    return results.map(this.toIResult);
  }

  async findRecentByUsername(username: string, limit: number): Promise<IResult[]> {
    const results = await MySqlResultModel.findAll({
      where: {
        [Sequelize.Op.or]: [
          { winner_username: username },
          { loser_username: username },
        ],
      },
      order: [['created_at', 'DESC']],
      limit,
    });
    return results.map(this.toIResult);
  }

  async getUserStats(username: string): Promise<{
    totalGames: number;
    wins: number;
    losses: number;
    averageScore: number;
  }> {
    const wins = await MySqlResultModel.count({ where: { winner_username: username } });
    const losses = await MySqlResultModel.count({ where: { loser_username: username } });
    const totalGames = wins + losses;

    const userResults = await MySqlResultModel.findAll({
      where: {
        [Sequelize.Op.or]: [
          { winner_username: username },
          { loser_username: username },
        ],
      },
    });

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

  private toIResult(model: MySqlResultModel): IResult {
    return {
      id: model.id,
      game_id: model.game_id,
      winner_username: model.winner_username,
      loser_username: model.loser_username,
      winner_score: model.winner_score,
      loser_score: model.loser_score,
      duration_seconds: model.duration_seconds,
      created_at: model.created_at,
    };
  }
}