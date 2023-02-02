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
  restored?: boolean;
}

export type AuthEvent =
  | { type: 'AUTHENTICATE' }
  | { type: 'LOGOUT' }
  | { type: 'READ_USER_DATA' }
  | { type: 'User verified' };

const createAuthMachine = (context: AuthContext) => {
  /** @xstate-layout N4IgpgJg5mDOIC5QEMCuAXAFgOgDIHsoYIACASQDsBiXAeQHFaBVAFQG0AGAXUVAAd8sAJboh+CrxAAPRAHYAHB2wBWACwBGAGwBOTco4AmDhzUAaEAE9EB9euzyAzE+fqO62R1UGAvt-NosbAA5fHQSAiJIcmoAQVYACQBRIJYyAGEYlkTOHiQQAWFRcUkZBFsHJU15WWUDeVVFNXllcysEG21sDkdnZWV1B0dffwwcAmQIIQooEgARZHRkKghxMGwpgDd8AGs1gCcwCaZYMD35xZzJApExCTzShSU1LV19IxNVVsQHdXlsTU86mU8m0P00shsymGIACY3wEymM3OS1Oe3we2wfAANgsAGbogC22AORxOZwWyEueWuRTuoAeihUGh0ekMxjMlmsmgM2GcTlUNVkqlUyhq0NheEIUER0SoVP4ghuxXuiHUBgq2FU2m1hn02nk3NkXwQoN5fJF2i1VXk4tGkqIMsocvUuQVhVuJVVygcsmwsm0WgcemUlq8n057Tq-x6DlFnjUeh8fhhdoASnB0OiwCQAGJogkkLH4ADGyCxAGVM3tkDAqKnEuWWLR6wB9WaZGLy-KK2me9rVaNC7Vq5qi0HGhp2eRq-SeUGOGy2wLp2BV7N5-AFoulitVmtgKhCCDbLs0j0q-u+qpDgN1Pr+hzGgw1Lo9DQeBrab2aXzJij4CA4EkWErh7c96UQABaTRjUg5RsG1RCkOQhwlzhSJSEoUD3WVCCEG5BxNR9WoNCDVRBm0Y1pxUYxjAFKo9BFG1kwlEIwgiYhomwpU6WkRBtBfBwDAMS0RO0QwfSfdROmBPk+gGIYWLtcZJmmOYKW43sL1UPRNSQwYDFFUUYIjQyeW6flxNsZ9tAMVQ0PtaU1Kw6kwNwviylUDhfX1L9wS8DhxP6J8DE0BC+SDXRbBMBz00mA5izCFh8BIeh8EILFswiKZNPAjz3GMf47K-ZREx9Oynw4QjyP5LRvIFYVYozLNc3zQsSzLSt0X3XL3NKLwn3kczaLcYV1WFIVf28IA */
  const m = createMachine(
    {
      id: 'auth',
      schema: {
        context: {} as AuthContext,
        events: {} as AuthEvent,
      },
      context,
      predictableActionArguments: true,
      initial: 'Restore From localStorage',
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

        'Restore From localStorage': {
          always: [
            {
              target: 'Logging In',
              cond: 'canRestoreData',
            },
            { target: 'Loading Data' },
          ],
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
          if (context.restored && context.user) return true;
          return !(context.user && context.loggedIn ? true : false);
        },
        isLoggedIn: (context, _event) => {
          return context.user && context.loggedIn ? true : false;
        },
        canRestoreData: (context, _event) => {
          return context.restored && context.user ? true : false;
        },
      },
    }
  );

  return m;
};

let context: AuthContext = { loggedIn: false, user: undefined };

// restore user from localStorage
try {
  const jsonState = localStorage.getItem('app-state');
  const parsed = JSON.parse(jsonState || '{}');
  context = { ...parsed, restored: true };
} catch (e) {
  // unable to read from localStorage
}

export const authMachine = createAuthMachine(context);

export const authService = interpret(authMachine, {
  devTools: true,
})
  .onTransition((state, event) => {
    const jsonState = JSON.stringify(state.context);

    if (state.context.loggedIn) {
      try {
        localStorage.setItem('app-state', jsonState);
      } catch (e) {
        // unable to save to localStorage
      }
    }
  })
  .start();
