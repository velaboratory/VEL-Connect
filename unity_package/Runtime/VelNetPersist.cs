using System;
using System.Collections.Generic;
using UnityEngine;
using VelNet;

namespace VELConnect
{
	public class VelNetPersist : MonoBehaviour
	{
		public SyncState syncState;
		private string Id => $"{Application.productName}_{VelNetManager.Room}_{syncState.networkObject.sceneNetworkId}_{syncState.networkObject.syncedComponents.IndexOf(syncState)}";
		private const float interval = 5f;
		private double nextUpdate;
		private bool loading;
		private const bool debugLogs = false;

		private void Update()
		{
			if (Time.timeAsDouble > nextUpdate && VelNetManager.InRoom && !loading)
			{
				nextUpdate = Time.timeAsDouble + interval + UnityEngine.Random.Range(0, interval);
				if (syncState.networkObject.IsMine)
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
				if (!data.data.TryGetValue("state", out string d))
				{
					Debug.LogError($"[VelNetPersist] Failed to parse {Id}");
					return;
				}

				if (syncState == null)
				{
					Debug.LogError("[VelNetPersist] Object doesn't exist anymore");
				}

				syncState.UnpackState(Convert.FromBase64String(d));
				if (debugLogs) Debug.Log($"[VelNetPersist] Loaded {Id}");
				loading = false;
			}, s =>
			{
				Debug.LogError(s);
				loading = false;
			});
		}

		private void Save()
		{
			if (debugLogs) Debug.Log($"[VelNetPersist] Saving {Id}");
			VELConnectManager.SetDataBlock(Id, new VELConnectManager.State.DataBlock()
			{
				category = "object_persist",
				data = new Dictionary<string, string>
				{
					{ "name", syncState.networkObject.name },
					{ "state", Convert.ToBase64String(syncState.PackState()) }
				}
			});
		}
	}
}