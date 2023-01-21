import { map } from 'nanostores';

export interface User {
  name: string;
  loggedIn: boolean;
}

export const user = map<User>({
  name: '',
  loggedIn: false,
});
