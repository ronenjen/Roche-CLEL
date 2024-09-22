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
    public class Remove_Finished_Plates_From_All_Plates_For_Job
    {

        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string JobPlates = context.GetGlobalVariableValue<string>("All Plates For Job");
            string JobFinishedPlates = context.GetGlobalVariableValue<string>("All Ready Plates For Job");
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


            Console.WriteLine($"List of CP Plates for job {CurrentJob} prior to update: {JobPlates}" + Environment.NewLine);
            Console.WriteLine($"List of finished Plates for job {CurrentJob} prior to update: {JobFinishedPlates}" + Environment.NewLine);

            // Split both JobPlates and JobFinishedPlates into lists
            List<string> CPPlatesList = JobPlates.Split(',').ToList();
            List<string> FinishedCPPlatesList = JobFinishedPlates.Split(',').ToList();

            // Remove the finished plates from the cpPlatesList
            CPPlatesList = CPPlatesList.Except(FinishedCPPlatesList).ToList();

            // Join the remaining items back into a comma-separated string
            JobPlates = string.Join(",", CPPlatesList);

            await context.AddOrUpdateGlobalVariableAsync("All Plates For Job", JobPlates);


            Console.WriteLine($"List of CP Plates for jop {CurrentJob} post update: {JobPlates}" + Environment.NewLine);


        }

    }
}

