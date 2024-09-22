#r Roche.LAMA1.dll

using Biosero.DataServices.Client;
using Biosero.Orchestrator.WorkflowService;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Roche.LAMA1;
using Roche.LAMA1.Models;
using Roche.LAMA1.MosaicTypes;
using Biosero.DataServices.RestClient;
using Biosero.DataModels.Events;
using Biosero.DataModels.Ordering;
using Biosero.DataModels.Clients;
using Biosero.DataModels.Resources;
using System.Security.Cryptography;
using System.Collections;


namespace Biosero.Scripting
{
    public class UpdateEBPlateTaskList
    {


        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"***********       Processing of UpdateEBPlateTaskList begins **********" + Environment.NewLine);
            //Retrieve current global variables value
            string PlateWorkRequired = context.GetGlobalVariableValue<string>("Work Required For Current EB Plate");
            string CurrentTaskPerformed = context.GetGlobalVariableValue<string>("EBCurrentWorkRequired");
            string EBSourcesToBeTransferred = context.GetGlobalVariableValue<string>("EBSourcesToBeTransferred");

            string[] AllWorkArray = PlateWorkRequired.Split(',');

            string[] filteredTasks = AllWorkArray.Where(x => x != CurrentTaskPerformed).ToArray();

            // Join the remaining members back into a comma-separated string
            string updatedWorkRequired = string.Join(",", filteredTasks);

            await context.AddOrUpdateGlobalVariableAsync("Work Required For Current EB Plate", updatedWorkRequired);

            Console.WriteLine($"***********    The updated list of work required is {updatedWorkRequired} " + Environment.NewLine);

            if (updatedWorkRequired=="")
            {

                string[] EBSourcesToBeTransferredArray = EBSourcesToBeTransferred.Split(',');

                // Skip the first member and get the remaining members
                string[] remainingSources= EBSourcesToBeTransferredArray.Skip(1).ToArray();

                // Join the remaining members back into a comma-separated string
                string updatedEBSources = string.Join(",", remainingSources);

                await context.AddOrUpdateGlobalVariableAsync("EBSourcesToBeTransferred", updatedEBSources);
                await context.AddOrUpdateGlobalVariableAsync("TotalEBReadySources", remainingSources.Count());

                Console.WriteLine($"***********    The updated list of sources to be transferred to EB {updatedEBSources} " + Environment.NewLine);
            }


        }
    }
}


