using Microsoft.VisualBasic.FileIO;
using Sitecore.Diagnostics;
using SitecoreFundamentals.SendBatchUploadAndMerge.Constants;
using SitecoreFundamentals.SendBatchUploadAndMerge.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SitecoreFundamentals.SendBatchUploadAndMerge.Constants.Enums;

namespace SitecoreFundamentals.SendBatchUploadAndMerge.Tasks
{
    public class Send
    {
        public async Task<Hashtable> GetEmailListsForSelectionAsync()
        {
            var result = new Hashtable();

            using (var sendGateway = new Gateways.SendGateway())
            {
                AllMailingLists allMailingLists = null;
                try
                {
                    allMailingLists = await sendGateway.GetAllActiveMailingListsAsync();

                    foreach (var mailingList in allMailingLists.Context.MailingLists)
                    {
                        result.Add(mailingList.Name, mailingList.Id.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Info($"Error getting email lists. ({ex.Message})", this);
                    return result;
                }
            }

            result.Add(string.Empty, string.Empty);

            return result;
        }

        public async Task<Tuple<bool, string>> BatchEmailListUpdateAsync(string fileLocation, string mailingListID, string mode)
        {
            var message = "";
            var sb = new StringBuilder();
            var added = 0;
            var skipped = 0;
            var skippedUnsubscribed = 0;
            var updated = 0;

            if (fileLocation == "cancel")
            {
                message = $"File selection was cancelled.";
                sb.AppendLine(message);
                Log.Info(message, this);
                return new Tuple<bool, string>(false, sb.ToString());
            }
            else if (string.IsNullOrWhiteSpace(mailingListID))
            {
                message = $"No mailing list was selected.";
                sb.AppendLine(message);
                Log.Info(message, this);
                return new Tuple<bool, string>(false, sb.ToString());
            }
            else if (string.IsNullOrWhiteSpace(mode))
            {
                message = $"Run mode selection was cancelled.";
                sb.AppendLine(message);
                Log.Info(message, this);
                return new Tuple<bool, string>(false, sb.ToString());
            }

            using (var sendGateway = new Gateways.SendGateway())
            {
                message = $"Getting email list: {mailingListID}";
                sb.AppendLine(message);
                Log.Info(message, this);

                MailingListSubscribers allSubscribed = null;
                try
                {
                    allSubscribed = await sendGateway.GetAllSubscribersOfMailingListAsync(mailingListID, MailingListMemberStatus.Subscribed);
                }
                catch (Exception ex)
                {
                    message = $"Error retrieving email list. It is likely the list is not found. ({ex.Message})";
                    sb.AppendLine(message);
                    Log.Info(message, this);
                    return new Tuple<bool, string>(false, sb.ToString());
                }

                message = $"There are {allSubscribed.Context.Subscribers.Count} subscribed people in the current email list.";
                sb.AppendLine(message);
                Log.Info(message, this);


                MailingListSubscribers allUnsubscribed = null;
                try
                {
                    allUnsubscribed = await sendGateway.GetAllSubscribersOfMailingListAsync(mailingListID, MailingListMemberStatus.Unsubscribed);
                }
                catch (Exception ex)
                {
                    message = $"Error retrieving email list. It is likely the list is not found. ({ex.Message})";
                    sb.AppendLine(message);
                    Log.Info(message, this);
                    return new Tuple<bool, string>(false, sb.ToString());
                }

                message = $"There are {allUnsubscribed.Context.Subscribers.Count} unsubscribed people in the current email list.";
                sb.AppendLine(message);
                Log.Info(message, this);


                message = $"Loading CSV file {fileLocation}.";
                Log.Info(message, this);

                var dt = DataTableFromCsv(fileLocation);
                var columnCount = dt.Columns.Count;
                var rowCount = dt.Rows.Count;

                BatchEmailListCsvColumn[] columns = new BatchEmailListCsvColumn[columnCount];

                if (rowCount < 1)
                {
                    message = "CSV file does not contain any subscribers.";
                    sb.AppendLine(message);
                    Log.Info(message, this);
                    return new Tuple<bool, string>(false, sb.ToString());
                }

                var csvSubscribers = new List<AddToListSubscriber>();

                int i = 0;
                foreach (DataColumn column in dt.Columns)
                {
                    string columnName = column.ColumnName;

                    var columnType = Lists.BatchEmailListCsvColumns().FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                    if (columnType == null)
                    {
                        columnType = Lists.BatchEmailListCsvColumns().FirstOrDefault(c => c.IsCustom);
                        columnType.ColumnName = columnName;
                    }

                    columns[i] = columnType;

                    i++;
                }

                for (int m = 0; m < rowCount; m++)
                {
                    var subscriber = new AddToListSubscriber();

                    for (i = 0; i < columnCount; i++)
                    {
                        var columnType = columns[i];

                        if (columnType.IsSkipped)
                            continue;

                        var columnValue = dt.Rows[m][i].ToString();

                        if (columnType.IsStatus && columnValue.ToLower() != "active")
                        {
                            skipped++;
                            continue;
                        }
                        if (columnType.IsStandard)
                        {
                            if (columnType.ColumnName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                            {
                                subscriber.Name = columnValue;
                            }
                            else if (columnType.ColumnName.Equals("Email", StringComparison.OrdinalIgnoreCase))
                            {
                                subscriber.Email = columnValue;
                            }
                            else if (columnType.ColumnName.Equals("Mobile", StringComparison.OrdinalIgnoreCase))
                            {
                                subscriber.Mobile = columnValue;
                            }
                        }
                        else if (columnType.IsCustom)
                        {
                            subscriber.AddCustomField(columnType.ColumnName, columnValue);
                        }
                        else if (columnType.IsList)
                        {
                            if (!string.IsNullOrWhiteSpace(columnValue))
                            {
                                var tags = columnValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                      .Select(tag => tag.Trim())
                                                      .ToList();
                                subscriber.Tags.AddRange(tags);
                            }
                        }
                    }

                    csvSubscribers.Add(subscriber);
                }

                message = $"There are {csvSubscribers.Count()} records in the CSV file.";
                sb.AppendLine(message);
                Log.Info(message, this);

                foreach (var subscriber in csvSubscribers)
                {
                    var subscribedUser = allSubscribed.Context.Subscribers.FirstOrDefault(s => s.Email.Equals(subscriber.Email, StringComparison.OrdinalIgnoreCase));

                    if (subscribedUser != null)
                    {
                        if (subscribedUser.Tags.Any())
                        {
                            subscriber.Tags.AddRange(subscribedUser.Tags);
                            subscriber.Tags = subscriber.Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        }

                        updated++;
                    }
                    else
                    {
                        var unsubscribedUser = allUnsubscribed.Context.Subscribers.FirstOrDefault(s => s.Email.Equals(subscriber.Email, StringComparison.OrdinalIgnoreCase));

                        if (unsubscribedUser != null)
                        {
                            skippedUnsubscribed++;
                        }
                        else
                        {
                            added++;
                        }
                    }
                }

                var resultCounts = $"Added {added}, Updated {updated}, Skipped (unsubscribed): {skippedUnsubscribed}, Skipped (not active) {skipped}.";

                if (mode != "execute")
                {
                    message = $"When complete, the result will be:";
                    sb.AppendLine(message);
                    Log.Info(message, this);

                    sb.AppendLine(resultCounts);
                    Log.Info(resultCounts, this);

                    message = $"Program is in reporting mode. Exiting.";
                    sb.AppendLine(message);
                    Log.Info(message, this);

                    return new Tuple<bool, string>(true, sb.ToString());
                }

                var implementWait = false;

                int rateLimit;
                var rateLimitField = sendGateway.GlobalConfigItem.Fields[Templates.IntegrationSettings.Send.Fields.AddMultipleSubscribersRateLimit];
                if (!int.TryParse(rateLimitField?.Value, out rateLimit) || rateLimit < 1)
                    rateLimit = 2;

                int maxRecordsPerCall;
                var maxRecordsPerCallField = sendGateway.GlobalConfigItem.Fields[Templates.IntegrationSettings.Send.Fields.AddMultipleSubscribersMaxRecordsPerCall];
                if (!int.TryParse(maxRecordsPerCallField?.Value, out maxRecordsPerCall) || maxRecordsPerCall < 1)
                    maxRecordsPerCall = 1000;

                var numberOfCalls = (int)Math.Ceiling((double)csvSubscribers.Count / maxRecordsPerCall);

                if (csvSubscribers.Count > maxRecordsPerCall)
                {
                    implementWait = true;
                    message = $"There are more than {maxRecordsPerCall} so the API must be used in increments (total of {numberOfCalls}).";
                    sb.AppendLine(message);
                    Log.Info(message, this);
                }

                for (int j = 0; j < numberOfCalls; j++)
                {
                    var batchSubscribers = csvSubscribers.Skip(j * maxRecordsPerCall).Take(maxRecordsPerCall).ToList();
                    if (await sendGateway.AddMultipleSubscribersAsync(mailingListID, batchSubscribers))
                    {
                        message = $"Successfully processed {batchSubscribers.Count} subscribers in batch {j + 1} of {numberOfCalls}.";
                        sb.AppendLine(message);
                        Log.Info(message, this);
                    }
                    else
                    {
                        message = $"Failed to update mailing list due to error at Send API in batch {j + 1}.";
                        sb.AppendLine(message);
                        Log.Info(message, this);
                        return new Tuple<bool, string>(false, sb.ToString());
                    }

                    if (implementWait)
                    {
                        message = $"Waiting {rateLimit} seconds between calls.";
                        sb.AppendLine(message);
                        Log.Info(message, this);

                        Thread.Sleep(rateLimit * 1000);
                    }
                }

                message = $"Job finished. Processed {csvSubscribers.Count} subscribers.";
                sb.AppendLine(message);
                Log.Info(message, this);

                sb.AppendLine(resultCounts);
                Log.Info(resultCounts, this);
            }

            return new Tuple<bool, string>(true, sb.ToString());
        }

        private DataTable DataTableFromCsv(string fileLocation)
        {
            var table = new DataTable();

            using (var parser = new TextFieldParser(fileLocation))
            {
                parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                parser.SetDelimiters(";");

                var isHeader = true;
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();

                    if (isHeader)
                    {
                        foreach (string field in fields)
                            table.Columns.Add(field);

                        isHeader = false;
                    }
                    else
                    {
                        var row = table.NewRow();
                        for (int i = 0; i < fields.Length; i++)
                            row[i] = fields[i];

                        table.Rows.Add(row);
                    }
                }
            }

            return table;
        }
    }
}