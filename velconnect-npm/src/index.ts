import PocketBase from "pocketbase";
import { TypedPocketBase } from "./types/pocketbase-types";

type VelConnectOptions = {
  url?: string;
  debugLog?: boolean;
};

export let pb: TypedPocketBase = new PocketBase(
  "https://velconnect-v4.ugavel.com"
) as TypedPocketBase;
let debugLog = false;
let initialized = false;

export function initVelConnect(options: VelConnectOptions = {}) {
  if (options.debugLog) {
    debugLog = true;
  }

  if (!initialized) {
    pb = new PocketBase(
      options.url ? options.url : "https://velconnect-v4.ugavel.com"
    ) as TypedPocketBase;
    log(`Initialized velconnect on ${pb.baseUrl}`);
  }
  // pb.authStore.onChange((auth) => {
  //   console.log("authStore changed", auth);
  // });
  initialized = true;
}

export function signOut() {
  pb.authStore.clear();
}

export async function pair(pairingCode: string) {
  try {
    log("Pairing...");

    // find the device by pairing code
    const device = await pb
      .collection("Device")
      .getFirstListItem(`pairing_code="${pairingCode}"`);

    // add it to my account if logged in
    const u = pb.authStore.model;
    if (u) {
      // add the device to the user's devices
      u["devices"].push(device.id);

      // add the account data to the device
      if (
        u["user_data"] == null ||
        u["user_data"] == undefined ||
        u["user_data"] == ""
      ) {
        // create a new user data block if it doesn't exist on the user already
        const userDataBlock = await pb.collection("DataBlock").create({
          category: "device",
          data: {},
          owner: u["id"],
        });
        u["user_data"] = userDataBlock.id;
      }
      device["data"] = u["user_data"];
      device["owner"] = u["id"];
      device["past_owners"] = [...device["past_owners"], u["id"]];

      await pb.collection("Device").update(device.id, device);
      await pb.collection("Users").update(u["id"], u);
    }

    return { error: null, deviceId: device.id };
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
    const ret = await pb
      .collection("Users")
      .authWithPassword(username, password);
    return { ret };
  } catch (error: any) {
    return { error };
  }
}

export async function signUp(username: string, password: string) {
  try {
    const data = {
      username: username,
      password,
      passwordConfirm: password,
    };
    await pb.collection("Users").create(data);
    return await login(username, password);
  } catch (err: any) {
    return err;
  }
}

function log(msg: any) {
  if (debugLog) {
    console.log(msg);
  }
}
