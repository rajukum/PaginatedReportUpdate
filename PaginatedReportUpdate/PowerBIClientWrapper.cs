using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using static PaginatedReportUpdate.ElementNameConstants;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using System.Configuration;
using Microsoft.PowerBI.Api.Models.Credentials;
using System.IO.Enumeration;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using System.Data.SqlClient;

namespace PaginatedReportUpdate
{
    public sealed class PowerBIClientWrapper
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string GatewayId { get; set; }

        private Group workspace;

        private HashSet<string> workspaceReports = new HashSet<string>();

        private PowerBIClient client;
        private IImportsOperations importClient;
        private IReportsOperations reportsClient;
        private IGroupsOperations groupsClient;

        public PowerBIClientWrapper(string workspaceName, string clientId, string tenantId, string gatewayId, string[] scopes)
        {
            this.ClientId = clientId;
            this.TenantId = tenantId;
            this.GatewayId = gatewayId;
            InitializeClients(scopes);
            workspace=GetWorkspaces(workspaceName);
        }

        public PowerBIClientWrapper(string workspaceName, string clientId, string tenantId, string[] scopes)
        {
            this.ClientId = clientId;
            this.TenantId = tenantId;
            InitializeClients(scopes);
            workspace=GetWorkspaces(workspaceName);
        }


        private void InitializeClients(string[] scopes)
        {
            var AccessToken = DoInteractiveSignIn(scopes);
            try
            {
                TokenCredentials tokenCredentials = new TokenCredentials(AccessToken, "Bearer");
                client = new PowerBIClient(new Uri(PowerBIWrapperConstants.PowerBiApiUri), tokenCredentials);

                importClient = new ImportsOperations(client);
                reportsClient = new ReportsOperations(client);
                groupsClient = new GroupsOperations(client);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Failed to initialize client: {ex.Message}");
            }
        }


