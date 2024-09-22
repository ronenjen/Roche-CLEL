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
using System.IO;


namespace Biosero.Scripting
{
    public class CP_Is_Only_A_CP_Job
    {  
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            string CurrentJobNumber = context.GetGlobalVariableValue<string>("Job Number");
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");

            //instantiate LAMA1 objects

            string API_BASE_URL =  context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";
            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);


            IdentityHelper _identityHelper;


            //Build out and register the root identities (i.e Mosaic Job) if they do not exist
            _identityHelper = new IdentityHelper(_queryClient, _accessioningClient, _eventClient);
            _identityHelper.BuildBaseIdentities();


            //Get all the sources associated with this order
            var sources = _identityHelper.GetSources(RequestedOrder).ToList();

            //Get all the destinations associated with this order
            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();
            //Get all the jobs
            var jobs = _identityHelper.GetJobs(RequestedOrder).ToList();



            foreach (var dest in destinations)
            {
                string DestinationName = dest.Name;
                string DestinationDescription = dest.Description;
                string DestinationSampleTransfers = dest.SampleTransfers;
                string DestinationOperationType = dest.OperationType.ToString();
                string DestinationJobId = dest.JobId.ToString();
                string DestinationParent = dest.ParentIdentifier != null ? dest.ParentIdentifier.ToString() : null;


                if ((DestinationParent == null) && (DestinationJobId == CurrentJobNumber) && (DestinationOperationType == "CherryPick"))
                {


                    Serilog.Log.Information("DestinationName= {DestinationName}", DestinationName.ToString());
                }
            }

        }

    }
}

