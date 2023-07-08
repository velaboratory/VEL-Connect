<script lang="ts">
	import { currentDevice, currentUser, pb, type DeviceData, pairedDevices } from '$lib/js/velconnect';

	let pairingCode: string;
	let errorMessage: string | null;

	async function pair() {
		// find the device by pairing code
		try {
			let device = (await pb
				.collection('Device')
				.getFirstListItem(`pairing_code="${pairingCode}"`)) as DeviceData;

			// add it to the local data
			currentDevice.set(device.id);
			pairedDevices.set([...$pairedDevices, device.id]);

			// add it to my account if logged in
			if ($currentUser) {
				$currentUser.devices.push(device.id);
				await pb.collection('Users').update($currentUser.id, $currentUser);
			}
			errorMessage = null;
		} catch (e) {
			if (e == "ClientResponseError 404: The requested resource wasn't found.") {
				errorMessage = 'Device not found with this pairing code.';
			}
			console.error('Not found: ' + e);
		}
	}
</script>

<input bind:value={pairingCode} placeholder="Paring Code" />
<button on:click={pair}>Pair</button>
{#if errorMessage}
	<p>{errorMessage}</p>
{/if}
