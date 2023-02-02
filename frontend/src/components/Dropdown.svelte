<script>
  export let title = 'Dropdown';
</script>

<div class="dropdown inline-block relative">
  <button class="border-solid border-gray-800 border  font-semibold py-2 px-4 rounded inline-flex items-center">
    <span class="mr-4 text-sm">{title}</span>
    <slot name="icon" />
  </button>
  <ul class="dropdown-menu absolute hidden text-gray-700 pt-1">
    <!-- <li class=""><a class="rounded-t bg-gray-200 hover:bg-gray-400 py-2 px-4 block whitespace-no-wrap" href="#">One</a></li>
    <li class=""><a class="bg-gray-200 hover:bg-gray-400 py-2 px-4 block whitespace-no-wrap" href="#">Two</a></li>
    <li class=""><a class="rounded-b bg-gray-200 hover:bg-gray-400 py-2 px-4 block whitespace-no-wrap" href="#">Three is the magic number</a></li> -->
    <slot />
  </ul>
</div>

<style>
  .dropdown:hover .dropdown-menu {
    display: block;
  }
</style>