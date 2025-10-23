import { apiFetch } from './apiClient';

export type AgentUser = { userId: string; username: string; email?: string; role?: string };
export type AssigneeOption = { id: string; name: string };

export async function listAgents(): Promise<AssigneeOption[]> {
  const users = await apiFetch<AgentUser[]>('/api/users?role=AGENT', { method: 'GET' });
  return users.map(u => ({ id: u.userId, name: u.username }));
}
