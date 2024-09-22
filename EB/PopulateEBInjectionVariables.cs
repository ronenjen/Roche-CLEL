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
    public class PopulateEBInjectionVariables
    {


        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"***********       Processing of PopulateEBInjectionVariables begins **********" + Environment.NewLine);
            //Retrieve current global variables value
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string CurrentSourcePlate = context.GetGlobalVariableValue<string>("CurrentSourcePlate");
            string EBWorkToPerform = context.GetGlobalVariableValue<string>("EBCurrentWorkRequired");


            string InterpretedEBWork = "";

            if (EBWorkToPerform == "Echo"|| EBWorkToPerform == "Bravo")
            {
                InterpretedEBWork = "Replicate";
            }
            else
            {
                InterpretedEBWork = EBWorkToPerform;
            }

            string DestinationsList = "";
            string InstructionsList = "";
            string SolventTransfersList = "";
            string SampleTransfersList = "";
            string DestinationPriority = "";

            int DestinationPriorityNum = 0;

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

            var EBSource = sources
            .Where(x => x.Name == CurrentSourcePlate && x.OperationType.ToString() == InterpretedEBWork)
            .FirstOrDefault();


            string SourceName = EBSource.Name;
            int SourceJobId = EBSource.JobId;
            string SourceLabwareType = EBSource.CommonName;
            string SourceId = EBSource.Identifier;

            var EBDestination = destinations
            .Where(x => x.SiblingIdentifier == SourceId && x.OperationType.ToString() == InterpretedEBWork)
            .ToList();



            // Loop through all items in EBDestination
            foreach (var destination in EBDestination)
            {

                if (!string.IsNullOrEmpty(DestinationsList))
                {
                    DestinationsList += ";";
                }


                if (!string.IsNullOrEmpty(InstructionsList))
                {
                    InstructionsList += ";";
                }


                if (!string.IsNullOrEmpty(SolventTransfersList))
                {
                    SolventTransfersList += ";";
                }


                if (!string.IsNullOrEmpty(SampleTransfersList))
                {
                    SampleTransfersList += ";";
                }


                string DestinationName = destination.Name;
                string DestinationSolventTransfers = destination.SolventTransfers;
                string DestinationSampleTransfers = destination.SampleTransfers;

                if (DestinationPriority == "")
                {
                    DestinationPriority = destination.Priority;


                    switch (DestinationPriority)
                    {
                        case "High":
                            DestinationPriorityNum = 1;
                            break;
                        case "Medium":
                            DestinationPriorityNum = 2;
                            break;
                        case "Low":
                            DestinationPriorityNum = 3;
                            break;
                        default:
                            DestinationPriorityNum = 2;
                            break;
                    }
                }


                Console.WriteLine($"***********    Adding {DestinationName} to the destination list" + Environment.NewLine);


                InstructionsList = InstructionsList + SourceName + "-" + destination.Name;
                DestinationsList = DestinationsList + DestinationName;
                SolventTransfersList = SolventTransfersList + DestinationSolventTransfers;
                SampleTransfersList = SampleTransfersList + DestinationSampleTransfers;

            }



            await context.AddOrUpdateGlobalVariableAsync("InstructionsForEB", InstructionsList);
            await context.AddOrUpdateGlobalVariableAsync("DestinationForEB", DestinationsList);
            await context.AddOrUpdateGlobalVariableAsync("SourceForEB", SourceName);



            await context.AddOrUpdateGlobalVariableAsync("Job Number", SourceJobId);
            await context.AddOrUpdateGlobalVariableAsync("EchoSourceLabwareType", SourceLabwareType);

            await context.AddOrUpdateGlobalVariableAsync("EBSolventTransfers", SolventTransfersList);
            await context.AddOrUpdateGlobalVariableAsync("EBSampleTransfers", SampleTransfersList);
            await context.AddOrUpdateGlobalVariableAsync("Job Priority", DestinationPriorityNum.ToString());


            //Set status of plate to PROCESSING
            var cc = sources
            .Where(x => x.Name == SourceName && x.OperationType.ToString() == InterpretedEBWork)
            .FirstOrDefault();

            int SourceJobID = cc.JobId;
            string SourceIdentifier = cc.Identifier;
            string SourceOperationType = cc.OperationType.ToString();

            cc.Properties.SetValue("Status", "Processing");
            _identityHelper.Register(cc, SourceJobID, RequestedOrder);

            Console.WriteLine($"***********    Source plate {SourceName} with ID {SourceIdentifier} for operation {SourceOperationType} status was set to Processing" + Environment.NewLine);


            //Set status of plate to PROCESSING
            var cc1 = destinations
            .Where(x => x.SiblingIdentifier == SourceIdentifier && x.OperationType.ToString() == InterpretedEBWork)
            .FirstOrDefault();

            int DestinationJobID = cc1.JobId;
            string DestinationIdentifier = cc1.Identifier;
            string DestinationOperationType = cc1.OperationType.ToString();
            string DestinationName2 = cc1.Name;

            cc1.Properties.SetValue("Status", "Processing");
            _identityHelper.Register(cc1, DestinationJobID, RequestedOrder);

            Console.WriteLine($"***********    Destination plate {DestinationName2} with ID {DestinationIdentifier} for operatrion {DestinationOperationType} status was set to Processing" + Environment.NewLine);

        }

    }
}



