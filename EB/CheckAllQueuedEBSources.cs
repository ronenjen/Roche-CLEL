


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
using static Microsoft.CodeAnalysis.IOperation;


namespace Biosero.Scripting
{

    public class CheckAllQueuedEBSources
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
            Console.WriteLine($"***********      START OF CHECKALLQUEUED ***************" + Environment.NewLine);
            //retrieve initial global variables values
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string CPSourcesForEB = context.GetGlobalVariableValue<string>("CPSourcesForEB");
            string CurrentJob = context.GetGlobalVariableValue<string>("Current EB Job");
            string AllPlatesForJobOnEB = context.GetGlobalVariableValue<string>("All Plates For Job");
            string AllFinishedPlatesForJobOnEB = context.GetGlobalVariableValue<string>("All Ready Plates For Job");

            bool CrashOperation = false;
            string DestLabwareType = "";
            string EBOperationOne = "";
            string EBOperationTwo = "";
            string EBOperationThree = "";
            string EBOperationFour = "";
            string EBOperationFive = "";
            string SerialiseDestinationId = "";
            string NewStatusForSource = "";
            string FirstReplicationInstrument = "";
            bool SerialiseOperationRequired = false;
            string CrashEBWorkInitiates = "";

            string API_BASE_URL = context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";
            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);

            IdentityHelper _identityHelper;

            List<string> AllDestinationsForOrder = new List<string>();
            List<string> QueuedDestinationsForOrder = new List<string>();
            List<string> ReadyDestinationsForEB = new List<string>();
            List<string> ReadyDestinationsIDsForEB = new List<string>();
            List<string> TransportedSourcesForEB = new List<string>();
            List<string> EBRequiredOperations = new List<string>();
            List<string> AllQueuedSources = new List<string>();


            //Add all required barcodes to a dedicated comma separated list
            List<string> CPToEBBarcodes = CPSourcesForEB.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            string EBSources = string.Join(",", CPToEBBarcodes);
            string initialReadyDestinations = "";// string.Join(",", ReadyDestinationsForEB);

            //Build out and register the root identities (i.e Mosaic Job) if they do not exist
            _identityHelper = new IdentityHelper(_queryClient, _accessioningClient, _eventClient);
            _identityHelper.BuildBaseIdentities();

            //Get all the sources associated with this order
            var sources = _identityHelper.GetSources(RequestedOrder).ToList();
            //Get all the Sources associated with this order
            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();
            //Get all the jobs
            var jobs = _identityHelper.GetJobs(RequestedOrder).ToList();

            //Loop through all destinations for the job
            foreach (var dest in destinations)
            {
                string DestID = dest.Identifier;
                string DestName = dest.Name;
                string DestType = dest.TypeIdentifier;
                string DestState = dest.Status.ToString();
                string DestOperationType = dest.OperationType.ToString();
                string DestSampleTransfers = dest.SampleTransfers.ToString();
                int DestJob = dest.JobId;
                string DestinationParent = dest.ParentIdentifier != null ? dest.ParentIdentifier.ToString() : null;
                DestLabwareType = dest.CommonName.ToString();

                if (AllFinishedPlatesForJobOnEB.Contains(DestName))
                {
                    bool isFirstOperation = string.IsNullOrWhiteSpace(dest.ParentIdentifier);

                    //if the destination is top of the destination tree (so no parent identifier)
                    if ((isFirstOperation) && (EBOperationOne == ""))
                    {
                        EBOperationOne = DestOperationType;
                        // First operation on EB can only be a Serialise or Replicate. Add to EBRequiredOperations. If replicate, also add the transfer volume needed

                        Console.WriteLine($"***********  First Operation for the job: {EBOperationOne} " + Environment.NewLine);
                        EBRequiredOperations.Add(EBOperationOne);

                        if (EBOperationOne == "Replicate")
                        {
                            string cleanedInputForReplicateOne = DestSampleTransfers.Trim();
                            // Convert the cleaned string to a double
                            double RepOneTransferVolume = double.Parse(cleanedInputForReplicateOne);
                        }

                        //Get the next destination plate after the cherry pick operation
                        var SecondDestination = destinations
                        .Where(x => x.ParentIdentifier == DestID)
                        .FirstOrDefault();

                        if ((SecondDestination != null) && (EBOperationTwo == ""))
                        {
                            string SecondDestID = SecondDestination.Identifier;
                            EBOperationTwo = SecondDestination.OperationType.ToString();
                            string SecondDestSampleTransfers = SecondDestination.SampleTransfers.ToString();

                            //Add to EBRequiredOperations. If Replicate, also add the transfer volume needed

                            if (EBOperationTwo == "Replicate")
                            {
                                string cleanedInputForReplicateTwo = SecondDestSampleTransfers.Trim();
                                // Convert the cleaned string to a double
                                double RepTwoTransferVolume = double.Parse(cleanedInputForReplicateTwo);

                                EBOperationTwo = EvaluateDouble(RepTwoTransferVolume);
                            }

                            Console.WriteLine($"***********  Second Operation for the order: {EBOperationTwo} " + Environment.NewLine);
                            EBRequiredOperations.Add(EBOperationTwo);

                            //Get the next destination plate after the cherry pick operation
                            var ThirdDestination = destinations
                            .Where(x => x.ParentIdentifier == SecondDestID)
                            .FirstOrDefault();

                            if ((ThirdDestination != null) && (EBOperationThree == ""))
                            {
                                string ThirdDestID = ThirdDestination.Identifier;
                                EBOperationThree = ThirdDestination.OperationType.ToString();
                                string ThirdDestSampleTransfers = ThirdDestination.SampleTransfers.ToString();

                                //Add to list of EB operation if not Cherry Pick
                                if (EBOperationThree == "Replicate")
                                {
                                    string cleanedInputForReplicateThree = ThirdDestSampleTransfers.Trim();
                                    // Convert the cleaned string to a double
                                    double RepThreeTransferVolume = double.Parse(cleanedInputForReplicateThree);

                                    EBOperationThree = EvaluateDouble(RepThreeTransferVolume);
                                }

                                //Add to EBRequiredOperations. If Replicate, also add the transfer volume needed
                                Console.WriteLine($"***********  Third Operation for the order: {EBOperationThree} " + Environment.NewLine);
                                EBRequiredOperations.Add(EBOperationThree);

                                //Get the next destination plate after the cherry pick operation
                                var FourthDestination = destinations
                                .Where(x => x.ParentIdentifier == ThirdDestID)
                                .FirstOrDefault();

                                if ((FourthDestination != null) && (EBOperationFour == ""))
                                {
                                    string FourthDestID = FourthDestination.Identifier;
                                    EBOperationFour = FourthDestination.OperationType.ToString();
                                    string FourthDestSampleTransfers = FourthDestination.SampleTransfers.ToString();

                                    //Add to list of EB operation if not Cherry Pick
                                    if ((EBOperationFour != "CherryPick") && (EBOperationFour == "Replicate"))
                                    {
                                        string cleanedInputForReplicateFour = FourthDestSampleTransfers.Trim();
                                        // Convert the cleaned string to a double
                                        double RepFourTransferVolume = double.Parse(cleanedInputForReplicateFour);

                                        EBOperationFour = EvaluateDouble(RepFourTransferVolume);
                                    }

                                    //Add to EBRequiredOperations. If Replicate, also add the transfer volume needed
                                    Console.WriteLine($"***********  Fourth Operation for the order: {EBOperationFour} " + Environment.NewLine);
                                    EBRequiredOperations.Add(EBOperationFour);

                                    //Get the next destination plate after the cherry pick operation
                                    var FifthDestination = destinations
                                    .Where(x => x.ParentIdentifier == SerialiseDestinationId)
                                    .FirstOrDefault();

                                    if ((FifthDestination != null) && (EBOperationFive == ""))
                                    {
                                        string FifthDestID = FifthDestination.Identifier;
                                        EBOperationFive = FifthDestination.OperationType.ToString();
                                        string FifthDestSampleTransfers = FifthDestination.SampleTransfers.ToString();


                                        //Add to list of EB operation if not Cherry Pick
                                        if ((EBOperationFive != "CherryPick") && (EBOperationFive == "Replicate"))
                                        {
                                            string cleanedInputForReplicateFive = FifthDestSampleTransfers.Trim();
                                            // Convert the cleaned string to a double
                                            double RepiveTransferVolume = double.Parse(cleanedInputForReplicateFive);

                                            EBOperationFive = EvaluateDouble(RepiveTransferVolume);
                                        }

                                        //Add to EBRequiredOperations. If Replicate, also add the transfer volume needed
                                        Console.WriteLine($"***********  Fifth Operation for the order: {EBOperationFive} " + Environment.NewLine);
                                        EBRequiredOperations.Add(EBOperationFive);
                                    }
                                }
                            }
                        }

                    }

                    var SourceForDestination = sources
                    .Where(x => x.ParentIdentifier == DestID)
                    .FirstOrDefault();

                    if (SourceForDestination != null)
                    {
                        string SourceOperation = SourceForDestination.OperationType.ToString();
                        string SameSourceName = SourceForDestination.Name;
                        string SameSourceStatus = SourceForDestination.Status.ToString();
                        string SameSourceIdentifier = SourceForDestination.Identifier;
                        int SourceJobID = SourceForDestination.JobId;


                        bool isFirstSource = string.IsNullOrWhiteSpace(SourceForDestination.ParentIdentifier);

                        //Add plate to list of plates only if of CherryPick opertion type and child source id either serialise or replicate

                        if ((!AllDestinationsForOrder.Contains(DestName)) && (DestOperationType == "CherryPick") && (SourceOperation == "Serialise" || SourceOperation == "Replicate") && (isFirstSource))
                        {
                            AllDestinationsForOrder.Add(DestName);
                            QueuedDestinationsForOrder.Add(DestName);
                            Serilog.Log.Information("### Adding {DestName} to QueuedDestinationsForOrder and AllDestinationsForOrder", DestName.ToString());
                        }


                        //Get the destination for the next child source
                        var ChildSourceDestination = destinations
                        .Where(x => x.SiblingIdentifier == SameSourceIdentifier)
                        .FirstOrDefault();

                        if (ChildSourceDestination != null)
                        {
                            string DestinationName = ChildSourceDestination.Name;
                            string DestinationStatus = ChildSourceDestination.Status.ToString();
                            string DestinationIdentifier = ChildSourceDestination.Identifier;
                            int DestinationJobID = ChildSourceDestination.JobId;
                            string DestinationOperationType = ChildSourceDestination.OperationType.ToString();

                            //Setting status to QUEUED for all destinations siblings for the child source. only if both CP and next destination are pending


                            if ((DestState == "Pending") && (DestOperationType == "CherryPick"))
                            {
                                SourceForDestination.Properties.SetValue("Status", "Queued");
                                _identityHelper.Register(SourceForDestination, SourceJobID, RequestedOrder);

                                Console.WriteLine($"***********  source plate  {SameSourceName} with id {SameSourceIdentifier} and operation {SourceOperation} was set to Queued" + Environment.NewLine);

                                ChildSourceDestination.Properties.SetValue("Status", "Queued");
                                _identityHelper.Register(ChildSourceDestination, DestinationJobID, RequestedOrder);

                                Console.WriteLine($"***********  destination plate  {DestinationName} with id {DestinationIdentifier} and operation {DestinationOperationType} was set to Queued" + Environment.NewLine);

                            }


                            string NextSourceStatus = SourceForDestination.Status.ToString();


                            if (NextSourceStatus == "Queued")
                            {
                                SourceForDestination.Properties.SetValue("Status", "Validating");
                                _identityHelper.Register(SourceForDestination, SourceJobID, RequestedOrder);


                                Console.WriteLine($"***********  source plate  {SameSourceName}  with id {SameSourceIdentifier} and operation {SourceOperation}  was set to Validating" + Environment.NewLine);


                                ChildSourceDestination.Properties.SetValue("Status", "Validating");
                                _identityHelper.Register(ChildSourceDestination, DestinationJobID, RequestedOrder);


                                Console.WriteLine($"***********  destination plate  {DestinationName} with id {DestinationIdentifier} and operation {DestinationOperationType}  was set to Validating" + Environment.NewLine);


                            }


                            if ((!ReadyDestinationsForEB.Contains(DestName)) && (DestState == "Finished") && (AllFinishedPlatesForJobOnEB.Contains(DestName)))
                            {
                                ReadyDestinationsForEB.Add(DestName);
                                ReadyDestinationsIDsForEB.Add(DestID);

                                //    var currentStatus = SourceIndentityState;
                                SourceForDestination.Properties.SetValue("Status", "Ready");
                                _identityHelper.Register(SourceForDestination, SourceJobID, RequestedOrder);

                                Console.WriteLine($"***********  source plate  {SameSourceName}  with id {SameSourceIdentifier} and operation {SourceOperation}  was set to Ready" + Environment.NewLine);


                                //Add source to the list of queued sources
                                AllQueuedSources.Add(SameSourceName);


                                ChildSourceDestination.Properties.SetValue("Status", "Ready");
                                _identityHelper.Register(ChildSourceDestination, DestinationJobID, RequestedOrder);


                                Console.WriteLine($"***********  destination plate  {DestinationName} with id {DestinationIdentifier} and operation {DestinationOperationType}  was set to Ready" + Environment.NewLine);


                                NewStatusForSource = ChildSourceDestination.Status.ToString();
                                string CurrentDestinationName = ChildSourceDestination.Name.ToString();
                                string CurrentDestinationIdentifier = ChildSourceDestination.Identifier;


                            }


                            if ((DestState == "Finished") && (DestOperationType == "CherryPick"))
                            {


                                var CPDestination = destinations
                                .Where(x => x.Identifier == DestID)
                                .FirstOrDefault();


                                CPDestination.Properties.SetValue("Status", "Completed");
                                _identityHelper.Register(CPDestination, SourceJobID, RequestedOrder);

                                Console.WriteLine($"***********  source plate  {DestName} with id {DestinationIdentifier} and operation {DestOperationType} was set to Completed" + Environment.NewLine);



                            }
                            else
                            {
                                Console.WriteLine($"****NOTHING TO SEE HERE" + Environment.NewLine);

                            }


                        }
                    }
                    else
                    {
                        Console.WriteLine($"**CRASH START********" + Environment.NewLine);

                        CrashOperation = true;

                        if ((!AllDestinationsForOrder.Contains(DestName)) && (DestOperationType == "Replicate") && (DestinationParent==null))
                        {
                            AllDestinationsForOrder.Add(DestName);
                            QueuedDestinationsForOrder.Add(DestName);
                            ReadyDestinationsForEB.Add(DestName);
                            ReadyDestinationsIDsForEB.Add(DestID);
                            Serilog.Log.Information("### Adding {DestName} to QueuedDestinationsForOrder and AllDestinationsForOrder", DestName.ToString());
                        }



                        string cleanedInputForReplicateTwo = DestSampleTransfers.Trim();
                        // Convert the cleaned string to a double
                        double CrashTransferVolume = double.Parse(cleanedInputForReplicateTwo);

                        string CrashEBOperation = EvaluateDouble(CrashTransferVolume);

                         CrashEBWorkInitiates = CrashEBOperation;


                        await context.AddOrUpdateGlobalVariableAsync("Crash Operation Done", CrashOperation);

                    }
         
                }
            }




            // Check if the first item is "CherryPick" and remove it
            if (EBRequiredOperations.First() == "CherryPick")
            {
                EBRequiredOperations.RemoveAt(0);
            }



            //format EB operations list to a comma separated list
            string AllEBOperationsForOrder = string.Join(",", EBRequiredOperations);

            //format EB operations list to a comma separated list
            //    string AllQueuedPlatesFromCP = string.Join(",", AllQueuedSources);


            string FirstEBOperation = EBRequiredOperations[0];
            string EBWorkInitiates = "";

            if (FirstEBOperation == "Serialise" || FirstEBOperation == "Bravo")
            {
                EBWorkInitiates = "Bravo";
            }
            else if (FirstEBOperation == "Echo")
            {
                EBWorkInitiates = "Echo";
            }



            //Assign to Required EB work to global variable EBOrderWorkInitiates

            if (CrashEBWorkInitiates != "")
            {
                await context.AddOrUpdateGlobalVariableAsync("EBOrderWorkInitiates", CrashEBWorkInitiates);
            }
            else
            {
                await context.AddOrUpdateGlobalVariableAsync("EBOrderWorkInitiates", EBWorkInitiates);
            }
            


            await context.AddOrUpdateGlobalVariableAsync("Queued EB Plates Count", AllQueuedSources.Count);
            //  await context.AddOrUpdateGlobalVariableAsync("All queued EB plates", AllQueuedPlatesFromCP);

            //   Console.WriteLine($"*********** The sources set to queued from CP: {AllQueuedPlatesFromCP} " + Environment.NewLine);

            //Assign to Required EB work to global variable EBOrderWorkType
            await context.AddOrUpdateGlobalVariableAsync("EBOrderWorkType", AllEBOperationsForOrder);



            //     Console.WriteLine($"***********  TOTAL QUEUED PLATES {AllQueuedSources.Count} " + Environment.NewLine);



            // Format all found plates array to a string
            string AllEBDestinedSources = string.Join(",", AllDestinationsForOrder);
            Console.WriteLine($"These EB destination plates were found for EB: {AllEBDestinedSources} " + Environment.NewLine);

            //Count total EB based plates
            int TotalEBDestinedSources = AllDestinationsForOrder.Count;
            Console.WriteLine($" A Total of {TotalEBDestinedSources} sources were found for the  EB workstation" + Environment.NewLine);


            string AllQueuedEBDestinedSources = string.Join(",", QueuedDestinationsForOrder);


            string AllEBReadySources = "";
            string AllEBReadySourcesIDs = "";
            int TotalEBReadySources = 0;
            //  string AllEBTransportedSources = "";
            //  int TotalEBTransportedSources = 0;


            //If at least one plate on EB is rteady - Retrieve list of plates and count
            if (ReadyDestinationsForEB.Count() > 0)
            {
                AllEBReadySources = string.Join(",", ReadyDestinationsForEB);
                AllEBReadySourcesIDs = string.Join(",", ReadyDestinationsIDsForEB);
                TotalEBReadySources = ReadyDestinationsForEB.Count;
            }
            else
            {
                AllEBReadySources = "";
                TotalEBReadySources = 0;
            }


            //   await context.AddOrUpdateGlobalVariableAsync("TotalEBQueuedDestinedSources", TotalEBQueuedDestinedSources);
            await context.AddOrUpdateGlobalVariableAsync("EBSourcesToBeTransferred", AllEBReadySources);
            await context.AddOrUpdateGlobalVariableAsync("EBSourceIDsToBeTransferred", AllEBReadySourcesIDs);
            await context.AddOrUpdateGlobalVariableAsync("TotalEBReadySources", TotalEBReadySources);
            await context.AddOrUpdateGlobalVariableAsync("EBRempSourceLabwareType", DestLabwareType);


            Console.WriteLine($"All CP Plates for job {CurrentJob}: {AllPlatesForJobOnEB}" + Environment.NewLine);
            if (CrashEBWorkInitiates != "")
            {
                Console.WriteLine($"This is the first instrument path needed for the job: {CrashEBWorkInitiates} " + Environment.NewLine);
            }
            else
            {
                Console.WriteLine($"This is the first instrument path needed for the job: {EBWorkInitiates} " + Environment.NewLine);
            }

            Console.WriteLine($"These are the required EB operations for the job: {AllEBOperationsForOrder} " + Environment.NewLine);
            Console.WriteLine($"These are the ready plates set to FINISHED and ready to move to EB: {AllEBReadySources} " + Environment.NewLine);
            Console.WriteLine($" A Total of {TotalEBReadySources} plate were ready to be sent to the  EB workstation" + Environment.NewLine);
            Console.WriteLine($"Labware type for EB is: {DestLabwareType} " + Environment.NewLine);

            //end of for destination loop 


        }

    }
}