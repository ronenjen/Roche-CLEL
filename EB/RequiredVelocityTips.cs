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
    public class RequiredVelocityTips
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {


            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            int VelocityThresholdVolume = context.GetGlobalVariableValue<int>("VelocityThresholdVolume");
            string RequestedJob = context.GetGlobalVariableValue<string>("Current EB Job");


            int TotalCP = 0;
            int TotalEcho = 0;
            double EchoVolume = 0;
            int TotalBravo = 0;
            double BravoVolume = 0;

            int EchoST10Total = 0;
            int EchoST30Total = 0;
            int BravoST10Total = 0;
            int BravoST30Total = 0;

            int V10WorkflowStepsTotal = 0;
            int V30WorkflowStepsTotal = 0;


            string VelocityTips10PlaceholderBarcodes = "";
            string VelocityTips30PlaceholderBarcodes = "";

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

            //Get all the destinations associated with this order
            var destinations = _identityHelper.GetDestinations(RequestedOrder).ToList();
            //Get all the jobs
            var jobs = _identityHelper.GetJobs(RequestedOrder).ToList();


            var i = jobs
            .Where(x => x.JobId == Int32.Parse(RequestedJob))
            .FirstOrDefault();


            int TotalReplicates = 0;
            int TotalSerialise = 0;
            double DestSampleTransfers = 0.0;
            string DestSibling = "";
            List<string> AllDestSiblings = new List<string>();


            foreach (var dest in destinations)
            {
                string DestinationOperation = dest.OperationType.ToString();
                int DestJob = dest.JobId; ;


                if ((DestinationOperation == "Replicate") && (DestJob==Int32.Parse(RequestedJob)) )
                {
                    DestSampleTransfers = double.Parse(dest.SampleTransfers);
                    DestSibling = dest.SiblingIdentifier.ToString();


                    if ((DestSampleTransfers >= 0.5) && (!AllDestSiblings.Contains(DestSibling)))
                    {
                        AllDestSiblings.Add(DestSibling);
                        TotalReplicates++;

                    }
                }
                else if ((DestinationOperation == "Serialise")&& (DestJob == Int32.Parse(RequestedJob)))
                {
                    TotalSerialise++;
                }

            }
            //Add an additional velocity tip for the DMSO

            if (TotalSerialise > 0)
            {
                Console.WriteLine($"  {TotalSerialise.ToString()} Tips are required for serialisation " + Environment.NewLine);
            }


            if (TotalReplicates > 0)
            {
                Console.WriteLine($" {TotalReplicates.ToString()} Tips are required for replication " + Environment.NewLine);
            }



            if (DestSampleTransfers >= 0.5)
            {
                Console.WriteLine($" Replication to be done on Bravo - Tips required for replication" + Environment.NewLine);
            }
            else
            {
                Console.WriteLine($" Replication to be done on Echo - No tips required for replication " + Environment.NewLine);
            }


            if (TotalSerialise > 0)
            {
                TotalSerialise = TotalSerialise + 1;
            }

            if (DestSampleTransfers < VelocityThresholdVolume)
            {
                TotalReplicates = TotalReplicates + TotalSerialise;
                for (int b = 1; b <= (TotalReplicates); b++)
                {
                    VelocityTips10PlaceholderBarcodes = VelocityTips10PlaceholderBarcodes + "Velocity10_" + b + ",";
                }
            }
            else if (DestSampleTransfers >= VelocityThresholdVolume)
            {
                TotalReplicates = TotalReplicates + TotalSerialise;
                for (int b = 1; b <= (TotalReplicates); b++)
                {
                    VelocityTips30PlaceholderBarcodes = VelocityTips30PlaceholderBarcodes + "Velocity30_" + b + ",";
                }
            }

            VelocityTips10PlaceholderBarcodes = VelocityTips10PlaceholderBarcodes.TrimEnd(',');
            VelocityTips30PlaceholderBarcodes = VelocityTips30PlaceholderBarcodes.TrimEnd(',');

            if (VelocityTips10PlaceholderBarcodes != "")
            {
                Console.WriteLine($"Velocity 10 placeholder barcodes: {VelocityTips10PlaceholderBarcodes} for job {RequestedJob}" + Environment.NewLine);
            }

            if (VelocityTips30PlaceholderBarcodes != "")
            {
                Console.WriteLine($"Velocity 30 placeholder barcodes: {VelocityTips30PlaceholderBarcodes} for job {RequestedJob} " + Environment.NewLine);
            }


            await context.AddOrUpdateGlobalVariableAsync("VelocityTips10PlaceholderBarcodes", VelocityTips10PlaceholderBarcodes);
            await context.AddOrUpdateGlobalVariableAsync("VelocityTips30PlaceholderBarcodes", VelocityTips30PlaceholderBarcodes);
        }

    }
}


