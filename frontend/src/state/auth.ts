import { createMachine, interpret } from 'xstate';
import { getDataFromServer } from '../lib/get-data';

export interface User {
  name: string;
  id: string;
  email: string;
}

export interface AuthContext {
  loggedIn: boolean;
  user?: User;
}

const createAuthMachine = (context: AuthContext) => {
  /** @xstate-layout N4IgpgJg5mDOIC5QEMCuAXAFgOgDIHsoYIACASQDsBiAqfDAbQAYBdRUAB31gEt0f8FdiAAeiAKwAmAOzZJTABwBmACwqAnJPHilANnEAaEAE9ECgIzZxTG0yULJK-VJUBfV0bRZsAOXzoSWmJyagBBDEwwCn4AY2R0MGY2JBAuXn5BYTEEFUVsc2k7Jy0FdQdzFSNTBArxbAUHXSZddRVpBV1zd08IvHxkCB4KKBIAEXjkKkoAN2QAGx5ScfRkJOE0vgEhFOzzDux9XUldBslVBXF1KrNZRSV7k8cmLRVxbpAvHAIBoZHlyf+JDAIh4sHQsDWKQ2GW2oF2+0Ox1O50u1wQWkk2Gk1lsFlKCmk5l07g8IAo+AgcGEn3W3E2mR2iAAtLo0Uy6upOZzpOp2lpLkd3p8+kRICFaektllELp7AddArsQS9kpsWjzEo6rYbHt5AT9JIhb0-AEgmLKBL6bDRIh7kx6uYtEw9jyNSolGiCVZlA8HLkXm9ScLvoNhmMJpaYdKcpI0a1sNq7OZxLppJJ1CTXEA */
  const m = createMachine(
    {
      id: 'auth',
      schema: {
        context: {} as AuthContext,
      },
      context,
      predictableActionArguments: true,
      initial: 'Loading Data',
      states: {
        'Logged In': {
          on: {
            Logout: 'Not Logged In',
          },
        },

        'Not Logged In': {
          on: {
            Authenticate: 'Logged In',
          },
        },

        'Loading Data': {
          on: {
            'Invalid Data': 'Not Logged In',
            'Data exists': 'Logged In',
          },
        },
      },
      tsTypes: {} as import('./auth.typegen').Typegen0,
    },
    {
      actions: {
        logout: (context, event) => {
          console.log('logout');
        },
      },
    }
  );

  return m;
};

let context: AuthContext = { loggedIn: false, user: undefined };

if (typeof window !== 'undefined') {
  const dataFromServer = getDataFromServer<User>('data-temp-user-store');
  if (dataFromServer.id) {
    context = {
      loggedIn: true,
      user: dataFromServer,
    };
  }
}

export const authMachine = createAuthMachine(context);

export const authService = interpret(authMachine).onTransition((state) => {
  if (state.changed) {
    console.log(state.value);
  }
});

authService.start();
