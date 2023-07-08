import PocketBase from 'pocketbase';
import { writable } from 'svelte/store';
import { type Record } from 'pocketbase';
import { get } from 'svelte/store';

export const pb = new PocketBase('http://127.0.0.1:8090');

export const currentUser = writable(pb.authStore.model);

pb.authStore.onChange((auth) => {
	console.log('authStore changed', auth);
	currentUser.set(pb.authStore.model);
});

export const pairedDevices = writable<string[]>([]);
export const currentDevice = writable('');

interface HasData extends Record {
	data: { [key: string]: string };
}
export interface DeviceData extends Record {
	current_room: string;
	current_app: string;
	data: { [key: string]: string };
}
export interface RoomData extends HasData {}

const device = get(currentDevice);
if (device == '' && device.length > 0) {
	currentDevice.set(device[0]);
}

let unsubscribeDeviceData: () => void;
let unsubscribeRoomData: () => void;
let unsubscribeCurrentDevice: () => void;
let unsubscribeCurrentUser: () => void;

export let deviceData = writable<DeviceData | null>(null);
export let roomData = writable<RoomData | null>(null);

export let sending = false;

export async function startListening() {
	if (get(currentDevice) != '') {
		deviceData = await pb.collection('Device').getOne(get(currentDevice));
	}

	unsubscribeCurrentDevice = currentDevice.subscribe(async (val) => {
		console.log('current device changed');
		unsubscribeDeviceData?.();
		if (val != '') {
			const d = (await pb.collection('Device').getOne(get(currentDevice))) as DeviceData;
			deviceData.set(d);
			if (d != null) getRoomData(d);
			unsubscribeDeviceData = await pb.collection('Device').subscribe(val, async (data) => {
				const d = data.record as DeviceData;
				deviceData.set(d);
				getRoomData(d);
			});
		} else {
			deviceData.set(null);
			roomData.set(null);
		}
	});

	unsubscribeCurrentUser = currentUser.subscribe((user) => {
		pairedDevices.set(user?.devices ?? []);
	});
}

export function stopListening() {
	unsubscribeCurrentDevice?.();
	unsubscribeDeviceData?.();
	unsubscribeRoomData?.();
	unsubscribeCurrentUser?.();
}

async function getRoomData(deviceData: DeviceData) {
	unsubscribeRoomData?.();

	// create or just fetch room by name
	const r = await fetch(
		`${pb.baseUrl}/data_block/${deviceData.current_app}_${deviceData.current_room}`,
		{
			method: 'POST'
		}
	).then((r) => r.json());
	roomData.set(r);
	if (r) {
		unsubscribeDeviceData = await pb.collection('DataBlock').subscribe(r.id, (data) => {
			roomData.set(data.record as RoomData);
			unsubscribeRoomData?.();
		});
	} else {
		console.error('Failed to get or create room');
	}
}

let abortController = new AbortController();
export function delayedSend() {
	console.log('fn: delayedSend()');

	// abort the previous send
	abortController.abort();
	const newAbortController = new AbortController();
	abortController = newAbortController;
	setTimeout(() => {
		if (!newAbortController.signal.aborted) {
			send();
		} else {
			console.log('aborted');
		}
	}, 1000);
}

export function send() {
	console.log('sending...');
	sending = true;
	let promises = [];
	const r = get(roomData);
	const d = get(deviceData);
	if (d) {
		promises.push(pb.collection('Device').update(d.id, d));
	}
	if (r) {
		promises.push(pb.collection('DataBlock').update(r.id, r));
	}
	Promise.all(promises).then(() => {
		sending = false;
	});
}

export function removeDevice(d: string) {
	pairedDevices.set(get(pairedDevices).filter((i) => i != d));

	if (get(currentDevice) == d) {
		console.log('Removed current device');

		// if there are still devices left
		if (get(pairedDevices).length > 0) {
			currentDevice.set(get(pairedDevices)[0]);
		} else {
			currentDevice.set('');
		}
	}

	const user = get(currentUser);
	if (user) {
		user.devices.filter((i: string) => i != d);
		pb.collection('Users').update(user.id, user);
	}
}
