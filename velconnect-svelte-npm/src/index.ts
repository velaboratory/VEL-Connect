import PocketBase from "pocketbase";
import { writable } from "svelte/store";
import type { Record } from "pocketbase";
import { get } from "svelte/store";

export const pb = new PocketBase();

export const currentUser = writable(pb.authStore.model);

pb.authStore.onChange((auth) => {
  console.log("authStore changed", auth);
  currentUser.set(pb.authStore.model);
  if (pb.authStore.isValid) {
  }
});

export const pairedDevices = writable<string[]>([]);
export const currentDeviceId = writable("");

export interface Device extends Record {
  os_info: string;
  friendly_name: string;
  current_room: string;
  current_app: string;
  pairing_code: string;
  data: string;
  expand: { data?: DataBlock };
}
export interface DataBlock extends Record {
  data: { [key: string]: string };
}

// const device = get(currentDevice);
// if (device == '' && device.length > 0) {
// 	currentDevice.set(device[0]);
// }

let unsubscribeDeviceFields: () => void;
let unsubscribeDeviceData: () => void;
let unsubscribeRoomData: () => void;
let unsubscribeCurrentDevice: () => void;
let unsubscribeCurrentUser: () => void;

export let deviceFields = writable<Device | null>(null);
export let deviceData = writable<DataBlock | null>(null);
export let roomData = writable<DataBlock | null>(null);

export let sending = false;

export async function startListening(baseUrl: string) {
  pb.baseUrl = baseUrl;
  if (get(currentDeviceId) != "") {
    const d = (await pb.collection("Device").getOne(get(currentDeviceId), {
      expand: "data",
    })) as Device;
    deviceFields.set(d);
    deviceData.set(d.expand.data as DataBlock);
  }

  unsubscribeCurrentDevice = currentDeviceId.subscribe(async (val) => {
    console.log("current device changed");
    unsubscribeDeviceFields?.();
    unsubscribeDeviceData?.();
    if (val != "") {
      const d = (await pb
        .collection("Device")
        .getOne(get(currentDeviceId), { expand: "data" })) as Device;
      deviceFields.set(d);
      deviceData.set(d.expand.data as DataBlock);

      unsubscribeDeviceFields = await pb
        .collection("Device")
        .subscribe(val, async (data) => {
          const d = data.record as Device;
          deviceFields.set(d);

          unsubscribeDeviceData = await pb
            .collection("DataBlock")
            .subscribe(d.id, async (data) => {
              deviceData.set(data.record as DataBlock);
            });

          getRoomData(d);
        });

      if (d != null) getRoomData(d);
    } else {
      deviceFields.set(null);
      deviceData.set(null);
      roomData.set(null);
    }
  });

  unsubscribeCurrentUser = currentUser.subscribe((user) => {
    pairedDevices.set(user?.["devices"] ?? []);
    currentDeviceId.set(get(pairedDevices)[0] ?? "");
  });
}

export function stopListening() {
  unsubscribeCurrentDevice?.();
  unsubscribeDeviceFields?.();
  unsubscribeDeviceData?.();
  unsubscribeRoomData?.();
  unsubscribeCurrentUser?.();
  console.log("Stop listening");
}

async function getRoomData(device: Device) {
  unsubscribeRoomData?.();

  // create or just fetch room by name
  const r = (await pb
    .collection("DataBlock")
    .getFirstListItem(
      `block_id="${device.current_app}_${device.current_room}"`
    )) as DataBlock;
  roomData.set(r);
  if (r) {
    unsubscribeRoomData = await pb
      .collection("DataBlock")
      .subscribe(r.id, (data) => {
        roomData.set(data.record as DataBlock);
      });
  } else {
    console.error("Failed to get or create room");
  }
}

let abortController = new AbortController();
export function delayedSend() {
  console.log("fn: delayedSend()");

  // abort the previous send
  abortController.abort();
  const newAbortController = new AbortController();
  abortController = newAbortController;
  setTimeout(() => {
    if (!newAbortController.signal.aborted) {
      send();
    } else {
      console.log("aborted");
    }
  }, 1000);
}

export function send() {
  console.log("sending...");
  sending = true;
  let promises: Promise<any>[] = [];
  const room = get(roomData);
  const device = get(deviceFields);
  const data = get(deviceData);
  if (device) {
    promises.push(pb.collection("Device").update(device.id, device));
  }
  if (data) {
    promises.push(pb.collection("DataBlock").update(data.id, data));
  }
  if (room) {
    promises.push(pb.collection("DataBlock").update(room.id, room));
  }
  Promise.all(promises).then(() => {
    sending = false;
  });
}

export function removeDevice(d: string) {
  pairedDevices.set(get(pairedDevices).filter((i) => i != d));

  if (get(currentDeviceId) == d) {
    console.log("Removed current device");

    // if there are still devices left
    if (get(pairedDevices).length > 0) {
      currentDeviceId.set(get(pairedDevices)[0] ?? "");
    } else {
      currentDeviceId.set("");
    }
  }

  const user = get(currentUser);
  if (user) {
    user["devices"] = user["devices"].filter((i: string) => i != d);
    pb.collection("Users").update(user.id, user);
  }
}

export async function pair(pairingCode: string) {
  try {
    // find the device by pairing code
    const device = (await pb
      .collection("Device")
      .getFirstListItem(`pairing_code="${pairingCode}"`)) as Device;

    // add it to the local data
    currentDeviceId.set(device.id);
    pairedDevices.set([...get(pairedDevices), device.id]);

    // add it to my account if logged in
    const u = get(currentUser);
    if (u) {
      // add the device to the user's devices
      u["devices"].push(device.id);

      // add the account data to the device
      if (
        u.user_data == null ||
        u.user_data == undefined ||
        u.user_data == ""
      ) {
        // create a new user data block if it doesn't exist on the user already
        const userDataBlock = await pb.collection("DataBlock").create({
          category: "device",
          data: {},
          owner: u.id,
        });
        u.user_data = userDataBlock.id;
      }
      device["data"] = u.user_data;
      device["owner"] = u.id;
      device["past_owners"] = [...device["past_owners"], u.id];

      await pb.collection("Device").update(device.id, device);
      await pb.collection("Users").update(u.id, u);
    }

    return { error: null };
  } catch (e) {
    console.error("Not found: " + e);
    if (e == "ClientResponseError 404: The requested resource wasn't found.") {
      return {
        error: "Device not found with this pairing code.",
      };
    }
    return {
      error: e as string,
    };
  }
}

export async function login(username: string, password: string) {
  try {
    await pb.collection("Users").authWithPassword(username, password);
    return "";
  } catch (err) {
    return err as string;
  }
}

export async function signUp(username: string, password: string) {
  try {
    const data = {
      email: username,
      password,
      passwordConfirm: password,
    };
    await pb.collection("Users").create(data);
    return await login(username, password);
  } catch (err) {
    return err as string;
  }
}

export function signOut() {
  pb.authStore.clear();
}