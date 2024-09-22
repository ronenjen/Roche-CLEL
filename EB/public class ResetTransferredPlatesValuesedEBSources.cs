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
    public class ResetTransferredPlatesValuesedEBSources
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string CurrentSource = context.GetGlobalVariableValue<string>("CurrentSourcePlate");
            string EBSourcesToBeTransferred = context.GetGlobalVariableValue<string>("EBSourcesToBeTransferred");
            Serilog.Log.Information("XYXYXY = {EBSourcesToBeTransferred}", EBSourcesToBeTransferred);

            int RequestedJob = context.GetGlobalVariableValue<int>("Job Number");
            int identityJobID = 0;


            string SourceIndentityState = "";
            string SourceIdentityPriority = "";
            string SourceIdentityID = "";

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

            string SourcesToBeTransferred = context.GetGlobalVariableValue<string>("CPSourcesForEB");


            Serilog.Log.Information("The change status to Transferred relies on this list:  = {SourcesToBeTransferred}", SourcesToBeTransferred);


            string SourceIdentifierssToBeTransferred = context.GetGlobalVariableValue<string>("CPSourcesIdentifiersForEB");


            //       Serilog.Log.Information("SourcesToBeTransferred = {SourcesToBeTransferred}", SourcesToBeTransferred);

            //  Serilog.Log.Information("SourceIdentifierssToBeTransferred = {SourceIdentifierssToBeTransferred}", SourceIdentifierssToBeTransferred);
            string TransferredSources = context.GetGlobalVariableValue<string>("EBTransferredSources");

            List<string> AllTransferredSources = TransferredSources.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> AllSourcesToBeTransferred = SourcesToBeTransferred.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> AllSourceIdentifiersToBeTransferred = SourceIdentifierssToBeTransferred.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();


            //
            Serilog.Log.Information("The change status to Transferred relies on this list:  = {AllSourcesToBeTransferred}", AllSourcesToBeTransferred);



            foreach (var source in sources)
            {
                identityJobID = source.JobId; // Identity Job as string
                SourceIdentityID = source.Identifier;
                string SourceIdentityName = source.Name;
                SourceIdentityPriority = source.Priority;
                string SourceIdentityType = source.TypeIdentifier;
                SourceIndentityState = source.Status.ToString();


                Serilog.Log.Information("THESE PLATES WERE SENT====");
                Serilog.Log.Information("SourceIndentityState = {SourceIndentityState}", SourceIndentityState);
                Serilog.Log.Information("SourceIdentityName = {SourceIdentityName}", SourceIdentityName);
                Serilog.Log.Information("CurrentSource = {CurrentSource}", EBSourcesToBeTransferred);
                Serilog.Log.Information("SourceIdentityName = {SourceIdentityName}", SourceIdentityName);


                Serilog.Log.Information("VVVVVVVVVVVVVVVVVVVVVVVVV");
                Serilog.Log.Information("SourceIdentityName = {SourceIdentityName}", SourceIdentityName);
                Serilog.Log.Information("SourceIndentityState = {SourceIndentityState}", SourceIndentityState);
                Serilog.Log.Information("CurrentSource = {CurrentSource}", CurrentSource);
                Serilog.Log.Information("VVVVVVVVVVVVVVVVVVVVVVVVV");

                if ((AllSourcesToBeTransferred.Contains(SourceIdentityName)) && (SourceIndentityState == "Pending") && (CurrentSource == SourceIdentityName))
                {
                    Serilog.Log.Information("This Source was actually set as a TR = {SourceIdentityName}", SourceIdentityName);
                    var currentStatus = SourceIndentityState;
                    source.Properties.SetValue("Status", "Transporting");
                    _identityHelper.Register(source, identityJobID, RequestedOrder);


                    AllTransferredSources.Add(SourceIdentityName);

                }

            }


            string SourcesSentToTransfer = string.Join(",", AllTransferredSources);
            Serilog.Log.Information("THESE PLATES WERE SENT====");
            Serilog.Log.Information("SourcesSentToTransfer = {SourcesSentToTransfer}", SourcesSentToTransfer);



            Serilog.Log.Information("0000000000000 = {SourcesSentToTransfer}", SourcesSentToTransfer);



            if (EBSourcesToBeTransferred != "")
            {
                Serilog.Log.Information("11111111");

                await context.AddOrUpdateGlobalVariableAsync("SourcesSentToTransfer", SourcesSentToTransfer);
                await context.AddOrUpdateGlobalVariableAsync("EBSourcesToBeTransferred", "");
                await context.AddOrUpdateGlobalVariableAsync("Job Number", identityJobID);


                int JobPriorityNumber = 0;

                switch (SourceIdentityPriority)
                {
                    case "High":
                        JobPriorityNumber = 1;
                        break;
                    case "Medium":
                        JobPriorityNumber = 2;
                        break;
                    case "Low":
                        JobPriorityNumber = 3;
                        break;
                }





                var c = sources
                .Where(x => x.Name == CurrentSource)
                .FirstOrDefault();

                string CurrentSourceID = c.Identifier;
                string RepPlateSourceName = c.Name;


                Serilog.Log.Information("2222222 = {RepPlateSourceName}", RepPlateSourceName);


                var d = destinations
                .Where(x => x.SiblingIdentifier == CurrentSourceID)
                .ToList();

                string RepPlateName = string.Join(", ", d.Select(x => x.Name));
                string InstructionsForEcho = "";
                int PriotiyNum = 0;


                foreach (var destination in d)
                {
                    InstructionsForEcho += RepPlateSourceName + "-" + destination.Name + ", ";
                }


                InstructionsForEcho = InstructionsForEcho.TrimEnd(',', ' ');


                await context.AddOrUpdateGlobalVariableAsync("InstructionsForEB", InstructionsForEcho);
                await context.AddOrUpdateGlobalVariableAsync("SourceForEB", RepPlateSourceName);
                await context.AddOrUpdateGlobalVariableAsync("DestinationForEB", RepPlateName);

                Serilog.Log.Information("3333 = {RepPlateSourceName}", InstructionsForEcho);
                Serilog.Log.Information("3333 = {RepPlateSourceName}", RepPlateSourceName);
                Serilog.Log.Information("3333 = {RepPlateSourceName}", RepPlateName);

                //     Serilog.Log.Information("looklook = {RepPlateSourceName}", RepPlateSourceName);


                Serilog.Log.Information("4444 = {RepPlateSourceName}", RepPlateSourceName);
                if (RepPlateSourceName != "")
                {
                    var e = destinations
                    .Where(x => x.Name == RepPlateSourceName)
                    .FirstOrDefault();

                    string SourcePriority = e.Priority;
                    //     Serilog.Log.Information("looklook2 = {SourcePriority}", SourcePriority);

                    switch (SourcePriority)
                    {
                        case "High":
                            PriotiyNum = 1;
                            break;
                        case "Medium":
                            PriotiyNum = 2;
                            break;
                        case "Low":
                            PriotiyNum = 3;
                            break;
                    }

                    await context.AddOrUpdateGlobalVariableAsync("JobPriorityNumber", PriotiyNum);
                }
            }



        }


    }
}