        public string DoInteractiveSignIn(string[] scopes)
        {
            try
            {
                //PowerBIPermissionScopes.OnPremGatewayManagement
                var appPublic = PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority(PowerBIWrapperConstants.AuthorityUri + TenantId)
                    .WithRedirectUri(PowerBIWrapperConstants.RedirectUrlv2)
                    .Build();
                Logger.WriteLog("Getting AccessToken");
                AuthenticationResult autResult = appPublic.AcquireTokenInteractive(scopes).ExecuteAsync().Result;
                return autResult.AccessToken;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message);
                return null;
            }
        }

        private Group GetWorkspaces(string workspaceName)
        {
            if (workspaceName == PowerBIWrapperConstants.MyWorkspace)
            {
                workspace = null;
                var reportNames = reportsClient.GetReports().Value.Select(report => report.Name);
                workspaceReports = new HashSet<string>(reportNames);
                return null;
            }

            var workspaces = groupsClient.GetGroups().Value;
            var groups = workspaces.Where(g => (g.Name == workspaceName));
            if (groups.Count() == 1)
            {
                workspace = groups.First();
                if (workspace.IsOnDedicatedCapacity == false)
                {
                    throw new Exception($"WORKSPACE {workspaceName} IS NOT A PREMIUM WORKSPACE. Only premium workspaces can upload reports");
                }
                var reportNames = reportsClient.GetReportsInGroup(workspace.Id).Value.Select(report => report.Name);
                workspaceReports = new HashSet<string>(reportNames);    
                return workspace;
            }
            else if (groups.Count() == 0)
            {
                throw new Exception($"WORKSPACE {workspaceName} NOT FOUND.  Please make sure it is a valid workspace");
            }
            else
            {
                throw new Exception($"MULTIPLE PREMIUM WORKSPACE {workspaceName} FOUND. This should not happen, make sure you have valid workspaces");
            }
        }

        public void getPaginatedReports()
        {

            var reportNames = reportsClient.GetReportsInGroup(workspace.Id).Value;
            foreach ( var report in reportNames ) 
            {
                Logger.WriteLog(report.Name + "Report Type " + report.ReportType);
            }
            
        }

        public void SetPaginatedReportDataSources()
        {

            var reportNames = reportsClient.GetReportsInGroup(workspace.Id).Value;
            foreach (var report in reportNames)
            {
                if(report.ReportType == "PaginatedReport")
                {
                    var serverName = ConfigurationManager.AppSettings.Get("ServerName");
                    var reportDatasources = reportsClient.GetDatasourcesInGroup(workspace.Id, report.Id);
                    Datasource reportDatasource = reportDatasources.Value[0];
                    if (reportDatasource != null) 
                    {
                        Logger.WriteLog("ServerName:" + reportDatasource.ConnectionDetails.Server +"/n DatabaseName" + reportDatasource.ConnectionDetails.Database + "/n NameOfDatasource " + reportDatasource.Name);
                        var rdldatasourceconndetail= new RdlDatasourceConnectionDetails();
                        
                        rdldatasourceconndetail.Server = serverName;
                        rdldatasourceconndetail.Database = reportDatasource.ConnectionDetails.Database;
                        var updaterdldatasourcedetails = new UpdateRdlDatasourceDetails();
                        updaterdldatasourcedetails.DatasourceName = reportDatasource.Name;
                        updaterdldatasourcedetails.ConnectionDetails = rdldatasourceconndetail;

                        List<UpdateRdlDatasourceDetails> updateRdlDatasourceDetailsList = new List<UpdateRdlDatasourceDetails>();
                        updateRdlDatasourceDetailsList.Add(updaterdldatasourcedetails);
                        UpdateRdlDatasourcesRequest updateDatasourcesRequest = new UpdateRdlDatasourcesRequest(updateRdlDatasourceDetailsList);
                        reportsClient.UpdateDatasourcesInGroup(workspace.Id, report.Id, updateDatasourcesRequest);
                    }
                }
            }

        }

        public void SetPaginatedReportDataSourceswithProvider()
        {
            var logPath = ConfigurationManager.AppSettings.Get("logPath");
            var ServerName = ConfigurationManager.AppSettings.Get("ServerName");
            string logDirectory = Path.GetDirectoryName(logPath);
            logDirectory = logDirectory + "/output";

            try
            {
                if(!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                var reportNames = reportsClient.GetReportsInGroup(workspace.Id).Value;

                foreach (var report in reportNames)
                {
                   string fileName = logDirectory+"/" + report.Name +".rdl";
                    
                   if (report.ReportType == "PaginatedReport")
                    {

                        var reportDatasources = reportsClient.GetDatasourcesInGroup(workspace.Id, report.Id);

                        if(reportDatasources.Value.Count > 0)
                        { 

                                var reportContent = reportsClient.ExportReportInGroup(workspace.Id, report.Id);
                        
                                using(MemoryStream ms  = new MemoryStream()) 
                                { 
                                    //reportContent.Seek(0, SeekOrigin.Begin);
                                    reportContent.CopyTo(ms);
                                    using (StreamReader reader = new StreamReader(ms))
                                    {
                                        ms.Position = 0;
                                        string xDocAsStr = reader.ReadToEnd();
                                        var xDoc = XDocument.Parse(xDocAsStr);

                                        ms.Seek(0, SeekOrigin.Begin);

                                        //Save the main .rdl first
                                        using (FileStream file = new FileStream(fileName, FileMode.Create))
                                        {
                                            ms.CopyTo(file);
                                            Logger.WriteLog($"Backup for report {report.Name} worked.");
                                        }

                                        XmlDocument doc = new XmlDocument();
                                        doc.LoadXml(xDocAsStr);

                                        try
                                        {

                                            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                                            nsmgr.AddNamespace("ns", "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition");
                                            XmlNode dataSourceNode = doc.SelectSingleNode("//ns:DataSource", nsmgr);
                                            if (dataSourceNode==null)
                                            {
                                             nsmgr.AddNamespace("ns", "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition");
                                             dataSourceNode = doc.SelectSingleNode("//ns:DataSource", nsmgr);
                                            }
                                            if (dataSourceNode == null)
                                            {
                                                nsmgr.AddNamespace("ns", "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition");
                                                dataSourceNode = doc.SelectSingleNode("//ns:DataSource", nsmgr);
                                            }

                                            if (dataSourceNode == null)
                                            {
                                               throw new Exception($" {report.Name} Doesn't have either 2008 or 2016 Namesapce");
                                            }
                                            // Select the DataProvider element
                                            XmlNode dataProviderNode = dataSourceNode.SelectSingleNode("ns:ConnectionProperties/ns:DataProvider", nsmgr);
                                            XmlNode ConnectStringNode = dataSourceNode.SelectSingleNode("ns:ConnectionProperties/ns:ConnectString", nsmgr);
                                            dataProviderNode.InnerText = "SQLAZURE";
                                            string connectString = ConnectStringNode.InnerText;
                                            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectString);
                                            string initialCatalog = builder.InitialCatalog;
                                            string newConnectString = "Data Source=" + ServerName + ";Initial Catalog=" + initialCatalog ;
                                            ConnectStringNode.InnerText = newConnectString;

                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.WriteLog("Erron Getting dataProviderNode Check to see if the XMLNS is 2016 or 2008" + ex.Message);
                                        }

                                        if(doc.HasChildNodes)
                                        {

                                            StringWriter sw = new StringWriter();
                                            XmlTextWriter tx = new XmlTextWriter(sw);
                                            doc.WriteTo(tx);
                                            string rdlImportName = report.Name + ".rdl";
                                            string str = sw.ToString();// 

                                            try
                                            {
                                                client.Reports.DeleteReportInGroup(workspace.Id, report.Id);
                                                Logger.WriteLog($"Deleting report {report.Name} worked.");
                                            }
                                            catch (Exception ex) { Logger.WriteLog("Deleting report failed " + ex.Message); }

                                            byte[] byteArray = Encoding.ASCII.GetBytes(str);
                                            MemoryStream RdlFileContentStream = new MemoryStream(byteArray);

                                            var import = client.Imports.PostImportWithFileInGroup(workspace.Id,
                                                                                                     RdlFileContentStream,
                                                                                                     rdlImportName,
                                                                                                     ImportConflictHandlerMode.Abort);

                                            // poll to determine when import operation has complete
                                            do { import = client.Imports.GetImportInGroup(workspace.Id, import.Id); }
                                            while (import.ImportState.Equals("Publishing"));

                                            Logger.WriteLog($"Uploading report {report.Name} worked.");

                                        }
                                    }

                                }
                        }
                        else
                        {
                            Logger.WriteLog($"Report {report.Name}  Doesn't have datasource.");
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                Logger.WriteLog(ex.Message);
            }   

        }

        private XmlDocument getModifiedXMLDoc(XmlDocument doc, XmlNamespaceManager nsmgr)
        {

            try
            {
                var ServerName = ConfigurationManager.AppSettings.Get("ServerName");
                // Select the DataSource element
                XmlNode dataSourceNode = doc.SelectSingleNode("//ns:DataSource", nsmgr);
                // Select the DataProvider element
                XmlNode dataProviderNode = dataSourceNode.SelectSingleNode("ns:ConnectionProperties/ns:DataProvider", nsmgr);
                XmlNode ConnectStringNode = dataSourceNode.SelectSingleNode("ns:ConnectionProperties/ns:ConnectString", nsmgr);
                dataProviderNode.InnerText = "SQLAZURE";
                string connectString = ConnectStringNode.InnerText;
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectString);
                string initialCatalog = builder.InitialCatalog;
                string newConnectString = "Data Source=" + ServerName + ";Initial Catalog=" + initialCatalog + ";Authentication=\"Sql Password\"";
                ConnectStringNode.InnerText = newConnectString;
                
                return doc;

            }
            catch (Exception ex) 
            { 
                Logger.WriteLog($"Exception Modifing XML Document {ex.Message}");
                doc.RemoveAll();
                return doc; 
            }

        }

        public void setPaginatedReportCredential()
        {

            var reportNames = reportsClient.GetReportsInGroup(workspace.Id).Value;
            foreach (var report in reportNames)
            {
                if (report.ReportType == "PaginatedReport")
                {
                    var sqlUserName = ConfigurationManager.AppSettings.Get("SqlUserName");
                    var sqlUserPassword = ConfigurationManager.AppSettings.Get("SqlUserPassword");
                    var reportDatasources = reportsClient.GetDatasourcesInGroup(workspace.Id, report.Id);

                    if (reportDatasources.Value.Count > 0)
                    {
                        // In case there is multiple data sources we need to extend this logic
                        Datasource reportDatasource = reportDatasources.Value[0];
                        if (reportDatasource != null)
                        {
                            var datasourceId = reportDatasource.DatasourceId;
                            var gatewayId = reportDatasource.GatewayId;

                            // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
                            UpdateDatasourceRequest req = new UpdateDatasourceRequest
                            {
                                CredentialDetails = new CredentialDetails(
                                new BasicCredentials(sqlUserName, sqlUserPassword),
                                PrivacyLevel.Organizational,
                                EncryptedConnection.Encrypted)
                            };

                            // Execute Patch command to update Azure SQL datasource credentials
                            try
                            {
                                client.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);
                                Logger.WriteLog($"Update credential worked for Report {report.Name}");
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog($"Issue calling Update Datasource API {ex.Message}");
                            }

                        }
                    }
                    {
                        Logger.WriteLog($" {report.Name} Doesn't have a datasource");
                    }
                }
            }

        }

        public void uploadPaginatedReports()
        {
            var logPath = ConfigurationManager.AppSettings.Get("logPath");
            string logDirectory = Path.GetDirectoryName(logPath);
            logDirectory = logDirectory + "/Reports";

            foreach (string filePath in Directory.GetFiles(logDirectory, "*.rdl"))
            {
                
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(filePath);
                StringWriter sw = new StringWriter();
                XmlTextWriter tx = new XmlTextWriter(sw);
                xmlDocument.WriteTo(tx);

                string rdlImportName = Path.GetFileName(filePath);
                string str = sw.ToString();// 

                byte[] byteArray = Encoding.ASCII.GetBytes(str);
                MemoryStream RdlFileContentStream = new MemoryStream(byteArray);

                var import = client.Imports.PostImportWithFileInGroup(workspace.Id,
                                                                         RdlFileContentStream,
                                                                         rdlImportName,
                                                                         ImportConflictHandlerMode.Abort);

                // poll to determine when import operation has complete
                do { import = client.Imports.GetImportInGroup(workspace.Id, import.Id); }
                while (import.ImportState.Equals("Publishing"));

                Logger.WriteLog($"Uploading report {rdlImportName} worked.");

            }

        }

    }
}
