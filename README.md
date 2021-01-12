# Slack.NetStandard.Endpoint.HttpRequest
Small library to help make building slack apps easier when you have access to an HttpRequest object


```csharp
  [FunctionName("slacktest")]
  public async Task<IActionResult> Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
      ILogger log)
  {
      var slackInfo = await Endpoint.Process(req);
      if(slackInfo.Command != null && slackInfo.Command.Command == "/weather"){
       ...
      }
  }
```
