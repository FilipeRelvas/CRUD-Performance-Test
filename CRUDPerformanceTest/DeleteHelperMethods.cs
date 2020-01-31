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
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Pfe.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;

namespace CRUDPerformanceTest
{
    public static class DeleteHelperMethods
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Executes multiple deletes concurrently within one or more Batches, with each batch limited to 1000 records, based on the provided FetchXML.
        /// </summary>
        /// <param name="serviceClient"></param>
        /// <param name="serviceManager"></param>
        /// <param name="totalRequestsPerBatch"></param>
        /// <returns></returns>
        public static List<Guid> DeleteFetchXml(CrmServiceClient serviceClient, OrganizationServiceManager serviceManager, int totalRequestsPerBatch)
        {
            EntityCollection recordsToBeDeleted = RetrieveHelperMethods.RetrieveMultipleFetchXml(serviceClient);
            return DeleteParallelExecuteMultiple(serviceManager, recordsToBeDeleted, totalRequestsPerBatch);
        }

        /// <summary>
        /// Executes multiple deletes concurrently within one or more Batches, with each batch limited to 1000 records.
        /// </summary>
        /// <param name="serviceManager"></param>
        /// <param name="recordsToBeDeleted"></param>
        /// <param name="totalRequestsPerBatch"></param>
        /// <returns></returns>
        public static List<Guid> DeleteParallelExecuteMultiple(OrganizationServiceManager serviceManager, EntityCollection recordsToBeDeleted, int totalRequestsPerBatch)
        {
            int batchSize = 0;
            int recordsToBeDeletedCount = recordsToBeDeleted.Entities.Count;
            int batchNumber = (int) Math.Ceiling((recordsToBeDeleted.Entities.Count * 1.0d) / (totalRequestsPerBatch * 1.0d));

            Console.WriteLine();
            log.Info("Delete Mode: Parallel Execute Multiple");
           
            List<Guid> ids = new List<Guid>();
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

                for (int j = recordsToBeDeleted.Entities.Count - 1; j >= 0; j--)
                {
                    Entity entityToBeDeleted = recordsToBeDeleted.Entities[j];
                    ids.Add(entityToBeDeleted.Id);

                    DeleteRequest deleteRequest = new DeleteRequest()
                    {
                        Target = new EntityReference(entityToBeDeleted.LogicalName, entityToBeDeleted.Id),
                        RequestId = Guid.NewGuid()
                    };
                    executeMultipleRequest.Requests.Add(deleteRequest);
                    recordsToBeDeleted.Entities.RemoveAt(j);

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
            log.InfoFormat("Deleting {0} record(s)...", recordsToBeDeletedCount);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Parallel execution of all ExecuteMultipleRequest in the requests Dictionary
            IDictionary<string, ExecuteMultipleResponse> responseForDeleteRecords = serviceManager.ParallelProxy.Execute<ExecuteMultipleRequest, ExecuteMultipleResponse>(requests);
            int threadsCount = Process.GetCurrentProcess().Threads.Count;
            sw.Stop();

            log.InfoFormat("Number of threads used: {0}", threadsCount);
            log.InfoFormat("Seconds to delete {0} record(s): {1}s", recordsToBeDeletedCount, sw.Elapsed.TotalSeconds);
 
            return ids;
        }
    }
}