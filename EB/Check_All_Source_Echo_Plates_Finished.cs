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
    public class Check_All_Source_Echo_Plates_Finished
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {



            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");

            int RequestedJobNo = context.GetGlobalVariableValue<int>("Job Number");


            await context.AddOrUpdateGlobalVariableAsync("Source Echo Plates Not Finished", true);

            string API_BASE_URL =  context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";





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
            List<string> AllCrashsourcesEB = new List<string>();
            List<string> EchoPairList = new List<string>();
            List<string> EchoFinishedSourcePlates = new List<string>();


            IdentityHelper _identityHelper;


            //Build out and register the root identities (i.e Mosaic Job) if they do not exist
            _identityHelper = new IdentityHelper(_queryClient, _accessioningClient, _eventClient);
            _identityHelper.BuildBaseIdentities();


            //Get all the sources associated with this order
            var sources = _identityHelper.GetSources(RequestedOrder).ToList();

            //Get all the sources associated with this order
            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();
            //Get all the jobs
            var jobs = _identityHelper.GetJobs(RequestedOrder).ToList();


            //  Mosaicsource? source = sources?.FirstOrDefault(d => d.Description == "777");
            foreach (var src in sources)
            {
                bool echoSourceFound = false;
                string sourceName = src.Name;
                string sourceDescription = src.Description;
                string sourceOperationType = src.OperationType.ToString();
                string sourceJobId = src.JobId.ToString();
                string sourceId = src.Identifier.ToString();
                string sourceStatus = src.Status.ToString();
                string sourceLabwareType = src.CommonName.ToString();
                string sourceParent = src.ParentIdentifier != null ? src.ParentIdentifier.ToString() : null;
                    
                    
			            foreach (var src1 in sources)
			            {
			              string sourceName1 = src1.Name.ToString();
			              
			              if (sourceName1 == sourceName )
			              {
			              echoSourceFound = true;
			              }
			            
			            }

          Serilog.Log.Information(">>>>>");
          Serilog.Log.Information(">>sourceName= {sourceName}", sourceName.ToString());
          Serilog.Log.Information(">>sourceStatus = {sourceStatus}", sourceStatus.ToString());
          Serilog.Log.Information(">>RequestedJobNo = {RequestedJobNo}", RequestedJobNo.ToString());
          Serilog.Log.Information(">>sourceJobId = {sourceJobId}", sourceJobId.ToString());
          Serilog.Log.Information(">>echoSourceFound  Finished = {echoSourceFound}", echoSourceFound.ToString());
          Serilog.Log.Information(">>>>>");


                if ((sourceStatus != "Finished") && (echoSourceFound==true))
                {
                    Serilog.Log.Information(">>Sources not finished yetd = {srcName}", sourceName.ToString());

                    await context.AddOrUpdateGlobalVariableAsync("Source Echo Plates Not Finished", true);
                }
                else if ((sourceStatus == "Finished") &&  (echoSourceFound==true))
                {
                    Serilog.Log.Information(">>Sources  Finished = {srcName} for order {RequestedOrder} ", sourceName.ToString(), RequestedOrder.ToString());

                    await context.AddOrUpdateGlobalVariableAsync("Source Echo Plates Not Finished", false);


                    EchoFinishedSourcePlates.Add(sourceName);
                    
          	  await context.AddOrUpdateGlobalVariableAsync("EchoSourceLabwareType", sourceLabwareType);
                }

            }



          
            
            
            string  FinishedEchoSourcePlates = string.Join(",", EchoFinishedSourcePlates);
            await context.AddOrUpdateGlobalVariableAsync("Finished Source Plates", FinishedEchoSourcePlates);
                    Serilog.Log.Information("FinishedEchoSourcePlates= {FinishedEchoSourcePlates}", FinishedEchoSourcePlates.ToString());

        }

      

    }
}


