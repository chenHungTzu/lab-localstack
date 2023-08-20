using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace sns;

public class Function
{

    const string AWS_SERVICE_URL = "http://localhost:4566/"; //host.docker.internal
    const string AWS_QUEUE_URL = "http://localhost:4566/000000000000/sample-queue"; //host.docker.internal
    const string AWS_AUTH_REGION = "ap-northeast-1";
    const string AWS_BUCKET_NAME = "sample-bucket";

    private AmazonS3Client? AmazonS3Client;
    private AmazonSQSClient? AmazonSQSClient;

    public Function()
    {
        InitS3Services();
    }

    private void InitS3Services()
    {
        var s3Config = new AmazonS3Config()
        {
            ServiceURL = AWS_SERVICE_URL,
            AuthenticationRegion = AWS_AUTH_REGION,
            UseHttp = true,
            ForcePathStyle = true,
        };
        AmazonS3Client = new AmazonS3Client(s3Config);

        var sqsConfig = new AmazonSQSConfig()
        {
            ServiceURL = AWS_SERVICE_URL,
            AuthenticationRegion = AWS_AUTH_REGION,
            UseHttp = true,

        };

        AmazonSQSClient = new AmazonSQSClient(sqsConfig);
    }



    public async Task FunctionHandler(SNSEvent evnt, ILambdaContext context)
    {
        foreach (var record in evnt.Records)
        {
            await ProcessRecordAsync(record, context);
        }
    }

    private async Task ProcessRecordAsync(SNSEvent.SNSRecord record, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed record {record.Sns.Message}");

        try
        {

            // Create a GetObject request
            var request = new GetObjectRequest
            {
                BucketName = AWS_BUCKET_NAME,
                Key = $"{record.Sns.Message}.json",
            };

            using GetObjectResponse response = await AmazonS3Client.GetObjectAsync(request);
            using Stream responseStream = response.ResponseStream;

            var bytes = ReadStream(responseStream);
            var text = System.Text.Encoding.UTF8.GetString(bytes);
            var unescapeText = text.Replace("\t", "").Replace("\n", "").Replace("\r", "").Replace(@"\", "");
            var result = JsonSerializer.Deserialize<Sample_Result>(unescapeText);

            result.DATETIME = DateTime.Now;

            await AmazonSQSClient.SendMessageAsync(AWS_QUEUE_URL, JsonSerializer.Serialize(result));

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving {record.Sns.Message}: {ex.Message}");
            throw;
        }

        // TODO: Do interesting work based on the new message
        await Task.CompletedTask;
    }

    public static byte[] ReadStream(Stream responseStream)
    {
        byte[] buffer = new byte[16 * 1024];
        using (MemoryStream ms = new MemoryStream())
        {
            int read;
            while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }
}

public class Sample_Result
{
    public string ID { get; set; }
    public string KEY { get; set; }
    public DateTime DATETIME { get; set; }
}

// {
//   "Records": [
//     {
//       "EventSource": "aws:sns",
//       "EventVersion": "1.0",
//       "EventSubscriptionArn": "arn:{partition}:sns:EXAMPLE",
//       "Sns": {
//         "Type": "Notification",
//         "MessageId": "95df01b4-ee98-5cb9-9903-4c221d41eb5e",
//         "TopicArn": "arn:{partition}:sns:EXAMPLE",
//         "Subject": "TestInvoke",
//         "Message": "v.0000000001",
//         "Timestamp": "1970-01-01T00:00:00Z",
//         "SignatureVersion": "1",
//         "Signature": "EXAMPLE",
//         "SigningCertUrl": "EXAMPLE",
//         "UnsubscribeUrl": "EXAMPLE",
//         "MessageAttributes": {
//           "Test": {
//             "Type": "String",
//             "Value": "TestString"
//           },
//           "TestBinary": {
//             "Type": "Binary",
//             "Value": "TestBinary"
//           }
//         }
//       }
//     }
//   ]
// }
