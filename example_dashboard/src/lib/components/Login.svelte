<script lang="ts">
	import { currentUser, pb } from '../velconnect';

	let email: string;
	let password: string;

	async function login() {
		const user = await pb.collection('Users').authWithPassword(email, password);
		console.log(user);
	}

	async function signUp() {
		try {
			const data = {
				email,
				password,
				passwordConfirm: password
			};
			const createdUser = await pb.collection('Users').create(data);
			await login();
		} catch (err) {
			console.error(err);
		}
	}

	function signOut() {
		pb.authStore.clear();
	}
</script>

{#if $currentUser}
	<p>
		Signed in as {$currentUser.email}
		<button on:click={signOut}>Sign Out</button>
	</p>
{:else}
	<form on:submit|preventDefault>
		<input placeholder="Email" type="text" bind:value={email} />

		<input placeholder="Password" type="password" bind:value={password} />
		<button on:click={signUp}>Sign Up</button>
		<button on:click={login}>Login</button>
	</form>
{/if}
