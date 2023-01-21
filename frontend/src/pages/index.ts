import type { APIRoute } from 'astro';

export const get: APIRoute = ({ redirect }) => {
  return redirect('/home', 301);
};
