using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaginatedReportUpdate
{
    internal class PowerBIPermissionScopes
    {
        public static readonly string[] OnPremGatewayManagement = new string[]
    {
            "https://analysis.windows.net/powerbi/api/Capacity.ReadWrite.All",
            "https://analysis.windows.net/powerbi/api/Content.Create",
            "https://analysis.windows.net/powerbi/api/Report.ReadWrite.All",
            "https://analysis.windows.net/powerbi/api/Dataset.ReadWrite.All",
            "https://analysis.windows.net/powerbi/api/Dataflow.ReadWrite.All",
            "https://analysis.windows.net/powerbi/api/Workspace.ReadWrite.All",
            "https://analysis.windows.net/powerbi/api/Gateway.ReadWrite.All",
    };
    }
}
