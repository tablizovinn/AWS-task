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
using System.Runtime.InteropServices;

namespace TextractExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Set up your AWS credentials and region
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials("AKIAQ3EGU7XLRCMRPIV7", "sXOD098hyYQhcbpsqZRfcm/5fWXI8h52z9c8IdSm");
            var awsRegion = RegionEndpoint.APSoutheast1; // Change to your desired region

            // Initialize Textract client
            var textractClient = new AmazonTextractClient(awsCredentials, awsRegion);

            // Specify the S3 bucket and object key of the document
            string s3BucketName = "dadadada";
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

            try
            {
                // Call Amazon Textract
                var response = await textractClient.AnalyzeDocumentAsync(request);

                Table extractedTable = new Table();




                // Extract column header
                var headerTexts = response.Blocks
                                .Where(block => block.BlockType.Value == "CELL" && block.EntityTypes != null && block.EntityTypes.Contains("COLUMN_HEADER"))
                                .OrderBy(block => block.ColumnIndex)
                                .Select(block => block.Relationships.FirstOrDefault()?.Ids.FirstOrDefault())
                                .Select(id => response.Blocks.FirstOrDefault(block => block.Relationships.Any(rel => rel.Ids.Contains(id)))?.Text);

                foreach (var column in headerTexts)
                {
                    extractedTable.table.Columns.Add(column);
                }

                // Extract rows starting from the second row
                var rows = response.Blocks
                    .Where(block => block.BlockType.Value == "CELL" && block.RowIndex > 1) // Adjusted to start from the second row
                    .GroupBy(block => block.RowIndex);

                // Add row values to DataTable
                foreach (var row in rows)
                {
                    var dataRow = extractedTable.table.NewRow();
                    foreach (var cell in row.OrderBy(block => block.ColumnIndex))
                    {
                        // Adjust the column index by subtracting 1 to align with the DataTable column indexing
                        var columnIndex = cell.ColumnIndex - 1;

                        var relationshipId = cell.Relationships.FirstOrDefault()?.Ids.FirstOrDefault();
                        if (relationshipId != null)
                        {
                            var correspondingBlock = response.Blocks.FirstOrDefault(b => b.Relationships.Any(r => r.Ids.Contains(relationshipId))
                                && b.BlockType.Value == "LINE");
                            if (correspondingBlock != null)
                            {
                                // Set the cell value in the DataRow at the adjusted column index
                                dataRow[columnIndex] = correspondingBlock.Text;
                            }
                        }
                    }
                    extractedTable.table.Rows.Add(dataRow);
                }

                var json = JsonConvert.SerializeObject(extractedTable, Formatting.Indented);
                Console.WriteLine(json);































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