<script lang="ts">
	import { pair } from '@velaboratory/velconnect-svelte';

	let pairingCode: string;
	let errorMessage: string | null;

	async function pairClick() {
		const ret = await pair(pairingCode);
		errorMessage = ret.error ?? '';
	}
</script>

<input bind:value={pairingCode} placeholder="Paring Code" />
<button on:click={pairClick}>Pair</button>
{#if errorMessage}
	<p>{errorMessage}</p>
{/if}
