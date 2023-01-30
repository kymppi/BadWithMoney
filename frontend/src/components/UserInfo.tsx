import { useMachine } from '@xstate/react';
import { useEffect, useState } from 'preact/hooks';
import type { JSX } from 'preact/jsx-runtime';
import { authMachine } from '../state/auth';

export const UserInfo = ({ children }: { children: JSX.Element }) => {
  const [state, send] = useMachine(authMachine);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    setIsReady(true);
    console.log(state.matches('Logged In'));
  }, []);

  if (isReady && state.matches('Logged In')) {
    return (
      <div>
        <p>{state.context.user?.name}</p>
        <button onClick={() => send('logout')}>Logout</button>
      </div>
    );
  }

  return children;
};
