using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;
using Newtonsoft.Json;
using ConsoleApp3;
using System.Data;
using System.Text;
using System.Net.Http;

namespace TextractExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Set up your AWS credentials and region
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials ("AKIATRMF5LV4HOEP45HR", "PjMUHt2sHUmfqd05v/ezbtbp6bbkwD4wTo0D9yum");
            var awsRegion = RegionEndpoint.APSoutheast1; // Change to your desired region

            // Initialize Textract client
            var textractClient = new AmazonTextractClient(awsCredentials, awsRegion);

            // Specify the S3 bucket and object key of the document
            string s3BucketName = "armansample";
            string s3ObjectKey = "example1img.jpg";

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

            try
            {
                // Call Amazon Textract
                var response = await textractClient.AnalyzeDocumentAsync(request);

               
                var extracted = new List<TextClass>();

                Console.WriteLine("\n\nExtracted text from PDF | Image:");

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

                        extracted.Add(rawText);
                    }
                }

                // Output Text
                foreach (var text in extracted)
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    };
                    string json = JsonConvert.SerializeObject(text, settings);

                    Console.WriteLine(json);
                }



                var extractedDataTable = new Table();
                foreach (var item in response.Blocks)
                {
                    if (item.BlockType == "CELL")
                    {
                        // Extract text from table cell
                        StringBuilder cellText = new StringBuilder();
                        foreach (var relationship in item.Relationships)
                        {
                            if (relationship.Type == "CHILD")
                            {
                                foreach (var childId in relationship.Ids)
                                {
                                    var childBlock = response.Blocks.Find(b => b.Id == childId);
                                    if (childBlock != null && childBlock.BlockType == "WORD")
                                    {
                                        cellText.Append(childBlock.Text);
                                        cellText.Append(" ");
                                    }
                                }
                            }
                        }

                        // Add the extracted text to the DataTable
                        extractedDataTable.Tables.Rows.Add(cellText.ToString().Trim());
                    }
                }

                Console.WriteLine("\nExtracted table data:");
                foreach (DataRow row in extractedDataTable.Tables.Rows)
                {
                    var json = JsonConvert.SerializeObject(row["TEXT"], Formatting.Indented);
                    Console.WriteLine(json);
                }







                // Create a new Form object to store key-value pairs
                Form form = new Form();
                // Get key and value maps
                List<Block> keyMap = new List<Block>();
                List<Block> valueMap = new List<Block>();
                List<Block> blockMap = response.Blocks.ToList();

                foreach (Block block in response.Blocks)
                {
                    var blockId = block.Id;
                    blockMap.Add(block);
                    if (block.BlockType == "KEY_VALUE_SET")
                    {
                        if (block.EntityTypes.Contains("KEY"))
                        {
                            keyMap.Add(block);
                        }
                        else
                        {
                            valueMap.Add(block);
                        }
                    }
                }

                // Get Key Value relationship and store it in the Form object
                form.KeyValueRelationship = GetKeyValueRelationship(keyMap, valueMap, blockMap);

                // Output key-value pairs
                
                var jsonForm = JsonConvert.SerializeObject(form, Formatting.Indented);
                Console.WriteLine(jsonForm);



            }

            catch (AmazonTextractException ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ReadLine();
            }

            Console.ReadLine();
        }

       
       

        public static Dictionary<string, string> GetKeyValueRelationship(List<Block> keyMap, List<Block> valueMap, List<Block> blockMap)
        {
            Dictionary<string, string> kvs = new Dictionary<string, string>();
            foreach (var block in keyMap)
            {
                var valueBlock = FindValueBlock(block, valueMap);
                var key = GetText(block, blockMap);
                var val = GetText(valueBlock, blockMap);

                // Check if key already exists in dictionary
                if (!kvs.ContainsKey(key))
                {
                    kvs.Add(key, val);
                }
                else
                {
                    Console.WriteLine("Key is already exist in the dictionary");
                }
            }
            return kvs;
        }


        public static Block FindValueBlock(Block block, List<Block> valueMap)
        {
            Block valueBlock = new Block();
            foreach (var relationship in block.Relationships)
            {
                if (relationship.Type == "VALUE")
                {
                    foreach (var valueId in relationship.Ids)
                    {
                        valueBlock = valueMap.First(x => x.Id == valueId);
                    }
                }
            }
            return valueBlock;
        }

        public static string GetText(Block result, List<Block> blockMap)
        {
            string text = string.Empty;
            if (result.Relationships.Count > 0)
            {
                foreach (var relationship in result.Relationships)
                {
                    if (relationship.Type == "CHILD")
                    {
                        foreach (var childId in relationship.Ids)
                        {
                            var word = blockMap.First(x => x.Id == childId);
                            if (word.BlockType == "WORD")
                            {
                                text += word.Text + " ";
                            }
                            if (word.BlockType == "SELECTION_ELEMENT")
                            {
                                if (word.SelectionStatus == "SELECTED")
                                {
                                    text += "X ";
                                }
                            }
                        }
                    }
                }
            }
            return text;
        }
    }
}
