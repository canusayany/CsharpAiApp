public class AiSetting
  {
      public Service[] Services { get; set; }
      public string SelectServiceName { get; set; }
      public Relevant_Example[] relevant_example { get; set; }
  }

  public class Service
  {
      public string Name { get; set; }
      public string Type { get; set; }
      public string Endpoint { get; set; }
      public string ApiKey { get; set; }
      public string Model { get; set; }
      public string SystemPrompt { get; set; }
  }

  public class Relevant_Example
  {
      public string description { get; set; }
      public string lua_code { get; set; }
  }
  public class AiChatClientFactory
  {
      public static string ServiceName;
      public static IChatClient ChatClient { get; set; }
      public static void StartNewChatClient()
      {
          if (ChatClient != null)
          {
              ChatClient.Dispose();
          }
      }

      // public string 
      public static IChatClient CreateAiChatClient(string serviceName)
      {
          var aiSetting = LoadAiSettings();
          var service = aiSetting.Services.FirstOrDefault(s => s.Name == serviceName);
          if (service == null)
          {
              throw new Exception("未找到指定的服务");
          }
          SystemPrompt = service.SystemPrompt;
          return CreateChatClient(service);
      }

      public static IChatClient CreateAiChatClient()
      {
          var aiSetting = LoadAiSettings();
          var service = aiSetting.Services.FirstOrDefault(s => s.Name == aiSetting.SelectServiceName);
          if (service == null)
          {
              throw new Exception("未找到指定的服务");
          }
          return CreateChatClient(service);
      }

      private static AiSetting LoadAiSettings()
      {
          IJson json = DI.Resolve<IJson>();
          string str = File.ReadAllText("ai-config.json");
          AiSetting ai = json.Deserialize<AiSetting>(str);
          Relevant_Example = json.Serialize(ai.relevant_example);
          return ai;
      }

      private static IChatClient CreateChatClient(Service service)
      {
          SystemPrompt = service.SystemPrompt;
          return service.Type switch
          {
              "Ollama" => new OllamaChatClient(service.Endpoint, service.Model),
              "OpenAI" => new OpenAIClient(new ApiKeyCredential(service.ApiKey), new OpenAIClientOptions() { Endpoint = new Uri(service.Endpoint) }).AsChatClient(service.Model),
              _ => throw new Exception("不支持的服务类型"),
          };
      }

      private static string SystemPrompt { get; set; }
      private static string Relevant_Example { get; set; }
      public static async IAsyncEnumerable<string> GetResponseAsync(string userInput, CancellationToken cancellationToken, string codeDataContext = "")
      {
          if (ChatClient == null)
          {
              if (string.IsNullOrEmpty(ServiceName))
              {
                  ChatClient = CreateAiChatClient();
              }
              else
              {
                  ChatClient = CreateAiChatClient(ServiceName);
              }
          }
          userInput = $"{SystemPrompt}\n参考示例:{Relevant_Example}\n 当前代码上下文:{codeDataContext} 用户问题为:{userInput}";
          await foreach (var token in ChatClient.GetStreamingResponseAsync(userInput, null, cancellationToken))
          {
              yield return token.Text ?? string.Empty;
          }

      }
  }
  await foreach (var token in AiChatClientFactory.GetResponseAsync(currentInput, _cancellationTokenSource.Token, currentCode))
 {
     if (isFirstToken)
     {
         // 收到第一个token时，清空"思考中..."文本
         UpdateMessage(aiMessage, "");
         isFirstToken = false;
     }

     // 追加新的token
     AppendToMessage(aiMessage, token);

     // 添加一个小延迟，确保UI有时间更新
     await Task.Delay(1);
 }

