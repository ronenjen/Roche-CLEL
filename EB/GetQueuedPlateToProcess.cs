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
    public class GetQueuedPlateToProcess
    {
        public string EvaluateDouble(double value)
        {
            if (value >= 0.5)
            {
                return "Bravo";
            }
            else
            {
                return "Echo";
            }
        }


        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"***********       Processing of GetQueuedPlateToProcess begins **********" + Environment.NewLine);
            //Retrieve current global variables value
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string EBSourcesToBeTransferred = context.GetGlobalVariableValue<string>("EBSourcesToBeTransferred");
            string EBWorkRequired = context.GetGlobalVariableValue<string>("EBOrderWorkType");
            string EBWorkInitiatesd = context.GetGlobalVariableValue<string>("EBOrderWorkInitiates");



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

            //Format list of transferred sources to an array
            string[] QueuedEBSourcesArray = EBSourcesToBeTransferred.Split(',');


            string firstPlate = QueuedEBSourcesArray[0];

            string updatedQueuedEBSourcesArray = string.Join(",", QueuedEBSourcesArray, 1, QueuedEBSourcesArray.Length - 1);

            // Convert the array to a List
            List<string> EBSourcesList = new List<string>(QueuedEBSourcesArray);


            var aa = sources
            .Where(a => a.Name == firstPlate)
            .FirstOrDefault();

            if (aa != null)
            {
                //Find the source name and priority for each ready plate
                string SourceName = aa.Name;
                string SourcePriority = aa.Priority;
                int SourceJobId = aa.JobId;
                string SourceIdentifier = aa.Identifier;
                string SourceOperation = aa.OperationType.ToString();
                string SourceLabwareType = aa.CommonName.ToString();

                string SourceParentIdentifier = aa.ParentIdentifier != null ? aa.ParentIdentifier.ToString() : null;



                var cc = destinations
                .Where(x => x.Identifier == SourceParentIdentifier)
                .FirstOrDefault();

                if (cc != null)
                {
                    string DestId = cc.Name;
                    int DestJobId = cc.JobId;
                    string DestName = cc.Name;
                    string DestOperation = cc.OperationType.ToString();
                    //reset statuses
                    //set cp destination status to "Completed"


                    var bb = destinations
                    .Where(a => a.SiblingIdentifier == SourceIdentifier)
                    .FirstOrDefault();

                    if (bb != null)
                    {

                        string NewDestinationName = bb.Name;
                        string NewDestinationId = bb.Identifier;
                        string NewDestinationOperation = bb.OperationType.ToString();


                        cc.Properties.SetValue("Status", "Completed");
                        _identityHelper.Register(cc, DestJobId, RequestedOrder);
                        Console.WriteLine($"*********** source  plate  {DestName} with ID {DestId} and operation {DestOperation} status was set to Completed " + Environment.NewLine);


                        //set serialisation source status to "Transporting"

                        aa.Properties.SetValue("Status", "Transporting");
                        _identityHelper.Register(aa, SourceJobId, RequestedOrder);
                        Console.WriteLine($"*********** source  plate  {SourceName} with Id {SourceIdentifier} and operation {SourceOperation} status was set to Transporting " + Environment.NewLine);

                        bb.Properties.SetValue("Status", "Transporting");
                        _identityHelper.Register(bb, SourceJobId, RequestedOrder);
                        Console.WriteLine($"*********** destination  plate  {NewDestinationName} with Id {NewDestinationId} and operation {NewDestinationOperation} status was set to Transporting " + Environment.NewLine);
                    }
                    await context.AddOrUpdateGlobalVariableAsync("CurrentSourcePlate", SourceName);
                    await context.AddOrUpdateGlobalVariableAsync("CPPlateLabwareType", SourceLabwareType);
                    Console.WriteLine($"***********   The CP source Labware type  {SourceLabwareType} " + Environment.NewLine);

                    string updatedEBSourceList = string.Join(",", QueuedEBSourcesArray, 1, QueuedEBSourcesArray.Length - 1);

                    await context.AddOrUpdateGlobalVariableAsync("Queued EB Plates Count", QueuedEBSourcesArray.Length - 1);
                    await context.AddOrUpdateGlobalVariableAsync("All queued EB plates", updatedEBSourceList);
                    await context.AddOrUpdateGlobalVariableAsync("Work Required For Current EB Plate", EBWorkRequired);


                    Console.WriteLine($"***********   Current source plate  {SourceName} " + Environment.NewLine);
                    Console.WriteLine($"***********   New total EB Plates is (Queued EB Plates Count) {QueuedEBSourcesArray.Length - 1} " + Environment.NewLine);
                    Console.WriteLine($"***********   New list of EB Plates (All queued EB plates) {updatedEBSourceList} " + Environment.NewLine);
                    Console.WriteLine($"***********   Work Required For Current EB Plate  {EBWorkRequired} " + Environment.NewLine);
                }
            }
            else
            {

                Console.WriteLine($"**CRASH IS DONE " + Environment.NewLine);
                Console.WriteLine($"*********** source  plate  {firstPlate}  " + Environment.NewLine);


                var CrashDestination = destinations
                .Where(x => x.Name == firstPlate)
                .FirstOrDefault();

                string CrashDestSibling = CrashDestination.SiblingIdentifier;
                string CrashDestName = CrashDestination.Name;
                string CrashDestId = CrashDestination.Identifier;
                string CrashDestOperation = CrashDestination.OperationType.ToString();
                string CrashDestTransfers = CrashDestination.SampleTransfers.ToString();
                int CrashJob = CrashDestination.JobId;

                var CrashSource = sources
                .Where(x => x.Identifier == CrashDestSibling)
                .FirstOrDefault();

                string CrashSourceName = CrashSource.Name;
                string CrashSourceIdentifier = CrashSource.Identifier;
                string CrashSourceId = CrashSource.Identifier;
                string CrashLabwareType = CrashSource.CommonName.ToString();
                string CrashSourceOperation = CrashSource.OperationType.ToString();
                int CrashSourceJob = CrashSource.JobId;


                string cleanedInputForReplicateTwo = CrashDestTransfers.Trim();
                // Convert the cleaned string to a double
                double CrashTransferVolume = double.Parse(cleanedInputForReplicateTwo);

                string CrashInstrument = EvaluateDouble(CrashTransferVolume);


                Console.WriteLine($"**CrashDestName {CrashDestName}" + Environment.NewLine);
                Console.WriteLine($"***CrashSourceName  {CrashSourceName}  " + Environment.NewLine);

                CrashSource.Properties.SetValue("Status", "Transporting");
                _identityHelper.Register(CrashSource, CrashSourceJob, RequestedOrder);
                Console.WriteLine($"*********** source  plate  {CrashSourceName} with Id {CrashSourceId} and operation {CrashSourceOperation} status was set to Transporting " + Environment.NewLine);

                CrashDestination.Properties.SetValue("Status", "Transporting");
                _identityHelper.Register(CrashDestination, CrashJob, RequestedOrder);
                Console.WriteLine($"*********** destination  plate  {CrashDestName} with Id {CrashDestId} and operation {CrashDestOperation} status was set to Transporting " + Environment.NewLine);


                await context.AddOrUpdateGlobalVariableAsync("CurrentSourcePlate", CrashSourceName);
                await context.AddOrUpdateGlobalVariableAsync("CurrentDestinationPlate", CrashDestName);
                await context.AddOrUpdateGlobalVariableAsync("CPPlateLabwareType", CrashLabwareType);
                Console.WriteLine($"***********   The crash source Labware type  {CrashLabwareType} " + Environment.NewLine);

                string updatedEBSourceList = string.Join(",", QueuedEBSourcesArray, 1, QueuedEBSourcesArray.Length - 1);

                await context.AddOrUpdateGlobalVariableAsync("Queued EB Plates Count", QueuedEBSourcesArray.Length - 1);
                await context.AddOrUpdateGlobalVariableAsync("All queued EB plates", updatedEBSourceList);
                await context.AddOrUpdateGlobalVariableAsync("Work Required For Current EB Plate", CrashInstrument);

                

                Console.WriteLine($"***********   Current crash source plate  {CrashSourceName} " + Environment.NewLine);
                Console.WriteLine($"***********   New total EB  crash Plates is (Queued EB Plates Count) {QueuedEBSourcesArray.Length - 1} " + Environment.NewLine);
                Console.WriteLine($"***********   New list of EB  crash Plates (All queued EB plates) {updatedEBSourceList} " + Environment.NewLine);
                Console.WriteLine($"***********    crash Work Required For Current EB Plate  {CrashInstrument} " + Environment.NewLine);

            }
        }
    }
}


