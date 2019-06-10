﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Pfe.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace CRUDPerformanceTest
{
    public static class CreateHelperMethods
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Executes single create requests.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="entityTuple"></param>
        /// <returns></returns>
        public static List<Guid> CreateExecuteSingle(OrganizationServiceProxy serviceProxy, Tuple<EntityMetadata, Entity> entityTuple, int totalRequestBatches, int totalRequestsPerBatch)
        {
            Console.WriteLine();
            log.Info("Create Mode: Execute Single...");
            log.InfoFormat("Creating {0} records...", totalRequestBatches * totalRequestsPerBatch);

            double totalSeconds = 0.0;

            Stopwatch sw = new Stopwatch();
            List<Guid> ids = new List<Guid>();

            for (int i = 0; i < (totalRequestBatches * totalRequestsPerBatch); i++)
            {
                Guid? preAssignedId = null;
                preAssignedId = Guid.NewGuid();

                ids.Add(preAssignedId.Value);
                Entity entity = CreateEntity(entityTuple, i, preAssignedId);

                CreateRequest mRequest = new CreateRequest()
                {
                    Target = entity,
                    RequestId = Guid.NewGuid()
                };
                mRequest.Parameters.Add("SuppressDuplicateDetection", true); // Disable duplicate detection 

                sw.Start();
                CreateResponse response = (CreateResponse)serviceProxy.Execute(mRequest);
                sw.Stop();

                log.InfoFormat("Request Id: {0} for record number: {1}", i, mRequest.RequestId);
                log.InfoFormat("Seconds to create record number {0}: {1}s", i, sw.Elapsed.TotalSeconds);
                totalSeconds = totalSeconds + sw.Elapsed.TotalSeconds;
                sw.Reset();
            }

            log.InfoFormat("Seconds to create {0} records: {1}s", totalRequestBatches * totalRequestsPerBatch, totalSeconds);
            return ids;
        }

        /// <summary>
        /// Executes multiple creates within one or more batches, with each batch limited to 1000 records.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="entityTuple"></param>
        /// <returns></returns>
        public static List<Guid> CreateExecuteMultiple(OrganizationServiceProxy serviceProxy, Tuple<EntityMetadata, Entity> entityTuple, int totalRequestBatches, int totalRequestPerBatch)
        {
            Console.WriteLine();
            log.Info("Create Mode: Execute Multiple...");
            log.InfoFormat("Creating {0} records...", totalRequestBatches * totalRequestPerBatch);
         
            ExecuteMultipleRequest executeMultipleRequest = new ExecuteMultipleRequest()
            {
                Requests = new OrganizationRequestCollection(),
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = false,
                    ReturnResponses = true
                },
                RequestId = Guid.NewGuid()
            };

            double totalSeconds = 0.0;

            Stopwatch sw = new Stopwatch();
            List<Guid> ids = new List<Guid>();

            for (int i = 0; i < totalRequestBatches; i++)
            {
                for (int j = 0; j < totalRequestPerBatch; j++)
                {
                    Guid? preAssignedId = null;
                    preAssignedId = Guid.NewGuid();

                    ids.Add(preAssignedId.Value);
                    Entity entity = CreateEntity(entityTuple, i, preAssignedId);

                    CreateRequest mRequest = new CreateRequest()
                    {
                        Target = entity,
                        RequestId = Guid.NewGuid()
                    };
                    mRequest.Parameters.Add("SuppressDuplicateDetection", true); // Disable duplicate detection 
                    executeMultipleRequest.Requests.Add(mRequest);
                }

                OrganizationResponse responseForCreateRecords = null;

                sw.Start();
                responseForCreateRecords = (ExecuteMultipleResponse)serviceProxy.Execute(executeMultipleRequest);
                sw.Stop();

                totalSeconds = totalSeconds + sw.Elapsed.TotalSeconds;
                log.InfoFormat("Request Id: {0} for batch number {1}", executeMultipleRequest.RequestId, i);
                log.InfoFormat("Seconds to create {0} records for batch number {1}: {2}s", totalRequestPerBatch, i, sw.Elapsed.TotalSeconds);

                sw.Reset();
                executeMultipleRequest.Requests.Clear();
                executeMultipleRequest.RequestId = Guid.NewGuid();
            }

            log.InfoFormat("Seconds to create {0} records: {1}s", totalRequestBatches * totalRequestPerBatch, totalSeconds);
            return ids;
        }

        /// <summary>
        /// Executes multiple creates concurrently within one or more Batches, with each Batch limited to 1000 records.
        /// </summary>
        /// <param name="serviceManager"></param>
        /// <param name="serviceProxyOptions"></param>
        /// <param name="entityTuple"></param>
        /// <returns></returns>
        public static List<Guid> CreateParallelExecuteMultiple(OrganizationServiceManager serviceManager, OrganizationServiceProxyOptions serviceProxyOptions, Tuple<EntityMetadata, Entity> entityTuple, int totalRequestBatches, int totalRequestsPerBatch)
        {
            Console.WriteLine();
            log.Info("Create Mode: Parallel Execute Multiple...");
            log.InfoFormat("Creating {0} records...", totalRequestBatches * totalRequestsPerBatch);
         
            List<Guid> ids = new List<Guid>();
            IDictionary<string, ExecuteMultipleRequest> requests = new Dictionary<string, ExecuteMultipleRequest>();

            for (int i = 0; i < totalRequestBatches; i++)
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

                for (int j = 0; j < totalRequestsPerBatch; j++)
                {
                    Guid? preAssignedId = null;
                    preAssignedId = Guid.NewGuid();

                    ids.Add(preAssignedId.Value);
                    var entity = CreateEntity(entityTuple, j, preAssignedId);

                    CreateRequest mRequest = new CreateRequest()
                    {
                        Target = entity,
                        RequestId = Guid.NewGuid()
                    };
                    mRequest.Parameters.Add("SuppressDuplicateDetection", true); // Disable duplicate detection 
                    executeMultipleRequest.Requests.Add(mRequest);
                }
                requests.Add(new KeyValuePair<string, ExecuteMultipleRequest>(i.ToString(), executeMultipleRequest));
                log.InfoFormat("Request Id: {0} for request batch number {1}", executeMultipleRequest.RequestId, i);
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Parallel execution of all ExecuteMultipleRequest in the requests Dictionary
            IDictionary<string, ExecuteMultipleResponse> responseForCreateRecords = serviceManager.ParallelProxy.Execute<ExecuteMultipleRequest, ExecuteMultipleResponse>(requests, serviceProxyOptions);
            int threadsCount = Process.GetCurrentProcess().Threads.Count;
            log.InfoFormat("Number of threads used: {0}", threadsCount);

            sw.Stop();
            log.InfoFormat("Seconds to create {0} records: {1}s", totalRequestBatches * totalRequestsPerBatch, sw.Elapsed.TotalSeconds);
      
            return ids;
        }

        /// <summary>
        /// Creates an Entity object based on newly generated Guid, EntityTuple and Index.
        /// </summary>
        /// <param name="entityTuple"></param>
        /// <param name="index"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static Entity CreateEntity(Tuple<EntityMetadata, Entity> entityTuple, int index, Guid? id)
        {
            Entity entity = new Entity(entityTuple.Item2.LogicalName);

            if (id != null)
            {
                entity.Id = id.Value;
            }

            entity.Attributes.Add(entityTuple.Item1.PrimaryIdAttribute, id.Value);
            entity.Attributes.Add(entityTuple.Item1.PrimaryNameAttribute, entityTuple.Item2.LogicalName + (index + 1));

            return entity;
        }
    }
}