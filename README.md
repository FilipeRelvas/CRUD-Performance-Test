# CRUD-Performance-Test

CRUD Performance Test is a Microsoft Dynamics 365 SDK based tool to quickly measure performance indexes, namely request 
response times, when performing operations such as:
- Create;
- Retrieve;
- Update;
- Delete.


<b>Release v2.0:</b>
- Updated all the ADAL and CRM SDK DLLs to the latest version;
- Updated PFE Xrm Core to the latest version;
- Removed every reference for OrganizationServiceProxy;
- Service connection is now made through the CrmServiceClient;
- ServerConnection class was removed;
- Connection String is now used and can be configured through the App.Config file.

 ```xml
   <connectionStrings>
     <!-- Online using OAuth -->
     <add name="CrmConnect" connectionString="AuthType=OAuth;Username=;Password=;Url=;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97"/>
   </connectionStrings>
   ```


<b>Release v1.2:</b>
- Added Update Operation through FetchXML:
  - Generate the XML through CRM or a 3rd Party Tool and paste it directly in the console when asked;
- Update Operation will take the Attributes from the FetchXML or from the AppConfig through the AttributesToUpdate app setting (check below);
  - <b>Note</b>: Currently we only support Attributes of type String (Single Line of Text, Multi Line of Text...);
- Updated CrmSdk to the latest version:
  - Microsoft.CrmSdk.CoreAssemblies to 9.0.2.18;
  - Microsoft.CrmSdk.Deployment to 9.0.2.18;
  - Microsoft.CrmSdk.Workflow to 9.0.2.18.
- Modified the Log App Settings function to dynamically log with the addition or removal of new AppSettings;
- Minor string fixes regarding the options / information presented in the screen;
- <b>Update:</b> Application configuration can be defined through the project AppConfig file:

 ```xml
  <appSettings>
		<add key="AttributesToUpdate" value="" /> <!-- List the attributes to be updated by separating them with commas (,) -->
		<add key="OobEntities" value="Account,Contact,Lead,Opportunity,Incident" /> <!-- List the OOB entities by separating them with commas (,) -->
		<add key="CustomEntities" value="" /> <!-- List the custom entities by separating them with commas (,) -->
		<add key="TimeoutInMinutes" value="2" />
		<add key="DefaultConnectionLimit" value="2" />
		<add key="TotalRequestBatches" value="2" />
		<add key="TotalRequestsPerBatch" value="20" /> <!-- Limited to 1000 (Batch Size) -->
	</appSettings>
  ```


<b>Release v1.1:</b>
- Added Retrieve Operation through FetchXML:
  - Generate the XML through CRM or a 3rd Party Tool and paste it directly in the console when asked;
- Added Delete Operation through FetchXML:
  - Generate the XML through CRM or a 3rd Party Tool and paste it directly in the console when asked;
  - Function is executed through the Parallel Execute Multiple implementation;
  - It takes into account the values defined for TotalRequestsPerBatch to calculate the number of Batches needed.
- Updated the Application Logging to take Retrieve and Delete;
- Updated microsoft.identitymodel.clients.activedirectory from 5.1.0 to 5.2.0;
- Updated the Entity Create name to use GUID in order to avoid repetitions or duplicates;


<b>Release v1.0:</b>
- Server Configuration allows for a straight forward instance configuration;
- Create Operation is available with 3 different implementations:
  - Execute Single;
  - Execute Multiple;
    - https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/org-service/execute-multiple-requests
  - Parallel Execute Multiple
    - https://archive.codeplex.com/?p=pfexrmcore
- Application logging is available through log4net;
- Application configuration can be defined through the project AppConfig file:
  
  ```xml
  <appSettings>
		<add key="OobEntities" value="Account,Contact,Lead,Opportunity,Incident" /> <!-- List the OOB entities by separating them with commas (,) -->
		<add key="CustomEntities" value="" /> <!-- List the custom entities by separating them with commas (,) -->
		<add key="TimeoutInMinutes" value="2" />
		<add key="DefaultConnectionLimit" value="2" />
		<add key="TotalRequestBatches" value="2" />
		<add key="TotalRequestsPerBatch" value="20" /> <!-- Limited to 1000 (Batch Size) -->
	</appSettings>
  ```
