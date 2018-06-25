using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RestClient
{
	public class Response<T>
	{
		public T Data { get; private set; }
		public HttpStatusCode StatusCode { get; private set; }
		public string Json { get; private set; }
		public Exception Error { get; private set; }

		public async Task<Response<T>> Parse(HttpResponseMessage result)
		{
			StatusCode = result.StatusCode;

			try
			{
				Json = await result.Content.ReadAsStringAsync();

				if (result.IsSuccessStatusCode)
				{
					Data = JsonConvert.DeserializeObject<T>(Json, new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore
					});
				}
			}
			catch (Exception error)
			{
				Error = error;
			}

			return this;
		}
	}
}
