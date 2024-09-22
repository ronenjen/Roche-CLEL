#r Roche.LAMA1.dll

using Biosero.DataModels.Clients;
using Biosero.DataModels.Events;
using Biosero.DataModels.Ordering;
using Biosero.DataModels.Resources;
using Biosero.DataServices.Client;
using Biosero.DataServices.RestClient;
using Biosero.Orchestrator.WorkflowService;
using Newtonsoft.Json;
using Roche.LAMA1.Models;
using Roche.LAMA1.MosaicTypes;
using Roche.LAMA1;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;



namespace Biosero.Scripting
{
    public class FindDetailsOfCurrentPlateForEB
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            //Retrieve all current global variable values
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string CurrentSource = context.GetGlobalVariableValue<string>("CurrentSourcePlate");
            string CurrentSourcesOnEB = context.GetGlobalVariableValue<string>("CurrentSourcesOnEB");
            string CurrentPriority = context.GetGlobalVariableValue<string>("CurrentEBPriority").ToString();
            int RequestedJob = context.GetGlobalVariableValue<int>("Job Number");

            Serilog.Log.Information("Current source plate is {CurrentSource}", CurrentSource.ToString());
            Serilog.Log.Information("Current EB priority is {CurrentPriority}", CurrentPriority.ToString());

            //initialize conductor global variables
            await context.AddOrUpdateGlobalVariableAsync("HigherPriorityJob", false);
            await context.AddOrUpdateGlobalVariableAsync("CurrentSourcePlate", "");


            string SourceStatus = "";
            int CurrentJobPriorityNumber = 0;
            int identityJobID = 0;
            bool JobFoundOnEB = false;
            bool HigherPriorityJob = false;
            string SourceIndentityState = "";
            string SourceIdentityPriority = "";

            if (CurrentPriority != "")
            {
                CurrentJobPriorityNumber = Int32.Parse(CurrentPriority);
            }

            string API_BASE_URL = context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";
            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);


            IdentityHelper _identityHelper;


            //Build out and register the root identities (i.e Mosaic Job) if they do not exist
            _identityHelper = new IdentityHelper(_queryClient, _accessioningClient, _eventClient);
            _identityHelper.BuildBaseIdentities();

            var sources = _identityHelper.GetSources(RequestedOrder).ToList();

            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();

            // look for a destination with same barcode as the current source
            var c = CurrentSource != null
                ? destinations.Where(x => x.Name == CurrentSource).FirstOrDefault()
                : null;

            if (c != null)
            {
                string DestName = c.Name;
                string DestPriority = c.Priority;
                int DestJob = c.JobId;
                int DestPriorityNumber = 0;


                // get the priority for the current plate
                switch (DestPriority)
                {
                    case "High":
                        DestPriorityNumber = 1;
                        break;
                    case "Medium":
                        DestPriorityNumber = 2;
                        break;
                    case "Low":
                        DestPriorityNumber = 3;
                        break;
                }

                // assign job number and priority to global variables
                await context.AddOrUpdateGlobalVariableAsync("JobPriorityNumber", DestPriorityNumber);
                await context.AddOrUpdateGlobalVariableAsync("Job Number", DestJob);

                //get all sources currently on EB
                string[] sourcesArray = CurrentSourcesOnEB.Split(',', ' ', StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine($"The following sources are currently on EB: {sourcesArray}" + Environment.NewLine);

                int SourceArrayCount = 0;
                //loop through every source on EB
                foreach (string src in sourcesArray)
                {
                    SourceArrayCount++;

                    Console.WriteLine($"The number {SourceArrayCount} source currently on EB is: {src}" + Environment.NewLine);


                    if (src == DestName)
                    {
                        //found a match between destination plate in CP and source on EB currently
                        var SourceDetails = sources
                       .Where(x => x.Name == src)
                       .First();
                        //get the status of the current source

                        SourceStatus = SourceDetails.Status.ToString();

                        Console.WriteLine($"The current plate status is  {SourceStatus}" + Environment.NewLine);

                        if ((src != "") && (SourceStatus == "Pending"))
                        {
                            //If plate is found working on EB, set "waiting" variable to TRUE
                            JobFoundOnEB = true;
                            await context.AddOrUpdateGlobalVariableAsync("JobFoundOnEB", JobFoundOnEB);

                            Console.WriteLine($"Jobs Found on EB =  {JobFoundOnEB.ToString()}" + Environment.NewLine);
                        }

                    }

                    var CurrentSourceObject = sources
                   .Where(x => x.Name == CurrentSource)
                   .First();

                    //get the status of the current source
                    string SourceStatus1 = CurrentSourceObject.Status.ToString();

                    Console.WriteLine($"Current Source status is: {SourceStatus1}" + Environment.NewLine);
                    Console.WriteLine($"Current destination priority is: {DestPriorityNumber}" + Environment.NewLine);
                    Console.WriteLine($"Current job priority number is: {CurrentJobPriorityNumber}" + Environment.NewLine);
                    Console.WriteLine($"Current job number is: {RequestedJob}" + Environment.NewLine);

                    if ((DestPriorityNumber < CurrentJobPriorityNumber) && (SourceStatus1 == "Pending"))
                    {
                        Console.WriteLine($"Higher priority job found - {RequestedJob}" + Environment.NewLine);

                        HigherPriorityJob = true;

                        await context.AddOrUpdateGlobalVariableAsync("HigherPriorityJob", HigherPriorityJob);
                        await context.AddOrUpdateGlobalVariableAsync("CurrentSourcePlate", DestName);
                    }

                }
            }

        }

    }
}
