import { createMachine, interpret } from 'xstate';
import {
  logoutUser,
  readUserFromDOM,
  redirectToGoogleLogin,
} from '../../lib/user';

export interface User {
  name: string;
  id: string;
  email: string;
}

export interface AuthContext {
  loggedIn: boolean;
  user?: User;
}

export type AuthEvent =
  | { type: 'AUTHENTICATE' }
  | { type: 'LOGOUT' }
  | { type: 'READ_USER_DATA' }
  | { type: 'User verified' };

const createAuthMachine = (context: AuthContext) => {
  /** @xstate-layout N4IgpgJg5mDOIC5QEMCuAXAFgOgDIHsoYIACASQDsBiXAeQHFaBVAFQG0AGAXUVAAd8sAJboh+CrxAAPRAGYALAA5s8gKyqAnLIDssrdvmyAjABoQAT0QAmbRuxWdHVbMVXFHeRoC+Xs2izYAHL46CQERJDk1ACCrAASAKKBLGQAwtEsCZw8SCACwqLikjII2hza2EaK2gBs5eraRp7aZpYIVg4qtk0cGh41NdqKPn4YOATIEEIUUCQAIsjoyFQQ4mDY0wBu+ADW6wBOYJNMsGD7C0vZkvkiYhK5JS4VOu5lLvIGitWtiDVGKgNVEZZDUdBobKonCMQP5xvhJtNZhdlmd9vh9tg+AAbRYAM3RAFtsIdjqdzotkFdcjdCvdQI95DVKgNDPIOIMPFZ5D8EFUKr0jBwFDYahpwbJobC8IQoIiolQqfxBLcig9EBoavJsBohjY1BqrEZdDyrFp7BwjFYnOC2ZqrJKxtKiHLKAqjDklQU7sVEEZLRxsIMgYa2aoBooTWarf7NFz2fImj5fCAKPgIHBJLDrsraT6EABaGo8-NWJlChRaKpNVSKMXyB0BcLEKLZr2q+mIdSyFTOK2yU1c4Em6rMoXabSqNRKLQNnDBUJNyKUVsqunSTtA7CuU1fDjs8euE2dHUaS3GWSqN4ce3JqUTKYzeYUle5tUIeRWHnVrqnkFGP5uBKt6Ok2LprjS3pvjqWrTjUri9BqtSmBY1iGj+Z7ApeQocMMwEBAASpAQiHAAxqELD4CQ9D4IQWJgGEhDTC+kEdrygrdkMihqJeoofKWX5cdgl5qIomh+g4GjyPWSZAA */
  const m = createMachine(
    {
      id: 'auth',
      schema: {
        context: {} as AuthContext,
        events: {} as AuthEvent,
      },
      context,
      predictableActionArguments: true,
      initial: 'Loading Data',
      states: {
        'Logged In': {
          exit: 'logout',
          on: {
            LOGOUT: 'Not Logged In',
          },
        },

        'Not Logged In': {
          on: {
            AUTHENTICATE: { target: 'Logging In', cond: 'canLogin' },
          },
        },

        'Loading Data': {
          invoke: {
            id: 'readUserData',
            src: 'readUserData',
            onDone: {
              target: 'Logging In',
              actions: 'saveUser',
            },
            onError: {
              target: 'Not Logged In',
            },
          },
        },

        'Logging In': {
          always: [
            {
              target: 'Logged In',
              cond: 'isLoggedIn',
            },
            { target: 'Redirect To Google Login' },
          ],
        },

        'Redirect To Google Login': {
          entry: 'redirectToGoogleLogin',
        },
      },
      tsTypes: {} as import('./auth.typegen').Typegen0,
    },
    {
      actions: {
        logout: (context, event) => {
          logoutUser().then(() => {
            context.loggedIn = false;
            context.user = undefined;
          });
        },
        saveUser: (context, event) => {
          context.user = event.data as User;
          context.loggedIn = true;
        },
        redirectToGoogleLogin: (_context, _event) => redirectToGoogleLogin(),
      },
      services: {
        readUserData: (context, _event) => readUserFromDOM(context.loggedIn),
      },
      guards: {
        canLogin: (context, _event) => {
          return !(context.user && context.loggedIn ? true : false);
        },
        isLoggedIn: (context, _event) => {
          return context.user && context.loggedIn ? true : false;
        },
      },
    }
  );

  return m;
};

let context: AuthContext = { loggedIn: false, user: undefined };

export const authMachine = createAuthMachine(context);

export const authService = interpret(authMachine, {
  devTools: true,
}).start();
