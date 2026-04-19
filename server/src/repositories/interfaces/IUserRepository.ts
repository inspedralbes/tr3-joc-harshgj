export interface IUser {
  id?: number;
  username: string;
  password_hash: string;
  created_at?: Date;
}

export interface IUserRepository {
  findById(id: number): Promise<IUser | null>;
  findByUsername(username: string): Promise<IUser | null>;
  create(user: Omit<IUser, 'id' | 'created_at'>): Promise<IUser>;
  update(id: number, user: Partial<IUser>): Promise<IUser | null>;
  delete(id: number): Promise<boolean>;
  findAll(): Promise<IUser[]>;
  findByUsernameAsync(username: string): Promise<IUser | null>;
}
