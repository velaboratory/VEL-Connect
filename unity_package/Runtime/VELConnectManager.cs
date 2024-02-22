using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using VelNet;

namespace VELConnect
{
	// ReSharper disable once InconsistentNaming
	public class VELConnectManager : MonoBehaviour
	{
		public string velConnectUrl = "http://localhost";
		public static string VelConnectUrl => instance.velConnectUrl;
		private static VELConnectManager instance;

		public class State
		{
			public Device device;
			public DataBlock room;
			public User user;


			public class User
			{
				public string id;
				public string username;
				public string created;
				public string updated;
			}

			public class Device
			{
				[CanBeNull] public readonly string id;
				[CanBeNull] public string created = null;
				[CanBeNull] public string updated = null;
				[CanBeNull] public string os_info;
				[CanBeNull] public string friendly_name;
				[CanBeNull] public string modified_by;
				[CanBeNull] public string current_app;
				[CanBeNull] public string current_room;
				[CanBeNull] public string pairing_code;
				[CanBeNull] public string last_online;
				[CanBeNull] public string owner;
				[CanBeNull] public string[] past_owners;
				[CanBeNull] public DeviceExpand expand;
				public DataBlock userData => expand?.data;

				public class DeviceExpand
				{
					public DataBlock data;
				}


				/// <summary>
				/// Returns the value if it exists, otherwise null
				/// </summary>
				public string TryGetData(string key)
				{
					return userData.TryGetData(key);
				}
			}

			public class DataBlock
			{
				public readonly string id;
				public readonly DateTime created;
				public readonly DateTime updated;
				public string block_id;
				public string owner;
				public string category;
				public string modified_by;
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
		}

		public class UserCount
		{
			[CanBeNull] public readonly string id;
			public readonly DateTime? created;
			public readonly DateTime? updated;
			public string device_id;
			public string app_id;
			public string room_id;
			public int total_users;
			public int room_users;
			public string version;
			public string platform;
		}

		public enum DeviceField
		{
			id,
			os_info,
			friendly_name,
			modified_by,
			current_app,
			current_room,
			pairing_code,
			last_online,
			data,
			owner,
			past_owners
		}

		private State lastState;
		private State state;

		[CanBeNull] public static State CurrentState => instance == null ? null : instance.state;

		public static Action<State> OnInitialState;
		public static Action<DeviceField, string> OnDeviceFieldChanged;
		public static Action<string, object> OnUserDataChanged;
		public static Action<string, object> OnRoomDataChanged;

		private static readonly Dictionary<DeviceField, List<CallbackListener>> deviceFieldCallbacks =
			new Dictionary<DeviceField, List<CallbackListener>>();

		private static readonly Dictionary<string, List<CallbackListener>> userDataCallbacks =
			new Dictionary<string, List<CallbackListener>>();

		private static readonly Dictionary<string, List<CallbackListener>> roomDataCallbacks =
			new Dictionary<string, List<CallbackListener>>();

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

		public static string PairingCode
		{
			get
			{
				Hash128 hash = new Hash128();
				hash.Append(deviceId);
				// change once a day
				hash.Append(DateTime.UtcNow.DayOfYear);
				// between 1000 and 9999 inclusive (any 4 digit number)
				return (Math.Abs(hash.GetHashCode()) % 9000 + 1000).ToString();
			}
		}

		private static string deviceId;

		private void Awake()
		{
			if (instance != null) Debug.LogError("VELConnectManager instance already exists", this);
			instance = this;
			deviceId = CreateDeviceId();
			VelNetManager.OnLocalNetworkObjectSpawned += networkObject =>
			{
				if (!networkObject.ownershipLocked)
				{
					// TODO 
					// SetRoomData("spawned_" + networkObject.networkId, networkObject.prefabName);
				}
			};
		}

