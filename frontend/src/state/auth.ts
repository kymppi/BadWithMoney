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

let context: AuthContext = { loggedIn: false };

if (typeof window !== 'undefined') {
  setTimeout(() => {
    const dataFromServer = getDataFromServer<User>('data-temp-user-store');
    console.log(dataFromServer);
    if (dataFromServer.email && dataFromServer.id && dataFromServer.name) {
      context = {
        loggedIn: true,
        user: dataFromServer,
      };
    }
  }, 200);
}
export const authMachine =
  /** @xstate-layout N4IgpgJg5mDOIC5QEMCuAXAFgOgDIHsoYIACASQDsBiAqfDAbQAYBdRUAB31gEt0f8FdiAAeiAEwAWcdgDsADgCM4gMwBWNQrUA2RdvEAaEAE9ES7GqZWmK+U20PtkyQF8XRtFmwA5fOhK0xOTUAIIYmGAU-ADGyOhgzGxIIFy8-ILCYgiKAJxM2No5sjkqisraavKy4tpGptmSOdjWVjmKGuKyKrKu7iCeOATIEDwUUCQAInHIVJQAbsgANjykU+jIicKpfAJCyVkAtAp1iCoqFkzySiqSPUziUjluHuF4+MOj42sz3yRgIjxYOhYJtktt0ntQFlcvILIomNJ5NJqoouioTggDp05JIykirk4Sm4+hR8BA4MIBltuDsMvtEAdyhjGXpsI08UVlPIKsVnv1XoFIMFqWldplTmomkjus5JJYercMXlmvIzt1ZJVbKo+QMfH4AoQgpQRbTIaJENIMVULKqbgi5aj2r0Xl4hiMxpNpiaIeKEM5sPI1LjtDZUUjFHLmeJYTk2opVVJZFYkU9iUA */
  createMachine({
    id: 'auth',
    schema: {
      context: {} as AuthContext,
    },
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
  }).withContext(context);

export const authService = interpret(authMachine).onTransition((state) => {
  if (state.changed) {
    console.log(state.value);
  }
});

authService.start();
