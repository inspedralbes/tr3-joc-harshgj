import { IUser, IUserRepository } from '../interfaces/IUserRepository';

export class InMemoryUserRepository implements IUserRepository {
  private users: Map<number, IUser> = new Map();
  private nextId: number = 1;

  async findById(id: number): Promise<IUser | null> {
    return this.users.get(id) || null;
  }

  async findByUsername(username: string): Promise<IUser | null> {
    for (const user of this.users.values()) {
      if (user.username === username) return user;
    }
    return null;
  }

  async create(userData: Omit<IUser, 'id' | 'created_at'>): Promise<IUser> {
    const user: IUser = {
      ...userData,
      id: this.nextId++,
      created_at: new Date(),
    };
    this.users.set(user.id!, user);
    return user;
  }

  async update(id: number, userData: Partial<IUser>): Promise<IUser | null> {
    const existing = this.users.get(id);
    if (!existing) return null;

    const updated: IUser = { ...existing, ...userData };
    this.users.set(id, updated);
    return updated;
  }

  async delete(id: number): Promise<boolean> {
    return this.users.delete(id);
  }

  async findAll(): Promise<IUser[]> {
    return Array.from(this.users.values());
  }

  async findByUsernameAsync(username: string): Promise<IUser | null> {
    return this.findByUsername(username);
  }

  clear(): void {
    this.users.clear();
    this.nextId = 1;
  }
}