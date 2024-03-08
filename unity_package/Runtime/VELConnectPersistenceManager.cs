using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using VELConnect;
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
			if (VelNetManager.Players.Count == 0)
			{
				string spawnedObjects = VELConnectManager.GetRoomData("spawned_objects", "[]");
				List<string> spawnedObjectList = JsonConvert.DeserializeObject<List<string>>(spawnedObjects);
				List<NetworkObject> spawnedNetworkObjects = new List<NetworkObject>();
				GetSpawnedObjectData(spawnedObjectList, (list) =>
				{
					foreach (SpawnedObjectData obj in list)
					{
						NetworkObject spawnedObj = spawnedNetworkObjects.Find(i => i.networkId == obj.networkId);
						if (spawnedObj == null)
						{
							spawnedObj = VelNetManager.NetworkInstantiate(obj.prefabName);
							spawnedNetworkObjects.Add(spawnedObj);
						}

						spawnedObj.syncedComponents[obj.componentIdx].ReceiveBytes(Convert.FromBase64String(obj.base64ObjectData));
					}
				});
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

		public static void RegisterObject(NetworkObject obj)
		{
			instance.StartCoroutine(instance.RegisterObjectCo(obj));
		}

		private IEnumerator RegisterObjectCo(NetworkObject obj)
		{
			// upload all the persisted components, then add those components to the room data
			VelNetPersist[] persistedComponents = obj.GetComponents<VelNetPersist>();
			List<VELConnectManager.State.DataBlock> responses = new List<VELConnectManager.State.DataBlock>();
			double startTime = Time.timeAsDouble;
			foreach (VelNetPersist velNetPersist in persistedComponents)
			{
				velNetPersist.Save(s => { responses.Add(s); });
			}

			while (responses.Count < persistedComponents.Length && Time.timeAsDouble - startTime < 5)
			{
				yield return null;
			}

			VELConnectManager.SetRoomData("spawned_objects", JsonConvert.SerializeObject(responses.Select(i => i.block_id).ToList()));
		}
	}
}