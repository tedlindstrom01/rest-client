using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RestClient
{
    public static class Client
    {
		public static void Init(string baseUrl)
		{
			_baseUrl = baseUrl;
			_isOAuth2Enabled = false;
			_isInitialized = true;
		}

		public static void InitOAuth2(string baseUrl, string publicKey, string privateKey)
		{
			_baseUrl = baseUrl;
			_publicKey = publicKey;
			_privateKey = privateKey;
			_isOAuth2Enabled = true;
			_isInitialized = true;
		}

		static bool _isInitialized = false;
		static bool _isOAuth2Enabled;
		static string _publicKey;
		static string _privateKey;
		static string _baseUrl;

		static void CleanJson(JToken token)
		{
			var children = new List<JToken>(token.Children());

			foreach (var child in children)
			{
				if (child.Type == JTokenType.Null)
				{
					if (children.Count == 1)
						token.Remove();
					else
						child.Remove();
				}
				else
					CleanJson(child);
			}
		}

		public static HttpClient CreateClient(Dictionary<string, string> headers = null)
		{
			HttpClient client = new HttpClient();

			if (_isOAuth2Enabled)
			{
				var uniqueId = Environment.TickCount.ToString() + Guid.NewGuid().ToString();
				HMACSHA256 hmac = new HMACSHA256(new ASCIIEncoding().GetBytes(_privateKey));

				var hmacResult = hmac.ComputeHash(new ASCIIEncoding().GetBytes(_publicKey + uniqueId));

				string signature = string.Empty;
				foreach (var b in hmacResult)
					signature += Convert.ToString((b & 0xff) + 0x100, 16).Substring(1);

				client.DefaultRequestHeaders.Add("X-API-KEY", _publicKey);
				client.DefaultRequestHeaders.Add("X-API-NONCE", uniqueId);
				client.DefaultRequestHeaders.Add("X-API-HASH", signature);
			}

			if (headers != null)
			{
				foreach (var h in headers)
				{
					if (client.DefaultRequestHeaders.Contains(h.Key))
						client.DefaultRequestHeaders.Remove(h.Key);

					client.DefaultRequestHeaders.Add(h.Key, h.Value);
				}
			}

			return client;
		}

		public static async Task<Response<T>> GetAsync<T>(string requestMethod, Dictionary<string, string> parameters = null, Dictionary<string, string> headers = null)
		{
			if (_isInitialized)
				throw new Exception("Client isn't initialized. Please call Init");

			HttpClient client = CreateClient(headers);

			var requestPath = requestMethod;
			if (parameters != null && parameters.Count > 0)
			{
				requestPath += "?";
				foreach (var parameter in parameters)
				{
					requestPath += parameter.Key + "=" + parameter.Value;
					if (parameter.Key != parameters.Last().Key)
						requestPath += "&";
				}
			}

			var result = await client.GetAsync(_baseUrl + requestPath);

			return await new Response<T>().Parse(result);
		}

		public static async Task<Response<T>> PostAsync<T>(string requestMethod, object obj = null, Dictionary<string, string> headers = null)
		{
			if (_isInitialized)
				throw new Exception("Client isn't initialized. Please call Init");

			HttpClient client = CreateClient(headers);

			var json = "{ }";

			if (obj != null)
			{
				json = JsonConvert.SerializeObject(obj);

				var token = JToken.Parse(json);
				CleanJson(token);

				json = JsonConvert.SerializeObject(token);
			}

			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var result = await client.PostAsync(_baseUrl + requestMethod, content);
			return await new Response<T>().Parse(result);
		}

		public static async Task<Response<T>> Delete<T>(string requestMethod)
		{
			if (_isInitialized)
				throw new Exception("Client isn't initialized. Please call Init");

			HttpClient client = CreateClient();

			var result = await client.DeleteAsync(_baseUrl + requestMethod);
			return await new Response<T>().Parse(result);
		}

		public static async Task<Response<T>> Put<T>(string requestMethod, object obj = null)
		{
			if (_isInitialized)
				throw new Exception("Client isn't initialized. Please call Init");

			HttpClient client = CreateClient();

			var json = "{ }";

			if (obj != null)
			{
				json = JsonConvert.SerializeObject(obj);

				var token = JToken.Parse(json);
				CleanJson(token);

				json = JsonConvert.SerializeObject(token);
			}

			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var result = await client.PutAsync(_baseUrl + requestMethod, content);
			return await new Response<T>().Parse(result);
		}
	}
}
