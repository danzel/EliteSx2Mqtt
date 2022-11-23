using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace EliteSx2Mqtt.Tests;
public class BaseHelpers
{
	protected static async Task<T> XmlGet<T>(string path, string xml, Func<EliteSxClient, Task<T>> invocation)
	{
		var http = new MockHttpMessageHandler();

		http.When($"http://127.0.0.1/{path}?guid=")
			.Respond("text/xml", xml);
		var client = new EliteSxClient(NullLogger<EliteSxClient>.Instance, Options.Create(new EliteSxClientOptions
		{
			IpAddress = "127.0.0.1",
			UserName = "My User",
			Password = "1234"
		}), http.ToHttpClient());


		return await invocation(client);
	}
}
