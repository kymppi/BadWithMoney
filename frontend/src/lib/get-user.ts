import type { User } from '../state/auth';

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
