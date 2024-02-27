using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;
using Newtonsoft.Json;
using System;
using ConsoleApp3;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
namespace TextractExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Set up your AWS credentials and region
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials("AKIATRMF5LV4HOEP45HR", "PjMUHt2sHUmfqd05v/ezbtbp6bbkwD4wTo0D9yum");
            var awsRegion = RegionEndpoint.APSoutheast1; // Change to your desired region

            // Initialize Textract client
            var textractClient = new AmazonTextractClient(awsCredentials, awsRegion);

            // Specify the S3 bucket and object key of the document
            string s3BucketName = "armansample";
            string s3ObjectKey = "sampleimg1.jpg";

            // Call function to extract text from form and table
            await ExtractTextFromDocument(textractClient, s3BucketName, s3ObjectKey);
        }

        static async Task ExtractTextFromDocument(AmazonTextractClient textractClient, string s3BucketName, string s3ObjectKey)
        {
            // Configure request
            var request = new AnalyzeDocumentRequest
            {
                Document = new Document
                {
                    S3Object = new S3Object
                    {
                        Bucket = s3BucketName,
                        Name = s3ObjectKey
                    }
                },
                FeatureTypes = new List<string> { "TABLES", "FORMS" }
            };

            // Call Amazon Textract
            var response = await textractClient.AnalyzeDocumentAsync(request);

            var Extracted = new List<TextClass>();
            // Output extracted text

            
            // Output extracted text


          Console.WriteLine("\nExtracted text from PDF | Image:");

            foreach (var item in response.Blocks)
            {
                if (item.BlockType == "WORD")
                {

                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    };
                    string json = JsonConvert.SerializeObject(item, settings);



                    TextClass rawText = JsonConvert.DeserializeObject<TextClass>(json);

                   Extracted.Add(rawText);            
                }
            }

            //Output Text          
            foreach (var text in Extracted)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                string json = JsonConvert.SerializeObject(text, settings);

                Console.WriteLine(json);
            }
     

            




            var dictionary = new Dictionary<string, object>();

            Console.WriteLine("\nExtracted form:");
            
            foreach (var item in response.Blocks)
            {

                if(item.BlockType == "KEY_VALUE_SET")
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    };
                    string json = JsonConvert.SerializeObject(item, settings);

                    Console.WriteLine(json);
                }

            }
            //



     /*       //Output Text          
            foreach (var text in Extracted)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                string json = JsonConvert.SerializeObject(text, settings);

                Console.WriteLine(json);
            }
     */
            











            Console.ReadLine();
        }
    }
   
}