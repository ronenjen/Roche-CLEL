#r Roche.LAMA1.dll


/*
Script written by Ronen Peleg (ronenpeleg@biosero.com)

Description:
Initial script to determine the type of order jobs required to be processed and their contents.
The script also populates various required variables in dataservices in down the line processes
*/




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
using System.Numerics;


namespace Biosero.Scripting
{
    public class SetIOCEBSourcePlatesToFinished
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {

            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string TransportedSources = context.GetGlobalVariableValue<string>("EB To IOC Transported Sources");
          //  string TransportedDestination = context.GetGlobalVariableValue<string>("EB To IOC Transported Destinations");



            // connnect to the DS server, declare query, assecssioning and event clients for the URL
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
            //Get all the Sources associated with this order
            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();
            //Get all the jobs
            var jobs = _identityHelper.GetJobs(RequestedOrder).ToList();


            var TransportingSources = sources
            .Where(x => x.Status.ToString() == "Processing")
            .ToList();


            // Loop through each item in TransportingSources
            foreach (var source in TransportingSources)
            {
                string transportingSourceName = source.Name;
                string transportingSourceId = source.Identifier;
                string transportingSourceOperation = source.OperationType.ToString();
                int transportingSourceJob = source.JobId;


                if ((TransportedSources.Contains(transportingSourceName)) && (transportingSourceOperation == "Replicate"))
                {
                    source.Properties.SetValue("Status", "Finished");
                    _identityHelper.Register(source, transportingSourceJob, RequestedOrder);

                    Console.WriteLine($"  source  plate  {transportingSourceName} with ID {transportingSourceId} and operation {transportingSourceOperation} was set to FINISHED " + Environment.NewLine);

                }
            }



            /*

            var TransportingDestinations = destinations
            .Where(x => x.Status.ToString() == "Processing")
            .ToList();


            // Loop through each item in TransportingSources
            foreach (var destination in TransportingDestinations)
            {
                string transportingDestinationName = destination.Name;
                string transportingDestinationId = destination.Identifier;
                string transportingDestinationOperation = destination.OperationType.ToString();
                int transportingDestinationJob = destination.JobId;


                if ((TransportedDestination.Contains(transportingDestinationName)) && (transportingDestinationOperation == "Replicate"))
                {
                    destination.Properties.SetValue("Status", "Finished");
                    _identityHelper.Register(destination, transportingDestinationJob, RequestedOrder);

                    Console.WriteLine($"  source  plate  {transportingDestinationName} with ID {transportingDestinationId} and operation {transportingDestinationOperation} was set to FINISHED " + Environment.NewLine);

                }
            }

            */



        }
    }
}


