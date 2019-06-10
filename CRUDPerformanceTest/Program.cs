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
using System.ServiceModel;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.ServiceModel.Security;
using Microsoft.Pfe.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Samples;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace CRUDPerformanceTest
{
    public class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            // The connection to the Organization web service.
            OrganizationServiceProxy serviceProxy = null;
            OrganizationServiceManager serviceManager = null;

            int timeoutInMinutes = int.Parse(ConfigurationManager.AppSettings["TimeoutInMinutes"]);
            int defaultConnectionLimit = int.Parse(ConfigurationManager.AppSettings["DefaultConnectionLimit"]);

            // Allows .NET to run multiple threads https://msdn.microsoft.com/en-us/library/system.net.servicepointmanager.defaultconnectionlimit(v=vs.110).aspx
            System.Net.ServicePointManager.DefaultConnectionLimit = defaultConnectionLimit;

            try
            {
                // Obtain the target organization's web address and client logon credentials
                // from the user by using a helper class.
                ServerConnection serverConnect = new ServerConnection();
                ServerConnection.Configuration config = serverConnect.GetServerConfiguration();

                // Establish an authenticated connection to the Organization web service. 
                serviceProxy = new OrganizationServiceProxy(config.OrganizationUri, config.HomeRealmUri, config.Credentials, config.DeviceCredentials)
                {
                    Timeout = new TimeSpan(0, timeoutInMinutes, 0)
                };

                var serviceProxyOptions = new OrganizationServiceProxyOptions()
                {
                    Timeout = new TimeSpan(0, timeoutInMinutes, 0)
                };
                serviceManager = new OrganizationServiceManager(config.OrganizationUri, config.Credentials.UserName.UserName, config.Credentials.UserName.Password);

                LogAppSettings(); // Display App Settings
                DetermineOperationType(serviceProxy, serviceProxyOptions, serviceManager);
               
                Console.WriteLine();
                Console.WriteLine("Completed!");
            }
            catch (FaultException<OrganizationServiceFault> e) { HandleException(e); }
            catch (TimeoutException e) { HandleException(e); }
            catch (SecurityTokenValidationException e) { HandleException(e); }
            catch (ExpiredSecurityTokenException e) { HandleException(e); }
            catch (MessageSecurityException e) { HandleException(e); }
            catch (SecurityNegotiationException e) { HandleException(e); }
            catch (SecurityAccessDeniedException e) { HandleException(e); }
            catch (FormatException e) { HandleException(e); }
            catch (InvalidOperationException e) { HandleException(e); }
            catch (Exception e) { HandleException(e); }

            finally
            {
                // Always dispose the service object to close the service connection and free resources.
                if (serviceProxy != null) serviceProxy.Dispose();

                Console.WriteLine("Press <Enter> to exit.");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Determines the operation type to be performed: Create, Update or Delete.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="serviceProxyOptions"></param>
        /// <param name="serviceManager"></param>
        private static void DetermineOperationType(OrganizationServiceProxy serviceProxy, OrganizationServiceProxyOptions serviceProxyOptions, OrganizationServiceManager serviceManager)
        {
            Console.WriteLine("\nThe following operation types are available: ");

            Console.WriteLine("(0) Create");
            Console.WriteLine("(1) Retrieve");
            Console.WriteLine("(2) Update");
            Console.WriteLine("(3) Delete");

            Console.Write("Specify the desired operation type: ");

            string response = Console.ReadLine();
            int createOperationType = int.Parse(response);
          
            switch (createOperationType)
            {
                case 0: // Create
                    log.Info("Selected operation: Create");
                    bool isOob = DetermineEntityType(serviceProxy); 
                    Tuple<EntityMetadata, Entity> entityTuple = ListEntities(serviceProxy, isOob);
                    DetermineCreateOperationType(serviceProxy, serviceProxyOptions, serviceManager, entityTuple);
                    break;
                case 1: //TODO: Retrieve
                    break;
                case 2: //TODO: Update
                    break;
                case 3: //TODO: Delete
                    break;
                default:
                    throw new InvalidOperationException("The specified operation type is not valid: " + response);
            }
            return;
        }

        /// <summary>
        /// Determines the entity type by asking the user (y/n), with "yes" being OOB entities and "no" custom entities.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <returns>True if OOB Entities and False if Custom Entity</returns>
        private static bool DetermineEntityType(OrganizationServiceProxy serviceProxy)
        {
            Console.Write("\nDo you want to retrieve OOB Entities (y/n)? ");
            string response = Console.ReadLine();

            if (response.Equals("y") || response.Equals("Y")) // OOB Entities
            {
                return true;
            }
            else if (response.Equals("n") || response.Equals("N")) // Custom Entities
            {
                return false;
            }
            else // Invalid Operation
            {
                throw new InvalidOperationException("Expected y/Y or n/N as an action but received: " + response);
            }
        }

        /// <summary>
        /// Lists all OOB or Custom Entities depending on isOOB's value (True or False) and queries which one to choose.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="isOob"></param>
        /// <returns>A Tuple with the EntityMetadata and Entity object from the chosen Entity.</returns>
        private static Tuple<EntityMetadata, Entity> ListEntities(OrganizationServiceProxy serviceProxy, bool isOob)
        {
            string[] oobEntities = ConvertEntityStringToList("OobEntities");
            string[] customEntities = ConvertEntityStringToList("CustomEntities");

            EntityMetadata[] entitiesMetadata = RetrieveEntityMetadata(serviceProxy, (isOob ? oobEntities : customEntities), isOob);

            if (entitiesMetadata != null && entitiesMetadata.Length > 0)
            {
                Console.WriteLine("\nThe following " + (isOob ? "OOB Entities" : "Custom Entities") + " are available:");

                for (int i = 0; i < entitiesMetadata.Length; i++)
                {
                    Console.WriteLine("(" + i + ") " + entitiesMetadata[i].LogicalName);
                }

                Console.Write("Specify the desired entity: ");

                // Check the chosen entity number and extract the entity metadata object
                string response = Console.ReadLine();
                EntityMetadata entityMetadata = entitiesMetadata[int.Parse(response)];
                log.InfoFormat("Selected entity: {0}", entityMetadata.LogicalName);

                var queryExpression = new QueryExpression(entityMetadata.LogicalName)
                {
                    ColumnSet = new ColumnSet(true),
                    TopCount = 1
                };

                DataCollection<Entity> Entities = serviceProxy.RetrieveMultiple(queryExpression).Entities;

                if (Entities.Count > 0) // Check if we have any entity records for the selected entity
                {
                    Entity entity = serviceProxy.RetrieveMultiple(queryExpression).Entities[0];
                    return new Tuple<EntityMetadata, Entity>(entityMetadata, entity);
                }
                else
                {
                    throw new Exception("No records found for " + entityMetadata.LogicalName + ". Please create a record and retry this process.");
                }
            }
            else
            {
                throw new Exception((isOob ? "OOB Entities" : "Custom Entities") + " were not found. Please make sure they exist in the environment.");
            }
        }

        /// <summary>
        /// Retrieves all OOB or Custom Entities Metadata depending on isOOB's value (True or False).
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="includedEntities"></param>
        /// <param name="isOOB"></param>
        /// <returns></returns>
        public static EntityMetadata[] RetrieveEntityMetadata(OrganizationServiceProxy serviceProxy, string[] includedEntities, bool isOOB)
        {
            MetadataFilterExpression EntityFilter = new MetadataFilterExpression(LogicalOperator.And);

            if (includedEntities != null && includedEntities.Length > 0) // If the list of included entities does contain entities
            {
                EntityFilter.Conditions.Add(new MetadataConditionExpression("SchemaName", MetadataConditionOperator.In, includedEntities));
            }
            else // Otherwise we need to verify the OOB flag to decide which type of entities to return (OOB or Custom)
            {
                if (!isOOB) // Return Custom Entities (Object Type Code >= 10000)
                {
                    EntityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.GreaterThan, 9999));
                }
            }

            // An entity query expression to combine the filter expressions and property expressions for the query.
            EntityQueryExpression entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = EntityFilter
            };

            RetrieveMetadataChangesRequest request = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            RetrieveMetadataChangesResponse response = (RetrieveMetadataChangesResponse)serviceProxy.Execute(request);
            return response.EntityMetadata.ToArray();
        }                           
    
        /// <summary>
        /// Determines which create operation should be performed: Single, Execute Multiple or Parallel Execute Multiple.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="serviceProxyOptions"></param>
        /// <param name="serviceManager"></param>
        /// <param name="entityTuple"></param>
        private static void DetermineCreateOperationType(OrganizationServiceProxy serviceProxy, OrganizationServiceProxyOptions serviceProxyOptions, OrganizationServiceManager serviceManager, Tuple<EntityMetadata, Entity> entityTuple)
        {
            int totalRequestBatches = int.Parse(ConfigurationManager.AppSettings["TotalRequestBatches"]);
            int totalRequestsPerBatch = int.Parse(ConfigurationManager.AppSettings["TotalRequestsPerBatch"]);

            Console.WriteLine("\nThe following create operation types are available: ");

            Console.WriteLine("(0) Single");
            Console.WriteLine("(1) Execute Multiple");
            Console.WriteLine("(2) Parallel Execute Multiple");

            Console.Write("Specify the desired create operation type: ");
            string response = Console.ReadLine();
            int createOperationType = int.Parse(response);

            switch (createOperationType)
            {
                case 0: // Execute Single
                    CreateHelperMethods.CreateExecuteSingle(serviceProxy, entityTuple, totalRequestBatches, totalRequestsPerBatch);
                    break;
                case 1: // Execute Multiple
                    CreateHelperMethods.CreateExecuteMultiple(serviceProxy, entityTuple, totalRequestBatches, totalRequestsPerBatch);
                    break;
                case 2: // Parallel Execute Multiple
                    CreateHelperMethods.CreateParallelExecuteMultiple(serviceManager, serviceProxyOptions, entityTuple, totalRequestBatches, totalRequestsPerBatch);
                    break;
                default:
                    throw new InvalidOperationException("The specified create operation type is not valid: " + response);
            }
            return;
        }

        /// <summary>
        /// Logs all the App Settings defined in the App.config solution file.
        /// </summary>
        private static void LogAppSettings()
        {
            Console.WriteLine("(App Settings)\n");

            log.InfoFormat("OOB Entities: {0}", ConfigurationManager.AppSettings["OobEntities"]);
            log.InfoFormat("Custom Entities: {0}", ConfigurationManager.AppSettings["CustomEntities"]);

            log.InfoFormat("Timeout In Minutes: {0}", ConfigurationManager.AppSettings["TimeoutInMinutes"]);
            log.InfoFormat("Default Connection Limit: {0}", ConfigurationManager.AppSettings["DefaultConnectionLimit"]);

            log.InfoFormat("Total Request Batches: {0}", ConfigurationManager.AppSettings["TotalRequestBatches"]);
            log.InfoFormat("Total Requests Per Batch: {0}", ConfigurationManager.AppSettings["TotalRequestsPerBatch"]);       
        }

        /// <summary>
        /// Retrieves the entity schema name contained in the App.config string.
        /// </summary>
        /// <param name="entityString"></param>
        /// <returns></returns>
        private static string[] ConvertEntityStringToList(string appSetting)
        {
            string entityString = ConfigurationManager.AppSettings[appSetting].Trim();
            return entityString.Split(',');        
        }

        /// Handle a thrown exception.
        /// </summary>
        /// <param name="ex">An exception.</param>
        private static void HandleException(Exception e)
        {
            // Display the details of the exception.
            log.Error(e.Message);
 
            if (e.InnerException != null) HandleException(e.InnerException);
        }
    }
}    