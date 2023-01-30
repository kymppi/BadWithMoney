import { useEffect, useState } from 'preact/hooks';
import type { JSX } from 'preact/jsx-runtime';
import { authService } from '../state/auth';

export const UserInfo = ({ children }: { children: JSX.Element }) => {
  const context = authService.machine.context;
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    setIsReady(true);
    console.log(context);
  }, []);

  if (isReady && context.loggedIn && context.user) {
    return (
      <div>
        <p>{context.user.name}</p>
      </div>
    );
  }

  return children;
};
