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
    public class Process_EB_Sorted_Jobs
    {

        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            string PrioritisedJobs = context.GetGlobalVariableValue<string>("Prioritised Jobs");
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string FirstReplicationBarcodes = context.GetGlobalVariableValue<string>("RepOnePlaceholderBarcodes");
            string SecondReplicationBarcodes = context.GetGlobalVariableValue<string>("RepTwoPlaceholderBarcodes");
            string SourcesCurrentlyOnEB = context.GetGlobalVariableValue<string>("CurrentSourcesOnEB");

            int PriorityId = 0;



            Console.WriteLine($"Prioritised jobs for the order: = {PrioritisedJobs}" + Environment.NewLine);
            //   Console.WriteLine($"Barcodes for first replication: = {FirstReplicationBarcodes}" + Environment.NewLine);
            //  Console.WriteLine($"Barcodes for second replication: = {SecondReplicationBarcodes}" + Environment.NewLine);

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


            List<string> JobReplicationOneBarcodes = new List<string>();
            List<string> JobReplicationTwoBarcodes = new List<string>();
            List<string> PrioritisedJobsList = PrioritisedJobs.Split(',').ToList();

            // Get highest priority job
            string PrioritisedJobId = PrioritisedJobsList[0];


            Console.WriteLine($"The current highest priority job: {PrioritisedJobId.ToString()}" + Environment.NewLine);


            //Retrienve job name and identifier from Identities dataset
            var i = jobs
            .Where(x => x.Properties.GetValue<string>("JobId") == PrioritisedJobId)
            .FirstOrDefault();


            string JobName = i.Name;
            string JobIdentifier = i.Identifier;
            int CurrentJobId = i.JobId;
            string Priority = i.Priority;
            string JobFragment = i.WorkflowFragment;
            int JobId = i.JobId;

            await context.AddOrUpdateGlobalVariableAsync("Job Number", CurrentJobId);

            //Retrieve destination details from identities dataset for the highest priority job
            var a = destinations
            .Where(a => a.Properties.GetValue<string>("JobId") == PrioritisedJobId)
            .FirstOrDefault();

            //Find the labware type for the destinaiton plate
            string AssayLabwareType = a.CommonName;

            switch (Priority)
            {
                case "High":
                    PriorityId = 1;
                    break;
                case "Medium":
                    PriorityId = 2;
                    break;
                case "Low":
                    PriorityId = 3;
                    break;
            }

            Console.WriteLine($"The current job is: {JobName}" + Environment.NewLine);
            Console.WriteLine($"The current job priority is: {Priority.ToString()}" + Environment.NewLine);

            if (JobFragment.Contains("Replicate"))
            {
                Console.WriteLine($"Currently performing operation = Replicate for job {JobId.ToString()}" + Environment.NewLine);
                await context.AddOrUpdateGlobalVariableAsync("Job Fragment", "Replicate");
            }
            else
            {
                Console.WriteLine($"No EB work is required for Job {JobId.ToString()} on the CP workcell" + Environment.NewLine);
                await context.AddOrUpdateGlobalVariableAsync("Job Fragment", "");
            }

            //loop through all destinaitons for the order
            foreach (var dest in destinations)
            {
                //Retrieve plate name and job
                string currentDestName = dest.Name;
                int currentDestJob = dest.JobId;
                string currentDestOperation = dest.OperationType.ToString();
                string CurrentDestParent = dest.ParentIdentifier != null ? dest.ParentIdentifier.ToString() : null;
                bool IsFirstDest = false;


                var DestParent = destinations
                .Where(a => a.Identifier == CurrentDestParent)
                .FirstOrDefault();

                string DestParentOperation = "";

                if (DestParent != null)
                {
                    DestParentOperation = DestParent.OperationType.ToString();
                }


                //    if ((DestParent != null) && (currentDestOperation=="Replicate"))
                //   {

                if (DestParentOperation == "Replicate")
                {
                    IsFirstDest = false;
                }
                else
                {
                    IsFirstDest = true;
                }

                // If the found plate is in the first rplication list, is not yet in the replicaiton barcodes array and is part of the current high priority job -
                // Add it to the replication barcodes array
                if (IsFirstDest && (!JobReplicationOneBarcodes.Contains(currentDestName)) && (currentDestJob == CurrentJobId) && (currentDestOperation == "Replicate"))
                {

                    Console.WriteLine($"found a dest= {currentDestName} for job {JobId.ToString()} on FirstReplication" + Environment.NewLine);
                    JobReplicationOneBarcodes.Add(currentDestName);

                }

                // If the found plate is in the second rplication list, is not yet in the replicaiton barcodes array and is part of the current high priority job -
                // Add it to the replication barcodes array
                if (IsFirstDest = false && (!JobReplicationTwoBarcodes.Contains(currentDestName)) && (currentDestJob == CurrentJobId) && (currentDestOperation == "Replicate"))
                {
                    Console.WriteLine($"found a dest= {currentDestName} for job {JobId.ToString()} on second Replication" + Environment.NewLine);
                    JobReplicationTwoBarcodes.Add(currentDestName);

                }
                // }
            }

            // Format the replication one barcodes array to a string AllBarcodesRequiredForReplicteOne
            string AllBarcodesRequiredForReplicteOne = String.Join(", ", JobReplicationOneBarcodes);

            // Format the replication two barcodes array to a string AllBarcodesRequiredForReplicteTwo
            string AllBarcodesRequiredForReplicteTwo = String.Join(", ", JobReplicationTwoBarcodes);

            //remove the current job form the list of Job IDs
            string result = string.Join(",", PrioritisedJobs.Split(',').Where(s => s != PrioritisedJobId));

            // Update global variable list of jobs 
            await context.AddOrUpdateGlobalVariableAsync("Prioritised Jobs", result);

            string CombinedBarcodes = "";

            int firstlength = AllBarcodesRequiredForReplicteOne.Length;
            int secondlength = AllBarcodesRequiredForReplicteTwo.Length;


            // Combine both strings into one to loop through all barcodes
            if (firstlength > 0 || secondlength == 0)
            {
                CombinedBarcodes = AllBarcodesRequiredForReplicteOne;
            }
            else if (firstlength == 0 || secondlength > 0)
            {
                CombinedBarcodes = AllBarcodesRequiredForReplicteTwo;
            }
            else if (firstlength > 0 || secondlength > 0)
            {
                CombinedBarcodes = AllBarcodesRequiredForReplicteOne + ", " + AllBarcodesRequiredForReplicteTwo;
            }

            if (CombinedBarcodes.Length > 0)
            {
                Console.WriteLine($"The combined barcodes are  {CombinedBarcodes} for job {JobId.ToString()}" + Environment.NewLine);
                await context.AddOrUpdateGlobalVariableAsync("All EB Assay Plates", CombinedBarcodes);
                // Split the combined string into an array of barcodes
                string[] allBarcodesArray = CombinedBarcodes.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each member of the array
                foreach (string barcode in allBarcodesArray)
                {

                        var CurrentReplicateDestination = destinations
                       .Where(x => x.Name == barcode)
                       .FirstOrDefault();

                        int DestinationJobID = CurrentReplicateDestination.JobId;
                        string DestinationName = CurrentReplicateDestination.Name;
                        string DestinationId = CurrentReplicateDestination.Identifier;
                        string DestinationSibling = CurrentReplicateDestination.SiblingIdentifier;

                    var CurrentReplicateSource = sources
                   .Where(x => x.Identifier == DestinationSibling)
                   .FirstOrDefault();

                    string SourceName = CurrentReplicateSource.Name;

                    if (SourcesCurrentlyOnEB.Contains(SourceName))
                    {
                        await context.AddOrUpdateGlobalVariableAsync("EB Job Ready To be Done", true);
                        CurrentReplicateDestination.Properties.SetValue("Status", "Queued");
                        _identityHelper.Register(CurrentReplicateDestination, DestinationJobID, RequestedOrder);

                        Console.WriteLine($"Destinaiton plate {DestinationName} was set to status QUEUED" + Environment.NewLine);
                    }
                    else
                    {
                        await context.AddOrUpdateGlobalVariableAsync("EB Job Ready To be Done", false);
                        Console.WriteLine($"work for job {JobId.ToString()} not yet ready to be performed. Source plate {SourceName} is not on EB" + Environment.NewLine);
                    }
                }
            }

            Console.WriteLine($"££££££££££££££££££££££" + Environment.NewLine);

            await context.AddOrUpdateGlobalVariableAsync("Current EB Job", PrioritisedJobId);


        }

    }
}

