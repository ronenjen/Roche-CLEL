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
    public class FindDestAndInstructionsForEchoSource
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");

            string EBSources = context.GetGlobalVariableValue<string>("EBSourcesToBeTransferred");


            string SourceIndentityState = "";
             string dstFinished  = "";
             int JobPriorityNumber = 0;

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
            string SourceIdentifierssToBeTransferred = context.GetGlobalVariableValue<string>("CPSourcesIdentifiersForEB");
            
            
             List<string> AllParentIDs  = new List<string>();
             List<string> EchoDestination  = new List<string>();
             
             List<object[]> transportList = new List<object[]();
             
            
            
                                            var FinishedPlates =_identityHelper.GetDestinations(RequestedOrder)
                                .Where(x => x.Status == Status.Finished)
                               .ToList();
                               
                       foreach (var dst in FinishedPlates)
                       {
                           dstFinished = dst.Name.ToString();
                           dstJobId= dst.JobId;
                           dstPriority = dst.Priority.ToString();
                           
                           
		                                   /*   foreach (var source in sources)
							            {
							                int SourceJobID = source.JobId; 
							                string SourceOperation = source.OperationType.ToString();
							                string SourceIdentifier = source.Identifier.ToString();
							                string SourceName = source.Name.ToString();
							
									                if ((SourceOperation == "Replicate") && (SourceName==dstFinished))
									                {
									                
									                            Serilog.Log.Information("SourceIdentifier SourceIdentifier = {SourceIdentifier}", SourceIdentifier);
									                            Serilog.Log.Information("SourceName SourceName = {SourceName}", SourceName);
									                            
									                            
									                            var DestSibling =_identityHelper.GetDestinations(RequestedOrder)
							                                .Where(x => x.SiblingIdentifier == SourceIdentifier)
							                               .FirstOrDefault();
							                               
							                               string SiblingName = DestSibling.Name.ToString();
							                               int SiblingJob = DestSibling.JobId;
							                               string SiblingPriority = DestSibling.Priority.ToString();
							                               
							                               string RepEchoInstruction = SourceName + "-" + SiblingName;
									                           
									                   Serilog.Log.Information("RepEchoInstruction  = {RepEchoInstruction}", RepEchoInstruction);
									                   AllParentIDs.Add(RepEchoInstruction);
									                   EchoDestination.Add(SiblingName);
									                   
		         								 //   {parentIds, destination, jobId, OrderId, Priority}
									                   
					                   
							        //        await context.AddOrUpdateGlobalVariableAsync("Job Number", SiblingJob);
							                
							                switch (SiblingPriority)
							                {
							                	case "High";
								                	JobPriorityNumber = 1;
								                	break;
							                	case "Medium";
								                	JobPriorityNumber = 2;
								                	break;
							                	case "Low";
								                	JobPriorityNumber = 3;
								                	break;
							                	
							                }
							                
									                   transportList.Add(new object[] { SourceName ,   SiblingName, SiblingJob,   RequestedOrder,  });
							                
							          //      await context.AddOrUpdateGlobalVariableAsync("JobPriorityNumber", JobPriorityNumber);
							                
							                               
							                               
				
				                }*/

            }
                          
                    
                               
                            Serilog.Log.Information("ParentID ParentID = {dstFinished}", dstFinished);
                        }
                        
              
            
                string InstructionsForEcho  = String.Join(", ", AllParentIDs).Replace(", " , ",");
                string DestinationsForEcho  = String.Join(", ", EchoDestination).Replace(", " , ",");
                
                await context.AddOrUpdateGlobalVariableAsync("InstructionsForEcho", InstructionsForEcho);
                await context.AddOrUpdateGlobalVariableAsync("DestinationsForEcho", DestinationsForEcho);
		                   Serilog.Log.Information("InstructionsForEcho  = {InstructionsForEcho}", InstructionsForEcho);
		                   Serilog.Log.Information("DestinationsForEcho  = {DestinationsForEcho}", DestinationsForEcho);



        }


    }
}