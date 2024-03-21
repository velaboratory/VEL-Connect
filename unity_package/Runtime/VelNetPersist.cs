using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using VelNet;

namespace VELConnect
{
	public class VelNetPersist : NetworkComponent
	{
		private const float interval = 5f;
		private double nextUpdate;
		private bool loading;
		private const bool debugLogs = false;
		public string persistId;

		private void Update()
		{
			if (Time.timeAsDouble > nextUpdate && VelNetManager.InRoom && !loading)
			{
				nextUpdate = Time.timeAsDouble + interval + UnityEngine.Random.Range(0, interval);
				if (networkObject.IsMine)
				{
					Save();
				}
			}
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
			Load();
		}

		private void Load()
		{
			loading = true;
			if (debugLogs) Debug.Log($"[VelNetPersist] Loading {name}");

			if (networkObject.isSceneObject)
			{
				// It looks like a PocketBase bug is preventing full filtering from happening:
				// $"/api/collections/PersistObject/records?filter=(app='{Application.productName}' && room='{VelNetManager.Room}' && network_id='{networkObject.sceneNetworkId}')",
				VELConnectManager.GetRequestCallback(
					VELConnectManager.VelConnectUrl +
					$"/api/collections/PersistObject/records?filter=(app='{Application.productName}')",
					s =>
					{
						VELConnectManager.RecordList<VELConnectManager.PersistObject> obj =
							JsonConvert.DeserializeObject<VELConnectManager.RecordList<VELConnectManager.PersistObject>>(s);
						obj.items = obj.items.Where(i => i.network_id == networkObject.sceneNetworkId.ToString() && i.room == VelNetManager.Room).ToList();
						if (obj.items.Count < 1)
						{
							Debug.LogError("[VelNetPersist] No data found for " + name);
							loading = false;
							return;
						}
						else if (obj.items.Count > 1)
						{
							Debug.LogError(
								$"[VelNetPersist] Multiple records found for app='{Application.productName}' && room='{VelNetManager.Room}' && network_id='{networkObject.sceneNetworkId}'. Using the first one.");
						}

						LoadData(obj.items.FirstOrDefault());
					}, s => { loading = false; });
			}
			else
			{
				VELConnectManager.GetRequestCallback(VELConnectManager.VelConnectUrl + "/api/collections/PersistObject/records/" + persistId, s =>
					{
						VELConnectManager.PersistObject obj = JsonConvert.DeserializeObject<VELConnectManager.PersistObject>(s);
						LoadData(obj);
					},
					s => { loading = false; });
			}
		}

		public void LoadData(VELConnectManager.PersistObject obj)
		{
			if (string.IsNullOrEmpty(obj.data))
			{
				Debug.LogError($"[VelNetPersist] No data found for {name}");
				loading = false;
				return;
			}

			persistId = obj.id;

			using BinaryReader reader = new BinaryReader(new MemoryStream(Convert.FromBase64String(obj.data)));
			networkObject.UnpackState(reader);

			if (debugLogs) Debug.Log($"[VelNetPersist] Loaded {name}");
			loading = false;
		}


		public void Save(Action<VELConnectManager.PersistObject> successCallback = null)
		{
			if (debugLogs) Debug.Log($"[VelNetPersist] Saving {name}");

			List<SyncState> syncStateComponents = networkObject.syncedComponents.OfType<SyncState>().ToList();

			if (networkObject == null)
			{
				Debug.LogError("NetworkObject is null on SyncState", this);
				return;
			}

			using BinaryWriter writer = new BinaryWriter(new MemoryStream());
			networkObject.PackState(writer);
			string data = Convert.ToBase64String(((MemoryStream)writer.BaseStream).ToArray());

			// if we have a persistId, update the record, otherwise create a new one
			if (string.IsNullOrEmpty(persistId))
			{
				Debug.LogWarning($"We don't have an existing persistId, so we are creating a new record for {networkObject.name}");
				VELConnectManager.PostRequestCallback(VELConnectManager.VelConnectUrl + "/api/collections/PersistObject/records", JsonConvert.SerializeObject(
					new VELConnectManager.PersistObject()
					{
						app = Application.productName,
						room = VelNetManager.Room,
						network_id = networkObject.sceneNetworkId.ToString(),
						spawned = !networkObject.isSceneObject,
						name = networkObject.isSceneObject ? networkObject.name : networkObject.prefabName,
						data = data,
					}), null, s =>
				{
					VELConnectManager.PersistObject resp = JsonConvert.DeserializeObject<VELConnectManager.PersistObject>(s);
					persistId = resp.id;
					successCallback?.Invoke(resp);
				});
			}
			else
			{
				VELConnectManager.PostRequestCallback(VELConnectManager.VelConnectUrl + "/api/collections/PersistObject/records/" + persistId, JsonConvert.SerializeObject(
					new VELConnectManager.PersistObject()
					{
						app = Application.productName,
						room = VelNetManager.Room,
						network_id = networkObject.sceneNetworkId.ToString(),
						spawned = !networkObject.isSceneObject,
						name = networkObject.prefabName,
						data = data,
					}), null, s =>
				{
					VELConnectManager.PersistObject resp = JsonConvert.DeserializeObject<VELConnectManager.PersistObject>(s);
					successCallback?.Invoke(resp);
				}, method: "PATCH");
			}
		}

		public void Delete(Action<VELConnectManager.PersistObject> successCallback = null)
		{
			if (string.IsNullOrEmpty(persistId))
			{
				Debug.LogError("We can't delete an object that doesn't have a persistId");
				return;
			}

			VELConnectManager.PostRequestCallback(VELConnectManager.VelConnectUrl + "/api/collections/PersistObject/records/" + persistId, null, null,
				s =>
				{
					VELConnectManager.PersistObject resp = JsonConvert.DeserializeObject<VELConnectManager.PersistObject>(s);
					successCallback?.Invoke(resp);
				}, Debug.LogError,
				method: "DELETE");
		}

		public override void ReceiveBytes(byte[] message)
		{
			throw new NotImplementedException();
		}
	}
}