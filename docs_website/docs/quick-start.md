---
title: Quick Start
---


## Setup 

1. [Install the package](/)
2. Add the VelConnectManager script to an object in your scene. If you transition between scenes in your application, mark the object as `DontDestroyOnLoad`
3. Set the `Vel Connect Url` field on the component to a valid velconnect server. `https://velconnect-v4.ugavel.com` is useful for VEL projects.

## Usage

### Setting data

To set user data in VEL-Connect use the static function `SetUserData`.

You can add a single key and value:
```cs
VELConnectManager.SetUserData("key1", "val1");
```

Or set multiple keys with the dictionary syntax:
```cs
VELConnectManager.SetUserData(new Dictionary<string, string>
{
    { "key2", "val2" },
    { "key3", "val3" }
});
```
Data will be set instantly locally, then pushed to the server. You don't have to wait for VEL-Connect to initialize at the beginning of your game to set data.

### Getting data

Fetching data from a remote server can be more tricky because it won't be available immediately when the game starts. Data can also be set from other applications (such as a dashboard or other users in the case of room data), so change listeners are useful.

To fetch a single value from a key:
```cs
string value1 = VELConnectManager.GetUserData("key1");
```
The latest local value will be returned. This will always return null in `Start()` because no data has been fetched yet, so you could wrap this call in the `OnInitialState` callback:
```cs
VELConnectManager.OnInitialState += state =>
{
    VELConnectManager.GetUserData("key1");
};
```
If the data was already on the server before the start of your application, the correct value will be returned.


#### Change listeners

If you want to subscribe to changes in a key you can set up change listeners:
```cs
VELConnectManager.AddUserDataListener("key1", this, value =>
{
    Debug.Log($"key1: {value}");
}, true);
```
Passing in `this` binds the lifetime of the listener to the lifetime of the current script. It is often tedious to make sure to unsubscribe to all of your listeners OnDisable or OnDestroy to prevent the event emitter from sending events to objects that no longer exist, but VEL-Connect will remove listeners when their `keepAliveObject` parameter becomes null. The last parameter in this function (`true` in the example) tells VEL-Connect to activate the callback immediately or when the first value is received. You can add the listener on `Start()` and the first invokation of the callback will have the previous value of the server.



---

Full example:
```cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VELConnect;

public class VELConnectTesting : MonoBehaviour
{
	private IEnumerator Start()
	{
		VELConnectManager.OnInitialState += state =>
		{
			Debug.Log($"[OnInitialState] key1: {VELConnectManager.GetUserData("key1")}");
		};
		
		VELConnectManager.AddUserDataListener("key1", this, value =>
		{
			Debug.Log($"[Listener] key1: {value}");
		}, true);
		
		VELConnectManager.AddUserDataListener("key2", this, value =>
		{
			Debug.Log($"[Listener] key2: {value}");
		}, false);
		
		yield return new WaitForSeconds(1f);
		
		VELConnectManager.SetUserData("key1", "val1");

		VELConnectManager.SetUserData(new Dictionary<string, string>
		{
			{ "key1", "val1" },
			{ "key2", "val2" },
		});
		
		yield return new WaitForSeconds(1f);
		
		VELConnectManager.SetUserData("key1", "val1_later");
	}
}
```

---

JSON.Net Example

```cs
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VELConnect;
public class VelConnectDemo1 : MonoBehaviour
{
	class ExampleJSON
	{
		public string a_string="a"; //you can use initializers
		public int a_int=0;
		public List<ExampleChildJSON> a_list = new List<ExampleChildJSON>(); // you can use lists of objects
	}
	class ExampleChildJSON
	{
		public string a_string; //if you don't, that's fine too, but you probably want a constructor then
		public int a_int;
		public ExampleChildJSON() { } //you need to make sure you have a blank constructor for deserialization
		public ExampleChildJSON(string a_string, int a_int) 
		{
			this.a_string = a_string;
			this.a_int = a_int;
		}
	}

	ExampleJSON dataToPersist = null;

    // Start is called before the first frame update
    void Start()
    {
		VELConnectManager.OnInitialState += (state) =>
		{
			try
			{ 
				dataToPersist = JsonConvert.DeserializeObject<ExampleJSON>(state.device.TryGetData("mydata"));
			}
			catch (Exception e)
			{
				dataToPersist = new ExampleJSON();
			}
		};

		StartCoroutine(exampleProcess());
    }

	IEnumerator exampleProcess()
	{
		while(dataToPersist == null)
		{
			yield return null; //wait for persistent data
		}

		dataToPersist.a_list.Add(
			new ExampleChildJSON("" + UnityEngine.Random.Range(0, 10), 
			UnityEngine.Random.Range(0, 10))
			);
		Debug.Log(JsonConvert.SerializeObject(dataToPersist));
	}
	private void OnApplicationQuit()
	{
		VELConnectManager.SetUserData("mydata", JsonConvert.SerializeObject(dataToPersist));
	}

}

```
