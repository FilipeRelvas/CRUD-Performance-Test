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
using System.IO;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace CRUDPerformanceTest
{
    public static class RetrieveHelperMethods
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Retrieves all OOB or Custom Entities depending on isOOB's value (True or False) and queries which one to choose.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="isOob"></param>
        /// <returns>A Tuple with the EntityMetadata and Entity object from the chosen Entity.</returns>
        public static Tuple<EntityMetadata, Entity> RetrieveEntityList(OrganizationServiceProxy serviceProxy, bool isOob)
        {
            string[] oobEntities = ConvertKeyStringToList("OobEntities");
            string[] customEntities = ConvertKeyStringToList("CustomEntities");

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
        /// Executes a retrieve multiple based on the FetchXML copied from the command line.
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <returns></returns>
        public static EntityCollection RetrieveMultipleFetchXml(OrganizationServiceProxy serviceProxy)
        {
            int pageNumber = 1;
            int fetchMaxCount = 5000;
            string pagingCookie = null;
            string fetch = string.Empty;

            Console.WriteLine();
            log.Info("Retrieve Mode: Execute Multiple FetchXML");
            Console.WriteLine("Please paste your FetchXML here: ");

            while (true) // Read FetchXML indefinitely until we reach the close tag
            {
                string line = Console.ReadLine();
                fetch = fetch + line;

                // Check if the close tag is included
                if (line.Contains("</fetch>")) 
                {
                    break;
                }
            }
            fetch = fetch.Replace("\"", "\'");
            
            EntityCollection retrievedRecords = new EntityCollection();
            IDictionary<string, ExecuteMultipleRequest> requests = new Dictionary<string, ExecuteMultipleRequest>();

            double totalSeconds = 0.0d;
            Stopwatch sw = new Stopwatch();

            while (true)
            {
                string xml = CreateXml(fetch, pagingCookie, pageNumber, fetchMaxCount);

                RetrieveMultipleRequest retrieveMultipleRequest = new RetrieveMultipleRequest
                {
                    Query = new FetchExpression(xml),
                    RequestId = Guid.NewGuid()
                };

                sw.Start();
                EntityCollection returnCollection = ((RetrieveMultipleResponse)serviceProxy.Execute(retrieveMultipleRequest)).EntityCollection;
                sw.Stop();

                log.InfoFormat("Request Id for request page number {0}: {1}", pageNumber, retrieveMultipleRequest.RequestId);
                log.InfoFormat("Seconds to retrieve {0} record(s) for page number {1}: {2}s", returnCollection.Entities.Count, pageNumber, sw.Elapsed.TotalSeconds);
                totalSeconds = totalSeconds + sw.Elapsed.TotalSeconds;
                sw.Reset();

                foreach (var entity in returnCollection.Entities)
                {
                    retrievedRecords.Entities.Add(entity);
                }

                // Check for more records, if it returns true, keep iterating 
                if (returnCollection.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = returnCollection.PagingCookie;
                }
                else
                {
                    break;
                }
            }

            log.InfoFormat("Seconds to retrieve {0} record(s): {1}s", retrievedRecords.Entities.Count, totalSeconds);
            return retrievedRecords;
        }

        /// <summary>
        /// Generates the XML Document and loads it based on the specified reader.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="cookie"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static string CreateXml(string xml, string cookie, int page, int count)
        {
            StringReader stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            // Load document
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            return CreateXml(doc, cookie, page, count);
        }

        /// <summary>
        /// Generates the XML attributes and returns the XML Document based on the specified writer.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="cookie"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static string CreateXml(XmlDocument doc, string cookie, int page, int count)
        {
            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                XmlAttribute pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            XmlAttribute pageAttr = doc.CreateAttribute("page");
            pageAttr.Value = System.Convert.ToString(page);
            attrs.Append(pageAttr);

            XmlAttribute countAttr = doc.CreateAttribute("count");
            countAttr.Value = System.Convert.ToString(count);
            attrs.Append(countAttr);

            StringBuilder sb = new StringBuilder(1024);
            StringWriter stringWriter = new StringWriter(sb);

            XmlTextWriter writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
        }

        /// <summary>
        /// Retrieves the key string value contained in the App.config as a string array.
        /// </summary>
        /// <param name="entityString"></param>
        /// <returns></returns>
        public static string[] ConvertKeyStringToList(string appSetting)
        {
            string entityString = ConfigurationManager.AppSettings[appSetting].Trim();
            return entityString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}