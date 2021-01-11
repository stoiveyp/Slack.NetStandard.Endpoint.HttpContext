using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http.Internal;
using Newtonsoft.Json;
using Slack.NetStandard.EventsApi;
using Slack.NetStandard.Interaction;

namespace Slack.NetStandard.Endpoint.HttpRequest
{
    public class HttpRequestEndpoint:SlackEndpoint<Microsoft.AspNetCore.Http.HttpRequest>
    {
        public HttpRequestEndpoint(string signingSecret, bool requireBodyRewind = false, TimeSpan? verifierTolerance = null)
        {
            Verifier = new RequestVerifier(signingSecret,verifierTolerance);
            RequireBodyRewind = requireBodyRewind;
        }

        protected bool RequireBodyRewind { get; set; }
        protected RequestVerifier Verifier { get; set; }

        private bool HasHeaders(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            return request.Headers.ContainsKey(RequestVerifier.SignatureHeaderName) &&
                   request.Headers.ContainsKey(RequestVerifier.TimestampHeaderName) &&
                   long.TryParse(request.Headers[RequestVerifier.TimestampHeaderName], out var _);
        }

        protected virtual async Task<(bool Result, string Body)> IsValid(RequestVerifier verifier, Microsoft.AspNetCore.Http.HttpRequest request)
        {
            long.TryParse(request.Headers[RequestVerifier.TimestampHeaderName], out var timestamp);
            if (RequireBodyRewind)
            {
                request.EnableRewind();
            }

            using var sr = new StreamReader(request.Body);
            var bodyText = await sr.ReadToEndAsync();
            if (RequireBodyRewind)
            {
                request.Body.Position = 0;
            }
            var result = verifier.Verify(request.Headers[RequestVerifier.SignatureHeaderName], timestamp, bodyText);
            return (result, bodyText);
        }

        protected override async Task<SlackInformation> GenerateInformation(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            var validHeaders = HasHeaders(request);
            if (!validHeaders)
            {
                return new SlackInformation(SlackRequestType.UnknownRequest);
            }

            var validity = await IsValid(Verifier, request);

            if (!validity.Result)
            {
                return new SlackInformation(SlackRequestType.NotVerifiedRequest);
            }

            var result = request.Headers["Content-Type"].First() switch
            {
                "application/json" => new SlackInformation(JsonConvert.DeserializeObject<Event>(validity.Body)),
                "application/x-www-form-urlencoded" => validity.Body.StartsWith("payload=") ?
                    new SlackInformation(JsonConvert.DeserializeObject<InteractionPayload>(HttpUtility.UrlDecode(validity.Body.Substring(8)))) :
                    new SlackInformation(new SlashCommand(validity.Body)),
                _ => new SlackInformation(SlackRequestType.UnknownRequest)
            };

            return result;
        }
    }
}
