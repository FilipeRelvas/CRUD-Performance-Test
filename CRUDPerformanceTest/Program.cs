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
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;

namespace CRUDPerformanceTest
{
    public class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main()
        {
            // The connection to the Organization web service.
            CrmServiceClient serviceClient = null;
            OrganizationServiceManager serviceManager;
            
            int timeoutInMinutes = int.Parse(ConfigurationManager.AppSettings["TimeoutInMinutes"]);
            int defaultConnectionLimit = int.Parse(ConfigurationManager.AppSettings["DefaultConnectionLimit"]);

            // Allows .NET to run multiple threads https://msdn.microsoft.com/en-us/library/system.net.servicepointmanager.defaultconnectionlimit(v=vs.110).aspx
            System.Net.ServicePointManager.DefaultConnectionLimit = defaultConnectionLimit;

            try
            {
                // Establish an authenticated connection to Crm.
                string connectionString = ConfigurationManager.ConnectionStrings["CrmConnect"].ConnectionString;
                
                CrmServiceClient.MaxConnectionTimeout = new TimeSpan(0, timeoutInMinutes, 0);
                serviceClient = new CrmServiceClient(connectionString);          
                serviceManager = new OrganizationServiceManager(serviceClient);

                LogAppSettings(); // Display App Settings
                DetermineOperationType(serviceClient, serviceManager);
               
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
                if (serviceClient != null) serviceClient.Dispose();

                Console.WriteLine("Press <Enter> to exit.");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Determines the operation type to be performed: Create, Update or Delete.
        /// </summary>
        /// <param name="serviceClient"></param>
        /// <param name="serviceManager"></param>
        private static void DetermineOperationType(CrmServiceClient serviceClient, OrganizationServiceManager serviceManager)
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
                    bool isOob = DetermineEntityType(); 
                    Tuple<EntityMetadata, Entity> entityTuple = RetrieveHelperMethods.RetrieveEntityList(serviceClient, isOob);
                    DetermineCreateOperationType(serviceClient, serviceManager, entityTuple);
                    break;
                case 1: // Retrieve
                    log.Info("Selected operation: Retrieve");
                    DetermineRetrieveOperationType(serviceClient);
                    break;
                case 2: // Update
                    log.Info("Selected operation: Update");
                    DetermineUpdateOperationType(serviceClient, serviceManager);
                    break;
                case 3: // Delete
                    log.Info("Selected operation: Delete");
                    DetermineDeleteOperationType(serviceClient, serviceManager);
                    break;
                default:
                    throw new InvalidOperationException("The specified operation type is not valid: " + response);
            }
            return;
        }

        /// <summary>
        /// Determines the entity type by asking the user (y/n), with "yes" being OOB entities and "no" custom entities.
        /// </summary>
        /// <returns>True if OOB Entities and False if Custom Entity</returns>
        private static bool DetermineEntityType()
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
        /// Determines which create operation should be performed: Single, Execute Multiple or Parallel Execute Multiple.
        /// </summary>
        /// <param name="serviceClient"></param>
        /// <param name="serviceManager"></param>
        /// <param name="entityTuple"></param>
        private static void DetermineCreateOperationType(CrmServiceClient serviceClient, OrganizationServiceManager serviceManager, Tuple<EntityMetadata, Entity> entityTuple)
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
                    CreateHelperMethods.CreateExecuteSingle(serviceClient, entityTuple, totalRequestBatches, totalRequestsPerBatch);
                    break;
                case 1: // Execute Multiple
                    CreateHelperMethods.CreateExecuteMultiple(serviceClient, entityTuple, totalRequestBatches, totalRequestsPerBatch);
                    break;
                case 2: // Parallel Execute Multiple
                    CreateHelperMethods.CreateParallelExecuteMultiple(serviceManager, entityTuple, totalRequestBatches, totalRequestsPerBatch);
                    break;
                default:
                    throw new InvalidOperationException("The specified create operation type is not valid: " + response);
            }
            return;
        }

        /// <summary>
        /// Determines which update operation should be performed: Parallel Execute Multiple | FetchXML.
        /// </summary>
        /// <param name="serviceClient"></param>
        /// <param name="serviceManager"></param>
        private static void DetermineUpdateOperationType(CrmServiceClient serviceClient, OrganizationServiceManager serviceManager)
        {
            int totalRequestsPerBatch = int.Parse(ConfigurationManager.AppSettings["TotalRequestsPerBatch"]);

            Console.WriteLine("\nThe following update operation types are available: ");
            Console.WriteLine("(0) Parallel Execute Multiple | FetchXML");
            
            Console.Write("Specify the desired update operation type: ");
            string response = Console.ReadLine();
            int updateOperationType = int.Parse(response);

            switch (updateOperationType)
            {
                case 0: // Execute Single
                    UpdateHelperMethods.UpdateFetchXml(serviceClient, serviceManager, totalRequestsPerBatch);
                    break;
                default:
                    throw new InvalidOperationException("The specified update operation type is not valid: " + response);
            }
            return;
        }

        /// <summary>
        /// Determines which retrieve operation should be performed: Retrieve Multiple | FetchXML.
        /// </summary>
        /// <param name="serviceClient"></param>
        private static void DetermineRetrieveOperationType(CrmServiceClient serviceClient)
        {
            Console.WriteLine("\nThe following retrieve operation types are available: ");
            Console.WriteLine("(0) Retrieve Multiple | FetchXML");
 
            Console.Write("Specify the desired retrieve operation type: ");
            string response = Console.ReadLine();
            int retrieveOperationType = int.Parse(response);

            switch (retrieveOperationType)
            {
                case 0: // Retrieve Multiple | FetchXML
                    RetrieveHelperMethods.RetrieveMultipleFetchXml(serviceClient);
                    break;
                default:
                    throw new InvalidOperationException("The specified retrieve operation type is not valid: " + response);
            }
            return;
        }

        /// <summary>
        /// Determines which delete operation should be performed: Execute Multiple | FetchXML.
        /// </summary>
        /// <param name="serviceClient"></param>
        /// <param name="serviceManager"></param>
        private static void DetermineDeleteOperationType(CrmServiceClient serviceClient, OrganizationServiceManager serviceManager)
        {
            int totalRequestsPerBatch = int.Parse(ConfigurationManager.AppSettings["TotalRequestsPerBatch"]);

            Console.WriteLine("\nThe following delete operation types are available: ");
            Console.WriteLine("(0) Parallel Execute Multiple | FetchXML");

            Console.Write("Specify the desired delete operation type: ");
            string response = Console.ReadLine();
            int deleteOperationType = int.Parse(response);

            switch (deleteOperationType)
            {
                case 0: // Parallel Execute Multiple | FetchXML
                    DeleteHelperMethods.DeleteFetchXml(serviceClient, serviceManager, totalRequestsPerBatch);
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

            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                string value = ConfigurationManager.AppSettings[key];

                if (value.Equals(string.Empty) || value == null)
                {
                    value = "{}";
                }
                log.InfoFormat(key + ": {0}", value);
            }
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