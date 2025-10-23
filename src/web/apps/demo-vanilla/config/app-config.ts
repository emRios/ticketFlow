// App configuration for dev: FSM and permissions

export type BoardScope = 'assigned' | 'team' | 'all';

export type AppConfig = {
  permissions: {
    // Scopes in which moving cards is allowed
    movableScopes: BoardScope[];
    // If true, agents can move tickets not assigned to them
    agentCanMoveUnassigned: boolean;
    // Whether admins can always move regardless of scope
    adminBypass: boolean;
  };
  fsm: Record<string, string[]>; // status -> allowed next statuses (Spanish)
};

export const appConfig: AppConfig = {
  permissions: {
    movableScopes: ['assigned'],
    agentCanMoveUnassigned: false,
    adminBypass: true
  },
  fsm: {
    'nuevo': ['en-proceso', 'en-espera'],
    'en-proceso': ['en-espera', 'resuelto'],
    'en-espera': ['en-proceso', 'resuelto'],
    'resuelto': []
  }
};
