import { useStore } from '@nanostores/preact';
import { useEffect, useState } from 'preact/hooks';
import type { JSX } from 'preact/jsx-runtime';
import { user } from '../state/user';

export const UserInfo = ({ children }: { children: JSX.Element }) => {
  const userInfo = useStore(user);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    setIsReady(true);
  }, []);

  if (isReady && userInfo.loggedIn) {
    return (
      <div>
        <p>{userInfo.name}</p>
      </div>
    );
  }

  return children;
};
