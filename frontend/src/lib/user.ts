import type { User } from '../state/auth/auth';
import { getDataFromServer } from './get-data';

export const getUser = async (cookies: string | null): Promise<User> => {
  const user = await fetch('http://localhost:3000/api/me', {
    headers: {
      Cookie: cookies || '',
    },
  })
    .then((data) => data.json())
    .catch(() => []);

  if (user.id) {
    return {
      id: user.id,
      name: user.name,
      email: user.email,
    };
  }

  return {
    id: '',
    name: '',
    email: '',
  };
};

export const logoutUser = () =>
  fetch('/api/logout', {
    method: 'POST',
  });

export const redirectToGoogleLogin =
  typeof window !== 'undefined'
    ? () => {
        window.location.href =
          '/api/google-signin/';
      }
    : () => {};

export const readUserFromDOM = (loggedIn: boolean) =>
  new Promise<User>((resolve, reject) => {
    if (typeof window !== 'undefined') {
      const dataFromServer = getDataFromServer<User>('data-temp-user-store');
      if (dataFromServer && dataFromServer.id && !loggedIn) {
        resolve(dataFromServer);
      } else {
        reject();
      }
    }
  });
