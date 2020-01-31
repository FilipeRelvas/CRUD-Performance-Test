// ==========================================================================
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// ==========================================================================
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Pfe.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;

namespace CRUDPerformanceTest
{
    public static class UpdateHelperMethods
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Executes multiple updates concurrently within one or more Batches, with each batch limited to 1000 records, based on the provided FetchXML.
        /// </summary>
        /// <param name="serviceClient"></param>
        /// <param name="serviceManager"></param>
        /// <param name="totalRequestsPerBatch"></param>
        /// <returns></returns>
        public static List<Guid> UpdateFetchXml(CrmServiceClient serviceClient, OrganizationServiceManager serviceManager, int totalRequestsPerBatch)
        {
            EntityCollection recordsToBeUpdated = RetrieveHelperMethods.RetrieveMultipleFetchXml(serviceClient);
            return UpdateParallelExecuteMultiple(serviceManager, recordsToBeUpdated, totalRequestsPerBatch);
        }

        /// <summary>
        /// Executes multiple updates concurrently within one or more Batches, with each batch limited to 1000 records.
        /// </summary>
        /// <param name="serviceManager"></param>
        /// <param name="recordsToBeUpdated"></param>
        /// <param name="totalRequestsPerBatch"></param>
        /// <returns></returns>
        public static List<Guid> UpdateParallelExecuteMultiple(OrganizationServiceManager serviceManager, EntityCollection recordsToBeUpdated, int totalRequestsPerBatch)
        {
            int batchSize = 0;
            int recordsToBeUpdatedCount = recordsToBeUpdated.Entities.Count;
            int batchNumber = (int)Math.Ceiling((recordsToBeUpdated.Entities.Count * 1.0d) / (totalRequestsPerBatch * 1.0d));

            Console.WriteLine();
            log.Info("Update Mode: Parallel Execute Multiple");
          
            List<Guid> ids = new List<Guid>();
            List<string> updatedAttributes = new List<string>(); // List of updated attributes to be logged
            IDictionary<string, ExecuteMultipleRequest> requests = new Dictionary<string, ExecuteMultipleRequest>();

            for (int i = 0; i < batchNumber; i++)
            {
                ExecuteMultipleRequest executeMultipleRequest = new ExecuteMultipleRequest()
                {
                    Requests = new OrganizationRequestCollection(),
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    RequestId = Guid.NewGuid()
                };

                for (int j = recordsToBeUpdated.Entities.Count - 1; j >= 0; j--)
                {
                    ids.Add(recordsToBeUpdated.Entities[j].Id);
                    UpdateStringAttributes(recordsToBeUpdated.Entities[j], updatedAttributes);

                    UpdateRequest updateRequest = new UpdateRequest()
                    {
                        Target = recordsToBeUpdated.Entities[j],
                        RequestId = Guid.NewGuid()
                    };
                    executeMultipleRequest.Requests.Add(updateRequest);
                    recordsToBeUpdated.Entities.RemoveAt(j);

                    if (batchSize == totalRequestsPerBatch - 1) // If we reach the batch limit, break from the loop
                    {
                        break;
                    }
                    batchSize++;
                }

                batchSize = 0;
                requests.Add(new KeyValuePair<string, ExecuteMultipleRequest>(i.ToString(), executeMultipleRequest));
                log.InfoFormat("Request Id for request batch number {0}: {1}", i, executeMultipleRequest.RequestId);
            }

            // Log the updated attributes
            log.Info("Attribute(s) to be updated:");
            LogUpdatedAttributes(updatedAttributes);
            log.InfoFormat("Updating {0} record(s)...", recordsToBeUpdatedCount);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Parallel execution of all ExecuteMultipleRequest in the requests Dictionary
            IDictionary<string, ExecuteMultipleResponse> responseForUpdatedRecords = serviceManager.ParallelProxy.Execute<ExecuteMultipleRequest, ExecuteMultipleResponse>(requests);
            int threadsCount = Process.GetCurrentProcess().Threads.Count;
            sw.Stop();

            log.InfoFormat("Number of threads used: {0}", threadsCount);
            log.InfoFormat("Seconds to Update {0} record(s): {1}s", recordsToBeUpdatedCount, sw.Elapsed.TotalSeconds);

            return ids;
        }

        /// <summary>
        /// Updates the attributes of type "string" corresponding to the entity passed as parameter with a default value.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static Entity UpdateStringAttributes(Entity entity, List<string> updatedAttributes)
        {
            string[] attributeKeysToBeUpdated = RetrieveHelperMethods.ConvertKeyStringToList("AttributesToUpdate");

            // Attributes passed through the fetchXML (Filters by string)
            foreach (KeyValuePair<string, object> attribute in entity.Attributes.ToList())
            {
                if (attribute.Value is string)
                {
                    entity.Attributes[attribute.Key] = attribute.Key + ":" + entity.Id;

                    if (!updatedAttributes.Contains(attribute.Key))
                    {
                        updatedAttributes.Add(attribute.Key); // Add the updated attribute to be logged.
                    }               
                }
            }
            
            // Attributes keys passed through the App.config (Does not filter by string, platform will throw a silent error)
            for (int i = 0; i < attributeKeysToBeUpdated.Length; i++)
            {
                if (!entity.Attributes.ContainsKey(attributeKeysToBeUpdated[i]))
                {
                    entity.Attributes.Add(attributeKeysToBeUpdated[i], attributeKeysToBeUpdated[i] + ":" + entity.Id);

                    if (!updatedAttributes.Contains(attributeKeysToBeUpdated[i]))
                    {
                        updatedAttributes.Add(attributeKeysToBeUpdated[i]); // Add the updated attribute to be logged.
                    }
                }
            }
            return entity;
        }

        /// <summary>
        /// Logs the information regarding a list of updated attributes.
        /// </summary>
        /// <param name="updatedAttributes"></param>
        private static void LogUpdatedAttributes(List<string> updatedAttributes)
        {
            for (int i = 0; i < updatedAttributes.Count; i++)
            {
                log.InfoFormat( "({0}) {1}", i, updatedAttributes[i]);
            }
        }
    }
}