		// Computes 15-char device id compatibly with pocketbase
		private static string CreateDeviceId()
		{
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			StringBuilder sb = new StringBuilder(SystemInfo.deviceUniqueIdentifier);
			sb.Append(Application.productName);
#if UNITY_EDITOR
			// allows running multiple builds on the same computer
			// return SystemInfo.deviceUniqueIdentifier + Hash128.Compute(Application.dataPath);
			sb.Append(Application.dataPath);
			sb.Append("EDITOR");
#endif
			string id = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
			return id[..15];
		}

		// Start is called before the first frame update
		private void Start()
		{
			SetDeviceField(new Dictionary<DeviceField, string>
			{
				{ DeviceField.os_info, SystemInfo.operatingSystem },
				{ DeviceField.friendly_name, SystemInfo.deviceName },
				{ DeviceField.current_app, Application.productName },
				{ DeviceField.pairing_code, PairingCode },
			});

			UpdateUserCount();

			StartCoroutine(SlowLoop());

			VelNetManager.OnJoinedRoom += room =>
			{
				SetDeviceField(new Dictionary<DeviceField, string>
				{
					{ DeviceField.current_app, Application.productName },
					{ DeviceField.current_room, room },
				});
			};
		}

		private void UpdateUserCount(bool leaving = false)
		{
			if (!VelNetManager.InRoom) return;

			VelNetManager.GetRooms(rooms =>
			{
				UserCount postData = new UserCount
				{
					device_id = deviceId,
					app_id = Application.productName,
					room_id = VelNetManager.Room ?? "",
					total_users = rooms.rooms.Sum(r => r.numUsers) - (leaving ? 1 : 0),
					room_users = VelNetManager.PlayerCount - (leaving ? 1 : 0),
					version = Application.version,
					platform = SystemInfo.operatingSystem,
				};
				PostRequestCallback(velConnectUrl + "/api/collections/UserCount/records", JsonConvert.SerializeObject(
					postData, Formatting.None, new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore
					}));
			});
		}

