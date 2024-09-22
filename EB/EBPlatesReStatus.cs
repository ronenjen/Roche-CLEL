


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


namespace Biosero.Scripting
{

    public class EBPlatesReStatus
    {
        public async Task RunAsync(DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"***********      START OF CHECKALLQUEUED ***************" + Environment.NewLine);
            //retrieve initial global variables values
            string RequestedOrder = context.GetGlobalVariableValue<string>("Input.OrderId");
            string CPSourcesForEB = context.GetGlobalVariableValue<string>("EBSourcesToBeTransferred");
            string NewEBSourcesStatus = context.GetGlobalVariableValue<string>("EBSourcesNewStatus");


            string DestLabwareType = "";


            string API_BASE_URL =  context.GetGlobalVariableValue<string>("_url"); // "http://1 92.168.14.10:8105/api/v2.0/";
            IQueryClient _queryClient = new QueryClient(API_BASE_URL);
            IAccessioningClient _accessioningClient = new AccessioningClient(API_BASE_URL);
            IEventClient _eventClient = new EventClient(API_BASE_URL);

            IdentityHelper _identityHelper;

            List<string> AllDestinationsForOrder = new List<string>();


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



            // Split the comma-separated string into an array
            string[] sourcesArray = CPSourcesForEB.Split(',');

            // Loop through each member
            foreach (string sourcemember in sourcesArray)
            {
                var cc = sources
                .Where(x => x.Name == sourcemember)
                .FirstOrDefault();

                int SourceJobID = cc.JobId;

                cc.Properties.SetValue("Status", NewEBSourcesStatus);
                _identityHelper.Register(cc, SourceJobID, RequestedOrder);


            }

        }

    }
}