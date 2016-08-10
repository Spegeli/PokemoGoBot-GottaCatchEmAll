using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Helpers;
using POGOProtos.Networking.Envelopes;

namespace PokemonGo.RocketAPI.Extensions
{
    using System;

    public enum ApiOperation
    {
        Retry,
        Abort
    }

    public interface IApiFailureStrategy
    {
        Task<ApiOperation> HandleApiFailure(RequestEnvelope request, ResponseEnvelope response);
        void HandleApiSuccess(RequestEnvelope request, ResponseEnvelope response);
    }

    public static class HttpClientExtensions
    {
        public static async Task<IMessage[]> PostProtoPayload<TRequest>(this System.Net.Http.HttpClient client, 
            string url, RequestEnvelope requestEnvelope,
            params Type[] responseTypes) where TRequest : IMessage<TRequest>
        {
            var result = new IMessage[responseTypes.Length];
            for (var i = 0; i < responseTypes.Length; i++)
            {
                result[i] = Activator.CreateInstance(responseTypes[i]) as IMessage;
                if (result[i] == null)
                {
                    throw new ArgumentException($"ResponseType {i} is not an IMessage");
                }
            }

            var loopsOnBadRepsonse = 1;

            ResponseEnvelope response = await PostProto<TRequest>(client, url, requestEnvelope);
            while (response.Returns.Count != responseTypes.Length && loopsOnBadRepsonse <= 5)
            {
                //Logger.Write($"Bad Payload Repsonse. Retry {loopsOnBadRepsonse} of 5 <- IGNORE THIS FUCKING MESSAGE...I KNOW IT", LogLevel.Warning);
                await RandomHelper.RandomDelay(250, 500);
                response = await PostProto<TRequest>(client, url, requestEnvelope);
                loopsOnBadRepsonse += 1;
            }

            if (response.Returns.Count != responseTypes.Length)
                throw new InvalidResponseException($"Expected {responseTypes.Length} responses, but got {response.Returns.Count} responses");

            for (var i = 0; i < responseTypes.Length; i++)
            {
                var payload = response.Returns[i];
                result[i].MergeFrom(payload);
            }
            return result;
        }

        public static async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(this System.Net.Http.HttpClient client,
            string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            Debug.WriteLine($"Requesting {typeof(TResponsePayload).Name}");
            var loopsOnBadRepsonse = 1;

            var response = await PostProto<TRequest>(client, url, requestEnvelope);
            while (response.Returns.Count == 0 && loopsOnBadRepsonse <= 5)
            {
                //Logger.Write($"Bad Payload Repsonse. Retry {loopsOnBadRepsonse} of 5 <- IGNORE THIS FUCKING MESSAGE...I KNOW IT", LogLevel.Warning);
                await RandomHelper.RandomDelay(250, 500);
                response = await PostProto<TRequest>(client, url, requestEnvelope);
                loopsOnBadRepsonse += 1;    
            }

            if (response.Returns.Count == 0)
                throw new InvalidResponseException();

            //Decode payload
            //todo: multi-payload support
            var payload = response.Returns[0];
            var parsedPayload = new TResponsePayload();
            parsedPayload.MergeFrom(payload);

            return parsedPayload;
        }

        public static async Task<ResponseEnvelope> PostProto<TRequest>(this System.Net.Http.HttpClient client, string url,
            RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            //Encode payload and put in envelop, then send
            var data = requestEnvelope.ToByteString();
            var result = await client.PostAsync(url, new ByteArrayContent(data.ToByteArray()));

            //Decode message
            var responseData = await result.Content.ReadAsByteArrayAsync();
            var codedStream = new CodedInputStream(responseData);
            var decodedResponse = new ResponseEnvelope();
            decodedResponse.MergeFrom(codedStream);

            return decodedResponse;
        }
    }
}