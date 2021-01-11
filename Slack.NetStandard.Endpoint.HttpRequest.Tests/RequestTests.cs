using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Slack.NetStandard.Endpoint.HttpRequest.Tests
{
    public class RequestTests
    {
        [Fact]
        public async Task InvalidHeadersProduceUnknownRequestType()
        {
            var request = Substitute.For<Microsoft.AspNetCore.Http.HttpRequest>();
            request.Headers.Returns(new HeaderDictionary());
            var endpoint = new HttpRequestEndpoint("xxx");
            var result = await endpoint.Process(request);
            Assert.Equal(SlackRequestType.UnknownRequest, result.Type);
        }

        [Fact]
        public async Task InvalidSignatureProducesNotVerifiedRequest()
        {
            var request = Substitute.For<Microsoft.AspNetCore.Http.HttpRequest>();
            request.Headers.Returns(new HeaderDictionary
            {
                {RequestVerifier.SignatureHeaderName,"v0=a2114d57b48eac39b9ad189dd8316235a7b4a8d21a10bd27519666489c69b503"},
                {RequestVerifier.TimestampHeaderName,"1531420617" },
                {"Content-Type","application/x-www-form-urlencoded" }
            });
            SetSubstituteBody(request);
            var endpoint = new HttpRequestEndpoint(TestSigningSecret,false,TimeSpan.FromDays(5000));
            var result = await endpoint.Process(request);
            Assert.Equal(SlackRequestType.NotVerifiedRequest, result.Type);
        }

        public string TestSigningSecret => "8f742231b10e8888abcd99yyyzzz85a5";

        [Fact]
        public async Task CommandParsesCorrectly()
        {
            var request = Substitute.For<Microsoft.AspNetCore.Http.HttpRequest>();
            request.Headers.Returns(new HeaderDictionary
            {
                {RequestVerifier.SignatureHeaderName,"v0=a2114d57b48eac39b9ad189dd8316235a7b4a8d21a10bd27519666489c69b503"},
                {RequestVerifier.TimestampHeaderName,"1531420618" },
                {"Content-Type","application/x-www-form-urlencoded" }
            });
            SetSubstituteBody(request);
            var endpoint = new HttpRequestEndpoint(TestSigningSecret, false, TimeSpan.FromDays(5000));
            var result = await endpoint.Process(request);
            Assert.Equal(SlackRequestType.Command, result.Type);
        }

        private void SetSubstituteBody(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            var ms = new MemoryStream();
            using var sw = new StreamWriter(ms, Encoding.UTF8, -1, true);
            sw.Write("token=xyzz0WbapA4vBCDEFasx0q6G&team_id=T1DC2JH3J&team_domain=testteamnow&channel_id=G8PSS9T3V&channel_name=foobar&user_id=U2CERLKJA&user_name=roadrunner&command=%2Fwebhook-collect&text=&response_url=https%3A%2F%2Fhooks.slack.com%2Fcommands%2FT1DC2JH3J%2F397700885554%2F96rGlfmibIGlgcZRskXaIFfN&trigger_id=398738663015.47445629121.803a0bc887a14d10d2c447fce8b6703c");
            sw.Flush();
            sw.Close();
            ms.Seek(0, SeekOrigin.Begin);
            request.Body.Returns(ms);
        }
    }
}
