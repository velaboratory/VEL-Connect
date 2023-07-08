import PocketBase from 'pocketbase';
import { writable } from 'svelte/store';
import { type Record } from 'pocketbase';

export const pb = new PocketBase('http://127.0.0.1:8090');

export const currentUser = writable(pb.authStore.model);

pb.authStore.onChange((auth) => {
	console.log('authStore changed', auth);
	currentUser.set(pb.authStore.model);
});

let pairedDevicesInit: string[] = [];
export const pairedDevices = writable(pairedDevicesInit);
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
