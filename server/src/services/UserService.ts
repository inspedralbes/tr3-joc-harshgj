import { IUser, IUserRepository } from '../repositories/interfaces';

export class UserService {
  constructor(private userRepository: IUserRepository) {}

  async findById(id: number): Promise<IUser | null> {
    return this.userRepository.findById(id);
  }

  async findByUsername(username: string): Promise<IUser | null> {
    return this.userRepository.findByUsername(username);
  }

  async register(username: string, passwordHash: string): Promise<{ success: boolean; error?: string }> {
    if (!username || username.length < 3) {
      return { success: false, error: 'Username must be at least 3 characters' };
    }

    if (!passwordHash || passwordHash.length < 6) {
      return { success: false, error: 'Password must be at least 6 characters' };
    }

    const existing = await this.userRepository.findByUsername(username);
    if (existing) {
      return { success: false, error: 'User already exists' };
    }

    await this.userRepository.create({ username, password_hash: passwordHash });
    return { success: true };
  }

  async validatePassword(username: string, passwordHash: string): Promise<{ valid: boolean; user?: IUser; error?: string }> {
    const user = await this.userRepository.findByUsername(username);
    if (!user) {
      return { valid: false, error: 'User not found' };
    }

    const bcrypt = require('bcryptjs');
    const valid = await bcrypt.compare(passwordHash, user.password_hash);
    
    if (!valid) {
      return { valid: false, error: 'Invalid password' };
    }

    return { valid: true, user };
  }

  async getAllUsers(): Promise<IUser[]> {
    return this.userRepository.findAll();
  }

  async deleteUser(id: number): Promise<boolean> {
    return this.userRepository.delete(id);
  }
}