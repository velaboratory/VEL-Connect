using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using VelNet;

namespace VELConnect
{
	public class VelNetPersist : MonoBehaviour
	{
		private class ComponentState
		{
			public int componentIdx;
			public string state;
		}

		public SyncState[] syncStateComponents;

		private string Id => $"{Application.productName}_{VelNetManager.Room}_{syncStateComponents.FirstOrDefault()?.networkObject.sceneNetworkId}";

		private const float interval = 5f;
		private double nextUpdate;
		private bool loading;
		private const bool debugLogs = false;

		private void Update()
		{
			if (Time.timeAsDouble > nextUpdate && VelNetManager.InRoom && !loading)
			{
				nextUpdate = Time.timeAsDouble + interval + UnityEngine.Random.Range(0, interval);
				if (syncStateComponents.FirstOrDefault()?.networkObject.IsMine == true)
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
			if (debugLogs) Debug.Log($"[VelNetPersist] Loading {Id}");
			VELConnectManager.GetDataBlock(Id, data =>
			{
				if (!data.data.TryGetValue("components", out string d))
				{
					Debug.LogError($"[VelNetPersist] Failed to parse {Id}");
					return;
				}


				List<ComponentState> componentData = JsonConvert.DeserializeObject<List<ComponentState>>(d);

				if (componentData.Count != syncStateComponents.Length)
				{
					Debug.LogError($"[VelNetPersist] Different number of components");
					return;
				}

				for (int i = 0; i < syncStateComponents.Length; i++)
				{
					syncStateComponents[i].UnpackState(Convert.FromBase64String(componentData[i].state));
				}

				if (debugLogs) Debug.Log($"[VelNetPersist] Loaded {Id}");
				loading = false;
			}, s => { loading = false; });
		}


		public void Save(Action<VELConnectManager.State.DataBlock> successCallback = null)
		{
			if (debugLogs) Debug.Log($"[VelNetPersist] Saving {Id}");

			if (syncStateComponents.FirstOrDefault()?.networkObject == null)
			{
				Debug.LogError("First SyncState doesn't have a NetworkObject", this);
				return;
			}

			List<ComponentState> componentData = new List<ComponentState>();
			foreach (SyncState syncState in syncStateComponents)
			{
				if (syncState == null)
				{
					Debug.LogError("SyncState is null for Persist", this);
					return;
				}

				if (syncState.networkObject == null)
				{
					Debug.LogError("Network Object is null for SyncState", syncState);
					return;
				}

				componentData.Add(new ComponentState()
				{
					componentIdx = syncState.networkObject.syncedComponents.IndexOf(syncState),
					state = Convert.ToBase64String(syncState.PackState())
				});
			}

			VELConnectManager.SetDataBlock(Id, new VELConnectManager.State.DataBlock()
			{
				id = Id,
				block_id = Id,
				category = "object_persist",
				data = new Dictionary<string, string>
				{
					{ "name", syncStateComponents.FirstOrDefault()?.networkObject.name },
					{ "components", JsonConvert.SerializeObject(componentData) }
				}
			}, s => { successCallback?.Invoke(s); });
		}
	}
}