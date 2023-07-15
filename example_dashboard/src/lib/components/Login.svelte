<script lang="ts">
	import { currentUser, signUp, signOut, login } from '@velaboratory/velconnect-svelte';

	let username: string;
	let password: string;
	let errorMessage = '';
</script>

{#if $currentUser}
	<p>
		Signed in as {$currentUser.username}
		<button on:click={signOut}>Sign Out</button>
	</p>
{:else}
	<form on:submit|preventDefault>
		<input placeholder="Username" type="text" bind:value={username} />

		<input placeholder="Password" type="password" bind:value={password} />
		<button
			on:click={() => {
				signUp(username, password);
			}}>Sign Up</button
		>
		<button
			on:click={() => {
				login(username, password);
			}}>Login</button
		>
		<p class="error">{errorMessage}</p>
	</form>
{/if}

<style>
</style>
