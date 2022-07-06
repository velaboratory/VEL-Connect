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
		private static VELConnectManager instance;

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
					string val = null;
					return data?.TryGetValue(key, out val) == true ? val : null;
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

				/// <summary>
				/// Returns the value if it exists, otherwise null
				/// </summary>
				public string TryGetData(string key)
				{
					string val = null;
					return data?.TryGetValue(key, out val) == true ? val : null;
				}
			}

			public Device device;
			public RoomState room;
		}

		public State lastState;

		public static Action<State> OnInitialState;
		public static Action<string, string> OnDeviceFieldChanged;
		public static Action<string, string> OnDeviceDataChanged;
		public static Action<string, string> OnRoomDataChanged;

		private static readonly Dictionary<string, List<CallbackListener>> deviceFieldCallbacks = new Dictionary<string, List<CallbackListener>>();
		private static readonly Dictionary<string, List<CallbackListener>> deviceDataCallbacks = new Dictionary<string, List<CallbackListener>>();
		private static readonly Dictionary<string, List<CallbackListener>> roomDataCallbacks = new Dictionary<string, List<CallbackListener>>();

		private struct CallbackListener
		{
			/// <summary>
			/// Used so that other objects don't have to remove listeners themselves
			/// </summary>
			public MonoBehaviour keepAliveObject;

			public Action<string> callback;

			/// <summary>
			/// Sends the first state received from the network or the state at binding time
			/// </summary>
			public bool sendInitialState;
		}

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
#if UNITY_EDITOR
				// allows running multiple builds on the same computer
				// return SystemInfo.deviceUniqueIdentifier + Hash128.Compute(Application.dataPath);
				return SystemInfo.deviceUniqueIdentifier + "_EDITOR";
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
				PostRequestCallback(velConnectUrl + "/api/update_user_count", JsonConvert.SerializeObject(postData));
			});
		}

		private IEnumerator SlowLoop()
		{
			while (true)
			{
				try
				{
					GetRequestCallback(velConnectUrl + "/api/v2/device/get_data/" + DeviceId, json =>
					{
						State state = JsonConvert.DeserializeObject<State>(json);
						if (state == null) return;

						bool isInitialState = false;

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

							isInitialState = true;
							// lastState = state;
							// return;
						}


						if (state.device.modified_by != DeviceId)
						{
							FieldInfo[] fields = state.device.GetType().GetFields();

							foreach (FieldInfo fieldInfo in fields)
							{
								string newValue = fieldInfo.GetValue(state.device) as string;
								string oldValue = lastState != null ? fieldInfo.GetValue(lastState.device) as string : null;
								if (newValue != oldValue)
								{
									try
									{
										if (!isInitialState) OnDeviceFieldChanged?.Invoke(fieldInfo.Name, newValue);
									}
									catch (Exception e)
									{
										Debug.LogError(e);
									}

									// send specific listeners data
									if (deviceFieldCallbacks.ContainsKey(fieldInfo.Name))
									{
										// clear the list of old listeners
										deviceFieldCallbacks[fieldInfo.Name].RemoveAll(e => e.keepAliveObject == null);

										// send the callbacks
										deviceFieldCallbacks[fieldInfo.Name].ForEach(e =>
										{
											if (!isInitialState || e.sendInitialState)
											{
												try
												{
													e.callback(newValue);
												}
												catch (Exception ex)
												{
													Debug.LogError(ex);
												}
											}
										});
									}
								}
							}

							if (state.device.data != null)
							{
								foreach (KeyValuePair<string, string> elem in state.device.data)
								{
									string oldValue = null;
									lastState?.device.data.TryGetValue(elem.Key, out oldValue);
									if (elem.Value != oldValue)
									{
										try
										{
											if (!isInitialState) OnDeviceDataChanged?.Invoke(elem.Key, elem.Value);
										}
										catch (Exception ex)
										{
											Debug.LogError(ex);
										}

										// send specific listeners data
										if (deviceDataCallbacks.ContainsKey(elem.Key))
										{
											// clear the list of old listeners
											deviceDataCallbacks[elem.Key].RemoveAll(e => e.keepAliveObject == null);

											// send the callbacks
											deviceDataCallbacks[elem.Key].ForEach(e =>
											{
												if (!isInitialState || e.sendInitialState)
												{
													try
													{
														e.callback(elem.Value);
													}
													catch (Exception ex)
													{
														Debug.LogError(ex);
													}
												}
											});
										}
									}
								}
							}
						}

						if (state.room.modified_by != DeviceId && state.room.data != null)
						{
							foreach (KeyValuePair<string, string> elem in state.room.data)
							{
								string oldValue = null;
								lastState?.room.data.TryGetValue(elem.Key, out oldValue);
								if (elem.Value != oldValue)
								{
									try
									{
										if (!isInitialState) OnRoomDataChanged?.Invoke(elem.Key, elem.Value);
									}
									catch (Exception e)
									{
										Debug.LogError(e);
									}

									// send specific listeners data
									if (roomDataCallbacks.ContainsKey(elem.Key))
									{
										// clear the list of old listeners
										roomDataCallbacks[elem.Key].RemoveAll(e => e.keepAliveObject == null);

										// send the callbacks
										roomDataCallbacks[elem.Key].ForEach(e =>
										{
											if (!isInitialState || e.sendInitialState)
											{
												try
												{
													e.callback(elem.Value);
												}
												catch (Exception ex)
												{
													Debug.LogError(ex);
												}
											}
										});
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
		/// Adds a change listener callback to a particular field name within the Device main fields.
		/// </summary>
		public static void AddDeviceFieldListener(string key, MonoBehaviour keepAliveObject, Action<string> callback, bool sendInitialState = false)
		{
			if (!deviceFieldCallbacks.ContainsKey(key))
			{
				deviceFieldCallbacks[key] = new List<CallbackListener>();
			}

			deviceFieldCallbacks[key].Add(new CallbackListener()
			{
				keepAliveObject = keepAliveObject,
				callback = callback,
				sendInitialState = sendInitialState
			});

			if (sendInitialState)
			{
				if (instance != null && instance.lastState?.device != null)
				{
					if (instance.lastState.device.GetType().GetField(key)?.GetValue(instance.lastState.device) is string val)
					{
						try
						{
							callback(val);
						}
						catch (Exception e)
						{
							Debug.LogError(e);
						}
					}
				}
			}
		}

		/// <summary>
		/// Adds a change listener callback to a particular field name within the Device data JSON.
		/// </summary>
		public static void AddDeviceDataListener(string key, MonoBehaviour keepAliveObject, Action<string> callback, bool sendInitialState = false)
		{
			if (!deviceDataCallbacks.ContainsKey(key))
			{
				deviceDataCallbacks[key] = new List<CallbackListener>();
			}

			deviceDataCallbacks[key].Add(new CallbackListener()
			{
				keepAliveObject = keepAliveObject,
				callback = callback,
				sendInitialState = sendInitialState
			});

			if (sendInitialState)
			{
				string val = GetDeviceData(key);
				if (val != null)
				{
					try
					{
						callback(val);
					}
					catch (Exception e)
					{
						Debug.LogError(e);
					}
				}
			}
		}

		/// <summary>
		/// Adds a change listener callback to a particular field name within the Room data JSON.
		/// </summary>
		public static void AddRoomDataListener(string key, MonoBehaviour keepAliveObject, Action<string> callback, bool sendInitialState = false)
		{
			if (!roomDataCallbacks.ContainsKey(key))
			{
				roomDataCallbacks[key] = new List<CallbackListener>();
			}

			roomDataCallbacks[key].Add(new CallbackListener()
			{
				keepAliveObject = keepAliveObject,
				callback = callback,
				sendInitialState = sendInitialState
			});

			if (sendInitialState)
			{
				string val = GetRoomData(key);
				if (val != null)
				{
					try
					{
						callback(val);
					}
					catch (Exception e)
					{
						Debug.LogError(e);
					}
				}
			}
		}

		public static string GetDeviceData(string key)
		{
			return instance != null ? instance.lastState?.device?.TryGetData(key) : null;
		}

		public static string GetRoomData(string key)
		{
			return instance != null ? instance.lastState?.room?.TryGetData(key) : null;
		}


		/// <summary>
		/// Sets data on the device keys themselves
		/// </summary>
		public static void SetDeviceBaseData(Dictionary<string, object> data)
		{
			instance.PostRequestCallback(
				instance.velConnectUrl + "/api/v2/device/set_data/" + DeviceId,
				JsonConvert.SerializeObject(data),
				new Dictionary<string, string> { { "modified_by", DeviceId } }
			);
		}

		/// <summary>
		/// Sets the 'data' object of the Device table
		/// </summary>
		public static void SetDeviceData(Dictionary<string, string> data)
		{
			instance.PostRequestCallback(
				instance.velConnectUrl + "/api/v2/device/set_data/" + DeviceId,
				JsonConvert.SerializeObject(new Dictionary<string, object> { { "data", data } }),
				new Dictionary<string, string> { { "modified_by", DeviceId } }
			);
		}

		public static void SetRoomData(Dictionary<string, string> data)
		{
			if (!VelNetManager.InRoom)
			{
				Debug.LogError("Can't set data for a room if you're not in a room.");
				return;
			}

			instance.PostRequestCallback(
				instance.velConnectUrl + "/api/v2/set_data/" + Application.productName + "_" + VelNetManager.Room,
				JsonConvert.SerializeObject(data),
				new Dictionary<string, string> { { "modified_by", DeviceId } }
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

		public void PostRequestCallback(string url, string postData, Dictionary<string, string> headers = null, Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			StartCoroutine(PostRequestCallbackCo(url, postData, headers, successCallback, failureCallback));
		}

		private static IEnumerator PostRequestCallbackCo(string url, string postData, Dictionary<string, string> headers = null, Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
			UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw);
			webRequest.uploadHandler = uploadHandler;
			webRequest.SetRequestHeader("Content-Type", "application/json");
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> keyValuePair in headers)
				{
					webRequest.SetRequestHeader(keyValuePair.Key, keyValuePair.Value);
				}
			}

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