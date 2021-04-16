using DataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataAccessLibrary
{
    public class CsvFileDataAccess
    {
        private readonly string _csvFilePath;

        public CsvFileDataAccess(string csvFilePath)
        {
            _csvFilePath = csvFilePath;
        }

        public void WriteTradesToCsv(Dictionary<string, TradesModel> trades)
        {
            List<string> lines = new List<string>();

            lines.Add("DateTime,Symbol,Side,Price,Shares,Commission,ExecId");

            foreach (var trade in trades)
            {
                lines.Add($"{ trade.Value.Execution.Time},{trade.Value.Contract.Symbol},{trade.Value.Execution.Side}, {trade.Value.Execution.Price},{trade.Value.Execution.Shares},{trade.Value.CommissionReport.Commission},{trade.Value.Execution.ExecId}");
            }

            File.WriteAllLines(_csvFilePath, lines);
        }
    }
}
