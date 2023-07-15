<script lang="ts">
	import {
		currentDeviceId,
		currentUser,
		delayedSend,
		deviceData,
		deviceFields,
		pairedDevices,
		removeDevice,
		roomData,
		sending,
		startListening,
		stopListening
	} from '@velaboratory/velconnect-svelte';
	import Login from '$lib/components/Login.svelte';
	import Pair from '$lib/components/Pair.svelte';
	import { prettyDate } from '$lib/js/util';
	import { onDestroy, onMount } from 'svelte';

	onMount(async () => {
		await startListening('http://localhost:8090');
	});
	onDestroy(() => {
		stopListening();
	});
</script>

<svelte:head>
	<title>VEL-Connect</title>
</svelte:head>

<h1>VEL-Connect</h1>
<img src="/img/velconnect_logo_1.png" alt="logo" width="70px" height="28px" />
<p>
	This is a demo dashboard. Visit the <a
		href="https://github.com/velaboratory/VEL-Connect"
		target="_blank">GitHub repo</a
	> to copy it and make your own.
</p>

<Login />
<Pair />

<div>
	<h3>Devices:</h3>
	<div class="device-list">
		{#each $pairedDevices as d}
			<div>
				<button
					on:click={() => {
						currentDeviceId.set(d);
					}}>{d}</button
				>
				<button
					on:click={() => {
						removeDevice(d);
					}}>x</button
				>
			</div>
		{/each}
	</div>
	{#if $pairedDevices.length == 0}
		<p>No devices paired. Enter a pairing code above.</p>
	{/if}
</div>

{#if sending}
	<div>
		<progress />
	</div>
{/if}

{#if $deviceFields != null && $deviceData != null}
	<div>
		<h3>Device Info</h3>

		<device-field>
			<h6>Device ID:</h6>
			<code>{$deviceFields.id}</code>
		</device-field>
		<device-field>
			<h6>Pairing Code:</h6>
			<code>{$deviceFields.pairing_code}</code>
		</device-field>
		<device-field>
			<h6>First Seen</h6>
			<p>{prettyDate($deviceFields.created)}</p>
		</device-field>
		<device-field>
			<h6>Last Seen</h6>
			<p>{prettyDate($deviceFields.updated)}</p>
		</device-field>
	</div>

	<div>
		<h3>Settings</h3>

		<label>
			User Name
			<input
				type="text"
				placeholder="Enter username..."
				bind:value={$deviceFields.friendly_name}
				on:input={delayedSend}
			/>
		</label>

		<label>
			Avatar URL
			<a href="https://demo.readyplayer.me" target="blank">
				Create New Avatar
				<svg style="width:1em;height:1em;margin-bottom:-.15em;" viewBox="0 0 24 24">
					<path
						fill="currentColor"
						d="M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3M19,19H5V5H12V3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V12H19V19Z"
					/>
				</svg>
			</a>
			<input
				type="text"
				placeholder="https://----.glb"
				bind:value={$deviceData.data.avatar_url}
				on:input={delayedSend}
			/>
		</label>

		<device-field>
			<h6>Current Room</h6>
			<a href="/join/{$deviceFields.current_app}/room_name" target="blank">
				Shareable Link
				<svg style="width:1em;height:1em;margin-bottom:-.15em;" viewBox="0 0 24 24">
					<path
						fill="currentColor"
						d="M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3M19,19H5V5H12V3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V12H19V19Z"
					/>
				</svg>
			</a>
			<input
				type="text"
				placeholder="room_1"
				bind:value={$deviceFields.current_room}
				on:input={delayedSend}
			/>
		</device-field>
	</div>

	<h3>Raw JSON:</h3>
	<h6>User</h6>
	<pre><code>{JSON.stringify($currentUser, null, 2)}</code></pre>
	<h6>Device</h6>
	<pre><code>{JSON.stringify($deviceFields, null, 2)}</code></pre>
	<h6>Device Data</h6>
	<pre><code>{JSON.stringify($deviceData, null, 2)}</code></pre>
	<h6>Room Data</h6>
	<pre><code>{JSON.stringify($roomData, null, 2)}</code></pre>
{/if}

<style lang="scss">
	device-field {
		display: flex;
		flex-direction: column;
		gap: 0em;
		width: fit-content;
		p {
			margin: 0;
		}
	}

	.device-list {
		display: flex;
		flex-direction: column;
		width: fit-content;
		gap: 0.5em;

		& > div {
			display: flex;
			gap: 0.2em;
			& > button:first-child {
				flex-grow: 1;
			}
		}
	}
</style>
