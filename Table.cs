using Amazon.Textract.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class TableCell
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        public static async Task<TableCell> ExtractFromBlock(Block block, AnalyzeDocumentResponse response)
        {
            TableCell tableCell = new TableCell();

            if (block.BlockType == "CELL")
            {
                StringBuilder textBuilder = new StringBuilder();

                foreach (var relationship in block.Relationships)
                {
                    if (relationship.Type == "CHILD")
                    {
                        foreach (var childId in relationship.Ids)
                        {
                            var childBlock = response.Blocks.Find(b => b.Id == childId);
                            if (childBlock != null && childBlock.BlockType == "WORD")
                            {
                                textBuilder.Append(childBlock.Text);
                                textBuilder.Append(" ");
                            }
                        }
                    }
                }

                tableCell.Text = textBuilder.ToString().Trim();
                tableCell.Confidence = block.Confidence;
                tableCell.RowIndex = block.RowIndex;
                tableCell.ColumnIndex = block.ColumnIndex;
            }

            return tableCell;
        }
    }
}
