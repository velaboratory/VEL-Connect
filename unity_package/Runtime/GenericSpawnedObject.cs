using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VelNet;

namespace VELConnect
{
	public class GenericSpawnedObject : SyncState
	{
		private string url;

		private string Url
		{
			get => url;
			set
			{
				if (url == value) return;

				url = value;

				if (url.EndsWith(".png") || url.EndsWith(".jpg"))
				{
					StartCoroutine(DownloadImage(url));
				}
				else
				{
					Debug.LogError("Invalid image url: " + url);
				}
			}
		}

		public RawImage rawImage;

		public void Init(string dataUrl)
		{
			Url = dataUrl;
		}

		protected override void SendState(BinaryWriter binaryWriter)
		{
			binaryWriter.Write(Url);
		}

		protected override void ReceiveState(BinaryReader binaryReader)
		{
			Url = binaryReader.ReadString();
		}

		private IEnumerator DownloadImage(string downloadUrl)
		{
			UnityWebRequest request = UnityWebRequestTexture.GetTexture(downloadUrl);
			yield return request.SendWebRequest();
			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.Log(request.error);
				yield break;
			}

			rawImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
			float aspect = (float)rawImage.texture.width / rawImage.texture.height;
			Transform t = transform;
			Vector3 s = t.localScale;
			s = new Vector3(aspect * s.y, s.y, s.z);
			t.localScale = s;
		}
	}
}