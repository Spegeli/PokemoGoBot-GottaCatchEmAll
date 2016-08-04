#region

using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.GeneratedCode;
using System.Diagnostics;
using PokemonGo.RocketAPI.Helpers;

#endregion

namespace PokemonGo.RocketAPI.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(this HttpClient client,
            string url, TRequest request) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            Debug.WriteLine($"Requesting {typeof(TResponsePayload).Name}");
            var counter = 1;

            var response = await PostProto(client, url, request);
            while (response.Payload.Count == 0 && counter <= 5)
            {
                //if (response.Payload.Count == 0)
                    //Logger.Write($"Bad Payload Repsonse. Retry {counter} of 5 <- IGNORE THIS FUCKING MESSAGE...I KNOW IT", LogLevel.Warning);
                await RandomHelper.RandomDelay(200, 300);
                response = await PostProto(client, url, request);
                counter += 1;
            }
            if (response.Payload.Count == 0)
            {
                throw new InvalidResponseException();
            }
            var payload = response.Payload[0];
            var parsedPayload = new TResponsePayload();
            parsedPayload.MergeFrom(payload);

            return parsedPayload;
        }

        public static async Task<Response> PostProto<TRequest>(this HttpClient client, string url, TRequest request)
            where TRequest : IMessage<TRequest>
        {
            //Encode payload and put in envelop, then send
            var data = request.ToByteString();
            var result = await client.PostAsync(url, new ByteArrayContent(data.ToByteArray()));

            //Decode message
            var responseData = await result.Content.ReadAsByteArrayAsync();
            var codedStream = new CodedInputStream(responseData);
            var decodedResponse = new Response();
            decodedResponse.MergeFrom(codedStream);

            return decodedResponse;
        }
    }
}