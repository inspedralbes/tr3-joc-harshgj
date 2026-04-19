import { Sequelize, Model, DataTypes } from 'sequelize';
import { IUser, IUserRepository } from '../interfaces/IUserRepository';

export class MySqlUserModel extends Model<IUser> implements IUser {
  declare id?: number;
  declare username: string;
  declare password_hash: string;
  declare created_at?: Date;
}

export class MySqlUserRepository implements IUserRepository {
  private sequelize: Sequelize;

  constructor(sequelize: Sequelize) {
    this.sequelize = sequelize;
    this.initModel();
  }

  private initModel(): void {
    MySqlUserModel.init(
      {
        id: {
          type: DataTypes.INTEGER,
          autoIncrement: true,
          primaryKey: true,
        },
        username: {
          type: DataTypes.STRING,
          unique: true,
          allowNull: false,
        },
        password_hash: {
          type: DataTypes.STRING,
          allowNull: false,
        },
        created_at: {
          type: DataTypes.DATE,
          defaultValue: DataTypes.NOW,
        },
      },
      {
        sequelize: this.sequelize,
        tableName: 'users',
        timestamps: false,
      }
    );
  }

  async findById(id: number): Promise<IUser | null> {
    const user = await MySqlUserModel.findByPk(id);
    return user ? this.toIUser(user) : null;
  }

  async findByUsername(username: string): Promise<IUser | null> {
    const user = await MySqlUserModel.findOne({ where: { username } });
    return user ? this.toIUser(user) : null;
  }

  async create(userData: Omit<IUser, 'id' | 'created_at'>): Promise<IUser> {
    const user = await MySqlUserModel.create(userData);
    return this.toIUser(user);
  }

  async update(id: number, userData: Partial<IUser>): Promise<IUser | null> {
    const user = await MySqlUserModel.findByPk(id);
    if (!user) return null;

    await user.update(userData);
    return this.toIUser(user);
  }

  async delete(id: number): Promise<boolean> {
    const deleted = await MySqlUserModel.destroy({ where: { id } });
    return deleted > 0;
  }

  async findAll(): Promise<IUser[]> {
    const users = await MySqlUserModel.findAll();
    return users.map(this.toIUser);
  }

  async findByUsernameAsync(username: string): Promise<IUser | null> {
    return this.findByUsername(username);
  }

  private toIUser(model: MySqlUserModel): IUser {
    return {
      id: model.id,
      username: model.username,
      password_hash: model.password_hash,
      created_at: model.created_at,
    };
  }
}
