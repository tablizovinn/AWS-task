using Amazon.Textract.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class Table
    {
        // Property to store table data
        public System.Data.DataTable Tables { get; }

        // Constructor to initialize the table
        public Table()
        {
            Tables = new System.Data.DataTable();
            Tables.Columns.Add("Text", typeof(string)); // Add a single column for text
        }
        
    }
    

}
