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
  /** @xstate-layout N4IgpgJg5mDOIC5QEMCuAXAFgOgDIHsoYIACASQDsBiXAeQHFaBVAFQG0AGAXUVAAd8sAJboh+CrxAAPRAHYAbAFZsAZlmKOigJwAWRToBMARiOKANCACeiABxHsGjk-U2NWrUYC+ni2izYAOXx0EgIiSHJqAEFWAAkAUQCWMgBhKJZ4zh4kEAFhUXFJGQR5WQNsG10DFSMbAzq9HQtrBCN5eWw9MvkbWRUtJ0Vq718MHAJkCCEKKBIAEWR0ZCoIcTBsaYA3fABrdYAnMEmmWDB9haWsyTyRMQkc4pVFWWwOHSNZDhtKmx0OLXUzUQ1XsQy+Q2+WlcAx0IxAfnG+Em01mF2WZ32+H22D4ABtFgAzLEAW2wh2Op3Oi2QVxyNwK91Aj2er3en0hv3+gKsiCMBmU73adRsaiUdVk3h8IAo+AgcEkCOugluhQeiAAtPIgQh1cp3PrtJyAX04Qi8IRiJElfk7kVbB5sB5SqV1GVFCoDNqTPYdPqtEN+vIBtpTWNAsFQhaIpRrSrGdJEIpFPY6gYASpenyVBwVF6DDpOm0XbJ3jp5P8tKH-BMpjN5tTYwy7QhDNrjMpHG8MzYOJ9FPJJZ4gA */
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
          exit: 'logout',
          on: {
            LOGOUT: 'Not Logged In',
          },
        },

        'Not Logged In': {
          on: {
            AUTHENTICATE: 'Logged In',
          },
        },

        'Loading Data': {
          invoke: {
            id: 'readUserData',
            src: 'readUserData',
            onDone: {
              target: 'Logged In',
              actions: 'saveUser',
            },
            onError: {
              target: 'Not Logged In',
            },
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
        saveUser: (context, event) => {
          context.user = event.data as User;
          context.loggedIn = true;
        },
      },
      services: {
        readUserData: (context, event) => {
          return new Promise((resolve, reject) => {
            if (typeof window !== 'undefined') {
              const dataFromServer = getDataFromServer<User>(
                'data-temp-user-store'
              );
              if (dataFromServer && dataFromServer.id && !context.loggedIn) {
                resolve(dataFromServer);
              } else {
                reject();
              }
            }
          });
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
}).onTransition((state) => {
  if (state.changed) {
    console.log(state.value);
  }
});

authService.start();
