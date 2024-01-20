<script lang="ts">
	import { pb } from '@velaboratory/velconnect';
	import { userData, userModel } from '$lib/stores';

	let username: string;
	let password: string;
</script>

<svelte:head>
	<title>VEL-Connect</title>
	<meta name="description" content="VEL-Connect Example" />
</svelte:head>

<section>
	<h1>VEL-Connect</h1>

	<p></p>

	<div>
		{#if $userModel}
			<button
				on:click={() => {
					pb.authStore.clear();
				}}>Log out</button
			>
		{:else}
			<form
				on:submit|preventDefault={async () => {
					await pb.collection('Users').authWithPassword(username, password);
				}}
			>
				<label>Username<input bind:value={username} /></label>
				<label>Password<input bind:value={password} type="password" /></label>
				<button type="submit">Log in</button>
			</form>
		{/if}
	</div>

	{#if $userData}
		<div id="content">
			<h4>Devices</h4>
			<table>
				<tr>
					<th>ID</th>
					<th>Name</th>
					<th>Room</th>
				</tr>
				{#each $userData.expand?.devices as device}
					<tr>
						<td>{device.id}</td>
						<td>{device.friendly_name}</td>
						<td>{device.current_room}</td>
					</tr>
				{/each}
			</table>
			<h4>Profiles</h4>
			<table>
				<tr>
					<th>ID</th>
					<th>Data</th>
				</tr>
				{#each $userData.expand?.profiles as profile}
					<tr>
						<td>{profile.id}</td>
						<td><pre><code>{JSON.stringify(profile.data, null, 2)}</code></pre></td>
					</tr>
				{/each}
			</table>
		</div>
	{/if}
</section>

<style>
	section {
		display: flex;
		flex-direction: column;
		justify-content: center;
		align-items: center;
		flex: 0.6;
	}

	h1 {
		width: 100%;
	}
</style>
