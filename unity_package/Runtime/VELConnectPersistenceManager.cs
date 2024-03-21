using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using VelNet;

namespace VELConnect
{
	public class VelConnectPersistenceManager : MonoBehaviour
	{
		public static VelConnectPersistenceManager instance;

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
			}
		}

		// We don't need to register objects, because they will do that automatically when they spawn if they have the VelNetPersist component
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