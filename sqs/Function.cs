using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace sqs;

public class Function
{
    const string AWS_SERVICE_URL = "http://localhost:4566/";
    const string AWS_DYNAMODB_TABLE_NAME = "sample-table";
    const string AWS_AUTH_REGION = "ap-northeast-1";

    private AmazonDynamoDBClient? AmazonDynamoDBClient;


    public Function()
    {
        InitS3Services();
    }

    private void InitS3Services()
    {
        var dynamoDBConfig = new AmazonDynamoDBConfig()
        {
            ServiceURL = AWS_SERVICE_URL,
            AuthenticationRegion = AWS_AUTH_REGION,
            UseHttp = true,

        };
        AmazonDynamoDBClient = new AmazonDynamoDBClient(dynamoDBConfig);

    }


    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed message {message.Body}");

        try
        {
            var text = message.Body;

            var payload = JsonSerializer.Deserialize<Sample_Result>(text);

            DateTime epochTime = DateTime.Parse("1970-01-01");

            var item = new Dictionary<string, AttributeValue>
            {
                ["ID"] = new AttributeValue { S = payload.ID },
                ["KEY"] = new AttributeValue { S = payload.KEY },
                ["DATETIME"] = new AttributeValue { N = payload.DATETIME.Subtract(epochTime).TotalMilliseconds.ToString() },
            };

            await AmazonDynamoDBClient.PutItemAsync(AWS_DYNAMODB_TABLE_NAME, item);

        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error saving {message.Body}: {ex.Message}");
            throw;
        }


        // TODO: Do interesting work based on the new message
        await Task.CompletedTask;
    }

    public class Sample_Result
    {
        public string ID { get; set; }
        public string KEY { get; set; }
        public DateTime DATETIME { get; set; }
    }

}
// {
//   "Records": [
//     {
//       "messageId": "19dd0b57-b21e-4ac1-bd88-01bbb068cb78",
//       "receiptHandle": "MessageReceiptHandle",
//       "body": "{\"ID\":\"0000000001\",\"KEY\":\"CHEN\",\"DATETIME\":\"2023-08-20T14:33:24.245478+08:00\"}",
//       "attributes": {
//         "ApproximateReceiveCount": "1",
//         "SentTimestamp": "1523232000000",
//         "SenderId": "123456789012",
//         "ApproximateFirstReceiveTimestamp": "1523232000001"
//       },
//       "messageAttributes": {},
//       "md5OfBody": "7b270e59b47ff90a553787216d55d91d",
//       "eventSource": "aws:sqs",
//       "eventSourceARN": "arn:{partition}:sqs:{region}:123456789012:MyQueue",
//       "awsRegion": "{region}"
//     }
//   ]
// }