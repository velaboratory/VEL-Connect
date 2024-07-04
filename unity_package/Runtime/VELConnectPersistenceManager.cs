using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using VelNet;

namespace VELConnect
{
	public class VelConnectPersistenceManager : MonoBehaviour
	{
		public static VelConnectPersistenceManager instance;

		// The items in this list are used when loading existing data from the server on room join
		private List<VelNetPersist> sceneObjects = new List<VelNetPersist>();

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
			StartCoroutine(OnJoinedRoomCo(roomName));
		}

		private IEnumerator OnJoinedRoomCo(string roomName)
		{
			foreach (VelNetPersist velNetPersist in sceneObjects)
			{
				velNetPersist.loading = true;
			}

			int pagesLeft = 1;
			int totalCounter = 200;
			int page = 1;
			List<VELConnectManager.PersistObject> allResults = new List<VELConnectManager.PersistObject>();
			while (pagesLeft > 0)
			{
				if (totalCounter < 0)
				{
					Debug.LogError("Too many pages of persisted objects. This must be a bug.");
					break;
				}

				using UnityWebRequest webRequest =
					UnityWebRequest.Get(VELConnectManager.VelConnectUrl + $"/api/collections/PersistObject/records?filter=(app='{Application.productName}')&page={page++}");
				yield return webRequest.SendWebRequest();

				switch (webRequest.result)
				{
					case UnityWebRequest.Result.ConnectionError:
					case UnityWebRequest.Result.DataProcessingError:
					case UnityWebRequest.Result.ProtocolError:
						Debug.LogError("Error: " + webRequest.error + "\n" + Environment.StackTrace);
						Debug.LogError("Failed to get persisted spawned objects");
						yield break;
						break;
					case UnityWebRequest.Result.Success:
						string text = webRequest.downloadHandler.text;
						VELConnectManager.RecordList<VELConnectManager.PersistObject> obj =
							JsonConvert.DeserializeObject<VELConnectManager.RecordList<VELConnectManager.PersistObject>>(text);
						allResults.AddRange(obj.items);
						pagesLeft = obj.totalPages - obj.page;
						totalCounter--;
						break;
				}
			}

			// Spawn items if we're the first to join this room
			if (VelNetManager.Players.Count == 1)
			{
				List<VELConnectManager.PersistObject> spawnedItems = allResults.Where(i => i.spawned && i.room == VelNetManager.Room).ToList();
				foreach (VELConnectManager.PersistObject persistObject in spawnedItems)
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
			}

			// load data for scene objects
			List<VELConnectManager.PersistObject> sceneObjectData = allResults.Where(i => i.room == VelNetManager.Room).ToList();
			foreach (VelNetPersist velNetPersist in sceneObjects)
			{
				List<VELConnectManager.PersistObject> thisObjectData = sceneObjectData.Where(i => i.network_id == velNetPersist.networkObject.sceneNetworkId.ToString()).ToList();
				switch (thisObjectData.Count)
				{
					case < 1:
						Debug.LogError($"[VelNetPersist] No data found for {velNetPersist.name} (network_id='{velNetPersist.networkObject.sceneNetworkId}')");
						velNetPersist.loading = false;
						continue;
					case > 1:
						Debug.LogError(
							$"[VelNetPersist] Multiple records found for app='{Application.productName}' && room='{VelNetManager.Room}' && network_id='{velNetPersist.networkObject.sceneNetworkId}'. Deleting all but the first one.");
						IEnumerable<VELConnectManager.PersistObject> toDelete = thisObjectData.Skip(1);
						foreach (VELConnectManager.PersistObject persistObject in toDelete)
						{
							VELConnectManager.PostRequestCallback(VELConnectManager.VelConnectUrl + "/api/collections/PersistObject/records/" + persistObject.id, null, null, null,
								Debug.LogError, method: "DELETE");
						}

						break;
				}

				velNetPersist.LoadData(thisObjectData.FirstOrDefault());
			}
		}

		public static void RegisterSceneObject(VelNetPersist obj)
		{
			instance.sceneObjects.Add(obj);
		}

		public static void UnregisterSceneObject(VelNetPersist obj)
		{
			instance.sceneObjects.Remove(obj);
		}

		// We don't need to register objects, because they will do that automatically when they spawn if they have the VelNetPersist component
		// We need to unregister objects when they are destroyed because destroying could happen because we left the scene (which shouldn't delete it from the server)
		public static void DestroySpawnedObject(NetworkObject obj)
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