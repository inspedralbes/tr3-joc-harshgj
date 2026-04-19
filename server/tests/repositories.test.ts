import { describe, it, expect, beforeEach } from 'vitest';
import { InMemoryUserRepository } from '../src/repositories/implementations/InMemoryUserRepository';
import { IUser } from '../src/repositories/interfaces';

describe('InMemoryUserRepository', () => {
  let repository: InMemoryUserRepository;

  beforeEach(() => {
    repository = new InMemoryUserRepository();
  });

  it('should create a user', async () => {
    const user = await repository.create({
      username: 'testuser',
      password_hash: 'hashedpassword',
    });

    expect(user.id).toBeDefined();
    expect(user.username).toBe('testuser');
    expect(user.password_hash).toBe('hashedpassword');
    expect(user.created_at).toBeDefined();
  });

  it('should find user by id', async () => {
    const created = await repository.create({
      username: 'testuser',
      password_hash: 'hashedpassword',
    });

    const found = await repository.findById(created.id!);
    expect(found).not.toBeNull();
    expect(found!.username).toBe('testuser');
  });

  it('should find user by username', async () => {
    await repository.create({
      username: 'testuser',
      password_hash: 'hashedpassword',
    });

    const found = await repository.findByUsername('testuser');
    expect(found).not.toBeNull();
    expect(found!.username).toBe('testuser');
  });

  it('should return null for non-existent user', async () => {
    const found = await repository.findByUsername('nonexistent');
    expect(found).toBeNull();
  });

  it('should update a user', async () => {
    const created = await repository.create({
      username: 'testuser',
      password_hash: 'oldhash',
    });

    const updated = await repository.update(created.id!, {
      password_hash: 'newhash',
    });

    expect(updated).not.toBeNull();
    expect(updated!.password_hash).toBe('newhash');
  });

  it('should delete a user', async () => {
    const created = await repository.create({
      username: 'testuser',
      password_hash: 'hashedpassword',
    });

    const deleted = await repository.delete(created.id!);
    expect(deleted).toBe(true);

    const found = await repository.findById(created.id!);
    expect(found).toBeNull();
  });

  it('should return all users', async () => {
    await repository.create({
      username: 'user1',
      password_hash: 'hash1',
    });
    await repository.create({
      username: 'user2',
      password_hash: 'hash2',
    });

    const all = await repository.findAll();
    expect(all.length).toBe(2);
  });

  it('should not allow duplicate usernames', async () => {
    await repository.create({
      username: 'testuser',
      password_hash: 'hash1',
    });

    await expect(
      repository.create({
        username: 'testuser',
        password_hash: 'hash2',
      })
    ).rejects.toThrow();
  });
});