		private IEnumerator SlowLoop()
		{
			while (true)
			{
				try
				{
					GetRequestCallback(velConnectUrl + "/state/" + deviceId, json =>
					{
						state = JsonConvert.DeserializeObject<State>(json);
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


						// if (state.device.modified_by != DeviceId)
						{
							FieldInfo[] fields = state.device.GetType().GetFields();

							// loop through all the fields in the device
							foreach (FieldInfo fieldInfo in fields)
							{
								string newValue = fieldInfo.GetValue(state.device) as string;
								string oldValue = lastState != null
									? fieldInfo.GetValue(lastState.device) as string
									: null;

								DeviceField fieldName;
								if (Enum.TryParse(fieldInfo.Name, out fieldName))
								{
									if (newValue != oldValue)
									{
										try
										{
											if (!isInitialState) OnDeviceFieldChanged?.Invoke(fieldName, newValue);
										}
										catch (Exception e)
										{
											Debug.LogError(e);
										}

										// send specific listeners data
										if (deviceFieldCallbacks.ContainsKey(fieldName))
										{
											// clear the list of old listeners
											deviceFieldCallbacks[fieldName].RemoveAll(e => e.keepAliveObject == null);

											// send the callbacks
											foreach (CallbackListener e in deviceFieldCallbacks[fieldName])
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
											}
										}
									}
								}
							}

							if (state.device.userData.data != null)
							{
								foreach (KeyValuePair<string, string> elem in state.device.userData.data)
								{
									string oldValue = null;
									lastState?.device?.userData?.data?.TryGetValue(elem.Key, out oldValue);
									if (elem.Value != oldValue)
									{
										try
										{
											if (!isInitialState) OnUserDataChanged?.Invoke(elem.Key, elem.Value);
										}
										catch (Exception ex)
										{
											Debug.LogError(ex);
										}

										// send specific listeners data
										if (userDataCallbacks.ContainsKey(elem.Key))
										{
											// clear the list of old listeners
											userDataCallbacks[elem.Key].RemoveAll(e => e.keepAliveObject == null);

											// send the callbacks
											foreach (CallbackListener e in userDataCallbacks[elem.Key])
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
											}
										}
									}
								}

								// on the initial state, also activate callbacks for null values
								if (isInitialState)
								{
									foreach ((string userDataKey, List<CallbackListener> callbackList) in
									         userDataCallbacks)
									{
										if (!state.device.userData.data.ContainsKey(userDataKey))
										{
											// send the callbacks
											callbackList.ForEach(e =>
											{
												if (e.sendInitialState)
												{
													try
													{
														e.callback(null);
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

						// if (state.room.modified_by != DeviceId && state.room.data != null)
						if (state.room?.data != null)
						{
							foreach (KeyValuePair<string, string> elem in state.room.data)
							{
								string oldValue = null;
								lastState?.room?.data.TryGetValue(elem.Key, out oldValue);
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

							// on the initial state, also activate callbacks for null values
							if (isInitialState)
							{
								foreach ((string key, List<CallbackListener> callbackList) in roomDataCallbacks)
								{
									if (!state.room.data.ContainsKey(key))
									{
										// send the callbacks
										callbackList.ForEach(e =>
										{
											if (e.sendInitialState)
											{
												try
												{
													e.callback(null);
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
						if (lastState?.device?.pairing_code == null)
						{
							Debug.LogError("Pairing code nulllll");
						}
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
		public static void AddDeviceFieldListener(DeviceField key, MonoBehaviour keepAliveObject,
			Action<string> callback,
			bool sendInitialState = false)
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
					if (instance.lastState.device.GetType().GetField(key.ToString())
						    ?.GetValue(instance.lastState.device) is string val)
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
		/// Adds a change listener callback to a particular field name within the User data JSON.
		/// If the initial state doesn't contain this key, this sends back null
		/// </summary>
		public static void AddUserDataListener(string key, MonoBehaviour keepAliveObject, Action<string> callback,
			bool sendInitialState = false)
		{
			if (!userDataCallbacks.ContainsKey(key))
			{
				userDataCallbacks[key] = new List<CallbackListener>();
			}

			userDataCallbacks[key].Add(new CallbackListener()
			{
				keepAliveObject = keepAliveObject,
				callback = callback,
				sendInitialState = sendInitialState
			});

			// if we have already received data, and we should send right away
			if (sendInitialState && instance.state != null)
			{
				string val = GetUserData(key);
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

		/// <summary>
		/// Adds a change listener callback to a particular field name within the Room data JSON.
		/// If the initial state doesn't contain this key, this sends back null
		/// </summary>
		public static void AddRoomDataListener(string key, MonoBehaviour keepAliveObject, Action<string> callback,
			bool sendInitialState = false)
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

			// if we have already received data, and we should send right away
			if (sendInitialState && instance.state != null)
			{
				string val = GetRoomData(key);
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

		public static string GetUserData(string key, string defaultValue = null)
		{
			return instance != null ? instance.lastState?.device?.TryGetData(key) : defaultValue;
		}

		public static string GetRoomData(string key, string defaultValue = null)
		{
			return instance != null ? instance.lastState?.room?.TryGetData(key) : defaultValue;
		}

		public static T GetRoomData<T>(string key, string defaultValue = null)
		{
			string value = instance != null ? instance.lastState?.room?.TryGetData(key) : defaultValue;

			if (typeof(T) == typeof(int))
			{
				if (int.TryParse(value, out int result))
				{
					return (T)Convert.ChangeType(result, typeof(T));
				}

				return (T)(object)0;
			}
			else if (typeof(T) == typeof(double))
			{
				if (double.TryParse(value, out double result))
				{
					return (T)Convert.ChangeType(result, typeof(T));
				}

				return (T)(object)0.0;
			}
			else if (typeof(T) == typeof(float))
			{
				if (float.TryParse(value, out float result))
				{
					return (T)Convert.ChangeType(result, typeof(T));
				}

				return (T)(object)0f;
			}

			throw new NotSupportedException($"Conversion to type {typeof(T)} is not supported.");
		}

		/// <summary>
		/// Sets data on the device keys themselves
		/// These are fixed fields defined for every application
		/// </summary>
		public static void SetDeviceField(Dictionary<DeviceField, string> device)
		{
			device[DeviceField.last_online] = DateTime.UtcNow.ToLongDateString();

			if (instance.state?.device != null)
			{
				// loop through all the fields in the device
				foreach (DeviceField key in device.Keys.ToArray())
				{
					FieldInfo field = instance.state.device.GetType().GetField(key.ToString());
					if ((string)field.GetValue(instance.state.device) != device[key])
					{
						if (instance.lastState?.device != null)
						{
							// update our local state, so we don't get change events on our own updates
							field.SetValue(instance.lastState.device, device[key]);
						}
					}
					else
					{
						// don't send this field, since it's the same
						device.Remove(key);
					}
				}

				// last_online field always changes
				if (device.Keys.Count <= 1)
				{
					// nothing changed, don't send
					return;
				}
			}

			PostRequestCallback(
				instance.velConnectUrl + "/device/" + deviceId,
				JsonConvert.SerializeObject(device)
			);
		}

		/// <summary>
		/// Sets the 'data' object of the Device table
		/// </summary>
		public static void SetUserData(Dictionary<string, string> data)
		{
			if (instance.state?.device != null)
			{
				foreach (string key in data.Keys.ToList())
				{
					// if the value is unchanged from the current state, remove it so we don't double-update
					if (instance.state.device.userData.data.TryGetValue(key, out string val) && val == data[key])
					{
						data.Remove(key);
					}
					else
					{
						// update our local state, so we don't get change events on our own updates
						if (instance.lastState?.device?.userData?.data != null)
						{
							instance.lastState.device.userData.data[key] = data[key];
						}
					}
				}

				// nothing was changed
				if (data.Keys.Count == 0)
				{
					return;
				}

				// if we have no data, just set the whole thing
				if (instance.lastState?.device?.userData != null) instance.lastState.device.userData.data ??= data;
			}


			Dictionary<string, object> device = new Dictionary<string, object>
			{
				{ "last_online", DateTime.UtcNow.ToLongDateString() },
				{ "data", data },
			};

			PostRequestCallback(
				instance.velConnectUrl + "/device/" + deviceId,
				JsonConvert.SerializeObject(device, Formatting.None, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				})
			);
		}

		public static void SetRoomData(string key, string value)
		{
			SetRoomData(new Dictionary<string, string> { { key, value } });
		}

		public static void SetRoomData(Dictionary<string, string> data)
		{
			if (!VelNetManager.InRoom)
			{
				Debug.LogError("Can't set data for a room if you're not in a room.");
				return;
			}

			State.DataBlock room = new State.DataBlock
			{
				category = "room",
				data = data
			};

			// remove keys that already match our current state
			if (instance.state?.room != null)
			{
				foreach (string key in data.Keys.ToArray())
				{
					instance.state.room.data.TryGetValue(key, out string currentValue);
					if (currentValue == data[key])
					{
						data.Remove(key);
					}
				}
			}

			// if we have no changed values
			if (data.Keys.Count == 0)
			{
				return;
			}

			// update our local state, so we don't get change events on our own updates
			if (instance.lastState?.room != null)
			{
				foreach (KeyValuePair<string, string> kvp in data)
				{
					instance.lastState.room.data[kvp.Key] = kvp.Value;
				}
			}

			PostRequestCallback(
				instance.velConnectUrl + "/data_block/" + Application.productName + "_" + VelNetManager.Room,
				JsonConvert.SerializeObject(room, Formatting.None, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				})
			);
		}

		public static void Unpair()
		{
			if (instance.state?.device != null)
			{
				PostRequestCallback(
					instance.velConnectUrl + "/unpair",
					JsonConvert.SerializeObject(new Dictionary<string, string>()
					{
						{ "device_id", instance.state.device.id },
						{ "user_id", instance.state.user.id }
					})
				);
			}
		}

		// TODO
		public static void UploadFile(string fileName, byte[] fileData, Action<string> successCallback = null)
		{
			// MultipartFormDataContent requestContent = new MultipartFormDataContent();
			// ByteArrayContent fileContent = new ByteArrayContent(fileData);
			//
			// requestContent.Add(fileContent, "file", fileName);
			//
			// Task.Run(async () =>
			// {
			// 	HttpResponseMessage r =
			// 		await new HttpClient().PostAsync(_instance.velConnectUrl + "/api/upload_file", requestContent);
			// 	string resp = await r.Content.ReadAsStringAsync();
			// 	Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);
			// 	successCallback?.Invoke(dict["key"]);
			// });
		}

		// TODO
		public static void DownloadFile(string key, Action<byte[]> successCallback = null)
		{
			// _instance.StartCoroutine(_instance.DownloadFileCo(key, successCallback));
		}

		private IEnumerator DownloadFileCo(string key, Action<byte[]> successCallback = null)
		{
			UnityWebRequest www = new UnityWebRequest(velConnectUrl + "/api/download_file/" + key);
			www.downloadHandler = new DownloadHandlerBuffer();
			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success)
			{
				Debug.Log(www.error);
			}
			else
			{
				// Show results as text
				Debug.Log(www.downloadHandler.text);

				// Or retrieve results as binary data
				byte[] results = www.downloadHandler.data;

				successCallback?.Invoke(results);
			}
		}


		public static void GetRequestCallback(string url, Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			instance.StartCoroutine(instance.GetRequestCallbackCo(url, successCallback, failureCallback));
		}

		private IEnumerator GetRequestCallbackCo(string url, Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			using UnityWebRequest webRequest = UnityWebRequest.Get(url);
			// Request and wait for the desired page.
			yield return webRequest.SendWebRequest();

			switch (webRequest.result)
			{
				case UnityWebRequest.Result.ConnectionError:
				case UnityWebRequest.Result.DataProcessingError:
				case UnityWebRequest.Result.ProtocolError:
					Debug.LogError(url + ": Error: " + webRequest.error + "\n" + Environment.StackTrace);
					failureCallback?.Invoke(webRequest.error);
					break;
				case UnityWebRequest.Result.Success:
					successCallback?.Invoke(webRequest.downloadHandler.text);
					break;
			}
		}

		public static void PostRequestCallback(string url, string postData, Dictionary<string, string> headers = null,
			Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			instance.StartCoroutine(PostRequestCallbackCo(url, postData, headers, successCallback, failureCallback));
		}


		private static IEnumerator PostRequestCallbackCo(string url, string postData,
			Dictionary<string, string> headers = null, Action<string> successCallback = null,
			Action<string> failureCallback = null)
		{
			UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
			UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw);
			webRequest.uploadHandler = uploadHandler;
			webRequest.downloadHandler = new DownloadHandlerBuffer();
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
					Debug.LogError(url + ": Error: " + webRequest.error + "\n" + webRequest.downloadHandler.text + "\n" + Environment.StackTrace);
					failureCallback?.Invoke(webRequest.error);
					break;
				case UnityWebRequest.Result.Success:
					successCallback?.Invoke(webRequest.downloadHandler.text);
					break;
			}

			uploadHandler.Dispose();
			webRequest.Dispose();
		}

		public static void SetDataBlock(string blockId, State.DataBlock dataBlock)
		{
			PostRequestCallback(instance.velConnectUrl + "/data_block/" + blockId, JsonConvert.SerializeObject(dataBlock, Formatting.None, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			}));
		}

		public static void GetDataBlock(string blockId, Action<State.DataBlock> successCallback = null, Action<string> failureCallback = null)
		{
			GetRequestCallback(instance.velConnectUrl + "/data_block/" + blockId, data =>
			{
				State.DataBlock dict = JsonConvert.DeserializeObject<State.DataBlock>(data);
				successCallback?.Invoke(dict);
			}, failureCallback);
		}

		private void OnApplicationFocus(bool focus)
		{
			UpdateUserCount(!focus);
		}
	}
}