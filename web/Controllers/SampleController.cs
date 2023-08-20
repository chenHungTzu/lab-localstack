using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.S3.Model;
using System.Text.Json;

namespace web.Controllers;

[ApiController]
[Route("[controller]")]
public class SampleController : ControllerBase
{

    const string AWS_SERVICE_URL = "http://localhost:4566/"; //host.docker.internal
    const string AWS_AUTH_REGION = "ap-northeast-1";
    const string AWS_BUCKET_NAME = "sample-bucket";
    const string AWS_SNS_TOPIC_ARN = "arn:aws:sns:ap-northeast-1:000000000000:sample-topic";

    private AmazonS3Client? AmazonS3Client;
    private AmazonSimpleNotificationServiceClient? AmazonSimpleNotificationServiceClient;


    private readonly ILogger<SampleController> _logger;



    public SampleController(ILogger<SampleController> logger)
    {
        _logger = logger;

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

        var snsConfig = new AmazonSimpleNotificationServiceConfig()
        {
            ServiceURL = AWS_SERVICE_URL,
            AuthenticationRegion = AWS_AUTH_REGION,
            UseHttp = true,

        };

        AmazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(snsConfig);
    }

    [HttpPut("{name}")]
    public async Task Put(string name = "")
    {
        try
        {

            _logger.LogInformation($"Processed message :{name} ");

            var payload = new Sample_Result()
            {
                ID = Guid.NewGuid().ToString(),
                KEY = name
            };

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(JsonSerializer.Serialize(payload));
            writer.Flush();
            stream.Position = 0;


            var filename = $"v.{new Random().Next(0, 999999999).ToString().PadLeft(9, '0')}";
            var putobjRequest = new PutObjectRequest
            {
                BucketName = AWS_BUCKET_NAME,
                Key = $"{filename}.json",
                InputStream = stream
            };

            await AmazonS3Client.PutObjectAsync(putobjRequest);


            var publishRequest = new PublishRequest
            {
                TopicArn = AWS_SNS_TOPIC_ARN,
                Message = filename,
            };

            await AmazonSimpleNotificationServiceClient.PublishAsync(publishRequest);

        }
        catch (System.Exception ex)
        {

            throw ex;
        }
    }
}

public class Sample_Result
{
    public string ID { get; set; }
    public string KEY { get; set; }
}
