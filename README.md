# Migrating SSRS rdl to Paginated report Power BI and modifying Provider to AzureSQL
While Migrating from SSRS to Paginated Report in Power BI, you may have reports which uses SQL Server. But as part of the migration effort, what if the SQL Server is also migrated to SQL Server MI. In that case we need to update the provider to Azure SQL instead of using SQL Server.  
<br>
This project helps modify the RDL to use AzureSQL Provider as well as allow you to update the credential to SQL Server based authentication. 
<br>
This project uses App.Config to provide following parameters to the application. 
* logPath :-- To write information in this text file. 
* ClientId :-- Client ID for an App registered in Microsoft Entra. Looking for more information below
* TenantId :-- Tenant ID of the AAD or Microsoft Entra
* workspaceName :-- Workspace in Power BI. Please make sure to name it as in power bi, kind of case sensitive. 
* ServerName    :-- ServerName of the Azure SQL MI. This will replace the existing ServerName. The database is taken from the existing Database name embeded in the report. 
* SqlUserName   :-- SQL Login. 
* SqlUserPassword :-- SQL user's password.
<br>Ted had recently presented on this topic and would strongly advice to watch to get insights and requirements for ClientId. <br> **Reference** [Ted Pattison's Datasource Credential ](https://www.powerbidevcamp.net/sessions/session33/)




