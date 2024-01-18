// See https://aka.ms/new-console-template for more information
using PaginatedReportUpdate;
using System.Configuration;

namespace PaginatedReportUpdate
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                    string ClientId, TenantId, urlEndPoint, inputPath, workspaceName;

                    ClientId = ConfigurationManager.AppSettings.Get("ClientId");
                    TenantId = ConfigurationManager.AppSettings.Get("TenantId");
                    workspaceName = ConfigurationManager.AppSettings.Get("workspaceName");

                    Logger.WriteLog("Starting the log-in window");

                    PowerBIClientWrapper powerBIClient = new PowerBIClientWrapper(workspaceName, ClientId, TenantId, PowerBIPermissionScopes.OnPremGatewayManagement);

                    Logger.WriteLog("Log-in successfully, retrieving the reports...");

                    int selection = -1;

                    while (selection != 0)
                    {
                        Console.Clear();
                        Console.WriteLine("Welcome to migration project");
                        Console.WriteLine("1: Check Paginated Reports" + "\n2: Update Data sources " + "\n3: Update Data sources credentials" + "\n4: Upload Paginated Report" + "\n0: Exit");
                        string input = Console.ReadLine();
                        if (input != null) selection = int.Parse(input);
                        switch (selection)
                        {
                            case 1:
                                fxGetPaginatedReport(powerBIClient);
                                break;
                            case 2:
                                fxSetDataSources(powerBIClient);
                                break;
                            case 3:
                                fxSetDatasourceCredential(powerBIClient);
                                break;
                            case 4:
                                fxUploadReports(powerBIClient);
                                break;
                            case 0:
                                Logger.WriteLog("User selected to exit");
                                return;
                        }
                        Console.WriteLine("Please press any key to continue ....");
                        Console.ReadLine();
                    }
                }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error calling logic {ex.Message}");
            }
        }
       public static void fxGetPaginatedReport(PowerBIClientWrapper powerBIClient) 
        {
            try
            {
                powerBIClient.getPaginatedReports();
            }
            catch (Exception e)
            {
                Logger.WriteLog($"Exception calling SetDataSources2PBI {e.Message}");
            }
        }

        public static void fxSetDataSources(PowerBIClientWrapper powerBIClient)
        {
            try
            {
                powerBIClient.SetPaginatedReportDataSourceswithProvider();
            }
            catch (Exception e)
            {
                Logger.WriteLog($"Exception calling SetDataSources2PBI {e.Message}");
            }
        }

        public static void fxSetDatasourceCredential(PowerBIClientWrapper powerBIClient)
        {
            try
            {
                powerBIClient.setPaginatedReportCredential();
            }
            catch (Exception e)
            {
                Logger.WriteLog($"Exception calling Update Datasource Credential {e.Message}");
            }
        }

        public static void fxUploadReports(PowerBIClientWrapper powerBIClient)
        {
            try
            {
                powerBIClient.uploadPaginatedReports();
            }
            catch (Exception e)
            {
                Logger.WriteLog($"Exception calling Update Datasource Credential {e.Message}");
            }
        }

    }
}


