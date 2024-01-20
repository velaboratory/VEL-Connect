import { pb } from '@velaboratory/velconnect';
import type { UsersResponse } from '@velaboratory/velconnect/src/types/pocketbase-types';
import { writable } from 'svelte/store';

export const userModel = writable(pb.authStore.model);
export const userData = writable<UsersResponse | null>();
pb.authStore.onChange(async (token, model) => {
	userModel.set(model);
	if (model) {
		const u = await pb.collection('Users').getOne(model.id, { expand: 'devices,profiles' });
		console.log(u);
		userData.set(u);
	} else {
		userData.set(null);
	}
}, true);
