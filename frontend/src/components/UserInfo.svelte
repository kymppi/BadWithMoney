<script>
  import { readable } from 'svelte/store';
  import { authService } from '../state/auth/auth';

  const state = readable(authService.getSnapshot(), (set) => {
    return authService.subscribe((state) => {
      if (state.changed) {
        set(state);
      }
    }).unsubscribe;
  });
</script>

<div>
  {#if $state.matches('Logged In')}
    <p>Welcome, {$state.context.user.name}!</p>
    <button on:click={() => authService.send('LOGOUT')}>Log out</button>
  {:else}
    <button on:click={() => authService.send('AUTHENTICATE')}>Log in</button>
  {/if}
</div>
