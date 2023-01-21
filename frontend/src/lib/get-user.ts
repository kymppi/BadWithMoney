import type { User } from '../state/user';

export const getUser = async (cookies: string | null): Promise<User> => {
  const user = await fetch('http://localhost:3000/api/claims', {
    headers: {
      Cookie: cookies || '',
    },
  })
    .then((data) => data.json())
    .catch(() => []);

  if (user.length > 0) {
    // item2 should be the id
    if (user[0].Item2) {
      return {
        loggedIn: true,
        id: user[0].Item2,
        name: user[1].Item2,
      };
    }
  }

  return {
    loggedIn: false,
    id: '',
    name: '',
  };
};
