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
    public class GetNextWorkForEB
    {


        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"***********       Processing of GetNextWorkForEB begins **********" + Environment.NewLine);
            //Retrieve current global variables value
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string QueuedEBSourcesToBeTransferred = context.GetGlobalVariableValue<string>("EBSourcesToBeTransferred");
            string PlateWorkRequired = context.GetGlobalVariableValue<string>("Work Required For Current EB Plate");
            string CurretPlate = context.GetGlobalVariableValue<string>("CurrentSourcePlate");



            string API_BASE_URL =  context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";

            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);


            IdentityHelper _identityHelper;


            //Build out and register the root identities (i.e Mosaic Job) if they do not exist
            _identityHelper = new IdentityHelper(_queryClient, _accessioningClient, _eventClient);
            _identityHelper.BuildBaseIdentities();

            var sources = _identityHelper.GetSources(RequestedOrder).ToList();

            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();


            Console.WriteLine($"***********   Work required for plate before update:= {PlateWorkRequired} " + Environment.NewLine);
            Console.WriteLine($"***********   The current plate being processed:= {CurretPlate} " + Environment.NewLine);


            string[] EBRquiredWorkForPlate = PlateWorkRequired.Split(',');


            string firstMember = EBRquiredWorkForPlate[0];

            string updatedEBWorkList = string.Join(",", EBRquiredWorkForPlate, 1, EBRquiredWorkForPlate.Length - 1);

            await context.AddOrUpdateGlobalVariableAsync("Work Required For Current EB Plate", updatedEBWorkList);
            await context.AddOrUpdateGlobalVariableAsync("EBCurrentWorkRequired", firstMember);

            Console.WriteLine($"***********      Update work for the plate is {updatedEBWorkList} " + Environment.NewLine);
            Console.WriteLine($"***********       The current task is {firstMember} " + Environment.NewLine);


        }
    }
}


