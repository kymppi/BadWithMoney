import { map } from 'nanostores';

export interface User {
  name: string;
  id: string;
  email: string;
  loggedIn: boolean;
}

export const user = map<User>({
  name: '',
  id: '',
  loggedIn: false,
});
