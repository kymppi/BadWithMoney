import { useStore } from '@nanostores/preact';
import { user } from '../state/user';

export const UserInfo = () => {
  const userInfo = useStore(user);

  return (
    <div>
      <p>{userInfo.name || 'wassup'}</p>
    </div>
  );
};
