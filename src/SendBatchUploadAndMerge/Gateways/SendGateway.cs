using Newtonsoft.Json;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using SitecoreFundamentals.SendBatchUploadAndMerge.Constants;
using SitecoreFundamentals.SendBatchUploadAndMerge.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static SitecoreFundamentals.SendBatchUploadAndMerge.Constants.Enums;

namespace SitecoreFundamentals.SendBatchUploadAndMerge.Gateways
{
    public class SendGateway : IDisposable
    {
        private readonly HttpClient _httpClient;
        internal Item GlobalConfigItem { get; set; }

        public SendGateway()
        {
            _httpClient = new HttpClient();

            var configItemID = Items.IntegrationSettings.ID;

            var databaseName = Helpers.RoleDefinition.MasterOrWebDatabase();

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                Log.Error("Database name is not set based on role. Cannot initialize SendGateway.", this);
                throw new InvalidOperationException("Database name is not set based on role. Cannot initialize SendGateway.");
            }

            var contextDb = Factory.GetDatabase(databaseName);

            GlobalConfigItem = contextDb.Items.GetItem(configItemID);

            if (GlobalConfigItem == null)
            {
                Log.Error($"Configuration item with ID {configItemID} not found.", this);
                throw new InvalidOperationException($"Configuration item with ID {configItemID} not found.");
            }
        }

        public async Task<AllMailingLists> GetAllActiveMailingListsAsync()
        {
            var endpointFormatter = new GatewayUtility.EndpointFormatter(_httpClient, GlobalConfigItem, Templates.IntegrationSettings.Send.Fields.GetAllActiveEmailLists);

            var endpoint = endpointFormatter.Process();

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Log.Error("Endpoint is not configured.", this);
                throw new InvalidOperationException("Endpoint is not configured.");
            }

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Failed to get email lists. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}", this);
                throw new HttpRequestException($"Failed to get email lists. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            AllMailingLists mailingLists;

            try
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                mailingLists = JsonConvert.DeserializeObject<AllMailingLists>(responseContent, jsonSerializerSettings);
            }
            catch (Exception ex)
            {
                Log.Error("Error deserializing email lists.", ex, this);
                throw;
            }

            if (mailingLists == null)
            {
                Log.Error("Deserialization of email lists returned null.", this);
                throw new InvalidOperationException("Deserialization of email lists returned null.");
            }

            if (mailingLists.Code != 0)
            {
                Log.Error($"Error in response: {mailingLists.Error}", this);
                throw new InvalidOperationException($"Error in response: {mailingLists.Error}");
            }

            return mailingLists;
        }

        public async Task<ResponseBase> AddMultipleSubscribersAsync(string mailingListID, List<AddToListSubscriber> subscribers)
            => await AddMultipleSubscribersAsync(mailingListID, new AddMultipleSubscribers { Subscribers = subscribers });

        public async Task<ResponseBase> AddMultipleSubscribersAsync(string mailingListID, AddMultipleSubscribers addMultipleSubscribers)
        {
            if (addMultipleSubscribers == null
                || addMultipleSubscribers.Subscribers == null
                || addMultipleSubscribers.Subscribers.Count == 0)
                return new ResponseBase()
                {
                    Code = 1
                };

            var endpointFormatter = new GatewayUtility.EndpointFormatter(_httpClient, GlobalConfigItem, Templates.IntegrationSettings.Send.Fields.AddMultipleSubscribers);

            endpointFormatter.AddToken("mailingListID", mailingListID);

            var endpoint = endpointFormatter.Process();

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Log.Error("Endpoint is not configured.", this);
                throw new InvalidOperationException("Endpoint is not configured.");
            }

            var json = JsonConvert.SerializeObject(addMultipleSubscribers);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Failed to post subscribers. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}", this);
                return new ResponseBase()
                {
                    Code = 1
                };
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            ResponseBase result = null;

            try
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                result = JsonConvert.DeserializeObject<ResponseBase>(responseContent, jsonSerializerSettings);
            }
            catch (Exception ex)
            {
                Log.Error($"Error deserializing subscribers.", ex, this);
                throw;
            }

            return result;
        }

        public async Task<MailingListSubscribers> GetAllSubscribersOfMailingListAsync(string mailingListID, MailingListMemberStatus status)
        {
            var endpointFormatter = new GatewayUtility.EndpointFormatter(_httpClient, GlobalConfigItem, Templates.IntegrationSettings.Send.Fields.GetAllSubscribersofEmailList);

            endpointFormatter.AddToken("mailingListID", mailingListID);
            endpointFormatter.AddToken("Status", status.ToString());

            var endpoint = endpointFormatter.Process();

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Log.Error("Endpoint is not configured.", this);
                throw new InvalidOperationException("Endpoint is not configured.");
            }

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Failed to get email list subscribers. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}", this);
                throw new HttpRequestException($"Failed to get email list subscribers. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            MailingListSubscribers subscribers;

            try
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                subscribers = JsonConvert.DeserializeObject<MailingListSubscribers>(responseContent, jsonSerializerSettings);
            }
            catch (Exception ex)
            {
                Log.Error("Error deserializing subscribers.", ex, this);
                throw;
            }

            if (subscribers == null)
            {
                Log.Error("Deserialization of subscribers returned null.", this);
                throw new InvalidOperationException("Deserialization of subscribers returned null.");
            }

            if (subscribers.Code != 0)
            {
                Log.Error($"Error in response: {subscribers.Error}", this);
                throw new InvalidOperationException($"Error in response: {subscribers.Error}");
            }

            return subscribers;
        }

        public void Dispose()
        {
            Log.Debug("Disposing resources.", this);

            _httpClient?.Dispose();
        }
    }
}