using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using VelNet;

namespace VELConnect
{
	public class VELConnectManager : MonoBehaviour
	{
		public string velConnectUrl = "http://localhost";
		public static VELConnectManager instance;

		public class State
		{
			public class Device
			{
				public string hw_id;
				public string os_info;
				public string friendly_name;
				public string modified_by;
				public string current_app;
				public string current_room;
				public int pairing_code;
				public string date_created;
				public string last_modified;
				public Dictionary<string, string> data;

				/// <summary>
				/// Returns the value if it exists, otherwise null
				/// </summary>
				public string TryGetData(string key)
				{
					return data?.TryGetValue(key, out string val) == true ? val : null;
				}
			}

			public class RoomState
			{
				public string error;
				public string id;
				public string category;
				public string date_created;
				public string modified_by;
				public string last_modified;
				public string last_accessed;
				public Dictionary<string, string> data;
			}

			public Device device;
			public RoomState room;
		}

		public State lastState;

		public static Action<State> OnInitialState;
		public static Action<string, string> OnDeviceFieldChanged;
		public static Action<string, string> OnDeviceDataChanged;
		public static Action<string, string> OnRoomDataChanged;

		public static int PairingCode
		{
			get
			{
				Hash128 hash = new Hash128();
				hash.Append(DeviceId);
				// change once a day
				hash.Append(DateTime.UtcNow.DayOfYear);
				// between 1000 and 9999 inclusive (any 4 digit number)
				return Math.Abs(hash.GetHashCode()) % 9000 + 1000;
			}
		}

		private static string DeviceId
		{
			get
			{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
			// allows running multiple builds on the same computer
			// return SystemInfo.deviceUniqueIdentifier + Hash128.Compute(Application.dataPath);
			return SystemInfo.deviceUniqueIdentifier + "_BUILD";
#else
				return SystemInfo.deviceUniqueIdentifier;
#endif
			}
		}

		private void Awake()
		{
			if (instance != null) Debug.LogError("VELConnectManager instance already exists", this);
			instance = this;
		}

		// Start is called before the first frame update
		private void Start()
		{
			SetDeviceBaseData(new Dictionary<string, object>
			{
				{ "current_app", Application.productName },
				{ "pairing_code", PairingCode }
			});

			UpdateUserCount();


			StartCoroutine(SlowLoop());

			VelNetManager.OnJoinedRoom += room =>
			{
				SetDeviceBaseData(new Dictionary<string, object>
				{
					{ "current_app", Application.productName },
					{ "current_room", room },
				});
			};
		}


		private void UpdateUserCount(bool leaving = false)
		{
			if (!VelNetManager.InRoom) return;

			VelNetManager.GetRooms(rooms =>
			{
				Dictionary<string, object> postData = new Dictionary<string, object>
				{
					{ "hw_id", DeviceId },
					{ "app_id", Application.productName },
					{ "room_id", VelNetManager.Room ?? "" },
					{ "total_users", rooms.rooms.Sum(r => r.numUsers) - (leaving ? 1 : 0) },
					{ "room_users", VelNetManager.PlayerCount - (leaving ? 1 : 0) },
					{ "version", Application.version },
					{ "platform", SystemInfo.operatingSystem },
				};
				PostRequestCallback(velConnectUrl + "/api/v2/update_user_count", JsonConvert.SerializeObject(postData));
			});
		}

