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

<section class="flex border-2 border-solid border-black p-2">
  {#if $state.matches('Logged In')}
    <h1 class="mr-4 items-center justify-center">{$state.context.user.email}</h1>
      <button on:click={() => authService.send('LOGOUT')}><slot name="icon" /></button>
  {:else}
    <button on:click={() => authService.send('AUTHENTICATE')}>Log in</button>
  {/if}
</section>
