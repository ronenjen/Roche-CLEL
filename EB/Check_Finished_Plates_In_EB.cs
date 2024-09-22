#r Roche.LAMA1.dll


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
using System.Numerics;


namespace Biosero.Scripting
{
    public class Check_Finished_Plates_In_EB
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {

            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string CurrentJob =  context.GetGlobalVariableValue<string>("Current EB Job");



            await context.AddOrUpdateGlobalVariableAsync("Finished EB Source Plates", "");

            await context.AddOrUpdateGlobalVariableAsync("Finished EB Destination Plates", "");

            string CompletedDestinationPlatesForIOC = "";
            string CompletedSourcePlatesForIOC = "";
            string TransportedDestinationPlatesForIOC =  context.GetGlobalVariableValue<string>("EB To IOC Transported Sources");
            string TransportedSourcePlatesForIOC = context.GetGlobalVariableValue<string>("EB To IOC Transported Destinations");



            // connnect to the DS server, declare query, assecssioning and event clients for the URL
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
            //Get all the Sources associated with this order
            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();
            //Get all the jobs
            var jobs = _identityHelper.GetJobs(RequestedOrder).ToList();



            List<string> AllDestinationPlates = new List<string>();
            List<string> AllCompletedDestinationPlates = new List<string>();
            List<string> AllSourcePlates = new List<string>();
            List<string> AllCompletedSourcePlates = new List<string>();
            List<string> AllTransportedDestinationPlates = new List<string>();
            List<string> AllTransportedSourcePlates = new List<string>();

            int TotalDestinationPlates = 0;
            int TotalCompletedDestinationPlates = 0;
            int TotalSourcePlates = 0;
            int TotalCompletedSourcePlates = 0;
            int TotalTransportedDestinationPlates = 0;
            int TotalTransportedSourcePlates = 0;

            foreach (var destination in destinations)
            {

                // Each dest should be set to pending, queued, validating, ready
                // compare the list of non completed dest to the list of all plates - if different set Destination EB Plates Not Finished to True
                string DestName = destination.Name;
                string DestStatus = destination.Status.ToString();
                string DestId = destination.Identifier.ToString();
                string DestOperation = destination.OperationType.ToString();
                int DestJob = destination.JobId;
                string DestLabwareType = destination.CommonName;
                string DestParent = destination.ParentIdentifier != null ? destination.ParentIdentifier.ToString() : null;


                if ((!AllDestinationPlates.Contains(DestName)) && (DestOperation == "Replicate" || DestOperation == "Serialise") && (DestJob== Int32.Parse(CurrentJob)))
                {
                    Console.WriteLine($" destination status is  {DestStatus} for {DestName} " + Environment.NewLine);
                    AllDestinationPlates.Add(DestName);
                }


                if (DestStatus == "Completed")
                {

                    var b = sources
                    .Where(x => x.ParentIdentifier == DestId)
                    .FirstOrDefault();

                    if ((b == null) && (DestOperation == "Replicate" || DestOperation == "Serialise"))
                    {
                        if (!AllCompletedDestinationPlates.Contains(DestName))
                        {
                            AllCompletedDestinationPlates.Add(DestName);
                        }

                        if (!AllTransportedDestinationPlates.Contains(DestName))
                        {
                            AllTransportedDestinationPlates.Add(DestName);
                        }



                        // Check if the string is not empty before adding a comma
                        if (!string.IsNullOrEmpty(CompletedDestinationPlatesForIOC))
                        {
                            CompletedDestinationPlatesForIOC += ", ";
                        }

                        // Add the current plate to the string
                        CompletedDestinationPlatesForIOC += DestName;
                        await context.AddOrUpdateGlobalVariableAsync("REPOneLabwareType", DestLabwareType);
                        Console.WriteLine($" the dest1 labware type is  {DestLabwareType} " + Environment.NewLine);


                        // Check if the string is not empty before adding a comma
                        if (!string.IsNullOrEmpty(TransportedDestinationPlatesForIOC))
                        {
                            TransportedDestinationPlatesForIOC += ", ";
                        }

                        // Add the current plate to the string
                        TransportedDestinationPlatesForIOC += DestName;

                        var destinationUpdate = destinations
                        .Where(x => x.Identifier == DestId)
                        .FirstOrDefault();

                        int JobToUpdateForDestination = destinationUpdate.JobId;
                        string CurrentDestinationName = destinationUpdate.Name;
                        string CurrentDestinationId = destinationUpdate.Identifier;
                        string CurrentDestinationOperation = destinationUpdate.OperationType.ToString();



                        //           destinationUpdate.Properties.SetValue("Status", "Transporting");
                        //   _identityHelper.Register(destinationUpdate, JobToUpdateForDestination, RequestedOrder);


                        //         Console.WriteLine($"  destination  plate  {CurrentDestinationName} with ID {CurrentDestinationId} and operation {CurrentDestinationOperation} was set to TRANSPORTING " + Environment.NewLine);

                    }

                }
                else if ((DestParent== null) && (DestJob == Int32.Parse(CurrentJob)) && (DestOperation=="Replicate"))
                {

                    Console.WriteLine($" reached the right dest place " + Environment.NewLine);
                    if (!AllCompletedDestinationPlates.Contains(DestName))
                    {
                        AllCompletedDestinationPlates.Add(DestName);
                    }

                    if (!AllTransportedDestinationPlates.Contains(DestName))
                    {
                        AllTransportedDestinationPlates.Add(DestName);
                    }



                    // Check if the string is not empty before adding a comma
                    if (!string.IsNullOrEmpty(CompletedDestinationPlatesForIOC))
                    {
                        CompletedDestinationPlatesForIOC += ", ";
                    }

                    // Add the current plate to the string
                    CompletedDestinationPlatesForIOC += DestName;


                    // Check if the string is not empty before adding a comma
                    if (!string.IsNullOrEmpty(TransportedDestinationPlatesForIOC))
                    {
                        TransportedDestinationPlatesForIOC += ", ";
                    }

                    // Add the current plate to the string
                    TransportedDestinationPlatesForIOC += DestName;
                }


            }


            TotalDestinationPlates = AllDestinationPlates.Count();
            TotalCompletedDestinationPlates = AllCompletedDestinationPlates.Count();
            TotalTransportedDestinationPlates = AllTransportedDestinationPlates.Count();

            if (TotalTransportedDestinationPlates != TotalDestinationPlates)
            {
                await context.AddOrUpdateGlobalVariableAsync("Destination EB Plates Not Finished", true);
                Console.WriteLine($"Destination EB Plates Not Finished is set to TRUE " + Environment.NewLine);
            }
            else
            {
                await context.AddOrUpdateGlobalVariableAsync("Destination EB Plates Not Finished", false);
                Console.WriteLine($"Destination EB Plates Not Finished is set to FALSE " + Environment.NewLine);
            }



            await context.AddOrUpdateGlobalVariableAsync("Finished EB Destination Plates", CompletedDestinationPlatesForIOC);
            await context.AddOrUpdateGlobalVariableAsync("Total Completed EB Destinartion Plates", TotalCompletedDestinationPlates);


            string DestinationsList = String.Join(", ", AllDestinationPlates);

            await context.AddOrUpdateGlobalVariableAsync("EB To IOC Transported Destinations", CompletedDestinationPlatesForIOC);

            Console.WriteLine($" Destination plates  {DestinationsList} need returning to the IOC " + Environment.NewLine);
            Console.WriteLine($" Destination plates  {CompletedDestinationPlatesForIOC} were set to completed in the list " + Environment.NewLine);
            Console.WriteLine($" Destination plates  {TransportedDestinationPlatesForIOC} were set to Transporting in the list " + Environment.NewLine);
            Console.WriteLine($" A total of  {TotalDestinationPlates - TotalTransportedDestinationPlates} remain to be sent " + Environment.NewLine);
            Console.WriteLine($" A total of  {TotalCompletedDestinationPlates} were completed destinations " + Environment.NewLine);

            foreach (var source in sources)
            {
                // Each source should be set to pending, queued, validating, ready
                // compare the list of non completed sources to the list of all source plates - if different set Source EB Plates Not Finished to True

                string SourceName = source.Name;
                string SourceStatus = source.Status.ToString();
                string SourceId = source.Identifier.ToString();
                string SourceOperation = source.OperationType.ToString();
                int SourceJob = source.JobId;
                string SourceLabwareType = source.CommonName;
                string SourceParent = source.ParentIdentifier != null ? source.ParentIdentifier.ToString() : null;


                if ((!AllSourcePlates.Contains(SourceName)) && (SourceOperation == "Replicate" || SourceOperation == "Serialise") && (SourceJob == Int32.Parse(CurrentJob)))
                {
                    Console.WriteLine($" source status is  {SourceStatus} for {SourceName} " + Environment.NewLine);
                    AllSourcePlates.Add(SourceName);
                }


                if (SourceStatus == "Completed")
                {


                    var a = destinations
                    .Where(x => x.SiblingIdentifier == SourceId)
                    .FirstOrDefault();

                    string DestStatus2 = a.Status.ToString();
                    string DestId2 = a.Identifier.ToString();

                    //   Console.WriteLine($"  1 {SourceId} , {DestId2}, {DestStatus2}" + Environment.NewLine);
                    if ((DestStatus2 == "Completed" || DestStatus2 == "Transporting"))
                    {
                        var c = sources
                        .Where(x => x.ParentIdentifier == DestId2)
                        .FirstOrDefault();
                        //  Console.WriteLine($"  2  {SourceId}, {DestId2}" + Environment.NewLine);

                        if ((c == null) && (SourceOperation == "Replicate" || SourceOperation == "Serialise"))
                        {
                            //      Console.WriteLine($"  3 " + Environment.NewLine);
                            if (!AllCompletedSourcePlates.Contains(SourceName))
                            {
                                AllCompletedSourcePlates.Add(SourceName);
                            }

                            if (!AllTransportedSourcePlates.Contains(SourceName))
                            {
                                AllTransportedSourcePlates.Add(SourceName);
                            }

                            // Check if the string is not empty before adding a comma
                            if (!string.IsNullOrEmpty(CompletedSourcePlatesForIOC))
                            {
                                CompletedSourcePlatesForIOC += ", ";
                            }

                            // Add the current plate to the string
                            CompletedSourcePlatesForIOC += SourceName;
                            await context.AddOrUpdateGlobalVariableAsync("CPPlateLabwareType", SourceLabwareType);
                            Console.WriteLine($" the source labware type is  {SourceLabwareType} " + Environment.NewLine);

                            // Check if the string is not empty before adding a comma
                            if (!string.IsNullOrEmpty(TransportedSourcePlatesForIOC))
                            {
                                TransportedSourcePlatesForIOC += ", ";
                            }

                            // Add the current plate to the string
                            TransportedSourcePlatesForIOC += SourceName;

                            //    Console.WriteLine($"  4 " + Environment.NewLine);

                            var sourceUpdate = sources
                            .Where(x => x.Identifier == SourceId)
                            .FirstOrDefault();

                            int JobToUpdateForSource = sourceUpdate.JobId;
                            string CurrentSourceName = sourceUpdate.Name;
                            string CurrentSourceId = sourceUpdate.Identifier;
                            string CurrentSourceOperation = sourceUpdate.OperationType.ToString();

                            Console.WriteLine($"  5 {CurrentSourceId} " + Environment.NewLine);

                        }

                    }


                }
                else if ((SourceParent==null) && (SourceJob == Int32.Parse(CurrentJob)) && (SourceOperation=="Replicate"))
                {
                    Console.WriteLine($" reached the right source place" + Environment.NewLine);
                    //      Console.WriteLine($"  3 " + Environment.NewLine);
                    if (!AllCompletedSourcePlates.Contains(SourceName))
                    {
                        AllCompletedSourcePlates.Add(SourceName);
                    }

                    if (!AllTransportedSourcePlates.Contains(SourceName))
                    {
                        AllTransportedSourcePlates.Add(SourceName);
                    }

                    // Check if the string is not empty before adding a comma
                    if (!string.IsNullOrEmpty(CompletedSourcePlatesForIOC))
                    {
                        CompletedSourcePlatesForIOC += ", ";
                    }

                    // Add the current plate to the string
                    CompletedSourcePlatesForIOC += SourceName;
                    await context.AddOrUpdateGlobalVariableAsync("CPPlateLabwareType", SourceLabwareType);
                    Console.WriteLine($" the source labware type is  {SourceLabwareType} " + Environment.NewLine);

                    // Check if the string is not empty before adding a comma
                    if (!string.IsNullOrEmpty(TransportedSourcePlatesForIOC))
                    {
                        TransportedSourcePlatesForIOC += ", ";
                    }

                    // Add the current plate to the string
                    TransportedSourcePlatesForIOC += SourceName;
                }

            }


            TotalSourcePlates = AllSourcePlates.Count();
            TotalCompletedSourcePlates = AllCompletedSourcePlates.Count();
            TotalTransportedSourcePlates = AllTransportedSourcePlates.Count();


            Console.WriteLine($"total 1 {TotalTransportedSourcePlates} " + Environment.NewLine);

            Console.WriteLine($"total 2 {TotalSourcePlates} " + Environment.NewLine);

            if (TotalTransportedSourcePlates != TotalSourcePlates)

            {
                await context.AddOrUpdateGlobalVariableAsync("Source EB Plates Not Finished", true);
                Console.WriteLine($"Source EB Plates Not Finished is set to TRUE " + Environment.NewLine);
            }
            else
            {
                await context.AddOrUpdateGlobalVariableAsync("Source EB Plates Not Finished", false);
                Console.WriteLine($"Source EB Plates Not Finished is set to FALSE " + Environment.NewLine);
            }

            await context.AddOrUpdateGlobalVariableAsync("Finished EB Source Plates", CompletedSourcePlatesForIOC);
            await context.AddOrUpdateGlobalVariableAsync("Total Completed EB Source Plates", TotalCompletedSourcePlates);


            string SourcesList = String.Join(", ", AllSourcePlates);



            await context.AddOrUpdateGlobalVariableAsync("EB To IOC Transported Sources", CompletedSourcePlatesForIOC);


            Console.WriteLine($" Source plates  {SourcesList} need returning to the IOC " + Environment.NewLine);
            Console.WriteLine($" Source plates  {CompletedSourcePlatesForIOC} were set to completed in the list " + Environment.NewLine);
            Console.WriteLine($" Source plates  {TransportedSourcePlatesForIOC} were set to Transporting in the list " + Environment.NewLine);
            Console.WriteLine($" A total of  {TotalSourcePlates - TotalTransportedSourcePlates} remain to be sent " + Environment.NewLine);
            Console.WriteLine($" A total of  {TotalCompletedSourcePlates} were completed sources " + Environment.NewLine);


        }
    }
}


