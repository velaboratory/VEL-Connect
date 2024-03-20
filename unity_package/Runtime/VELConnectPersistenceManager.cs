using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using VelNet;

namespace VELConnect
{
	public class VelConnectPersistenceManager : MonoBehaviour
	{
		public static VelConnectPersistenceManager instance;

		public class SpawnedObjectData
		{
			public string prefabName;
			public string base64ObjectData;
			public string networkId;
			public int componentIdx;
		}

		private void Awake()
		{
			instance = this;
		}

		private void OnEnable()
		{
			VelNetManager.OnJoinedRoom += OnJoinedRoom;
		}

		private void OnDisable()
		{
			VelNetManager.OnJoinedRoom -= OnJoinedRoom;
		}

		private void OnJoinedRoom(string roomName)
		{
			// if we're the first to join this room
			if (VelNetManager.Players.Count == 1)
			{
				VELConnectManager.GetRequestCallback(
					VELConnectManager.VelConnectUrl +
					$"/api/collections/PersistObject/records?filter=(app='{Application.productName}')",
					s =>
					{
						VELConnectManager.RecordList<VELConnectManager.PersistObject> obj =
							JsonConvert.DeserializeObject<VELConnectManager.RecordList<VELConnectManager.PersistObject>>(s);
						obj.items = obj.items.Where(i => i.spawned && i.room == VelNetManager.Room).ToList();

						foreach (VELConnectManager.PersistObject persistObject in obj.items)
						{
							if (string.IsNullOrEmpty(persistObject.data))
							{
								Debug.LogError("Persisted object has no data");
								continue;
							}
							NetworkObject spawnedObj = VelNetManager.NetworkInstantiate(persistObject.name, Convert.FromBase64String(persistObject.data));
							VelNetPersist persist = spawnedObj.GetComponent<VelNetPersist>();

							persist.persistId = persistObject.id;
							persist.LoadData(persistObject);
						}
					}, s => { Debug.LogError("Failed to get persisted spawned objects", this); });

				//
				// string spawnedObjects = VELConnectManager.GetRoomData("spawned_objects", "[]");
				// List<string> spawnedObjectList = JsonConvert.DeserializeObject<List<string>>(spawnedObjects);
				// List<NetworkObject> spawnedNetworkObjects = new List<NetworkObject>();
				// GetSpawnedObjectData(spawnedObjectList, (list) =>
				// {
				// 	foreach (SpawnedObjectData obj in list)
				// 	{
				// 		NetworkObject spawnedObj = spawnedNetworkObjects.Find(i => i.networkId == obj.networkId);
				// 		if (spawnedObj == null)
				// 		{
				// 			spawnedObj = VelNetManager.NetworkInstantiate(obj.prefabName);
				// 			spawnedNetworkObjects.Add(spawnedObj);
				// 		}
				//
				// 		spawnedObj.syncedComponents[obj.componentIdx].ReceiveBytes(Convert.FromBase64String(obj.base64ObjectData));
				// 	}
				// });
			}
		}

		private class DataBlocksResponse
		{
			public List<VELConnectManager.State.DataBlock> items;
		}

		private static void GetSpawnedObjectData(List<string> spawnedObjectList, Action<List<SpawnedObjectData>> callback)
		{
			VELConnectManager.GetRequestCallback($"/api/collections/DataBlock/records?filter=({string.Join(" || ", "id=\"" + spawnedObjectList + "\"")})", (response) =>
			{
				DataBlocksResponse parsedResponse = JsonConvert.DeserializeObject<DataBlocksResponse>(response);
				callback(parsedResponse.items.Select(i => new SpawnedObjectData()
				{
					networkId = i.block_id.Split("_")[-1],
					componentIdx = int.Parse(i.block_id.Split("_").Last()),
					prefabName = i.TryGetData("name"),
					base64ObjectData = i.TryGetData("state")
				}).ToList());
			});
		}

		// We don't need to register objects, because they will do that automatically when they spawn if they have the VelNetPersist component
		// public static void RegisterObject(NetworkObject obj)
		// {
		// 	if (instance == null)
		// 	{
		// 		Debug.LogError("VelConnectPersistenceManager not found in scene");
		// 		return;
		// 	}
		//
		// 	VelNetPersist[] persistedComponents = obj.GetComponents<VelNetPersist>();
		// 	if (persistedComponents.Length > 1)
		// 	{
		// 		Debug.LogError("NetworkObject has more than one VelNetPersist component");
		// 	}
		//
		// 	foreach (VelNetPersist velNetPersist in persistedComponents)
		// 	{
		// 		velNetPersist.Save();
		// 	}
		// }

		// We need to unregister objects when they are destroyed because destroying could happen because we left the scene
		public static void UnregisterObject(NetworkObject obj)
		{
			VelNetPersist[] persistedComponents = obj.GetComponents<VelNetPersist>();
			if (persistedComponents.Length > 1)
			{
				Debug.LogError("NetworkObject has more than one VelNetPersist component");
			}

			foreach (VelNetPersist velNetPersist in persistedComponents)
			{
				velNetPersist.Delete();
			}
		}
	}
}