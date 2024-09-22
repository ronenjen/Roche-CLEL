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
    public class Process_State_Machine_EB
    {


        static string ExtractNumber(string input)
        {
            // Initialize an empty result string
            string result = string.Empty;

            // Iterate through each character in the input string
            foreach (char c in input)
            {
                // Check if the character is a digit or a decimal point
                if (char.IsDigit(c) || c == '.')
                {
                    result += c;
                }
                else
                {
                    // Stop collecting characters once a non-numeric character is found
                    break;
                }
            }

            // Return the extracted number
            return result;
        }

        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {

            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string API_BASE_URL = context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";



            string FirstReplicateDestName = "";
            string FirstReplicateDestIdentifier = "";
            string FirstReplicateOperationType = "";
            string FirstReplicateSampleTransfers = "";
            string FirstReplicateID = "";
            string FirstReplicateLabware = "";
            string FirstReplicateSibling = "";
            string FirstReplicationInstructions = "";
            string FirstCrashReplicationInstructions = "";

            string FirstCrashReplicateDestName = "";
            string FirstCrashReplicateDestIdentifier = "";
            string FirstCrashReplicateOperationType = "";
            string FirstCrashReplicateSampleTransfers = "";
            string FirstCrashReplicateID = "";
            string FirstCrashReplicateLabware = "";
            string FirstCrashReplicateSibling = "";


            string RepOnePlaceholderBarcodes = "";
            string RepTwoPlaceholderBarcodes = "";


            string SecondReplicateDestName = "";
            string SecondReplicateDestIdentifier = "";
            string SecondReplicateOperationType = "";
            string SecondReplicateSampleTransfers = "";
            string SecondReplicateID = "";
            string SecondReplicateLabware = "";
            string SecondReplicationInstructions = "";
            string SecondCrashReplicateDestName = "";
            string SecondCrashReplicateDestIdentifier = "";
            string SecondCrashReplicateOperationType = "";
            string SecondCrashReplicateSampleTransfers = "";
            string SecondCrashReplicateID = "";
            string SecondCrashReplicateLabware = "";
            string SecondCrashReplicationInstructions = "";



            string ExtractedReplicationVolume = "";
            string ExtractedNextReplicationVolume = "";
            string ExtractedCrashReplicationVolume = "";
            string ExtractedCrashNextReplicationVolume = "";
            string FurtherReplicateLabware = "";
            string DestinationCommonName = "";



            int EBSourcesCount = 0;
            int RepOneCount = 0;
            int RepTwoCount = 0;
            int EBCrashSourcesCount = 0;
            int CrashRepOneCount = 0;
            int CrashRepTwoCount = 0;


            string JobWorkflowFragment = "";

            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);


            List<string> AllCPSourcesForEB = new List<string>();
            List<string> AllCPSourcesIdentifiersForEB = new List<string>();
            List<string> AllCrashSourcesForEB = new List<string>();
            List<string> AllCrashSourcesIdentifiersForEB = new List<string>();
            List<string> AllSerializePlates = new List<string>();
            List<string> FirstReplicatePlates = new List<string>();
            List<string> SecondReplicatePlates = new List<string>();
            List<string> FirstCrashReplicatePlates = new List<string>();
            List<string> SecondCrashReplicatePlates = new List<string>();
            List<string> FirstReplicaitonPairList = new List<string>();
            List<string> SecondReplicaitonPairList = new List<string>();
            List<string> FirstCrashReplicaitonPairList = new List<string>();
            List<string> SecondCrashReplicaitonPairList = new List<string>();
            List<string> ReplicateOneList = new List<string>();
            List<string> CrashReplicateOneList = new List<string>();

            List<string> highPriorityJobs = new List<string>();
            List<string> mediumPriorityJobs = new List<string>();
            List<string> lowPriorityJobs = new List<string>();
            List<string> sortedJobs = new List<string>();

            List<string> RepOneBarcodes = new List<string>();
            List<string> RepTwoBarcodes = new List<string>();


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


            //  Loop through all Destination plates for the order
            foreach (var dest in destinations)
            {
                string DestinationName = dest.Name;
                string DestinationDescription = dest.Description;
                string DestinationSampleTransfers = dest.SampleTransfers;
                string DestinationOperationType = dest.OperationType.ToString();
                string DestinationJobId = dest.JobId.ToString();
                string DestinationId = dest.Identifier.ToString();
                string DestinationSibling = dest.SiblingIdentifier != null ? dest.SiblingIdentifier.ToString() : null;
                string DestinationParent = dest.ParentIdentifier != null ? dest.ParentIdentifier.ToString() : null;

                // "First" variables are related to the first operation
                string FirstDestName = "";
                string FirstDestIdentifier = "";
                string FirstOperationType = "";
                string FirstCrashDestName = "";
                string FirstCrashDestIdentifier = "";
                string FirstCrashOperationType = "";




                // Find all plates of operation type = CherryPick (= those to be created onthe CP cell)
                if (DestinationOperationType == "CherryPick")
                {


                    //Retrieve the source object for the found destination
                    var RepParentPlate = sources
                                        .Where(x => x.Name == DestinationName)
                                        .FirstOrDefault();
                    if (RepParentPlate != null)
                    {

                        string RepParentPlateSourceName = RepParentPlate.Name;
                        string RepParentPlateSourceId = RepParentPlate?.Identifier;


                        // if not member already - add to a list of all EB CP sources for the order (for both name and identifier)
                        if ((!AllCPSourcesForEB.Contains(DestinationName)) && (DestinationParent == null))
                        {

                            Console.WriteLine($"**************" + Environment.NewLine);
                            Console.WriteLine($"*****REMP racks beins" + Environment.NewLine);
                            Console.WriteLine($"**************" + Environment.NewLine);
                            Console.WriteLine(Environment.NewLine);

                            Serilog.Log.Information("Plate {DestinationName}, ID {DestinationId} was added to the list of CP source plates for order", RepParentPlateSourceName.ToString(), RepParentPlateSourceId.ToString());

                            AllCPSourcesForEB.Add(DestinationName);
                            AllCPSourcesIdentifiersForEB.Add(DestinationId);
                        }

                        // Find a list of all first destinations for the source above
                        var FirstPlates = destinations
                        .Where(x => x.ParentIdentifier != null &&
                            x.ParentIdentifier == DestinationId)
                        .ToList();

                        if (FirstPlates != null)
                        {

                            //Loop through the list of all initial plates
                            foreach (var Plate in FirstPlates)
                            {
                                FirstDestName = Plate.Name;
                                FirstDestIdentifier = Plate.Identifier;
                                FirstOperationType = Plate.OperationType.ToString();
                                FirstDestIdentifier = Plate.Identifier.ToString();

                                Serilog.Log.Information("The first operation type for the order is  {FirstOperationType}", FirstOperationType.ToString());
                                string NextId = Plate.Identifier;


                                // If operation type = Serialise
                                if (FirstOperationType == "Serialise")
                                {

                                    //  if not member already - add to a list of all serilisation for the order for the order
                                    if (!AllSerializePlates.Contains(FirstDestName))
                                    {
                                        Serilog.Log.Information("Plate {FirstDestName} with ID {FirstDestIdentifier} was added to the list of serialise destination plates for order", FirstDestName.ToString(), FirstDestIdentifier.ToString());
                                        AllSerializePlates.Add(FirstDestName);
                                    }

                                    //add all first replicaitons from current serialisation to object named FirstReplicatePlates
                                    var FirstRepPlates = destinations
                                    .Where(x => x.ParentIdentifier != null &&
                                        x.ParentIdentifier == NextId)
                                    .ToList();

                                    if (FirstRepPlates != null)
                                    {
                                        foreach (var ReplicatePlate in FirstRepPlates)
                                        {
                                            //assign all first replication variables for each replicaiton plate
                                            FirstReplicateDestName = ReplicatePlate.Name;
                                            FirstReplicateDestIdentifier = ReplicatePlate.Identifier;
                                            FirstReplicateOperationType = ReplicatePlate.OperationType.ToString();
                                            FirstReplicateSampleTransfers = ReplicatePlate.SampleTransfers;
                                            FirstReplicateID = ReplicatePlate.Identifier;
                                            FirstReplicateLabware = ReplicatePlate.CommonName;
                                            FirstReplicateSibling = ReplicatePlate.SiblingIdentifier;


                                            // Extrasct the replicastion volume for the plate
                                            var ExtractNum = destinations
                                            .Where(x => x.ParentIdentifier != null && x.Identifier == FirstReplicateID)
                                            .FirstOrDefault();

                                            if ((ExtractedReplicationVolume == "") && (ExtractNum != null))
                                            {
                                                ExtractedReplicationVolume = ExtractNumber(ExtractNum.SampleTransfers);
                                            }



                                            // if not member already - add to a list of all Echo plates needed for the order
                                            if (!FirstReplicatePlates.Contains(FirstReplicateDestName))
                                            {
                                                Serilog.Log.Information("Plate {FirstReplicateDestName} with Id {FirstReplicateID} was added to the list of first replicate destination plates for order", FirstReplicateDestName.ToString(), FirstReplicateID.ToString());
                                                FirstReplicatePlates.Add(FirstReplicateDestName);
                                            }


                                            // find the source identity for the first replicaiton destinaiton
                                            var RepDestSource = sources
                                            .Where(x => x.Identifier == FirstReplicateSibling)
                                            .FirstOrDefault();

                                            if (RepDestSource != null)
                                            {
                                                string DestSourceName = RepDestSource.Name;


                                                FirstReplicationInstructions = DestSourceName + "-" + FirstReplicateDestName;

                                                // If not already a member, add to an array of replicaiton instructions
                                                if (!FirstReplicaitonPairList.Contains(FirstReplicationInstructions))
                                                {
                                                    Serilog.Log.Information("Plate {FirstReplicationInstructions} was added to the list of first replicate instructions for order", FirstReplicationInstructions.ToString());
                                                    FirstReplicaitonPairList.Add(DestSourceName + "-" + FirstReplicateDestName);
                                                }

                                            }

                                            // Find a list of all next replicaiton plates for each  serialsed plate (if exists)
                                            var SecondRepPlates = destinations
                                            .Where(x => x.ParentIdentifier != null &&
                                                x.ParentIdentifier == FirstReplicateID)
                                            .ToList();

                                            if (SecondRepPlates != null)
                                            {
                                                int CheckSecondReplicateExists = SecondRepPlates.Count();

                                                // Are there Second replicasitons needed? only proceed if required
                                                if (CheckSecondReplicateExists > 0)
                                                {
                                                    foreach (var SecondPlateRep in SecondRepPlates)
                                                    {
                                                        SecondReplicateDestName = SecondPlateRep.Name;
                                                        SecondReplicateDestIdentifier = SecondPlateRep.Identifier;
                                                        SecondReplicateOperationType = SecondPlateRep.OperationType.ToString();
                                                        SecondReplicateSampleTransfers = SecondPlateRep.SampleTransfers;
                                                        SecondReplicateID = SecondPlateRep.Identifier;
                                                        SecondReplicateLabware = SecondPlateRep.CommonName;

                                                        var ExtractSecondReplicateNum = destinations
                                                        .Where(x => x.ParentIdentifier != null && x.Identifier == SecondReplicateID)
                                                        .FirstOrDefault();

                                                        if (ExtractedNextReplicationVolume == "")
                                                        {
                                                            ExtractedNextReplicationVolume = ExtractNumber(ExtractSecondReplicateNum.SampleTransfers);
                                                        }

                                                        // if not member already - add to a list of all Echo plates needed for the order
                                                        if (!SecondReplicatePlates.Contains(SecondReplicateDestName))
                                                        {
                                                            Serilog.Log.Information("Plate {SecondReplicateDestName} was added to the list of second replicate destination plates for order", SecondReplicateDestName.ToString());
                                                            SecondReplicatePlates.Add(SecondReplicateDestName);
                                                        }

                                                        //Find the source for the replication destination
                                                        var SecondRepDestSource = sources
                                                        .Where(x => x.Identifier == FirstReplicateSibling)
                                                        .FirstOrDefault();

                                                        string SecondDestSourceName = SecondRepDestSource.Name;

                                                        SecondReplicationInstructions = SecondDestSourceName + "-" + SecondReplicateDestName;

                                                        // If not already a member, add to an array of replicaiton instructions
                                                        if (!SecondReplicaitonPairList.Contains(SecondReplicationInstructions))
                                                        {
                                                            Serilog.Log.Information("Plate {SecondReplicationInstructions} was added to the list of first replicate instructions for order", SecondReplicationInstructions.ToString());
                                                            SecondReplicaitonPairList.Add(SecondReplicationInstructions);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    RepOneCount = FirstReplicatePlates.Count();
                                    RepTwoCount = SecondReplicatePlates.Count();



                                    if (RepOneCount > 0)
                                    {


                                        string REPOneInstrument = "";
                                        double REPOneVolume = Double.Parse(ExtractedReplicationVolume);

                                        if (REPOneVolume < 0.5)
                                        {
                                            REPOneInstrument = "Echo";
                                        }
                                        else
                                        {
                                            REPOneInstrument = "Bravo";
                                        }
                                    }



                                    if (RepTwoCount > 0)
                                    {

                                        string REPTwoInstrument = "";
                                        double REPTwoVolume = 0;

                                        if (ExtractedNextReplicationVolume != "")
                                        {
                                            REPTwoVolume = Double.Parse(ExtractedNextReplicationVolume);

                                            if (REPTwoVolume < 0.5)
                                            {
                                                REPTwoInstrument = "Echo";
                                            }
                                            else
                                            {
                                                REPTwoInstrument = "Bravo";
                                            }
                                        }

                                    }


                                    string ReplicatePlates = string.Join(",", FirstReplicatePlates);
                                    string ReplicateTwoPlates = string.Join(",", SecondReplicatePlates);
                                    await context.AddOrUpdateGlobalVariableAsync("RepOnePlaceholderBarcodes", ReplicatePlates);
                                    await context.AddOrUpdateGlobalVariableAsync("RepTwoPlaceholderBarcodes", ReplicateTwoPlates);


                                    await context.AddOrUpdateGlobalVariableAsync("REPOneLabwareType", FirstReplicateLabware);
                                    await context.AddOrUpdateGlobalVariableAsync("REPTwoLabwareType", SecondReplicateLabware);


                                }
                                //if the first operation on EB is replicate operation
                                else if (FirstOperationType == "Replicate")
                                {
                                    // Find a list of all first replicates 
                                    var FirstRepPlates = destinations
                                    .Where(x => x.ParentIdentifier != null)
                                    .ToList();

                                    if (FirstOperationType != null)
                                    {
                                        //Loop through all first replicaiton plates
                                        foreach (var PlateReplicateOne in FirstRepPlates)
                                        {
                                            FirstReplicateDestName = PlateReplicateOne.Name;
                                            FirstReplicateDestIdentifier = PlateReplicateOne.Identifier;
                                            FirstReplicateOperationType = PlateReplicateOne.OperationType.ToString();
                                            FirstReplicateSampleTransfers = PlateReplicateOne.SampleTransfers;
                                            FirstReplicateID = PlateReplicateOne.Identifier;
                                            FirstReplicateLabware = PlateReplicateOne.CommonName;
                                            FirstReplicateSibling = PlateReplicateOne.SiblingIdentifier;


                                            if (ExtractedReplicationVolume == "")
                                            {
                                                ExtractedReplicationVolume = ExtractNumber(FirstReplicateSampleTransfers);
                                            }

                                            // if not member already - add to a list of all First replication plates needed for the order
                                            if (!FirstReplicatePlates.Contains(FirstReplicateDestName))
                                            {

                                                Serilog.Log.Information("Plate {FirstReplicateDestName} was added to the list of first replicate destination plates for order", FirstReplicateDestName.ToString());
                                                FirstReplicatePlates.Add(FirstReplicateDestName);
                                            }

                                            //Find the source for the replication destination
                                            var RepDestSource = sources
                                            .Where(x => x.Identifier == FirstReplicateSibling)
                                            .FirstOrDefault();

                                            string DestSourceName = RepDestSource.Name;

                                            FirstReplicationInstructions = DestSourceName + "-" + FirstReplicateDestName;

                                            // If not already a member, add to an array of replicaiton instructions
                                            if (!FirstReplicaitonPairList.Contains(FirstReplicationInstructions))
                                            {
                                                Serilog.Log.Information("Plate {FirstReplicationInstructions} was added to the list of first replicate instructions for order", FirstReplicationInstructions.ToString());
                                                FirstReplicaitonPairList.Add(DestSourceName + "-" + FirstReplicateDestName);
                                            }


                                            // If not already a member, add to an array of replicaiton instructions
                                            if (!ReplicateOneList.Contains(FirstReplicateDestName))
                                            {
                                                Serilog.Log.Information("Plate {FirstReplicateDestName} was added to the list of first replicate  for order", FirstReplicateDestName.ToString());
                                                ReplicateOneList.Add(FirstReplicateDestName);
                                            }


                                            // Find a list of all next replications for each plate (if exists)
                                            var SecondReplPlates = destinations
                                            .Where(x => x.ParentIdentifier != null &&
                                                x.ParentIdentifier == FirstReplicateID)
                                            .ToList();

                                            int CheckFurtherReplicateExists = SecondReplPlates.Count();

                                            //only continue if further replicaitons are required
                                            if (CheckFurtherReplicateExists > 0)
                                            {
                                                foreach (var SecondPlateRep in SecondReplPlates)
                                                {
                                                    SecondReplicateDestName = SecondPlateRep.Name;
                                                    SecondReplicateDestIdentifier = SecondPlateRep.Identifier;
                                                    SecondReplicateOperationType = SecondPlateRep.OperationType.ToString();
                                                    SecondReplicateSampleTransfers = SecondPlateRep.SampleTransfers;
                                                    SecondReplicateID = SecondPlateRep.Identifier;
                                                    SecondReplicateLabware = SecondPlateRep.CommonName;


                                                    //Get the replication volume for the second replication
                                                    var ExtractNectReplicateNum = destinations
                                                    .Where(x => x.ParentIdentifier != null && x.Identifier == SecondReplicateID)
                                                    .FirstOrDefault();

                                                    if (ExtractedNextReplicationVolume == "")
                                                    {
                                                        ExtractedNextReplicationVolume = ExtractNumber(ExtractNectReplicateNum.SampleTransfers);
                                                    }

                                                    // if not member already - add to a list of all next replicate plates needed for the order
                                                    if (!SecondReplicatePlates.Contains(SecondReplicateDestName))
                                                    {
                                                        Serilog.Log.Information("Plate {SecondReplicateDestName} was added to the list of second replicates for order", SecondReplicateDestName.ToString());
                                                        SecondReplicatePlates.Add(SecondReplicateDestName);
                                                    }

                                                    //Find the source for the replication destination
                                                    var SecondRepDestSource = sources
                                                    .Where(x => x.Identifier == FirstReplicateSibling)
                                                    .FirstOrDefault();

                                                    string SecondDestSourceName = SecondRepDestSource.Name;

                                                    SecondReplicationInstructions = SecondDestSourceName + "-" + SecondReplicateDestName;

                                                    // If not already a member, add to an array of replicaiton instructions
                                                    if (!SecondReplicaitonPairList.Contains(SecondReplicationInstructions))
                                                    {
                                                        Serilog.Log.Information("Plate {SecondReplicationInstructions} was added to the list of first replicate instructions for order", SecondReplicationInstructions.ToString());
                                                        SecondReplicaitonPairList.Add(SecondReplicationInstructions);
                                                    }
                                                }
                                            }

                                        }
                                    }
                                    RepOneCount = FirstReplicatePlates.Count();
                                    RepTwoCount = SecondReplicatePlates.Count();



                                    if (RepOneCount > 0)
                                    {
                                        //   for (int i = 1; i <= RepOneCount; i++)
                                        //   {
                                        //       RepOneBarcodes.Add("REPOne " + i);
                                        //   }

                                        //    RepOnePlaceholderBarcodes = String.Join(", ", RepOneBarcodes);

                                        string REPOneInstrument = "";
                                        double REPOneVolume = Double.Parse(ExtractedReplicationVolume);

                                        if (REPOneVolume < 0.5)
                                        {
                                            REPOneInstrument = "Echo";
                                        }
                                        else
                                        {
                                            REPOneInstrument = "Bravo";
                                        }
                                    }



                                    if (RepTwoCount > 0)
                                    {

                                        //    for (int i = 1; i <= 10; i++)
                                        //    {
                                        //        RepTwoBarcodes.Add("REPTwo " + i);
                                        //    }

                                        //    RepTwoPlaceholderBarcodes = String.Join(", ", RepTwoBarcodes);
                                        string REPTwoInstrument = "";
                                        double REPTwoVolume = 0;

                                        if (ExtractedNextReplicationVolume != "")
                                        {
                                            REPTwoVolume = Double.Parse(ExtractedNextReplicationVolume);

                                            if (REPTwoVolume < 0.5)
                                            {
                                                REPTwoInstrument = "Echo";
                                            }
                                            else
                                            {
                                                REPTwoInstrument = "Bravo";
                                            }
                                        }

                                    }

                                    string ReplicatePlates = string.Join(",", FirstReplicatePlates);
                                    string ReplicateTwoPlates = string.Join(",", SecondReplicatePlates);
                                    await context.AddOrUpdateGlobalVariableAsync("RepOnePlaceholderBarcodes", ReplicatePlates);
                                    await context.AddOrUpdateGlobalVariableAsync("RepTwoPlaceholderBarcodes", ReplicateTwoPlates);


                                    await context.AddOrUpdateGlobalVariableAsync("REPOneLabwareType", FirstReplicateLabware);
                                    await context.AddOrUpdateGlobalVariableAsync("REPTwoLabwareType", SecondReplicateLabware);
                                }
                            }
                        }
                    }
                }
                else if (DestinationOperationType == "Replicate")
                {


                    //Retrieve the source object for the found destination
                    var RepCrashParentPlate = sources
                                        .Where(x => x.Identifier == DestinationSibling)
                                        .FirstOrDefault();


                    if (ExtractedCrashReplicationVolume == "")
                    {
                        ExtractedCrashReplicationVolume = ExtractNumber(DestinationSampleTransfers);
                    }


                    if ((RepCrashParentPlate != null) && (DestinationParent == null))
                    {

                        string RepParentPlateSourceName = RepCrashParentPlate.Name;
                        string RepParentPlateSourceId = RepCrashParentPlate?.Identifier;


                        // if not member already - add to a list of all EB CP sources for the order (for both name and identifier)
                        if ((!AllCrashSourcesForEB.Contains(DestinationName)) && (DestinationParent == null))
                        {
                            Console.WriteLine($"**************" + Environment.NewLine); 
                            Console.WriteLine($"*****Crash Plates log beins" + Environment.NewLine);
                            Console.WriteLine($"**************" + Environment.NewLine);
                            Console.WriteLine( Environment.NewLine);

                            Serilog.Log.Information("Found crash plate order operations....");
                            Serilog.Log.Information("Plate {DestinationName}, ID {DestinationId} was added to the list of crash source plates for order", RepParentPlateSourceName.ToString(), RepParentPlateSourceId.ToString());

                            AllCrashSourcesForEB.Add(DestinationName);
                            AllCrashSourcesIdentifiersForEB.Add(DestinationId);
                        }

                        

                                Console.WriteLine($"The first operation type for the order is {DestinationOperationType} " + Environment.NewLine);



                                            if (ExtractedCrashReplicationVolume == "")
                                            {
                                                ExtractedCrashReplicationVolume = ExtractNumber(FirstCrashReplicateSampleTransfers);
                                            }

                                            // if not member already - add to a list of all First replication plates needed for the order
                                            if (!FirstCrashReplicatePlates.Contains(DestinationName))
                                            {

                                                Serilog.Log.Information("Crash plate {DestinationName} was added to the list of first replicate destination plates for the order", DestinationName.ToString());
                                                FirstCrashReplicatePlates.Add(DestinationName);
                                            }

                                            //Find the source for the replication destination
                                            var CrashRepDestSource = sources
                                            .Where(x => x.Identifier == DestinationSibling)
                                            .FirstOrDefault();

                                            string CrashDestSourceName = CrashRepDestSource.Name;

                                            FirstCrashReplicationInstructions = CrashDestSourceName + "-" + DestinationName;

                                            // If not already a member, add to an array of replicaiton instructions
                                            if (!FirstCrashReplicaitonPairList.Contains(FirstCrashReplicationInstructions))
                                            {
                                                Serilog.Log.Information("Instructions for plate {FirstCrashReplicationInstructions} was added to the list of first crash instructions for order", FirstCrashReplicationInstructions.ToString());
                                                FirstCrashReplicaitonPairList.Add(CrashDestSourceName + "-" + DestinationName);

                                            Serilog.Log.Information("Instructions for plate {FirstCrashReplicationInstr");

                                        }


                                    CrashRepOneCount = FirstReplicatePlates.Count();



                                    if (CrashRepOneCount > 0)
                                    {

                                        string CrashRePOneInstrument = "";
                                        double CrashrePOneVolume = Double.Parse(ExtractedCrashReplicationVolume);

                                        if (CrashrePOneVolume < 0.5)
                                        {
                                            CrashRePOneInstrument = "Echo";
                                        }
                                        else
                                        {
                                            CrashRePOneInstrument = "Bravo";
                                        }
                                    }



                                    string CrashReplicatePlates = string.Join(",", FirstCrashReplicatePlates);
                                    string CrashReplicateTwoPlates = string.Join(",", SecondCrashReplicatePlates);
                                    await context.AddOrUpdateGlobalVariableAsync("CrashRepOnePlaceholderBarcodes", CrashReplicatePlates);


                                    await context.AddOrUpdateGlobalVariableAsync("CrashRePOneLabwareType", FirstCrashReplicateLabware);
                                

                    }
                }

            }



            EBSourcesCount = AllCPSourcesForEB.Count();
            EBCrashSourcesCount = AllCrashSourcesForEB.Count();

            string EBSources = string.Join(",", AllCPSourcesForEB);
            string EBCrashSources = string.Join(",", AllCrashSourcesForEB);


            await context.AddOrUpdateGlobalVariableAsync("CPSourcesForEB", EBSources);
            await context.AddOrUpdateGlobalVariableAsync("CrashSourcesForEB", EBCrashSources);


            // Format the array of first replicate instructions to a string
            string FirstReplicaitonPairListString = String.Join(", ", FirstReplicaitonPairList);

            // Format the array of first crash replicate instructions to a string
            string FirstCrashReplicaitonPairListString = String.Join(", ", FirstCrashReplicaitonPairList);

            //Format the array of forst replicate destinaitons to a list
            string ReplicateOneListString = String.Join(", ", ReplicateOneList);

            //Format the array of forst replicate destinaitons to a list
            string CrashReplicateOneListString = String.Join(", ", CrashReplicateOneList);

            // Add first replicaiton instructions to Conductor variabke claled FirstReplicationInstructions
            await context.AddOrUpdateGlobalVariableAsync("FirstReplicationInstructions", FirstReplicaitonPairListString);

            // Add first replicaiton instructions to Conductor variabke claled FirstReplicationInstructions
            await context.AddOrUpdateGlobalVariableAsync("FirstCrashReplicationInstructions", FirstCrashReplicaitonPairListString);

            //Loop through all order jobs
            foreach (var job in jobs)
            {
                string JobPriority = job.Priority;
                string JobNumber = job.JobId.ToString();
                JobWorkflowFragment = job.WorkflowFragment.ToString();



                // Add each job to the correct job priority array
                switch (JobPriority)
                {
                    case "High":
                        highPriorityJobs.Add(JobNumber);
                        break;
                    case "Medium":
                        mediumPriorityJobs.Add(JobNumber);
                        break;
                    case "Low":
                        lowPriorityJobs.Add(JobNumber);
                        break;
                }

            }

            // Combine the lists in the desired order
            sortedJobs.AddRange(highPriorityJobs);
            sortedJobs.AddRange(mediumPriorityJobs);
            sortedJobs.AddRange(lowPriorityJobs);

            //Modify the array to a string type
            string PrioritisedJobs = string.Join(",", sortedJobs);


            Console.WriteLine($"**************" + Environment.NewLine);
            Console.WriteLine($"*****Jobs log beins" + Environment.NewLine);
            Console.WriteLine($"**************" + Environment.NewLine);
            Console.WriteLine(Environment.NewLine);

            Console.WriteLine($"Followed is the list of Prioritised jobs for order {RequestedOrder}  = {PrioritisedJobs}" + Environment.NewLine);


            //Add prioritised job list to the conductor variable called Prioritised Jobs
            await context.AddOrUpdateGlobalVariableAsync("Prioritised Jobs", PrioritisedJobs);


        }

    }
}


