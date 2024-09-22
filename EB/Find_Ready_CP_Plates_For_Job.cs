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
using System.Numerics;


namespace Biosero.Scripting
{
    public class Find_Ready_CP_Plates_For_Job
    {

        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string CurrentJob = context.GetGlobalVariableValue<string>("Current EB Job");



            //instantiate LAMA1 objects

            string API_BASE_URL = context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";
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

            //instantiate LAMA1 objects

            List<string> PlatesOnCPCell = new List<string>();
            List<string> FinishedPlatesOnCPCell = new List<string>();
            List<string> PlatesInCrashJob = new List<string>();
            List<string> ReadyPlatesInCrashJob = new List<string>();

            //Loop through all destinations for the job
            foreach (var dest in destinations)
            {
                string DestID = dest.Identifier;
                string DestName = dest.Name;
                string DestType = dest.TypeIdentifier;
                string DestState = dest.Status.ToString();
                string DestOperationType = dest.OperationType.ToString();
                string DestSampleTransfers = dest.SampleTransfers.ToString();
                string DestinationParent = dest.ParentIdentifier != null ? dest.ParentIdentifier.ToString() : null;
                int DestJob = dest.JobId;

                if ((DestJob.ToString() == CurrentJob) && (DestOperationType == "CherryPick"))
                {
                    PlatesOnCPCell.Add(DestName);

                    if (DestState == "Finished")
                    {
                        FinishedPlatesOnCPCell.Add(DestName);
                    }
                }
                else if ((DestJob.ToString() == CurrentJob) && (DestOperationType == "Replicate") && (DestinationParent == null))
                {
                    PlatesInCrashJob.Add(DestName);
                    ReadyPlatesInCrashJob.Add(DestName);
                }
            }

            if (FinishedPlatesOnCPCell.Count > 0)
            {

                string AllCPPlates = string.Join(", ", PlatesOnCPCell);
                string AllFinishedCPPlates = string.Join(", ", FinishedPlatesOnCPCell);

                await context.AddOrUpdateGlobalVariableAsync("All Plates For Job", AllCPPlates);
                await context.AddOrUpdateGlobalVariableAsync("All Ready Plates For Job", AllFinishedCPPlates);

                Console.WriteLine($"All Plates For Job {CurrentJob}: {AllCPPlates}" + Environment.NewLine);
                Console.WriteLine($"All Ready Plates For Job {CurrentJob}: {AllFinishedCPPlates}" + Environment.NewLine);
            }
            else if (ReadyPlatesInCrashJob.Count > 0)
            {
                string AllCrashPlatesForJob = string.Join(", ", PlatesInCrashJob);
                string AllFinishedCrashPlatesForJob = string.Join(", ", ReadyPlatesInCrashJob);
                await context.AddOrUpdateGlobalVariableAsync("All Plates For Job", AllCrashPlatesForJob);
                await context.AddOrUpdateGlobalVariableAsync("All Ready Plates For Job", AllFinishedCrashPlatesForJob);

                Console.WriteLine($"All Plates For Job {CurrentJob}: {AllCrashPlatesForJob}" + Environment.NewLine);
                Console.WriteLine($"All Ready Plates For Job {CurrentJob}: {AllFinishedCrashPlatesForJob}" + Environment.NewLine);
            }

        }

    }
}

