


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
using System.Text.RegularExpressions;


namespace Biosero.Scripting
{

    public class CheckForFinishedSerialisationWork
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"***********      START OF CheckForFinishedSerialisationWork ***************" + Environment.NewLine);
            //retrieve initial global variables values
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string SourcesForEB = context.GetGlobalVariableValue<string>("SourceForEB");
            string DestinationForEB = context.GetGlobalVariableValue<string>("DestinationForEB");
            string WorkPerformed = context.GetGlobalVariableValue<string>("EBCurrentWorkRequired");

            if (WorkPerformed == "Echo" || WorkPerformed == "Bravo")
            {
                WorkPerformed = "Replicate";
            }

            bool SourceDone = false;
            bool DestinationDone = false;


            string API_BASE_URL =  context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";
            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);

            IdentityHelper _identityHelper;

            List<string> AllDestinationsForOrder = new List<string>();



            //Build out and register the root identities (i.e Mosaic Job) if they do not exist
            _identityHelper = new IdentityHelper(_queryClient, _accessioningClient, _eventClient);
            _identityHelper.BuildBaseIdentities();

            //Get all the sources associated with this order
            var sources = _identityHelper.GetSources(RequestedOrder).ToList();
            //Get all the Sources associated with this order
            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();
            //Get all the jobs
            var jobs = _identityHelper.GetJobs(RequestedOrder).ToList();

            var a = sources
            .Where(x => x.Name == SourcesForEB && x.OperationType.ToString() == WorkPerformed)
            .FirstOrDefault();

            int SourceJobId = a.JobId;
            string SourceName = a.Name;
            string SourceId = a.Identifier;


            string SourceCurrentStatus = a.Status.ToString();

            await context.AddOrUpdateGlobalVariableAsync("EBCurrentSourceStatus", SourceCurrentStatus);

            if (SourceCurrentStatus == "Finished")
            {


                a.Properties.SetValue("Status", "Completed");
                _identityHelper.Register(a, SourceJobId, RequestedOrder);


                Console.WriteLine($"  source  plate  {SourceName} with ID {SourceId}  was set from FINISHED to COMPLETED " + Environment.NewLine);

                SourceCurrentStatus = a.Status.ToString();

                await context.AddOrUpdateGlobalVariableAsync("EBCurrentSourceStatus", SourceCurrentStatus);

            }

            var b = destinations
            .Where(x => x.Name == DestinationForEB && x.OperationType.ToString() == WorkPerformed)
            .FirstOrDefault();

            int DestinationJobId = b.JobId;
            string DestinationName = b.Name;
            string DestinationId = b.Identifier;

            string DestinationCurrentStatus = b.Status.ToString();

            await context.AddOrUpdateGlobalVariableAsync("EBCurrentDestinationStatus", DestinationCurrentStatus);

            if (DestinationCurrentStatus == "Finished")
            {


                b.Properties.SetValue("Status", "Completed");
                _identityHelper.Register(b, DestinationJobId, RequestedOrder);


                Console.WriteLine($"  Destination plate  {DestinationName} with ID {DestinationId}  was set from FINISHED to COMPLETED " + Environment.NewLine);

                DestinationCurrentStatus = b.Status.ToString();


                await context.AddOrUpdateGlobalVariableAsync("EBCurrentDestinationStatus", DestinationCurrentStatus);


                var c = sources
                .Where(x => x.ParentIdentifier == DestinationId)
                .FirstOrDefault();

                if (c != null)
                {
                    string NextSourceId = c.Identifier;
                    string NextSourceName = c.Name;
                    int NextSourceJobIde = c.JobId;
                    string NextSourceOperation = c.OperationType.ToString();

                    c.Properties.SetValue("Status", "Queued");
                    _identityHelper.Register(c, DestinationJobId, RequestedOrder);


                    Console.WriteLine($"  Source plate  {NextSourceName} with ID {NextSourceId}  with operation type {NextSourceOperation} was set to QUEUED " + Environment.NewLine);


                    c.Properties.SetValue("Status", "Validating");
                    _identityHelper.Register(c, DestinationJobId, RequestedOrder);


                    Console.WriteLine($"  Source plate  {NextSourceName} with ID {NextSourceId}  with operation type {NextSourceOperation} was set to VALIDATING " + Environment.NewLine);


                    c.Properties.SetValue("Status", "Ready");
                    _identityHelper.Register(c, DestinationJobId, RequestedOrder);


                    Console.WriteLine($"  Source plate  {NextSourceName} with ID {NextSourceId}  with operation type {NextSourceOperation} was set to READY " + Environment.NewLine);


                    c.Properties.SetValue("Status", "Transporting");
                    _identityHelper.Register(c, DestinationJobId, RequestedOrder);


                    Console.WriteLine($"  Source plate  {NextSourceName} with ID {NextSourceId}  with operation type {NextSourceOperation} was set to TRANSPORTING " + Environment.NewLine);


                    c.Properties.SetValue("Status", "Processing");
                    _identityHelper.Register(c, DestinationJobId, RequestedOrder);


                    Console.WriteLine($"  Source plate  {NextSourceName} with ID {NextSourceId}  with operation type {NextSourceOperation} was set to PROCESSING " + Environment.NewLine);
                

                    var d = destinations
                    .Where(x => x.SiblingIdentifier == NextSourceId)
                    .FirstOrDefault();



                    string NextDestinationId = d.Identifier;
                    string NextDestinationName = d.Name;
                    int NextDestinationJobIde = d.JobId;
                    string NextDestinationOperation = d.OperationType.ToString();

                    d.Properties.SetValue("Status", "Processing");
                    _identityHelper.Register(d, DestinationJobId, RequestedOrder);


                    Console.WriteLine($"  destination  plate  {NextDestinationName} with ID {NextDestinationId}  with operation type {NextDestinationOperation} was set to PROCESSING " + Environment.NewLine);

                }

            }

        }

    }
}