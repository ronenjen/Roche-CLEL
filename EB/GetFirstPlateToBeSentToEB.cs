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
    public class GetFirstPlateToBeSentToEB
    {


        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"***********       Processing of GetFirstPlateToBeSentToEB begins **********" + Environment.NewLine);
            //Retrieve current global variables value
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string EBSourcesToBeTransferred = context.GetGlobalVariableValue<string>("EBSourcesToBeTransferred");


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

            //Format list of transferred sources to an array
            string[] EBTransferredSourcesArray = EBSourcesToBeTransferred.Split(',');

            // Convert the array to a List
            List<string> EBSourcesList = new List<string>(EBTransferredSourcesArray);


            List<string> HighListOfSources = new List<string>();
            List<string> MediumListOfSources = new List<string>();
            List<string> LowListOfSources = new List<string>();
            List<string> ReorderedListOfSources = new List<string>();

            //Loop through each of the sources found n the array
            foreach (string EBSource in EBTransferredSourcesArray)
            {
                var a = sources
                .Where(a => a.Name == EBSource)
                .FirstOrDefault();

                //Find the source name and priority for each rady plate
                string SourceName = a.Name;
                string SourcePriority = a.Priority;

                //Add to the relevnt list of arrays based on source priority
                if (SourcePriority == "High")
                {
                    HighListOfSources.Add(SourceName);
                }
                else if (SourcePriority == "Medium")
                {
                    MediumListOfSources.Add(SourceName);
                }
                else
                {
                    LowListOfSources.Add(SourceName);
                }

            }

            // Joins arrays to a single, prioritised list
            ReorderedListOfSources.AddRange(HighListOfSources);
            ReorderedListOfSources.AddRange(MediumListOfSources);
            ReorderedListOfSources.AddRange(LowListOfSources);


            // Find the first member plate in the list
            string CurrentPlate = ReorderedListOfSources.FirstOrDefault();

            //Retrieve an object for the plate destination and source
            var dd = destinations
            .Where(x => x.Name == CurrentPlate)
            .FirstOrDefault();



            var cc = sources
            .Where(x => x.Name == CurrentPlate)
            .FirstOrDefault();

            //Retrieve the identity status for both the source and the destination
            string EchoPlateSourceStatus = cc.Status.ToString();
            string EchoPlateDestinationStatus = dd.Status.ToString();


            Console.WriteLine($"***********       The current plate processed is:= {CurrentPlate} " + Environment.NewLine);
            Console.WriteLine($"***********       The status source for the plate being processed is:= {EchoPlateSourceStatus} " + Environment.NewLine);
            Console.WriteLine($"***********       The status of the destination plate is:= {EchoPlateDestinationStatus} " + Environment.NewLine);

            //If the plate source and destination identities are marked as FINISHED status
            if ((CurrentPlate != null) && !((EchoPlateSourceStatus == "Finished") && (EchoPlateDestinationStatus == "Finished")))
            {
                Console.WriteLine($"***********       Found a plate to send! The plate is:= {CurrentPlate} " + Environment.NewLine);
                string NewSourcesToBeSent = string.Join(", ", ReorderedListOfSources);

                await context.AddOrUpdateGlobalVariableAsync("CurrentSourcePlate", CurrentPlate);

            }
            else
            {
                await context.AddOrUpdateGlobalVariableAsync("CurrentSourcePlate", "");
            }

        }
    }
}


