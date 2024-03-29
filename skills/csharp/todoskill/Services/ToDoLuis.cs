// <auto-generated>
// Code generated by luis:generate:cs
// Tool github: https://github.com/microsoft/botframework-cli
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace Luis
{
    public partial class ToDoLuis: IRecognizerConvert
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("alteredText")]
        public string AlteredText;

        public enum Intent {
            AddToDo,
            DeleteToDo,
            MarkToDo,
            None,
            ShowNextPage,
            ShowPreviousPage,
            ShowToDo
        };
        [JsonProperty("intents")]
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] ContainsAll;
            public string[] ListType;
            public string[] TaskContent;

            // Built-in entities
            public double[] ordinal;

            // Lists
            public string[][] FoodOfGrocery;
            public string[][] ShopVerb;


            // Instance
            public class _Instance
            {
                public InstanceData[] ContainsAll;
                public InstanceData[] FoodOfGrocery;
                public InstanceData[] ListType;
                public InstanceData[] ShopVerb;
                public InstanceData[] TaskContent;
                public InstanceData[] ordinal;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        [JsonProperty("entities")]
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<ToDoLuis>(
                JsonConvert.SerializeObject(
                    result,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MaxDepth = null, Error = OnError }
                )
            );
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        private static void OnError(object sender, ErrorEventArgs args)
        {
            // If needed, put your custom error logic here
            Console.WriteLine(args.ErrorContext.Error.Message);
            args.ErrorContext.Handled = true;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
