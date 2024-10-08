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
    public class Check_All_Echo_Plates_Finished
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {

            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            int RequestedJob = context.GetGlobalVariableValue<int>("Job Number");


            await context.AddOrUpdateGlobalVariableAsync("Echo Plates Not Finished", true);

            string API_BASE_URL =  context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";

            string ExtractedReplicationVolume = "";
            string ExtractedNextReplicationVolume = "";
            string NextReplicateLabware = "";
            string FurtherReplicateLabware = "";
            string DestinationCommonName = "";
            



            int EBSourcesCount = 0;
            int RepOneCount = 0;
            int RepTwoCount = 0;


            string JobWorkflowFragment = "";

            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);

            List<string> AllCPSourcesForEB = new List<string>();
            List<string> AllCPSourcesIdentifiersForEB = new List<string>();
            List<string> AllSerializePlates = new List<string>();
            List<string> AllReplicatePlates = new List<string>();
            List<string> AllNextReplicatePlates = new List<string>();
            List<string> AllCrashPlatesForEB = new List<string>();
            List<string> AllCrashPlateIdentierssForEB = new List<string>();
            List<string> AllCrashDestinationsEB = new List<string>();
            List<string> EchoPairList = new List<string>();
            List<string> EchoFinishedPlates = new List<string>();


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


            //  MosaicDestination? destination = destinations?.FirstOrDefault(d => d.Description == "777");
            foreach (var dest in destinations)
            {
                bool echoSourceFound = false;
                string DestinationName = dest.Name;
                string DestinationDescription = dest.Description;
                string DestinationSampleTransfers = dest.SampleTransfers;
                string DestinationOperationType = dest.OperationType.ToString();
                string DestinationJobId = dest.JobId.ToString();
                string DestinationId = dest.Identifier.ToString();
                string DestinationStatus = dest.Status.ToString();
                string DestinationLabwareType = dest.CommonName.ToString();
                string DestinationParent = dest.ParentIdentifier != null ? dest.ParentIdentifier.ToString() : null;
                    Serilog.Log.Information("Plate found in dest  = {DestinationName}", DestinationName.ToString());
                    
                    
			            foreach (var src in sources)
			            {
			              string sourceName = src.Name.ToString();
			              
			              if (DestinationName == sourceName)
			              {
			              echoSourceFound = true;
			              }
			            
			            }
			            
			            


                if ((DestinationStatus != "Finished") &&  (DestinationOperationType == "Replicate") && (echoSourceFound==false))
                {
                    Serilog.Log.Information("Echo Plate Not Finished = {DestinationName}", DestinationName.ToString());

                    await context.AddOrUpdateGlobalVariableAsync("Echo Plates Not Finished", true);
                }
                else if ((DestinationStatus == "Finished") && (DestinationOperationType == "Replicate") && (echoSourceFound==false))
                {
                    Serilog.Log.Information("Echo Plate  Finished = {DestinationName}", DestinationName.ToString());
                    
                     await context.AddOrUpdateGlobalVariableAsync("EchoDestLabwareType", DestinationLabwareType);

                    await context.AddOrUpdateGlobalVariableAsync("Echo Plates Not Finished", false);


                    EchoFinishedPlates.Add(DestinationName);
                }


            }


            string FinishedEchoPlates = string.Join(",", EchoFinishedPlates);
            await context.AddOrUpdateGlobalVariableAsync("Finished Echo Plates", FinishedEchoPlates);
                    Serilog.Log.Information("FinishedEchoPlates= {FinishedEchoPlates}", FinishedEchoPlates.ToString());


        }
        public List<Biosero.DataModels.Resources.Identity> GetPlatesWithNumberOfParents<T>(IdentityHelper _helper, QueryClient _queryClient, int nParents, string ownerId)
        {
            IEnumerable<DataModels.Resources.Identity> identities = typeof(T) switch
            {
                Type t when t == typeof(MosaicSource) => _helper.GetSources(ownerId).Select(od => od as Biosero.DataModels.Resources.Identity),
                Type t when t == typeof(MosaicDestination) => _helper.GetDestinations(ownerId).Select(od => od as Biosero.DataModels.Resources.Identity),
                Type t when t == typeof(MosaicJob) => _helper.GetJobs(ownerId).Select(od => od as Biosero.DataModels.Resources.Identity),
                _ => throw new Exception("Type not supported"),
            };

            //   Serilog.Log.Information($"There are {orderIdents.Count} identities associated with owner ID {ownerId}");
            Dictionary<int, List<Biosero.DataModels.Resources.Identity>> identitiesWithNumberOfParents = new Dictionary<int, List<Biosero.DataModels.Resources.Identity>>();

            int numberOfParentsCount;
            string parent;
            string Ident;

            foreach (var identity in identities)
            {
                numberOfParentsCount = 0;
                parent = identity.Properties.GetOrDefaultValue("ParentIdentifier", string.Empty);
                Ident = identity.Identifier;
                //	Serilog.Log.Information($"Parent ID = {parent}");
                while (parent != string.Empty)
                {                    //add null check here?
                    Biosero.DataModels.Resources.Identity parentId = _queryClient.GetIdentity(parent);
                    parent = parentId.Properties.GetOrDefaultValue("ParentIdentifier", string.Empty);
                    //Serilog.Log.Information($"Parent ID = {parent}");
                    numberOfParentsCount += 1;
                }

                if (identitiesWithNumberOfParents.ContainsKey(numberOfParentsCount))
                {
                    identitiesWithNumberOfParents[numberOfParentsCount].Add(identity);
                }

                else
                {
                    identitiesWithNumberOfParents.Add(numberOfParentsCount, new List<Biosero.DataModels.Resources.Identity> { identity });
                }
            }
            foreach (KeyValuePair<int, List<Biosero.DataModels.Resources.Identity>> kvp in identitiesWithNumberOfParents)
            {
                //   Serilog.Log.Information($"There are {kvp.Value.Count} identities with  {kvp.Key} parents");
            }
            if (identitiesWithNumberOfParents.ContainsKey(nParents))
            {
                return identitiesWithNumberOfParents[nParents];
            }
            else
            {
                return new List<Biosero.DataModels.Resources.Identity>();
            }
        }

    }
}