		private IEnumerator SlowLoop()
		{
			while (true)
			{
				try
				{
					GetRequestCallback(velConnectUrl + "/api/v2/get_state/" + DeviceId, json =>
					{
						State state = JsonConvert.DeserializeObject<State>(json);
						if (state == null) return;

						// first load stuff
						if (lastState == null)
						{
							try
							{
								OnInitialState?.Invoke(state);
							}
							catch (Exception e)
							{
								Debug.LogError(e);
							}

							lastState = state;
							return;
						}


						if (state.device.modified_by != DeviceId)
						{
							FieldInfo[] fields = state.device.GetType().GetFields();

							foreach (FieldInfo fieldInfo in fields)
							{
								string newValue = fieldInfo.GetValue(state.device) as string;
								string oldValue = fieldInfo.GetValue(lastState.device) as string;
								if (newValue != oldValue)
								{
									try
									{
										OnDeviceFieldChanged?.Invoke(fieldInfo.Name, newValue);
									}
									catch (Exception e)
									{
										Debug.LogError(e);
									}
								}
							}

							foreach (KeyValuePair<string, string> elem in state.device.data)
							{
								lastState.device.data.TryGetValue(elem.Key, out string oldValue);
								if (elem.Value != oldValue)
								{
									try
									{
										OnDeviceDataChanged?.Invoke(elem.Key, elem.Value);
									}
									catch (Exception e)
									{
										Debug.LogError(e);
									}
								}
							}
						}

						if (state.room.modified_by != DeviceId)
						{
							foreach (KeyValuePair<string, string> elem in state.room.data)
							{
								lastState.room.data.TryGetValue(elem.Key, out string oldValue);
								if (elem.Value != oldValue)
								{
									try
									{
										OnRoomDataChanged?.Invoke(elem.Key, elem.Value);
									}
									catch (Exception e)
									{
										Debug.LogError(e);
									}
								}
							}
						}

						lastState = state;
					});
				}
				catch (Exception e)
				{
					Debug.LogError(e);
					// this make sure the coroutine never quits
				}

				yield return new WaitForSeconds(1);
			}
		}


		/// <summary>
		/// Sets data on the device keys themselves
		/// </summary>
		public static void SetDeviceBaseData(Dictionary<string, object> data)
		{
			data["modified_by"] = DeviceId;
			instance.PostRequestCallback(
				instance.velConnectUrl + "/api/v2/device/set_data/" + DeviceId,
				JsonConvert.SerializeObject(data)
			);
		}

		/// <summary>
		/// Sets the 'data' object of the Device table
		/// </summary>
		public static void SetDeviceData(Dictionary<string, string> data)
		{
			instance.PostRequestCallback(
				instance.velConnectUrl + "/api/v2/device/set_data/" + DeviceId,
				JsonConvert.SerializeObject(new Dictionary<string, object>
				{
					{ "modified_by", DeviceId },
					{ "data", data }
				})
			);
		}

		public static void SetRoomData(Dictionary<string, string> data)
		{
			if (!VelNetManager.InRoom)
			{
				Debug.LogError("Can't set data for a room if you're not in a room.");
				return;
			}

			data["modified_by"] = DeviceId;
			instance.PostRequestCallback(
				instance.velConnectUrl + "/api/v2/set_data/" + Application.productName + "_" + VelNetManager.Room,
				JsonConvert.SerializeObject(data)
			);
		}


		public void GetRequestCallback(string url, Action<string> successCallback = null, Action<string> failureCallback = null)
		{
			StartCoroutine(GetRequestCallbackCo(url, successCallback, failureCallback));
		}

		private IEnumerator GetRequestCallbackCo(string url, Action<string> successCallback = null, Action<string> failureCallback = null)
		{
			using UnityWebRequest webRequest = UnityWebRequest.Get(url);
			// Request and wait for the desired page.
			yield return webRequest.SendWebRequest();

			switch (webRequest.result)
			{
				case UnityWebRequest.Result.ConnectionError:
				case UnityWebRequest.Result.DataProcessingError:
				case UnityWebRequest.Result.ProtocolError:
					Debug.LogError(url + ": Error: " + webRequest.error);
					failureCallback?.Invoke(webRequest.error);
					break;
				case UnityWebRequest.Result.Success:
					successCallback?.Invoke(webRequest.downloadHandler.text);
					break;
			}
		}

		public void PostRequestCallback(string url, string postData, Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			StartCoroutine(PostRequestCallbackCo(url, postData, successCallback, failureCallback));
		}

		private static IEnumerator PostRequestCallbackCo(string url, string postData, Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
			UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw);
			webRequest.uploadHandler = uploadHandler;
			webRequest.SetRequestHeader("Content-Type", "application/json");
			yield return webRequest.SendWebRequest();

			switch (webRequest.result)
			{
				case UnityWebRequest.Result.ConnectionError:
				case UnityWebRequest.Result.DataProcessingError:
				case UnityWebRequest.Result.ProtocolError:
					Debug.LogError(url + ": Error: " + webRequest.error);
					failureCallback?.Invoke(webRequest.error);
					break;
				case UnityWebRequest.Result.Success:
					successCallback?.Invoke(webRequest.downloadHandler.text);
					break;
			}

			uploadHandler.Dispose();
			webRequest.Dispose();
		}

		private void OnApplicationFocus(bool focus)
		{
			UpdateUserCount(!focus);
		}
	}
}