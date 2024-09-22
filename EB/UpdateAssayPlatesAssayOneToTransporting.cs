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
using System.IO;


namespace Biosero.Scripting
{
    public class UpdateAssayPlatesAssayOneToTransporting
    {

        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string EBAssayPlateOne = context.GetGlobalVariableValue<string>("RepOnePlaceholderBarcodes");

            //instantiate LAMA1 objects

            string API_BASE_URL =  context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";
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


            // Split the combined string into an array of barcodes
            string[] allBarcodesArray = EBAssayPlateOne.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

          
            // Loop through each member of the array
            foreach (string barcode in allBarcodesArray)
            {

                var cc = destinations
                .Where(x => x.Name == barcode)
                .FirstOrDefault();

                int DestinationJobID = cc.JobId;
                string DestinationName = cc.Name;

                cc.Properties.SetValue("Status", "Transporting");
                _identityHelper.Register(cc, DestinationJobID, RequestedOrder);
            Console.WriteLine($"Plate {DestinationName} status was set to TRANSPORTING " + Environment.NewLine);
            }






        }

    }
}

