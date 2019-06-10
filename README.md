# CRUD-Performance-Test

CRUD Performance Test is a Microsoft Dynamics 365 SDK based tool to quickly measure performance indexes, namely request 
response times, when performing operations such as:
- Create;
- Retrieve;
- Update;
- Delete.

<b>Pre-Release v1.0:</b>
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
		<add key="OobEntities" value="Account,Contact,Lead,Opportunity,Incident" />	<!-- List the OOB entities by separating them with commas (,) -->
		<add key="CustomEntities" value="" /> <!-- List the custom entities by separating them with commas (,) -->
		<add key="TimeoutInMinutes" value="2" />
		<add key="DefaultConnectionLimit" value="2" />
		<add key="TotalRequestBatches" value="2" />
		<add key="TotalRequestsPerBatch" value="20" /> <!-- Limited to 1000 (Batch Size) -->
	</appSettings>
  ```